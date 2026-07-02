namespace DragonBundles;

sealed class NUglifyScriptTransform(CodeSettings? settings = null) : IBundleTransform
{
    readonly CodeSettings _settings = settings ?? new CodeSettings();

    public void Process(BundleContext context, BundleResponse response)
    {
        response.Content = Uglify.Js(response.Content, _settings).Code;
        response.ContentType = "text/javascript";
    }
}
