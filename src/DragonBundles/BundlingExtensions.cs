using Microsoft.AspNetCore.StaticFiles;

namespace DragonBundles;

/// <summary>Extension methods for registering DragonBundles with ASP.NET Core.</summary>
public static class BundlingExtensions
{
    /// <summary>Registers DragonBundles services with the dependency injection container.</summary>
    public static IServiceCollection AddBundling(this IServiceCollection services)
    {
        services.AddSingleton<StyleBundleProvider>();
        services.AddSingleton<ScriptBundleProvider>();
        return services;
    }

    /// <summary>
    /// Configures bundles and adds the DragonBundles static file middleware.
    /// In non-Development environments, bundles are minified at startup and re-minified automatically when source files change.
    /// </summary>
    public static IApplicationBuilder UseBundling(this IApplicationBuilder app, Action<IBundleConfigurator> configure)
    {
        StyleBundleProvider styles = app.ApplicationServices.GetRequiredService<StyleBundleProvider>();
        ScriptBundleProvider scripts = app.ApplicationServices.GetRequiredService<ScriptBundleProvider>();
        IWebHostEnvironment webEnv = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        configure(new BundleConfigurator(styles, scripts));

        FileExtensionContentTypeProvider contentTypeProvider = new();
        contentTypeProvider.Mappings[".map"] = "application/json";

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new CompositeFileProvider(styles, scripts, webEnv.WebRootFileProvider),
            ContentTypeProvider = contentTypeProvider,
            OnPrepareResponse = ctx =>
            {
                // Source maps are named after the bundle, not content-hashed, so they must not
                // be cached immutably — the minified file they belong to is still fingerprinted.
                bool isSourceMap = ctx.Context.Request.Path.Value?.EndsWith(".map", StringComparison.Ordinal) == true;
                if (!webEnv.IsDevelopment()
                    && ctx.Context.Request.Path.StartsWithSegments("/bundles")
                    && !isSourceMap)
                {
                    ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                }
            }
        });

        return app;
    }
}
