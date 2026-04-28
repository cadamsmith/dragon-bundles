namespace DragonBundles;

/// <summary>Extension methods for rendering DragonBundles in ASP.NET MVC Razor views.</summary>
public static class HtmlHelperExtensions
{
    extension(HtmlHelper helper)
    {
        /// <summary>Renders the CSS bundle registered under <paramref name="name"/>.</summary>
        public static IHtmlString StyleBundle(string name) =>
            Styles.Render($"~/bundles/css/{name}");

        /// <summary>Renders the JavaScript bundle registered under <paramref name="name"/>.</summary>
        public static IHtmlString ScriptBundle(string name) =>
            Scripts.Render($"~/bundles/js/{name}");
    }
}
