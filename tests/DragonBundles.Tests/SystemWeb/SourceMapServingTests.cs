using System.Web;
using System.Web.Optimization;
using System.Web.Routing;

namespace DragonBundles.Tests.SystemWeb;

// Covers the net48-specific source-map serving glue that does NOT need a hosting environment:
// the handler (built over a hand-constructed HttpContext) and the route AddScriptBundle registers.
// The transform's Files->pairs path (BundleFile.ApplyTransforms / VirtualPathUtility.ToAbsolute)
// needs a live host and is left to ScriptMapMinifierTests + manual verification.
public class SourceMapServingTests
{
    static HttpContext MakeContext(out StringWriter body)
    {
        body = new StringWriter();
        return new HttpContext(
            new HttpRequest("map", "http://localhost/bundles/js/app.min.js.map", ""),
            new HttpResponse(body));
    }

    [Fact]
    public void Handler_ServesStoredMapAsJson()
    {
        SourceMapStore.Set("serve-bundle", "{\"version\":3,\"sources\":[\"/Scripts/app.js\"]}");
        HttpContext context = MakeContext(out StringWriter body);

        new SourceMapHandler("serve-bundle").ProcessRequest(context);
        context.Response.Flush();

        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Contains("\"version\":3", body.ToString());
    }

    [Fact]
    public void Handler_Returns404_WhenMapNotStored()
    {
        HttpContext context = MakeContext(out _);

        new SourceMapHandler("never-stored-bundle").ProcessRequest(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public void AddScriptBundle_RegistersMapRouteAtIndexZero()
    {
        BundleCollection bundles = [];

        bundles.AddScriptBundle("app", "~/Scripts/app.js");

        // Registered once, inserted at the front so a catch-all MVC route can't shadow it.
        RouteBase first = RouteTable.Routes[0];
        Route route = Assert.IsType<Route>(first);
        Assert.Equal("bundles/js/{name}.min.js.map", route.Url);
        Assert.IsType<SourceMapRouteHandler>(route.RouteHandler);
    }
}
