namespace DragonBundles;

internal sealed class BundleConfigurator(StyleBundleProvider styleProvider, ScriptBundleProvider scriptProvider)
    : IBundleConfigurator
{
    public IBundleConfigurator AddStyleBundle(string name, params string[] files)
    {
        styleProvider.Add(name, files);
        return this;
    }

    public IBundleConfigurator AddScriptBundle(string name, params string[] files)
    {
        scriptProvider.Add(name, files);
        return this;
    }
}
