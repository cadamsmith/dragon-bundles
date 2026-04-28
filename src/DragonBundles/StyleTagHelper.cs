namespace DragonBundles;

[HtmlTargetElement("style-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class StyleTagHelper(IServiceProvider services, IWebHostEnvironment env) : TagHelper
{
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        StyleBundleProvider provider = services.GetRequiredService<StyleBundleProvider>();
        output.TagName = null;
        output.SuppressOutput();

        IEnumerable<string> tags = env.IsEnvironment(Environments.Development)
            ? provider.GetSourceUrls(Name).Select(RenderTag)
            : [$"{RenderTag(provider.GetUrl(Name))}\n"];

        foreach (string tag in tags)
        {
            output.Content.AppendHtml(tag);
        }
    }

    string RenderTag(string url) =>
        $"<link rel=\"stylesheet\" href=\"{url}\" data-bundle=\"{Name}\" />";
}
