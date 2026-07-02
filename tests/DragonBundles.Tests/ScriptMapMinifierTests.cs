using System.Text.Json;
using NUglify.JavaScript;

namespace DragonBundles.Tests;

// Locks the TFM-neutral contract of the shared minify/map helper. The classic ASP.NET transform
// relies on this exact behaviour but can only exercise it on a live host (Windows), so these
// host-independent tests are the primary guard for both runtimes.
public class ScriptMapMinifierTests
{
    static (string Content, string? Map) Minify(params (string, string)[] files) =>
        ScriptMapMinifier.Minify("app", files, new CodeSettings(), ";\n");

    [Fact]
    public void AppendsSourceMappingUrlComment_WhenMinified()
    {
        (string content, string? map) = Minify(("~/a.js", "function alpha(name) { return 'hi ' + name; }"));

        Assert.EndsWith("//# sourceMappingURL=app.min.js.map", content.TrimEnd());
        Assert.NotNull(map);
    }

    [Fact]
    public void Map_ListsEverySource()
    {
        (_, string? map) = Minify(
            ("~/a.js", "function alpha(name) { return 'hi ' + name; }"),
            ("~/b.js", "function beta(x) { return x * 2; }"));

        using JsonDocument doc = JsonDocument.Parse(map!);
        List<string> sources = [.. doc.RootElement.GetProperty("sources").EnumerateArray().Select(s => s.GetString()!)];
        Assert.Contains("~/a.js", sources);
        Assert.Contains("~/b.js", sources);
    }

    [Fact]
    public void PreMinifiedFile_PassesThroughVerbatim()
    {
        const string preMinified = "var already=minified;";
        (string content, _) = Minify(("~/lib.min.js", preMinified));

        Assert.Contains(preMinified, content);
    }

    [Fact]
    public void OnlyPreMinifiedFiles_ProduceNoMap()
    {
        (string content, string? map) = Minify(("~/lib.min.js", "var a=1;"), ("~/other.min.js", "var b=2;"));

        Assert.Null(map);
        Assert.DoesNotContain("sourceMappingURL", content);
    }
}
