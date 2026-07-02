using System.Diagnostics;
using System.Text.Json;
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
    public void Add_WithGlobPattern_ExcludesPreMinifiedFiles()
    {
        WriteJsFile("/js/a.js", "var a = 1;");
        WriteJsFile("/js/a.min.js", "var a=1;");
        ScriptBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("app", "/js/*.js");

        Assert.Equal(["/js/a.js"], provider.GetSourceUrls("app"));
    }

    [Fact]
    public void Add_WithGlobPattern_ExcludesSourceMaps()
    {
        WriteJsFile("/js/a.js", "var a = 1;");
        WriteJsFile("/js/a.js.map", "{}");
        ScriptBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("app", "/js/*");

        Assert.DoesNotContain("/js/a.js.map", provider.GetSourceUrls("app"));
    }

    [Fact]
    public void Add_InProduction_PassesPreMinifiedLiteralThroughVerbatim()
    {
        string preMinified = "function hello(){return\"hi\";}var x=1;";
        WriteJsFile("/js/app.min.js", preMinified);
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/app.min.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();
        Assert.Equal(preMinified, content);
    }

    [Fact]
    public void Add_InProduction_MixedBundle_MinifiesNonPreMinifiedAndPreservesOrder()
    {
        WriteJsFile("/js/a.js", "var   a   =   1;");
        WriteJsFile("/js/b.min.js", "var b=2;");
        WriteJsFile("/js/c.js", "var   c   =   3;");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/a.js", "/js/b.min.js", "/js/c.js");

        IFileInfo fileInfo = provider.GetFileInfo("/bundles/js/app.min.js");
        using Stream stream = fileInfo.CreateReadStream();
        string content = new StreamReader(stream).ReadToEnd();

        int posA = content.IndexOf("a=", StringComparison.Ordinal);
        int posB = content.IndexOf("b=2", StringComparison.Ordinal);
        int posC = content.IndexOf("c=", StringComparison.Ordinal);

        Assert.True(posA >= 0);
        Assert.True(posB >= 0);
        Assert.True(posC >= 0);
        Assert.True(posA < posB && posB < posC);
        Assert.Contains("b=2", content);
    }

    static string ReadFileInfo(IFileInfo fileInfo)
    {
        using Stream stream = fileInfo.CreateReadStream();
        return new StreamReader(stream).ReadToEnd();
    }

    [Fact]
    public void Add_InProduction_AppendsSourceMappingUrlComment()
    {
        WriteJsFile("/js/app.js", "function hello() { return 'hi'; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/app.js");

        string content = ReadFileInfo(provider.GetFileInfo("/bundles/js/app.min.js"));
        Assert.EndsWith("//# sourceMappingURL=app.min.js.map", content.TrimEnd());
    }

    [Fact]
    public void Add_InProduction_ServesValidSourceMap()
    {
        WriteJsFile("/js/app.js", "function hello() { return 'hi'; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/app.js");

        IFileInfo mapInfo = provider.GetFileInfo("/bundles/js/app.min.js.map");
        Assert.True(mapInfo.Exists);

        using JsonDocument doc = JsonDocument.Parse(ReadFileInfo(mapInfo));
        JsonElement root = doc.RootElement;
        Assert.Equal(3, root.GetProperty("version").GetInt32());
        Assert.NotEmpty(root.GetProperty("mappings").GetString()!);
    }

    [Fact]
    public void Add_InProduction_MultiFile_SourceMapListsAllSources()
    {
        WriteJsFile("/js/a.js", "function alpha(name) { return 'hello ' + name; }");
        WriteJsFile("/js/b.js", "function beta(x) { return x * 2; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/a.js", "/js/b.js");

        using JsonDocument doc = JsonDocument.Parse(ReadFileInfo(provider.GetFileInfo("/bundles/js/app.min.js.map")));
        List<string> sources = [.. doc.RootElement.GetProperty("sources").EnumerateArray().Select(s => s.GetString()!)];
        Assert.Contains("/js/a.js", sources);
        Assert.Contains("/js/b.js", sources);
    }

    [Fact]
    public void Add_InProduction_SourceMap_MapsEachFileToItsGeneratedLine()
    {
        // Guards the multi-file output-offset logic: top-level function names are preserved by
        // NUglify, so each source's mappings must land on the generated line that actually
        // contains that file's code. If the offset tracking regresses, file b collapses onto
        // file a's line and this fails.
        WriteJsFile("/js/a.js", "function alpha(name) { return 'hello ' + name; }");
        WriteJsFile("/js/b.js", "function beta(x) { return x * 2; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Production);

        provider.Add("app", "/js/a.js", "/js/b.js");

        string[] generatedLines = ReadFileInfo(provider.GetFileInfo("/bundles/js/app.min.js")).Split('\n');
        using JsonDocument doc = JsonDocument.Parse(ReadFileInfo(provider.GetFileInfo("/bundles/js/app.min.js.map")));
        string[] sources = [.. doc.RootElement.GetProperty("sources").EnumerateArray().Select(s => s.GetString()!)];
        List<Segment> segments = DecodeMappings(doc.RootElement.GetProperty("mappings").GetString()!);

        Segment first = segments[0];
        Assert.Equal(0, first.GenLine);
        Assert.Equal(0, first.GenCol);
        Assert.Equal("/js/a.js", sources[first.SrcIndex]);
        Assert.Equal(0, first.SrcLine);

        int aGenLine = segments.First(s => sources[s.SrcIndex] == "/js/a.js").GenLine;
        int bGenLine = segments.First(s => sources[s.SrcIndex] == "/js/b.js").GenLine;
        Assert.Contains("alpha", generatedLines[aGenLine]);
        Assert.Contains("beta", generatedLines[bGenLine]);
        Assert.True(aGenLine < bGenLine);
    }

    readonly record struct Segment(int GenLine, int GenCol, int SrcIndex, int SrcLine, int SrcCol);

    // Decodes the VLQ `mappings` field into absolute segments that carry source references.
    static List<Segment> DecodeMappings(string mappings)
    {
        List<Segment> result = [];
        int genLine = 0, srcIndex = 0, srcLine = 0, srcCol = 0;
        foreach (string lineGroup in mappings.Split(';'))
        {
            int genCol = 0;
            foreach (string segment in lineGroup.Split(','))
            {
                if (segment.Length == 0)
                {
                    continue;
                }

                List<int> values = DecodeVlq(segment);
                genCol += values[0];
                if (values.Count >= 4)
                {
                    srcIndex += values[1];
                    srcLine += values[2];
                    srcCol += values[3];
                    result.Add(new Segment(genLine, genCol, srcIndex, srcLine, srcCol));
                }
            }
            genLine++;
        }
        return result;
    }

    static List<int> DecodeVlq(string segment)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        List<int> result = [];
        int shift = 0, value = 0;
        foreach (char c in segment)
        {
            int digit = alphabet.IndexOf(c);
            int continuation = digit & 32;
            digit &= 31;
            value += digit << shift;
            if (continuation != 0)
            {
                shift += 5;
            }
            else
            {
                bool negative = (value & 1) == 1;
                value >>= 1;
                result.Add(negative ? -value : value);
                value = 0;
                shift = 0;
            }
        }
        return result;
    }

    [Fact]
    public void Add_InDevelopment_DoesNotGenerateSourceMap()
    {
        WriteJsFile("/js/app.js", "function hello() { return 'hi'; }");
        ScriptBundleProvider provider = MakeProvider(Environments.Development);

        provider.Add("app", "/js/app.js");

        Assert.False(provider.GetFileInfo("/bundles/js/app.min.js.map").Exists);
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
