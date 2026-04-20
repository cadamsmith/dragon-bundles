namespace DragonBundles;

public class StyleBundleProvider(IWebHostEnvironment env) : BundleProvider<StyleBundle>(env, "/bundles/css/")
{
    protected override string Extension => "css";
    protected override StyleBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(StyleBundle bundle)
    {
        string webRoot = WebRootPath;
        string raw = string.Join(Environment.NewLine, bundle.SourceFiles
            .Select(f => Path.Combine(webRoot, f.TrimStart('/')))
            .Where(File.Exists)
            .Select(File.ReadAllText));

        bundle.MinifiedContent = Uglify.Css(raw).Code;
        bundle.LastModified = DateTime.UtcNow;
    }
}
