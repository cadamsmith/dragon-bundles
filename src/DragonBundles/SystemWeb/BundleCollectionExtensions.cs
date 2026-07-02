using System.Runtime.CompilerServices;
using System.Web.Routing;

namespace DragonBundles;

/// <summary>Extension methods for registering DragonBundles with <c>System.Web.Optimization</c>.</summary>
public static class BundleCollectionExtensions
{
    // Options are associated with a specific BundleCollection instance rather than held statically,
    // so configuration doesn't leak across collections (mirrors the per-container options on
    // ASP.NET Core). Configure before registering bundles.
    static readonly ConditionalWeakTable<BundleCollection, BundlingOptions> _optionsTable = new();

    extension(BundleCollection bundles)
    {
        /// <summary>Configures NUglify minification settings applied to all bundles registered on this collection.</summary>
        /// <param name="configure">Callback to configure <see cref="BundlingOptions"/>.</param>
        public BundleCollection ConfigureBundling(Action<BundlingOptions> configure)
        {
            configure(_optionsTable.GetOrCreateValue(bundles));
            return bundles;
        }

        /// <summary>Registers a CSS bundle using NUglify minification.</summary>
        /// <param name="name">Bundle name. Served at <c>~/bundles/css/{name}</c>.</param>
        /// <param name="files">Source file virtual paths (e.g. <c>~/Content/site.css</c>).</param>
        public BundleCollection AddStyleBundle(string name, params string[] files)
        {
            StyleBundle bundle = new($"~/bundles/css/{name}");
            bundle.Transforms.Clear();
            bundle.Transforms.Add(new NUglifyStyleTransform(_optionsTable.GetOrCreateValue(bundles).StyleSettings));
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
            bundle.Transforms.Add(new NUglifyScriptTransform(name, _optionsTable.GetOrCreateValue(bundles).ScriptSettings));
            foreach (string file in files)
            {
                bundle.Include(file);
            }

            bundles.Add(bundle);
            EnsureSourceMapRoute();
            return bundles;
        }
    }

    // 0 until the source-map route has been registered, 1 afterwards.
    static int _sourceMapRouteRegistered;

    // Registers a single route that serves every JS bundle's source map at
    // `bundles/js/{name}.min.js.map` — the path the browser resolves from the bundle's
    // `//# sourceMappingURL={name}.min.js.map` comment. Inserted at index 0 so it is matched before
    // a catch-all MVC route (which would otherwise bind it as controller=bundles/action=js).
    static void EnsureSourceMapRoute()
    {
        if (Interlocked.CompareExchange(ref _sourceMapRouteRegistered, 1, 0) != 0)
        {
            return;
        }

        Route route = new("bundles/js/{name}.min.js.map", new SourceMapRouteHandler());
        using (RouteTable.Routes.GetWriteLock())
        {
            RouteTable.Routes.Insert(0, route);
        }
    }
}
