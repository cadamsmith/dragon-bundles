namespace DragonBundles;

public static class BundleCollectionExtensions
{
    public static BundleCollection AddStyleBundle(
        this BundleCollection bundles, string name, params string[] files)
    {
        var bundle = new StyleBundle($"~/bundles/css/{name}");
        bundle.Transforms.Clear();
        bundle.Transforms.Add(new NUglifyStyleTransform());
        foreach (string file in files)
            bundle.Include(file);
        bundles.Add(bundle);
        return bundles;
    }

    public static BundleCollection AddScriptBundle(
        this BundleCollection bundles, string name, params string[] files)
    {
        var bundle = new ScriptBundle($"~/bundles/js/{name}");
        bundle.Transforms.Clear();
        bundle.Transforms.Add(new NUglifyScriptTransform());
        foreach (string file in files)
            bundle.Include(file);
        bundles.Add(bundle);
        return bundles;
    }
}
