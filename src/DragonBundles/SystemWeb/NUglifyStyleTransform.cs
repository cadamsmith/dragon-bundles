namespace DragonBundles;

internal sealed class NUglifyStyleTransform : IBundleTransform
{
    public void Process(BundleContext context, BundleResponse response)
    {
        response.Content = Uglify.Css(response.Content).Code;
        response.ContentType = "text/css";
    }
}
