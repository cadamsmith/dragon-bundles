namespace DragonBundles;

/// <summary>Fluent interface for registering CSS and JavaScript bundles.</summary>
public interface IBundleConfigurator
{
    /// <summary>Registers a CSS bundle.</summary>
    /// <param name="name">Bundle name. Served at <c>/bundles/css/{name}.min.css</c> in production.</param>
    /// <param name="files">Source file paths relative to the web root. Glob patterns are supported.</param>
    IBundleConfigurator AddStyleBundle(string name, params string[] files);

    /// <summary>Registers a JavaScript bundle.</summary>
    /// <param name="name">Bundle name. Served at <c>/bundles/js/{name}.min.js</c> in production.</param>
    /// <param name="files">Source file paths relative to the web root. Glob patterns are supported.</param>
    IBundleConfigurator AddScriptBundle(string name, params string[] files);
}
