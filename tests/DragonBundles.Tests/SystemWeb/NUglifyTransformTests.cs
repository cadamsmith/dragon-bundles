using System.Web.Optimization;

namespace DragonBundles.Tests.SystemWeb;

public class NUglifyTransformTests
{
    static BundleResponse MakeResponse(string content) =>
        new BundleResponse { Content = content };

    [Fact]
    public void NUglifyStyleTransform_Process_MinifiesCss()
    {
        var transform = new NUglifyStyleTransform();
        var response = MakeResponse("body   {   color:   red;   }");

        transform.Process(null!, response);

        Assert.NotEmpty(response.Content);
        Assert.DoesNotContain("   ", response.Content);
    }

    [Fact]
    public void NUglifyStyleTransform_Process_SetsCssContentType()
    {
        var transform = new NUglifyStyleTransform();
        var response = MakeResponse("body{}");

        transform.Process(null!, response);

        Assert.Equal("text/css", response.ContentType);
    }

    [Fact]
    public void NUglifyStyleTransform_Process_NullContextDoesNotThrow()
    {
        var transform = new NUglifyStyleTransform();
        var response = MakeResponse("body{}");

        var ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_MinifiesJs()
    {
        var transform = new NUglifyScriptTransform();
        var response = MakeResponse("function hello() { return 'hi'; }");

        transform.Process(null!, response);

        Assert.NotEmpty(response.Content);
        Assert.True(response.Content.Length < "function hello() { return 'hi'; }".Length);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_SetsJsContentType()
    {
        var transform = new NUglifyScriptTransform();
        var response = MakeResponse("var x=1;");

        transform.Process(null!, response);

        Assert.Equal("text/javascript", response.ContentType);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_NullContextDoesNotThrow()
    {
        var transform = new NUglifyScriptTransform();
        var response = MakeResponse("var x=1;");

        var ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }
}
