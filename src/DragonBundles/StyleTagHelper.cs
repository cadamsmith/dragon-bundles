namespace DragonBundles;

[HtmlTargetElement("style-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public class StyleTagHelper(StyleBundleProvider provider, IWebHostEnvironment env)
    : BundleTagHelper<StyleBundleProvider, StyleBundle>(provider, env)
{
    protected override string RenderTag(string url) =>
        $"<link rel=\"stylesheet\" href=\"{url}\" data-bundle=\"{Name}\" />";
}
