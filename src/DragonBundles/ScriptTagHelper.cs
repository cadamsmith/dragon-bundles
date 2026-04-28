namespace DragonBundles;

[HtmlTargetElement("script-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class ScriptTagHelper(IServiceProvider services, IWebHostEnvironment env) : TagHelper
{
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ScriptBundleProvider provider = services.GetRequiredService<ScriptBundleProvider>();
        output.TagName = null;
        output.SuppressOutput();

        IEnumerable<string> tags = env.IsEnvironment(Environments.Development)
            ? provider.GetSourceUrls(Name).Select(RenderTag)
            : [$"{RenderTag(provider.GetUrl(Name))}\n"];

        foreach (string tag in tags)
            output.Content.AppendHtml(tag);
    }

    string RenderTag(string url) =>
        $"<script src=\"{url}\" data-bundle=\"{Name}\"></script>";
}
