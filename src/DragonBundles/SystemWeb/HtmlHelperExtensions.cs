namespace DragonBundles;

/// <summary>Extension methods for rendering DragonBundles in ASP.NET MVC Razor views.</summary>
public static class HtmlHelperExtensions
{
    extension(HtmlHelper helper)
    {
        /// <summary>Renders the CSS bundle registered under <paramref name="name"/>.</summary>
        public static IHtmlString StyleBundle(string name) =>
            Render($"~/bundles/css/{name}", name, css: true);

        /// <summary>Renders the JavaScript bundle registered under <paramref name="name"/>.</summary>
        public static IHtmlString ScriptBundle(string name) =>
            Render($"~/bundles/js/{name}", name, css: false);
    }

    static IHtmlString Render(string virtualPath, string name, bool css)
    {
        Bundle bundle = BundleTable.Bundles.GetBundleFor(virtualPath);

        // When optimizations are off (debug), System.Web serves individual unbundled files, so there
        // is no single bundle to hash — fall back to the framework's per-file rendering with no SRI.
        // Mirrors the ASP.NET Core tag helpers' Development branch.
        if (bundle is null || !BundleTable.EnableOptimizations)
        {
            return css ? Styles.Render(virtualPath) : Scripts.Render(virtualPath);
        }

        string integrity = ComputeIntegrity(bundle, virtualPath);
        string url = (css ? Styles.Url(virtualPath) : Scripts.Url(virtualPath)).ToHtmlString();
        return new MvcHtmlString(css ? BuildLinkTag(url, integrity, name) : BuildScriptTag(url, integrity, name));
    }

    // Hashes the post-transform bundle content — the exact bytes the bundle handler serves, including
    // the JS sourceMappingURL comment — so the SRI hash matches what the browser downloads.
    // GenerateBundleResponse uses the same server cache the handler does, so the two agree.
    static string ComputeIntegrity(Bundle bundle, string virtualPath)
    {
        BundleContext context = new(new HttpContextWrapper(HttpContext.Current), BundleTable.Bundles, virtualPath);
        BundleResponse response = bundle.GenerateBundleResponse(context);
        return SriHash.Compute(response.Content);
    }

    // Attribute shape matches the ASP.NET Core tag helpers for parity.
    internal static string BuildLinkTag(string url, string integrity, string name) =>
        $"<link rel=\"stylesheet\" href=\"{url}\" integrity=\"{integrity}\" crossorigin=\"anonymous\" data-bundle=\"{name}\" />";

    internal static string BuildScriptTag(string url, string integrity, string name) =>
        $"<script src=\"{url}\" integrity=\"{integrity}\" crossorigin=\"anonymous\" data-bundle=\"{name}\"></script>";
}
