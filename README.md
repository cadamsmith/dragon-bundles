# DragonBundles

CSS and JavaScript bundling and minification for ASP.NET Core and classic ASP.NET (System.Web).

> [!WARNING]
> This package is a work in progress and not yet available on NuGet.

In development, source files are served individually for easy debugging. In production, they are concatenated, minified via [NUglify](https://github.com/trullock/NUglify), and served as a single file.

## Installation

```
dotnet add package DragonBundles
```

## ASP.NET Core (.NET 10)

### Setup

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

### Usage

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

In **Production**, a single minified bundle is rendered:

```html
<link rel="stylesheet" href="/bundles/css/site.min.css" data-bundle="site" />
```

Bundles are minified at startup and served in-memory — no files are written to disk.

## Classic ASP.NET / System.Web (.NET Framework 4.8)

DragonBundles integrates with `System.Web.Optimization` as a drop-in upgrade, replacing the default WebGrease minifier with NUglify.

### Setup

In `BundleConfig.cs`:

```csharp
using DragonBundles;

public static void RegisterBundles(BundleCollection bundles)
{
    bundles.AddStyleBundle("site", "~/Content/base.css", "~/Content/layout.css");
    bundles.AddScriptBundle("app", "~/Scripts/utils.js", "~/Scripts/app.js");
}
```

### Usage

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

## Migrating from System.Web to ASP.NET Core

DragonBundles is designed to smooth this migration. Bundle names are consistent across both runtimes — swap the registration and rendering calls when you upgrade, and bundle names stay the same.

## Requirements

| Target | Runtime |
|---|---|
| `net10.0` | .NET 10, ASP.NET Core |
| `net48` | .NET Framework 4.8, ASP.NET MVC 5 |
