namespace DragonBundles;

internal sealed class NUglifyScriptTransform : IBundleTransform
{
    public void Process(BundleContext context, BundleResponse response)
    {
        response.Content = Uglify.Js(response.Content).Code;
        response.ContentType = "text/javascript";
    }
}
