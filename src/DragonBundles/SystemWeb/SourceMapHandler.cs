using System.Web.Routing;

namespace DragonBundles;

/// <summary>
/// Serves a JS bundle's source map from <see cref="SourceMapStore"/> as <c>application/json</c>.
/// System.Web.Optimization owns the <c>~/bundles/js/{name}</c> path and has no slot for a companion
/// artifact, so the map is served through a dedicated route (registered by
/// <c>AddScriptBundle</c>) rather than the bundle handler.
/// </summary>
sealed class SourceMapHandler(string bundleName) : IHttpHandler
{
    // Carries the per-request bundle name, so a fresh instance is created per request.
    public bool IsReusable => false;

    public void ProcessRequest(HttpContext context)
    {
        if (SourceMapStore.TryGet(bundleName, out string map))
        {
            context.Response.ContentType = "application/json";
            // Map is served at a stable name (not content-hashed), so avoid immutable caching —
            // mirrors how the ASP.NET Core target skips the immutable header for .map files.
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Write(map);
        }
        else
        {
            context.Response.StatusCode = 404;
        }
    }
}

/// <summary>
/// Route handler that extracts the <c>{name}</c> token and hands off to a <see cref="SourceMapHandler"/>.
/// </summary>
sealed class SourceMapRouteHandler : IRouteHandler
{
    public IHttpHandler GetHttpHandler(RequestContext requestContext)
    {
        string bundleName = requestContext.RouteData.Values["name"] as string ?? string.Empty;
        return new SourceMapHandler(bundleName);
    }
}
