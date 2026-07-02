# 🐉 dragon-bundles

Modern CSS and JavaScript bundling and minification for **both** ASP.NET Core and classic ASP.NET (System.Web) — with one consistent API that makes migrating between them painless.

In development, source files are served individually for easy debugging. In production, they are concatenated, minified via [NUglify](https://github.com/trullock/NUglify), and served as a single fingerprinted file.

## why dragon-bundles

- **A migration bridge.** Bundle names and registration shape stay the same across .NET Framework and .NET Core. Modernize a legacy MVC5 app one step at a time without rewriting your view layer — swap the registration and rendering calls, keep the bundle names.
- **Kills WebGrease on classic ASP.NET.** `System.Web.Optimization` still ships the abandoned WebGrease minifier, which struggles with modern CSS/JS. DragonBundles drops in [NUglify](https://github.com/trullock/NUglify) as a replacement with no other code changes.
- **Secure by default.** Production bundles are emitted with a [Subresource Integrity](https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity) hash and `crossorigin` attribute automatically.
- **Zero build step, zero files on disk.** Bundles are built in-memory at startup and served through the pipeline. No node, no webpack, no generated artifacts to commit.
- **Automatic cache busting and hot reload.** Content-hash `?v=` suffixes update whenever sources change, and source files are watched and re-minified without a restart.

## installation

```
dotnet add package DragonBundles
```

## migrating from system.web to asp.net core

This is what DragonBundles is built for. Because bundle names are identical across both runtimes, upgrading is a mechanical swap rather than a rewrite:

| | classic ASP.NET (net48) | ASP.NET Core (net10) |
|---|---|---|
| register | `bundles.AddStyleBundle("site", ...)` | `bundles.AddStyleBundle("site", ...)` |
| render | `@Html.StyleBundle("site")` | `<style-bundle name="site">` |

The bundle name (`"site"`) never changes, so your migration is confined to the registration and rendering calls — everything referencing bundles by name keeps working.

## classic asp.net / system.web (.net framework 4.8)

DragonBundles integrates with `System.Web.Optimization` as a drop-in upgrade, replacing the default WebGrease minifier with NUglify.

### setup

In `BundleConfig.cs`:

```csharp
using DragonBundles;

public static void RegisterBundles(BundleCollection bundles)
{
    bundles.AddStyleBundle("site", "~/Content/base.css", "~/Content/layout.css");
    bundles.AddScriptBundle("app", "~/Scripts/utils.js", "~/Scripts/app.js");
}
```

### usage

In Razor views, add `@using DragonBundles` (or add it to `Web.config`), then:

```cshtml
<head>
    @Html.StyleBundle("site")
</head>
<body>
    ...
    @Html.ScriptBundle("app")
</body>
```

Dev/prod rendering is controlled by `BundleTable.EnableOptimizations` — individual files in debug, single bundle in release, matching standard `System.Web.Optimization` behavior.

To control NUglify's output, call `ConfigureBundling` before registering bundles — the same
global model as ASP.NET Core's `AddBundling`:

```csharp
using NUglify.Css;

bundles.ConfigureBundling(o =>
{
    o.ScriptSettings.PreserveImportantComments = true;
    o.StyleSettings.CommentMode = CssComment.None;
});
```

## asp.net core (.net 10)

### setup

In `Program.cs`:

```csharp
builder.Services.AddBundling();

// ...

app.UseBundling(bundles =>
{
    bundles
        .AddStyleBundle("site", "/css/base.css", "/css/layout.css")
        .AddScriptBundle("app", "/js/utils.js", "/js/app.js");
});
```

In `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, DragonBundles
```

### usage

```cshtml
<head>
    <style-bundle name="site"></style-bundle>
</head>
<body>
    ...
    <script-bundle name="app"></script-bundle>
</body>
```

In **Development**, each source file gets its own tag:

```html
<link rel="stylesheet" href="/css/base.css" data-bundle="site" />
<link rel="stylesheet" href="/css/layout.css" data-bundle="site" />
```

In **Production**, a single minified bundle is rendered with a [Subresource Integrity](https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity) hash:

```html
<link rel="stylesheet" href="/bundles/css/site.min.css?v=a1b2c3d4" integrity="sha384-..." crossorigin="anonymous" data-bundle="site" />
```

The `?v=...` suffix is a content hash for cache busting, updated automatically whenever source files change. The `integrity` attribute is a SHA-384 hash of the bundle bytes the browser uses to verify the response wasn't tampered with.

Script bundles also emit a [source map](https://developer.mozilla.org/en-US/docs/Glossary/Source_map) (`app.min.js.map`) referenced from the minified file, so browser devtools resolve the minified bundle back to the original source files.

Bundles are minified at startup and served in-memory — no files are written to disk. In non-Development environments, source files are watched for changes and re-minified automatically without a restart.

Relative `url()` references in CSS files are rewritten to absolute paths before minification, so stylesheets from different directories compose correctly.

Source file paths accept glob patterns:

```csharp
bundles.AddStyleBundle("site", "/css/**/*.css");
```

Missing source files throw `FileNotFoundException` at startup.

### configuring minification

Pass a callback to `AddBundling` to control NUglify's minification of all bundles via
[`CodeSettings`](https://github.com/trullock/NUglify) (scripts) and `CssSettings` (styles):

```csharp
using NUglify.Css;

builder.Services.AddBundling(o =>
{
    o.ScriptSettings.PreserveImportantComments = true;
    o.StyleSettings.CommentMode = CssComment.None;
});
```

## requirements

| Target | Runtime |
|---|---|
| `net10.0` | .NET 10, ASP.NET Core |
| `net48` | .NET Framework 4.8, ASP.NET MVC 5 |