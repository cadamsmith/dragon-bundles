namespace DragonBundles;

public class ScriptBundleProvider(IWebHostEnvironment env) : BundleProvider<ScriptBundle>(env, "/bundles/js/")
{
    protected override string Extension => "js";
    protected override ScriptBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(ScriptBundle bundle)
    {
        bundle.MinifiedContent = Uglify.Js(ReadSourceFiles(bundle)).Code;
        bundle.LastModified = DateTime.UtcNow;
    }
}
