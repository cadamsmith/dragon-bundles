namespace DragonBundles;

public static class BundleCollectionExtensions
{
    extension(BundleCollection bundles)
    {
        public BundleCollection AddStyleBundle(string name, params string[] files)
        {
            StyleBundle bundle = new($"~/bundles/css/{name}");
            bundle.Transforms.Clear();
            bundle.Transforms.Add(new NUglifyStyleTransform());
            foreach (string file in files)
            {
                bundle.Include(file);
            }

            bundles.Add(bundle);
            return bundles;
        }

        public BundleCollection AddScriptBundle(string name, params string[] files)
        {
            ScriptBundle bundle = new($"~/bundles/js/{name}");
            bundle.Transforms.Clear();
            bundle.Transforms.Add(new NUglifyScriptTransform());
            foreach (string file in files)
            {
                bundle.Include(file);
            }

            bundles.Add(bundle);
            return bundles;
        }
    }
}
