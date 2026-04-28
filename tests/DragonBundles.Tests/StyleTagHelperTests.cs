using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DragonBundles.Tests;

public class StyleTagHelperTests : IDisposable
{
    readonly string _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public StyleTagHelperTests() => Directory.CreateDirectory(_webRoot);
    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    void WriteCssFile(string relativePath, string content)
    {
        string full = Path.Combine(_webRoot, relativePath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    (StyleTagHelper helper, TagHelperOutput output) MakeTagHelper(string envName, string bundleName, params string[] sourceFiles)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(_webRoot);

        StyleBundleProvider provider = new(env);
        if (sourceFiles.Length > 0)
            provider.Add(bundleName, sourceFiles);

        IServiceProvider services = new ServiceCollection()
            .AddSingleton(provider)
            .BuildServiceProvider();

        StyleTagHelper helper = new(services, env) { Name = bundleName };
        TagHelperContext context = new([], new Dictionary<object, object>(), Guid.NewGuid().ToString());
        TagHelperOutput output = new("style-bundle", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(context, output);
        return (helper, output);
    }

    [Fact]
    public void Process_InDevelopment_RendersIndividualLinkTags()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Development, "site", "/css/a.css", "/css/b.css");
        string? html = output.Content.GetContent();

        Assert.Contains("href=\"/css/a.css\"", html);
        Assert.Contains("href=\"/css/b.css\"", html);
        Assert.DoesNotContain(".min.css", html);
    }

    [Fact]
    public void Process_InProduction_RendersSingleBundleTag()
    {
        WriteCssFile("/css/a.css", "body { color: red; }");
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Production, "site", "/css/a.css");
        string? html = output.Content.GetContent();

        Assert.Contains("href=\"/bundles/css/site.min.css?v=", html);
        Assert.Single(html.Split("<link", StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Process_SuppressesOriginalTag()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Production, "site");
        Assert.Null(output.TagName);
    }

    [Fact]
    public void Process_InDevelopment_IncludesBundleAttribute()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Development, "site", "/css/a.css");
        Assert.Contains("data-bundle=\"site\"", output.Content.GetContent());
    }
}
