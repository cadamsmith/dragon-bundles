namespace DragonBundles;

/// <summary>Extension methods for registering DragonBundles with <c>System.Web.Optimization</c>.</summary>
public static class BundleCollectionExtensions
{
    extension(BundleCollection bundles)
    {
        /// <summary>Registers a CSS bundle using NUglify minification.</summary>
        /// <param name="name">Bundle name. Served at <c>~/bundles/css/{name}</c>.</param>
        /// <param name="files">Source file virtual paths (e.g. <c>~/Content/site.css</c>).</param>
        public BundleCollection AddStyleBundle(string name, params string[] files)
        {
            StyleBundle bundle = new($"~/bundles/css/{name}");
            bundle.Transforms.Clear();
            bundle.Transforms.Add(new NUglifyStyleTransform());
            foreach (string file in files)
            {
                bundle.Include(file, new CssRewriteUrlTransform());
            }

            bundles.Add(bundle);
            return bundles;
        }

        /// <summary>Registers a JavaScript bundle using NUglify minification.</summary>
        /// <param name="name">Bundle name. Served at <c>~/bundles/js/{name}</c>.</param>
        /// <param name="files">Source file virtual paths (e.g. <c>~/Scripts/app.js</c>).</param>
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
