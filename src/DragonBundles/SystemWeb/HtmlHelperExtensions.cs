namespace DragonBundles;

public static class HtmlHelperExtensions
{
    public static IHtmlString StyleBundle(this HtmlHelper helper, string name) =>
        Styles.Render($"~/bundles/css/{name}");

    public static IHtmlString ScriptBundle(this HtmlHelper helper, string name) =>
        Scripts.Render($"~/bundles/js/{name}");
}
