namespace DragonBundles;

/// <summary>
/// Renders a JavaScript bundle. In Development, emits one <c>&lt;script&gt;</c> per source file;
/// in production, emits a single minified bundle script with a content-hash cache-busting suffix.
/// </summary>
[HtmlTargetElement("script-bundle", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class ScriptTagHelper(IServiceProvider services, IWebHostEnvironment env) : TagHelper
{
    /// <summary>The bundle name, as registered via <see cref="IBundleConfigurator.AddScriptBundle"/>.</summary>
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ScriptBundleProvider provider = services.GetRequiredService<ScriptBundleProvider>();
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
        $"<script src=\"{url}\" data-bundle=\"{Name}\"></script>";
}
