namespace DragonBundles;

public interface IBundleConfigurator
{
    IBundleConfigurator AddStyleBundle(string name, params string[] files);
    IBundleConfigurator AddScriptBundle(string name, params string[] files);
}
