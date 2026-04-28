namespace DragonBundles;

internal sealed class StyleBundleProvider(IWebHostEnvironment env) : BundleProvider<StyleBundle>(env, "/bundles/css/")
{
    protected override string Extension => "css";
    protected override StyleBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(StyleBundle bundle)
    {
        bundle.MinifiedContent = Uglify.Css(ReadSourceFiles(bundle)).Code;
        bundle.LastModified = DateTime.UtcNow;
    }
}
