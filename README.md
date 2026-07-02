# 🐉 dragon-bundles

Modern CSS and JavaScript bundling and minification for **both** ASP.NET Core and classic ASP.NET (System.Web) — with one consistent API that makes migrating between them painless.

In development, source files are served individually for easy debugging. In production, they are concatenated, minified via [NUglify](https://github.com/trullock/NUglify), and served as a single fingerprinted file.

## why dragon-bundles

- **A migration bridge.** Bundle names and registration shape stay the same across .NET Framework and .NET Core. Modernize a legacy MVC5 app one step at a time without rewriting your view layer — swap the registration and rendering calls, keep the bundle names.
- **Kills WebGrease on classic ASP.NET.** `System.Web.Optimization` still ships the abandoned WebGrease minifier, which struggles with modern CSS/JS. DragonBundles drops in [NUglify](https://github.com/trullock/NUglify) as a replacement with no other code changes.
- **Secure by default.** Production bundles are emitted with a [Subresource Integrity](https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity) hash and `crossorigin` attribute automatically.
- **Zero build step, zero files on disk.** Bundles are built in-memory at startup and served through the pipeline. No node, no webpack, no generated artifacts to commit.
- **Automatic cache busting and hot reload.** Outside Development, content-hash `?v=` suffixes update whenever sources change, and source files are watched and re-minified without a restart.

## installation

```
dotnet add package DragonBundles
```

## migrating from system.web to asp.net core

This is what DragonBundles is built for. Because bundle names are identical across both runtimes, upgrading is a mechanical swap rather than a rewrite:

| | classic ASP.NET (net48) | ASP.NET Core (net8 / net10) |
|---|---|---|
| register | `bundles.AddStyleBundle("site", ...)` | `bundles.AddStyleBundle("site", ...)` |
| render | `@Html.StyleBundle("site")` | `<style-bundle name="site">` |

The bundle name (`"site"`) never changes, so your migration is confined to the registration and rendering calls — everything referencing bundles by name keeps working.

## feature parity

Both builds share one API and produce the same production output shape (fingerprinted, minified, SRI-protected bundles). Where a capability is inherited from the host framework rather than implemented by DragonBundles, that's noted.

| Feature | classic ASP.NET (net48) | ASP.NET Core (net8 / net10) |
|---|---|---|
| Registration API | `AddStyleBundle` / `AddScriptBundle` | `AddStyleBundle` / `AddScriptBundle` |
| Rendering API | `@Html.StyleBundle` / `@Html.ScriptBundle` | `<style-bundle>` / `<script-bundle>` tag helpers |
| Minifier | NUglify (replaces WebGrease) | NUglify |
| Global minification settings | `ConfigureBundling` | `AddBundling` |
| Dev vs. prod switch | `BundleTable.EnableOptimizations` | `IWebHostEnvironment` (Development) |
| Minification timing | request time, cached by System.Web | startup |
| Content-hash cache busting (`?v=`) | ✓ (System.Web) | ✓ |
| Subresource Integrity + `crossorigin` | ✓ | ✓ |
| JS source maps | ✓ | ✓ |
| ASI-safe JS concatenation | ✓ | ✓ |
| CSS relative `url()` rebasing | ✓ (`CssRewriteUrlTransform`) | ✓ (custom rebasing) |
| Hot reload on source change | ✓ (System.Web cache invalidation) | ✓ (file watcher, non-Development) |
| Glob patterns in file lists | System.Web native wildcards | ✓ recursive (`**`) |
| Files written to disk | none | none |

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

Script bundles emit a [source map](https://developer.mozilla.org/en-US/docs/Glossary/Source_map) referenced from the minified file (`//# sourceMappingURL={name}.min.js.map`) and served at `~/bundles/js/{name}.min.js.map`, so browser devtools resolve the bundle back to the original source files — parity with the ASP.NET Core target. `AddScriptBundle` registers the route that serves it, so no extra wiring is needed.

When optimizations are enabled, `@Html.StyleBundle` / `@Html.ScriptBundle` render the bundle tag with a [Subresource Integrity](https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity) hash and `crossorigin="anonymous"` (`integrity="sha384-..."` over the exact served bytes) — matching the ASP.NET Core tag helpers. In debug (optimizations off), they fall back to plain per-file tags with no SRI. The hash assumes bundles are served as UTF-8 (the default); a non-UTF-8 `<globalization responseEncoding>` would change the served bytes and break the integrity check.

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

## asp.net core (.net 8 / .net 10)

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
| `net8.0` | .NET 8, ASP.NET Core |
| `net10.0` | .NET 10, ASP.NET Core |
| `net48` | .NET Framework 4.8, ASP.NET MVC 5 |