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

        if (env.IsEnvironment(Environments.Development))
        {
            foreach (string url in provider.GetSourceUrls(Name))
            {
                output.Content.AppendHtml(RenderTag(url, integrity: string.Empty));
            }
        }
        else
        {
            output.Content.AppendHtml($"{RenderTag(provider.GetUrl(Name), provider.GetIntegrity(Name))}\n");
        }
    }

    string RenderTag(string url, string integrity)
    {
        string sri = integrity.Length > 0
            ? $" integrity=\"{integrity}\" crossorigin=\"anonymous\""
            : string.Empty;
        return $"<script src=\"{url}\"{sri} data-bundle=\"{Name}\"></script>";
    }
}
