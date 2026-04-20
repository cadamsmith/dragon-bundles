# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # build the solution
dotnet test           # run tests (no test project yet)
dotnet pack           # produce the NuGet .nupkg
```

## Architecture

DragonBundles is an ASP.NET Core NuGet library that bundles and minifies CSS/JS files at startup and serves them through the static file middleware via a custom `IFileProvider`.

### Public API surface

Only four types are public — everything else is `internal`:

- `IBundleConfigurator` — fluent interface for registering bundles (`AddStyleBundle`, `AddScriptBundle`)
- `BundlingExtensions` — `IServiceCollection.AddBundling()` and `IApplicationBuilder.UseBundling(Action<IBundleConfigurator>)`
- `StyleTagHelper` — `<style-bundle name="...">` Razor tag helper
- `ScriptTagHelper` — `<script-bundle name="...">` Razor tag helper

### Internal type hierarchy

```
Bundle (abstract)
├── StyleBundle
└── ScriptBundle

BundleProvider<T> : IFileProvider (abstract)
├── StyleBundleProvider   — serves /bundles/css/*.min.css
└── ScriptBundleProvider  — serves /bundles/js/*.min.js

BundleTagHelper<TProvider, TBundle> : TagHelper (abstract)
├── StyleTagHelper
└── ScriptTagHelper

BundleConfigurator : IBundleConfigurator
```

### Request flow

1. `AddBundling()` registers `StyleBundleProvider` and `ScriptBundleProvider` as singletons.
2. `UseBundling(configure)` runs the configure action (populating the providers), then calls `UseStaticFiles` with a `CompositeFileProvider` that layers the two bundle providers over `WebRootFileProvider`.
3. At startup, `BundleProvider<T>.Add()` calls `Minify()` immediately in non-Development environments; in Development it skips minification and the tag helpers render individual source file tags instead.
4. Tag helpers use `Environments.Development` (not a custom string) to detect environment.
5. Minified content is served in-memory via `BundleFileInfo : IFileInfo` — no files are written to disk.

### Minification

Uses **NUglify** (`Uglify.Css` / `Uglify.Js`). Source files are read from `env.WebRootPath` (injected `IWebHostEnvironment`).

### Target framework

`net10.0`. ASP.NET Core types are available via `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (not individual packages).
