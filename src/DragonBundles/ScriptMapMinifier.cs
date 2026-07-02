using System.Text;
using NUglify;
using NUglify.JavaScript;

namespace DragonBundles;

/// <summary>
/// Minifies an ordered set of JavaScript source files into a single bundle sharing one V3 source
/// map. TFM-neutral so both the ASP.NET Core provider (startup minification) and the classic
/// ASP.NET <c>IBundleTransform</c> (request-time minification) produce byte-identical output.
/// Pre-minified <c>.min.js</c> inputs pass through verbatim while the map's output-line counter is
/// kept aligned, so their offsets stay correct in the combined bundle.
/// </summary>
static class ScriptMapMinifier
{
    /// <summary>
    /// Minifies <paramref name="files"/> (in order) into one bundle.
    /// </summary>
    /// <returns>
    /// The minified bundle content — with a trailing <c>//# sourceMappingURL={name}.min.js.map</c>
    /// comment when at least one file produced mappings — and the map JSON, or <c>null</c> when no
    /// file was minified (a bundle of only pre-minified files carries no map).
    /// </returns>
    public static (string Content, string? Map) Minify(
        string bundleName,
        IReadOnlyList<(string SourceUrl, string Content)> files,
        CodeSettings settings,
        string concatenationToken)
    {
        string mapFileName = $"{bundleName}.min.js.map";

        StringWriter mapWriter = new();
        V3SourceMap inner = new(mapWriter) { MakePathsRelative = false };
        DeferredSourceMap sourceMap = new(inner);
        sourceMap.StartPackage($"{bundleName}.min.js", mapFileName);

        CodeSettings mapSettings = settings.Clone();
        mapSettings.SymbolsMap = sourceMap;

        StringBuilder output = new();
        bool hasMappings = false;
        for (int i = 0; i < files.Count; i++)
        {
            string sourceUrl = files[i].SourceUrl;
            string content = files[i].Content;

            if (sourceUrl.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase))
            {
                // Pre-minified: pass through verbatim (no mappings), but keep the map's output
                // line counter aligned by notifying it of every newline we append.
                output.Append(content);
                NotifyNewLines(sourceMap, content);
            }
            else
            {
                output.Append(Uglify.Js(content, sourceUrl, mapSettings).Code);
                hasMappings = true;
            }

            if (i < files.Count - 1)
            {
                output.Append(concatenationToken);
                NotifyNewLines(sourceMap, concatenationToken);
            }
        }

        sourceMap.EndPackage();
        sourceMap.Dispose(); // flushes the map JSON to mapWriter

        // Only attach a map when at least one file was actually minified; a bundle of only
        // pre-minified files has no mappings and must pass through verbatim.
        string result = output.ToString();
        string? map = null;
        if (hasMappings)
        {
            result += Environment.NewLine + "//# sourceMappingURL=" + mapFileName;
            map = mapWriter.ToString();
        }

        return (result, map);
    }

    static void NotifyNewLines(DeferredSourceMap sourceMap, string text)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                sourceMap.NewLineInsertedInOutput();
            }
        }
    }
}
