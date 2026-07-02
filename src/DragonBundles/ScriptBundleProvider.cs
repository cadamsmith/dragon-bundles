namespace DragonBundles;

sealed class ScriptBundleProvider(IWebHostEnvironment env, BundlingOptions options) : BundleProvider<ScriptBundle>(env, "/bundles/js/")
{
    protected override string Extension => "js";
    protected override string ConcatenationToken => ";" + Environment.NewLine;
    protected override ScriptBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(ScriptBundle bundle)
    {
        List<(string, string)> files =
            [.. bundle.SourceFiles.Select(sourceUrl => (sourceUrl, ReadSourceFile(bundle.Name, sourceUrl)))];

        (string content, string? map) =
            ScriptMapMinifier.Minify(bundle.Name, files, options.ScriptSettings, ConcatenationToken);

        bundle.MinifiedContent = content;
        if (map is not null)
        {
            bundle.SourceMap = map;
        }

        bundle.LastModified = DateTime.UtcNow;
    }
}
