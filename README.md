# DragonBundles

CSS and JavaScript bundling and minification for ASP.NET Core.

In development, source files are served individually for easy debugging. In production, they are concatenated, minified via [NUglify](https://github.com/trullock/NUglify), and served as a single in-memory file.

## Installation

```
dotnet add package DragonBundles
```

## Setup

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

## Usage

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

## Requirements

- .NET 10
- ASP.NET Core
