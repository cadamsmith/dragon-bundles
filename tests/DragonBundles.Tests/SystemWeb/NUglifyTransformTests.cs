using System.Web.Optimization;

namespace DragonBundles.Tests.SystemWeb;

public class NUglifyTransformTests
{
    static BundleResponse MakeResponse(string content) => new() { Content = content };

    [Fact]
    public void NUglifyStyleTransform_Process_MinifiesCss()
    {
        NUglifyStyleTransform transform = new();
        BundleResponse response = MakeResponse("body   {   color:   red;   }");

        transform.Process(null!, response);

        Assert.NotEmpty(response.Content);
        Assert.DoesNotContain("   ", response.Content);
    }

    [Fact]
    public void NUglifyStyleTransform_Process_SetsCssContentType()
    {
        NUglifyStyleTransform transform = new();
        BundleResponse response = MakeResponse("body{}");

        transform.Process(null!, response);

        Assert.Equal("text/css", response.ContentType);
    }

    [Fact]
    public void NUglifyStyleTransform_Process_NullContextDoesNotThrow()
    {
        NUglifyStyleTransform transform = new();
        BundleResponse response = MakeResponse("body{}");

        Exception ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_MinifiesJs()
    {
        NUglifyScriptTransform transform = new();
        BundleResponse response = MakeResponse("function hello() { return 'hi'; }");

        transform.Process(null!, response);

        Assert.NotEmpty(response.Content);
        Assert.True(response.Content.Length < "function hello() { return 'hi'; }".Length);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_SetsJsContentType()
    {
        NUglifyScriptTransform transform = new();
        BundleResponse response = MakeResponse("var x=1;");

        transform.Process(null!, response);

        Assert.Equal("text/javascript", response.ContentType);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_NullContextDoesNotThrow()
    {
        NUglifyScriptTransform transform = new();
        BundleResponse response = MakeResponse("var x=1;");

        Exception ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }
}
