using System.Web.Optimization;
using NUglify.Css;
using NUglify.JavaScript;

namespace DragonBundles.Tests.SystemWeb;

public class BundleCollectionExtensionsTests
{
    static BundleCollection MakeBundles() => [];

    [Fact]
    public void AddStyleBundle_RegistersBundleWithCorrectVirtualPath()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddStyleBundle("css", "~/Content/site.css");

        Assert.NotNull(bundles.GetBundleFor("~/bundles/css/css"));
    }

    [Fact]
    public void AddStyleBundle_RegistersNUglifyTransform()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddStyleBundle("css", "~/Content/site.css");

        Bundle bundle = bundles.GetBundleFor("~/bundles/css/css");
        Assert.NotNull(bundle);
        Assert.Single(bundle!.Transforms);
        Assert.IsType<NUglifyStyleTransform>(bundle.Transforms[0]);
    }

    [Fact]
    public void AddStyleBundle_RegistersStyleBundleType()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddStyleBundle("theme", "~/Content/site.css", "~/Content/theme.css");

        Bundle bundle = bundles.GetBundleFor("~/bundles/css/theme");
        Assert.IsType<StyleBundle>(bundle);
    }

    [Fact]
    public void AddScriptBundle_RegistersBundleWithCorrectVirtualPath()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js");

        Assert.NotNull(bundles.GetBundleFor("~/bundles/js/app"));
    }

    [Fact]
    public void AddScriptBundle_RegistersNUglifyTransform()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js");

        Bundle bundle = bundles.GetBundleFor("~/bundles/js/app");
        Assert.NotNull(bundle);
        Assert.Single(bundle!.Transforms);
        Assert.IsType<NUglifyScriptTransform>(bundle.Transforms[0]);
    }

    [Fact]
    public void AddScriptBundle_RegistersScriptBundleType()
    {
        BundleCollection bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js", "~/Scripts/main.js");

        Bundle bundle = bundles.GetBundleFor("~/bundles/js/app");
        Assert.IsType<ScriptBundle>(bundle);
    }

    static string RunTransform(Bundle bundle, string content)
    {
        BundleResponse response = new() { Content = content };
        ((IBundleTransform)bundle.Transforms.Single()).Process(null!, response);
        return response.Content;
    }

    [Fact]
    public void ConfigureBundling_AppliesStyleSettingsToRegisteredBundle()
    {
        BundleCollection bundles = MakeBundles();

        bundles.ConfigureBundling(o => o.StyleSettings.CommentMode = CssComment.None);
        bundles.AddStyleBundle("css", "~/Content/site.css");

        string output = RunTransform(bundles.GetBundleFor("~/bundles/css/css")!, "/*! brand */\n.x { color: red; }");
        Assert.DoesNotContain("/*! brand */", output);
    }

    [Fact]
    public void ConfigureBundling_AppliesScriptSettingsToRegisteredBundle()
    {
        BundleCollection bundles = MakeBundles();

        bundles.ConfigureBundling(o => o.ScriptSettings.PreserveImportantComments = true);
        bundles.AddScriptBundle("app", "~/Scripts/app.js");

        string output = RunTransform(bundles.GetBundleFor("~/bundles/js/app")!, "/*! license */\nfunction f() { return 1; }");
        Assert.Contains("/*! license */", output);
    }

    [Fact]
    public void ConfigureBundling_DoesNotLeakAcrossCollections()
    {
        // Configure two collections with opposite settings; each must keep its own.
        const string source = "/*! license */\nfunction f() { return 1; }";

        BundleCollection keeps = MakeBundles();
        keeps.ConfigureBundling(o => o.ScriptSettings.PreserveImportantComments = true);
        keeps.AddScriptBundle("app", "~/Scripts/app.js");

        BundleCollection strips = MakeBundles();
        strips.ConfigureBundling(o => o.ScriptSettings.PreserveImportantComments = false);
        strips.AddScriptBundle("app", "~/Scripts/app.js");

        Assert.Contains("/*! license */", RunTransform(keeps.GetBundleFor("~/bundles/js/app")!, source));
        Assert.DoesNotContain("/*! license */", RunTransform(strips.GetBundleFor("~/bundles/js/app")!, source));
    }

    [Fact]
    public void ConfigureBundling_ReturnsSameBundleCollection()
    {
        BundleCollection bundles = MakeBundles();

        BundleCollection result = bundles.ConfigureBundling(o => o.ScriptSettings.TermSemicolons = true);

        Assert.Same(bundles, result);
    }

    [Fact]
    public void AddStyleBundle_ReturnsSameBundleCollection()
    {
        BundleCollection bundles = MakeBundles();

        BundleCollection result = bundles.AddStyleBundle("css", "~/Content/site.css");

        Assert.Same(bundles, result);
    }

    [Fact]
    public void AddScriptBundle_ReturnsSameBundleCollection()
    {
        BundleCollection bundles = MakeBundles();

        BundleCollection result = bundles.AddScriptBundle("app", "~/Scripts/main.js");

        Assert.Same(bundles, result);
    }
}
