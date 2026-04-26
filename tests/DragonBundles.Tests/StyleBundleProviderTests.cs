using DragonBundles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using NSubstitute;

namespace DragonBundles.Tests;

public class StyleBundleProviderTests : IDisposable
{
    readonly string _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public StyleBundleProviderTests() => Directory.CreateDirectory(_webRoot);
    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    StyleBundleProvider MakeProvider(string envName)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(_webRoot);
        return new StyleBundleProvider(env);
    }

    void WriteCssFile(string relativePath, string content)
    {
        string full = Path.Combine(_webRoot, relativePath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void Add_InProduction_MinifiesBundle()
    {
        WriteCssFile("/css/site.css", "body   {   color:   red;   }");
        StyleBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("site", "/css/site.css");

        List<string> bundle = provider.GetSourceUrls("site");
        Assert.NotEmpty(bundle);

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/css/site.min.css");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.NotEmpty(content);
        Assert.DoesNotContain("   ", content);
    }

    [Fact]
    public void Add_InDevelopment_DoesNotMinify()
    {
        WriteCssFile("/css/site.css", "body { color: red; }");
        StyleBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("site", "/css/site.css");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/css/site.min.css");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.Empty(content);
    }

    [Fact]
    public void GetUrl_WhenNoBundleRegistered_ReturnsBasePath()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Production);
        Assert.Equal("/bundles/css/site.min.css", provider.GetUrl("site"));
    }

    [Fact]
    public void GetUrl_InProduction_IncludesVersionQueryString()
    {
        WriteCssFile("/css/site.css", "body { color: red; }");
        StyleBundleProvider provider = MakeProvider(Environments.Production);
        provider.Add("site", "/css/site.css");

        string url = provider.GetUrl("site");
        Assert.StartsWith("/bundles/css/site.min.css?v=", url);
    }

    [Fact]
    public void GetUrl_InDevelopment_DoesNotIncludeVersion()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Development);
        provider.Add("site", "/css/site.css");

        Assert.Equal("/bundles/css/site.min.css", provider.GetUrl("site"));
    }

    [Fact]
    public void Add_WithGlobPattern_ExpandsToMatchingFiles()
    {
        WriteCssFile("/css/a.css", "body {}");
        WriteCssFile("/css/b.css", "h1 {}");
        StyleBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("site", "/css/*.css");

        Assert.Equal(["/css/a.css", "/css/b.css"], provider.GetSourceUrls("site"));
    }

    [Fact]
    public void Add_InProduction_WithGlobPattern_MinifiesAllMatchingFiles()
    {
        WriteCssFile("/css/a.css", "body { color: red; }");
        WriteCssFile("/css/b.css", "h1 { font-size: 24px; }");
        StyleBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("site", "/css/*.css");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/css/site.min.css");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.Contains("color", content);
        Assert.Contains("font-size", content);
    }

    [Fact]
    public void GetSourceUrls_ReturnsFiles_WhenBundleExists()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Development);
        provider.Add("site", "/css/a.css", "/css/b.css");

        List<string> urls = provider.GetSourceUrls("site");
        Assert.Equal(["/css/a.css", "/css/b.css"], urls);
    }

    [Fact]
    public void GetSourceUrls_ReturnsEmpty_WhenBundleNotFound()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Development);
        Assert.Empty(provider.GetSourceUrls("missing"));
    }

    [Fact]
    public void GetFileInfo_ReturnsFileInfo_WhenBundleExists()
    {
        WriteCssFile("/css/site.css", "body{}");
        StyleBundleProvider provider = MakeProvider(Environments.Production);
        provider.Add("site", "/css/site.css");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/css/site.min.css");
        Assert.True(fileInfo.Exists);
        Assert.Equal("site", fileInfo.Name);
    }

    [Fact]
    public void GetFileInfo_ReturnsNotFound_WhenBundleNotFound()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Production);
        IFileInfo fileInfo = provider.GetFileInfo("/bundles/css/missing.min.css");
        Assert.False(fileInfo.Exists);
    }

    [Fact]
    public void GetFileInfo_ReturnsNotFound_WhenPathOutsideBundleDirectory()
    {
        StyleBundleProvider provider = MakeProvider(Environments.Production);
        IFileInfo fileInfo = provider.GetFileInfo("/wwwroot/css/site.css");
        Assert.False(fileInfo.Exists);
    }
}
