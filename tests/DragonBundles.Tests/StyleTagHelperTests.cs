using DragonBundles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NSubstitute;

namespace DragonBundles.Tests;

public class StyleTagHelperTests
{
    static (StyleTagHelper helper, TagHelperOutput output) MakeTagHelper(string envName, string bundleName, params string[] sourceFiles)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(Path.GetTempPath());

        StyleBundleProvider provider = new(env);
        if (sourceFiles.Length > 0)
            provider.Add(bundleName, sourceFiles);

        StyleTagHelper helper = new(provider, env) { Name = bundleName };
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
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Production, "site", "/css/a.css");
        string? html = output.Content.GetContent();

        Assert.Contains("href=\"/bundles/css/site.min.css\"", html);
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
