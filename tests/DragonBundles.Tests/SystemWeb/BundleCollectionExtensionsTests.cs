using System.Web.Optimization;

namespace DragonBundles.Tests.SystemWeb;

public class BundleCollectionExtensionsTests
{
    static BundleCollection MakeBundles() => new();

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
