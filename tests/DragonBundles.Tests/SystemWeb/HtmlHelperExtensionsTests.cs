namespace DragonBundles.Tests.SystemWeb;

// Host-free coverage of the SRI tag format. Emitting the tags at render time needs BundleTable +
// HttpContext (a live host), so that path is verified on Windows; the tag shape — the parity-
// critical part — is locked here.
public class HtmlHelperExtensionsTests
{
    [Fact]
    public void BuildLinkTag_EmitsStylesheetWithIntegrityAndCrossorigin()
    {
        string tag = HtmlHelperExtensions.BuildLinkTag("/bundles/css/site?v=abc123", "sha384-ZZZ", "site");

        Assert.Equal(
            "<link rel=\"stylesheet\" href=\"/bundles/css/site?v=abc123\" integrity=\"sha384-ZZZ\" crossorigin=\"anonymous\" data-bundle=\"site\" />",
            tag);
    }

    [Fact]
    public void BuildScriptTag_EmitsScriptWithIntegrityAndCrossorigin()
    {
        string tag = HtmlHelperExtensions.BuildScriptTag("/bundles/js/app?v=abc123", "sha384-ZZZ", "app");

        Assert.Equal(
            "<script src=\"/bundles/js/app?v=abc123\" integrity=\"sha384-ZZZ\" crossorigin=\"anonymous\" data-bundle=\"app\"></script>",
            tag);
    }
}
