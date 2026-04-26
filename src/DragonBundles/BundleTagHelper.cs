namespace DragonBundles;

public abstract class BundleTagHelper<TProvider, TBundle>(TProvider provider, IWebHostEnvironment env)
    : TagHelper where TProvider : BundleProvider<TBundle> where TBundle : Bundle
{
    [HtmlAttributeName("name")]
    public required string Name { get; set; }

    protected abstract string RenderTag(string url);

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
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
}
