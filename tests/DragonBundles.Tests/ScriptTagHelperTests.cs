using DragonBundles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using NSubstitute;

namespace DragonBundles.Tests;

public class ScriptTagHelperTests
{
    static (ScriptTagHelper helper, TagHelperOutput output) MakeTagHelper(string envName, string bundleName, params string[] sourceFiles)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(Path.GetTempPath());

        ScriptBundleProvider provider = new ScriptBundleProvider(env);
        if (sourceFiles.Length > 0)
            provider.Add(bundleName, sourceFiles);

        ScriptTagHelper helper = new ScriptTagHelper(provider, env) { Name = bundleName };
        TagHelperContext context = new TagHelperContext([], new Dictionary<object, object>(), Guid.NewGuid().ToString());
        TagHelperOutput output = new TagHelperOutput("script-bundle", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(context, output);
        return (helper, output);
    }

    [Fact]
    public void Process_InDevelopment_RendersIndividualScriptTags()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Development, "app", "/js/a.js", "/js/b.js");
        string? html = output.Content.GetContent();

        Assert.Contains("src=\"/js/a.js\"", html);
        Assert.Contains("src=\"/js/b.js\"", html);
        Assert.DoesNotContain(".min.js", html);
    }

    [Fact]
    public void Process_InProduction_RendersSingleBundleTag()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Production, "app", "/js/a.js");
        string? html = output.Content.GetContent();

        Assert.Contains("src=\"/bundles/js/app.min.js\"", html);
        Assert.Single(html.Split("<script", StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void Process_SuppressesOriginalTag()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Production, "app");
        Assert.Null(output.TagName);
    }

    [Fact]
    public void Process_InDevelopment_IncludesBundleAttribute()
    {
        (_, TagHelperOutput output) = MakeTagHelper(Environments.Development, "app", "/js/a.js");
        Assert.Contains("data-bundle=\"app\"", output.Content.GetContent());
    }
}
