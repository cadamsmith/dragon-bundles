using System.Web.Optimization;
using NUglify.Css;
using NUglify.JavaScript;

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

        Exception? ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }

    [Fact]
    public void NUglifyStyleTransform_Process_HonorsCssSettings()
    {
        BundleResponse kept = MakeResponse("/*! brand */\n.x { color: red; }");
        new NUglifyStyleTransform(new CssSettings { CommentMode = CssComment.Important }).Process(null!, kept);
        Assert.Contains("/*! brand */", kept.Content);

        BundleResponse stripped = MakeResponse("/*! brand */\n.x { color: red; }");
        new NUglifyStyleTransform(new CssSettings { CommentMode = CssComment.None }).Process(null!, stripped);
        Assert.DoesNotContain("/*! brand */", stripped.Content);
    }

    [Fact]
    public void NUglifyScriptTransform_Process_HonorsCodeSettings()
    {
        BundleResponse kept = MakeResponse("/*! license */\nfunction f() { return 1; }");
        new NUglifyScriptTransform(new CodeSettings { PreserveImportantComments = true }).Process(null!, kept);
        Assert.Contains("/*! license */", kept.Content);

        BundleResponse stripped = MakeResponse("/*! license */\nfunction f() { return 1; }");
        new NUglifyScriptTransform(new CodeSettings { PreserveImportantComments = false }).Process(null!, stripped);
        Assert.DoesNotContain("/*! license */", stripped.Content);
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

        Exception? ex = Record.Exception(() => transform.Process(null!, response));
        Assert.Null(ex);
    }
}
