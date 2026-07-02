using NUglify.JavaScript;

namespace DragonBundles;

sealed class ScriptBundleProvider(IWebHostEnvironment env, BundlingOptions options) : BundleProvider<ScriptBundle>(env, "/bundles/js/")
{
    protected override string Extension => "js";
    protected override string ConcatenationToken => ";" + Environment.NewLine;
    protected override ScriptBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(ScriptBundle bundle)
    {
        string mapFileName = $"{bundle.Name}.min.js.map";

        StringWriter mapWriter = new();
        V3SourceMap inner = new(mapWriter) { MakePathsRelative = false };
        DeferredSourceMap sourceMap = new(inner);
        sourceMap.StartPackage($"{bundle.Name}.min.js", mapFileName);

        CodeSettings settings = options.ScriptSettings.Clone();
        settings.SymbolsMap = sourceMap;

        StringBuilder output = new();
        bool hasMappings = false;
        for (int i = 0; i < bundle.SourceFiles.Count; i++)
        {
            string sourceUrl = bundle.SourceFiles[i];
            string content = ReadSourceFile(bundle.Name, sourceUrl);

            if (sourceUrl.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase))
            {
                // Pre-minified: pass through verbatim (no mappings), but keep the map's output
                // line counter aligned by notifying it of every newline we append.
                output.Append(content);
                NotifyNewLines(sourceMap, content);
            }
            else
            {
                output.Append(Uglify.Js(content, sourceUrl, settings).Code);
                hasMappings = true;
            }

            if (i < bundle.SourceFiles.Count - 1)
            {
                output.Append(ConcatenationToken);
                NotifyNewLines(sourceMap, ConcatenationToken);
            }
        }

        sourceMap.EndPackage();
        sourceMap.Dispose(); // flushes the map JSON to mapWriter

        // Only attach a map when at least one file was actually minified; a bundle of only
        // pre-minified files has no mappings and must pass through verbatim.
        if (hasMappings)
        {
            output.Append(Environment.NewLine).Append("//# sourceMappingURL=").Append(mapFileName);
            bundle.SourceMap = mapWriter.ToString();
        }

        bundle.MinifiedContent = output.ToString();
        bundle.LastModified = DateTime.UtcNow;
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
