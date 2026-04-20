using System.Web.Optimization;

namespace DragonBundles.Tests.SystemWeb;

public class BundleCollectionExtensionsTests
{
    static BundleCollection MakeBundles() => new BundleCollection();

    [Fact]
    public void AddStyleBundle_RegistersBundleWithCorrectVirtualPath()
    {
        var bundles = MakeBundles();

        bundles.AddStyleBundle("css", "~/Content/site.css");

        Assert.NotNull(bundles.GetBundleFor("~/bundles/css/css"));
    }

    [Fact]
    public void AddStyleBundle_RegistersNUglifyTransform()
    {
        var bundles = MakeBundles();

        bundles.AddStyleBundle("css", "~/Content/site.css");

        var bundle = bundles.GetBundleFor("~/bundles/css/css");
        Assert.NotNull(bundle);
        Assert.Single(bundle!.Transforms);
        Assert.IsType<NUglifyStyleTransform>(bundle.Transforms[0]);
    }

    [Fact]
    public void AddStyleBundle_RegistersStyleBundleType()
    {
        var bundles = MakeBundles();

        bundles.AddStyleBundle("theme", "~/Content/site.css", "~/Content/theme.css");

        var bundle = bundles.GetBundleFor("~/bundles/css/theme");
        Assert.IsType<StyleBundle>(bundle);
    }

    [Fact]
    public void AddScriptBundle_RegistersBundleWithCorrectVirtualPath()
    {
        var bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js");

        Assert.NotNull(bundles.GetBundleFor("~/bundles/js/app"));
    }

    [Fact]
    public void AddScriptBundle_RegistersNUglifyTransform()
    {
        var bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js");

        var bundle = bundles.GetBundleFor("~/bundles/js/app");
        Assert.NotNull(bundle);
        Assert.Single(bundle!.Transforms);
        Assert.IsType<NUglifyScriptTransform>(bundle.Transforms[0]);
    }

    [Fact]
    public void AddScriptBundle_RegistersScriptBundleType()
    {
        var bundles = MakeBundles();

        bundles.AddScriptBundle("app", "~/Scripts/jquery.js", "~/Scripts/main.js");

        var bundle = bundles.GetBundleFor("~/bundles/js/app");
        Assert.IsType<ScriptBundle>(bundle);
    }

    [Fact]
    public void AddStyleBundle_ReturnsSameBundleCollection()
    {
        var bundles = MakeBundles();

        var result = bundles.AddStyleBundle("css", "~/Content/site.css");

        Assert.Same(bundles, result);
    }

    [Fact]
    public void AddScriptBundle_ReturnsSameBundleCollection()
    {
        var bundles = MakeBundles();

        var result = bundles.AddScriptBundle("app", "~/Scripts/main.js");

        Assert.Same(bundles, result);
    }
}
