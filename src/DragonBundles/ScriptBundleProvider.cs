namespace DragonBundles;

sealed class ScriptBundleProvider(IWebHostEnvironment env) : BundleProvider<ScriptBundle>(env, "/bundles/js/")
{
    protected override string Extension => "js";
    protected override string ConcatenationToken => ";" + Environment.NewLine;
    protected override ScriptBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(ScriptBundle bundle)
    {
        bundle.MinifiedContent = string.Join(ConcatenationToken, bundle.SourceFiles.Select(f =>
        {
            string content = ReadSourceFile(bundle.Name, f);
            return f.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase)
                ? content
                : Uglify.Js(content).Code;
        }));
        bundle.LastModified = DateTime.UtcNow;
    }
}
