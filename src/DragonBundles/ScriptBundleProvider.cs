namespace DragonBundles;

public class ScriptBundleProvider(IWebHostEnvironment env) : BundleProvider<ScriptBundle>(env, "/bundles/js/")
{
    protected override string Extension => "js";
    protected override ScriptBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(ScriptBundle bundle)
    {
        string webRoot = WebRootPath;
        string raw = string.Join(Environment.NewLine, bundle.SourceFiles
            .Select(f => Path.Combine(webRoot, f.TrimStart('/')))
            .Where(File.Exists)
            .Select(File.ReadAllText));

        bundle.MinifiedContent = Uglify.Js(raw).Code;
        bundle.LastModified = DateTime.UtcNow;
    }
}
