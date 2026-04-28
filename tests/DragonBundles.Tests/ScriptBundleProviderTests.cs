using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using NSubstitute;

namespace DragonBundles.Tests;

public class ScriptBundleProviderTests : IDisposable
{
    readonly string _webRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ScriptBundleProviderTests() => Directory.CreateDirectory(_webRoot);
    public void Dispose() => Directory.Delete(_webRoot, recursive: true);

    ScriptBundleProvider MakeProvider(string envName)
    {
        IWebHostEnvironment? env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(envName);
        env.WebRootPath.Returns(_webRoot);
        env.WebRootFileProvider.Returns(new NullFileProvider());
        return new ScriptBundleProvider(env);
    }

    void WriteJsFile(string relativePath, string content)
    {
        string full = Path.Combine(_webRoot, relativePath.TrimStart('/'));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    [Fact]
    public void Add_InProduction_MinifiesBundle()
    {
        WriteJsFile("/js/app.js", "function   hello()   {   return   'hi';   }");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/app.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.NotEmpty(content);
        Assert.DoesNotContain("   ", content);
    }

    [Fact]
    public void Add_InDevelopment_DoesNotMinify()
    {
        WriteJsFile("/js/app.js", "function hello() { return 'hi'; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("app", "/js/app.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.Empty(content);
    }

    [Fact]
    public void Add_InProduction_SeparatesJsFilesWithSemicolon()
    {
        // Without a `;` between files, ASI rules let a leading `(` on the next file
        // be parsed as a call against the previous statement's value.
        WriteJsFile("/js/a.js", "var a = 1");
        WriteJsFile("/js/b.js", "(function () { window.b = 2 })()");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/a.js", "/js/b.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();

        Assert.Contains("a=1;", content);
        Assert.DoesNotContain("a=1(", content);
    }

    [Fact]
    public void GetUrl_WhenNoBundleRegistered_ReturnsBasePath()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        Assert.Equal("/bundles/js/app.min.js", provider.GetUrl("app"));
    }

    [Fact]
    public void GetUrl_InProduction_IncludesVersionQueryString()
    {
        WriteJsFile("/js/app.js", "var x = 1;");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        provider.Add("app", "/js/app.js");

        string url = provider.GetUrl("app");
        Assert.StartsWith("/bundles/js/app.min.js?v=", url);
    }

    [Fact]
    public void GetUrl_InDevelopment_DoesNotIncludeVersion()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Development);
        provider.Add("app", "/js/app.js");

        Assert.Equal("/bundles/js/app.min.js", provider.GetUrl("app"));
    }

    [Fact]
    public void Add_InProduction_ThrowsWhenSourceFileNotFound()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        Assert.Throws<FileNotFoundException>(() => provider.Add("app", "/js/missing.js"));
    }

    [Fact]
    public void Add_WithGlobPattern_ExpandsToMatchingFiles()
    {
        WriteJsFile("/js/a.js", "var a = 1;");
        WriteJsFile("/js/b.js", "var b = 2;");
        ScriptBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("app", "/js/*.js");

        Assert.Equal(["/js/a.js", "/js/b.js"], provider.GetSourceUrls("app"));
    }

    [Fact]
    public void Add_InProduction_WithGlobPattern_MinifiesAllMatchingFiles()
    {
        WriteJsFile("/js/a.js", "function hello() { return 'hi'; }");
        WriteJsFile("/js/b.js", "var x = 1;");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/*.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.Contains("hello", content);
        Assert.Contains("x=1", content);
    }

    [Fact]
    public void GetSourceUrls_ReturnsFiles_WhenBundleExists()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Development);
        provider.Add("app", "/js/a.js", "/js/b.js");

        List<string> urls = provider.GetSourceUrls("app");
        Assert.Equal(["/js/a.js", "/js/b.js"], urls);
    }

    [Fact]
    public void GetSourceUrls_ReturnsEmpty_WhenBundleNotFound()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Development);
        Assert.Empty(provider.GetSourceUrls("missing"));
    }

    [Fact]
    public void GetFileInfo_ReturnsFileInfo_WhenBundleExists()
    {
        WriteJsFile("/js/app.js", "var x=1;");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        provider.Add("app", "/js/app.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        Assert.True(fileInfo.Exists);
        Assert.Equal("app", fileInfo.Name);
    }

    [Fact]
    public void GetFileInfo_ReturnsNotFound_WhenBundleNotFound()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/missing.min.js");
        Assert.False(fileInfo.Exists);
    }

    [Fact]
    public void GetFileInfo_ReturnsNotFound_WhenPathOutsideBundleDirectory()
    {
        ScriptBundleProvider provider = MakeProvider(Environments.Production);
        IFileInfo fileInfo = provider.GetFileInfo("/wwwroot/js/app.js");
        Assert.False(fileInfo.Exists);
    }

    [Fact]
    public async Task Add_InProduction_ReminifiesWhenSourceFileChanges()
    {
        WriteJsFile("/js/app.js", "var x = 1;");
        using PhysicalFileProvider fileProvider = new(_webRoot);

        IWebHostEnvironment env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Production);
        env.WebRootPath.Returns(_webRoot);
        env.WebRootFileProvider.Returns(fileProvider);

        ScriptBundleProvider provider = new(env);
        provider.Add("app", "/js/app.js");

        IFileInfo initial = provider.GetFileInfo("/bundles/js/app.min.js");
        await using Stream initialStream = initial.CreateReadStream();
        string initialContent = await new StreamReader(initialStream).ReadToEndAsync();

        WriteJsFile("/js/app.js", "var y = 2;");

        string updatedContent = initialContent;
        Stopwatch sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < 5 && updatedContent == initialContent)
        {
            await Task.Delay(100);
            IFileInfo updated = provider.GetFileInfo("/bundles/js/app.min.js");
            await using Stream s = updated.CreateReadStream();
            updatedContent = await new StreamReader(s).ReadToEndAsync();
        }

        Assert.NotEqual(initialContent, updatedContent);
        Assert.Contains("y", updatedContent);
    }
}
