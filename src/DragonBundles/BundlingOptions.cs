using NUglify.Css;
using NUglify.JavaScript;

namespace DragonBundles;

/// <summary>
/// Global NUglify minification settings for bundles. On ASP.NET Core, configure via
/// <c>AddBundling</c>; on classic ASP.NET, via <c>BundleCollection.ConfigureBundling</c>.
/// </summary>
public sealed class BundlingOptions
{
    /// <summary>NUglify settings applied when minifying script bundles.</summary>
    public CodeSettings ScriptSettings { get; set; } = new();

    /// <summary>NUglify settings applied when minifying style bundles.</summary>
    public CssSettings StyleSettings { get; set; } = new();
}
