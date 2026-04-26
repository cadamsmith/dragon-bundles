using System.IO;
using System.Web;
using System.Web.Optimization;

namespace DragonBundles.Tests.SystemWeb;

public class HtmlHelperExtensionsTests : IDisposable
{
    readonly bool _prevOptimizations = BundleTable.EnableOptimizations;

    public HtmlHelperExtensionsTests()
    {
        HttpContext.Current = new HttpContext(
            new HttpRequest(string.Empty, "http://localhost/", string.Empty),
            new HttpResponse(TextWriter.Null));
        BundleTable.EnableOptimizations = true;
    }

    public void Dispose()
    {
        BundleTable.EnableOptimizations = _prevOptimizations;
        HttpContext.Current = null;
    }

    [Fact]
    public void StyleBundle_RendersLinkTagWithCorrectPath()
    {
        string name = Guid.NewGuid().ToString("N");
        BundleTable.Bundles.AddStyleBundle(name, $"~/Content/{name}.css");

        string html = HtmlHelperExtensions.StyleBundle(null!, name).ToHtmlString();

        Assert.Contains($"/bundles/css/{name}", html);
        Assert.Contains("<link", html);
    }

    [Fact]
    public void ScriptBundle_RendersScriptTagWithCorrectPath()
    {
        string name = Guid.NewGuid().ToString("N");
        BundleTable.Bundles.AddScriptBundle(name, $"~/Scripts/{name}.js");

        string html = HtmlHelperExtensions.ScriptBundle(null!, name).ToHtmlString();

        Assert.Contains($"/bundles/js/{name}", html);
        Assert.Contains("<script", html);
    }
}