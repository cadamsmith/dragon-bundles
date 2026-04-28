namespace DragonBundles;

/// <summary>
/// Renders a CSS bundle. In Development, emits one <c>&lt;link&gt;</c> per source file;
/// in production, emits a single minified bundle link with a content-hash cache-busting suffix.
/// </summary>
[HtmlTargetElement("style-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class StyleTagHelper(IServiceProvider services, IWebHostEnvironment env) : TagHelper
{
    /// <summary>The bundle name, as registered via <see cref="IBundleConfigurator.AddStyleBundle"/>.</summary>
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    /// <inheritdoc />
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
