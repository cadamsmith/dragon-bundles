using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DragonBundles.Tests;

public class ScriptTagHelperTests : IDisposable
{
    readonly string _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ScriptTagHelperTests() => Directory.CreateDirectory(_webRoot);
    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    void WriteJsFile(string relativePath, string content)
    {
        string full = Path.Combine(_webRoot, relativePath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    TagHelperOutput MakeTagHelper(string envName, string bundleName, params string[] sourceFiles)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(_webRoot);

        ScriptBundleProvider provider = new(env);
        if (sourceFiles.Length > 0)
        {
            provider.Add(bundleName, sourceFiles);
        }

        IServiceProvider services = new ServiceCollection()
            .AddSingleton(provider)
            .BuildServiceProvider();

        ScriptTagHelper helper = new(services, env) { Name = bundleName };
        TagHelperContext context = new([], new Dictionary<object, object>(), Guid.NewGuid().ToString());
        TagHelperOutput output = new("script-bundle", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(context, output);
        return output;
    }

    [Fact]
    public void Process_InDevelopment_RendersIndividualScriptTags()
    {
        TagHelperOutput output = MakeTagHelper(Environments.Development, "app", "/js/a.js", "/js/b.js");
        string? html = output.Content.GetContent();

        Assert.Contains("src=\"/js/a.js\"", html);
        Assert.Contains("src=\"/js/b.js\"", html);
        Assert.DoesNotContain(".min.js", html);
    }

    [Fact]
    public void Process_InProduction_RendersSingleBundleTag()
    {
        WriteJsFile("/js/a.js", "var x=1;");
        TagHelperOutput output = MakeTagHelper(Environments.Production, "app", "/js/a.js");
        string? html = output.Content.GetContent();

        Assert.Contains("src=\"/bundles/js/app.min.js?v=", html);
        Assert.Single(html.Split("<script", StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Process_SuppressesOriginalTag()
    {
        TagHelperOutput output = MakeTagHelper(Environments.Production, "app");
        Assert.Null(output.TagName);
    }

    [Fact]
    public void Process_InDevelopment_IncludesBundleAttribute()
    {
        TagHelperOutput output = MakeTagHelper(Environments.Development, "app", "/js/a.js");
        Assert.Contains("data-bundle=\"app\"", output.Content.GetContent());
    }

    [Fact]
    public void Process_InProduction_EmitsIntegrityAndCrossoriginAttributes()
    {
        WriteJsFile("/js/a.js", "var x=1;");
        TagHelperOutput output = MakeTagHelper(Environments.Production, "app", "/js/a.js");
        string? html = output.Content.GetContent();

        Assert.Contains("integrity=\"sha384-", html);
        Assert.Contains("crossorigin=\"anonymous\"", html);
    }

    [Fact]
    public void Process_InDevelopment_DoesNotEmitIntegrityAttributes()
    {
        TagHelperOutput output = MakeTagHelper(Environments.Development, "app", "/js/a.js", "/js/b.js");
        string? html = output.Content.GetContent();

        Assert.DoesNotContain("integrity=", html);
        Assert.DoesNotContain("crossorigin=", html);
    }
}
