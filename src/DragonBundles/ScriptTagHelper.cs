namespace DragonBundles;

[HtmlTargetElement("script-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public class ScriptTagHelper(ScriptBundleProvider provider, IWebHostEnvironment env)
    : BundleTagHelper<ScriptBundleProvider, ScriptBundle>(provider, env)
{
    protected override string RenderTag(string url) =>
        $"<script src=\"{url}\" data-bundle=\"{Name}\"></script>";
}
