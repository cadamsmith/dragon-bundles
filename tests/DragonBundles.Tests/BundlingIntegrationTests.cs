using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace DragonBundles.Tests;

public class BundlingTestFixture : IAsyncLifetime
{
    readonly string _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    WebApplication? _app;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_webRoot);
        WriteFile("css/a.css", "body   {   color:   red;   }");
        WriteFile("css/b.css", "h1   {   font-size:   24px;   }");
        WriteFile("js/a.js", "function hello()   {   return 'hi';   }");
        WriteFile("js/b.js", "var   x   =   1;");
        WriteFile("static.txt", "hello static");

        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            WebRootPath = _webRoot,
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddBundling();

        _app = builder.Build();
        _app.UseBundling(bundles => bundles
            .AddStyleBundle("site", "/css/a.css", "/css/b.css")
            .AddScriptBundle("app", "/js/a.js", "/js/b.js"));

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_app != null)
        {
            await _app.DisposeAsync();
        }

        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    void WriteFile(string relativePath, string content)
    {
        string full = Path.Combine(_webRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }
}

public class BundlingIntegrationTests(BundlingTestFixture fixture) : IClassFixture<BundlingTestFixture>
{
    [Fact]
    public async Task StyleBundle_IsServedAtExpectedUrl()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/css/site.min.css");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScriptBundle_IsServedAtExpectedUrl()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/js/app.min.js");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StyleBundle_HasCssContentType()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/css/site.min.css");
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ScriptBundle_HasJsContentType()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/js/app.min.js");
        Assert.Equal("text/javascript", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task StyleBundle_ConcatenatesAndMinifiesMultipleFiles()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/css/site.min.css");
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("color:#f00", content);
        Assert.Contains("font-size:24px", content);
    }

    [Fact]
    public async Task ScriptBundle_ConcatenatesAndMinifiesMultipleFiles()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/js/app.min.js");
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("hello", content);
        Assert.Contains("x=1", content);
    }

    [Fact]
    public async Task StaticFile_IsStillServedViaCompositeProvider()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/static.txt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        Assert.Equal("hello static", content);
    }

    [Fact]
    public async Task UnknownBundle_Returns404()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/css/missing.min.css");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StyleBundle_HasLongLivedCacheControlHeader()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/css/site.min.css");
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task ScriptBundle_HasLongLivedCacheControlHeader()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/bundles/js/app.min.js");
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task StaticFile_DoesNotHaveLongLivedCacheControlHeader()
    {
        HttpResponseMessage response = await fixture.Client.GetAsync("/static.txt");
        Assert.Null(response.Headers.CacheControl);
    }
}
