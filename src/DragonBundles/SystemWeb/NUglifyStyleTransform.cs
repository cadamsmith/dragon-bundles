namespace DragonBundles;

sealed class NUglifyStyleTransform(CssSettings? settings = null) : IBundleTransform
{
    readonly CssSettings _settings = settings ?? new CssSettings();

    public void Process(BundleContext context, BundleResponse response)
    {
        response.Content = Uglify.Css(response.Content, _settings).Code;
        response.ContentType = "text/css";
    }
}
