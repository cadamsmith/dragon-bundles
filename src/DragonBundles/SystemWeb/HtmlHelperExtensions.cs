namespace DragonBundles;

public static class HtmlHelperExtensions
{
    extension(HtmlHelper helper)
    {
        public static IHtmlString StyleBundle(string name) =>
            Styles.Render($"~/bundles/css/{name}");

        public static IHtmlString ScriptBundle(string name) =>
            Scripts.Render($"~/bundles/js/{name}");
    }
}
