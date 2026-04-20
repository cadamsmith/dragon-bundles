namespace DragonBundles;

public static class BundlingExtensions
{
    public static IServiceCollection AddBundling(this IServiceCollection services)
    {
        services.AddSingleton<StyleBundleProvider>();
        services.AddSingleton<ScriptBundleProvider>();
        return services;
    }

    public static IApplicationBuilder UseBundling(this IApplicationBuilder app, Action<IBundleConfigurator> configure)
    {
        StyleBundleProvider styles = app.ApplicationServices.GetRequiredService<StyleBundleProvider>();
        ScriptBundleProvider scripts = app.ApplicationServices.GetRequiredService<ScriptBundleProvider>();
        IWebHostEnvironment webEnv = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        configure(new BundleConfigurator(styles, scripts));

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new CompositeFileProvider(styles, scripts, webEnv.WebRootFileProvider)
        });

        return app;
    }
}
