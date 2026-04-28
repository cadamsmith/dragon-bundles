# 🐉 dragon-bundles: claude.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## commands

```bash
dotnet build                                        # build the solution
dotnet test                                         # run all tests
dotnet test --filter "FullyQualifiedName~StyleBundle"  # run a single test class
dotnet pack                                         # produce the NuGet .nupkg
```

## architecture

DragonBundles is a multi-targeted NuGet library supporting two runtimes:

- **`net10.0`** — ASP.NET Core. Bundles CSS/JS at startup and serves them through static file middleware via a custom `IFileProvider`.
- **`net48`** — Classic ASP.NET (System.Web). Integrates with `System.Web.Optimization` as a drop-in, replacing WebGrease with NUglify.

The two TFMs have entirely separate source files. `src/DragonBundles/` contains the net10.0 implementation; `src/DragonBundles/SystemWeb/` contains the net48 implementation. MSBuild ItemGroup conditions in the `.csproj` select the right set per TFM.

### public api surface — net10.0

Only four types are public — everything else is `internal`:

- `IBundleConfigurator` — fluent interface for registering bundles (`AddStyleBundle`, `AddScriptBundle`)
- `BundlingExtensions` — `IServiceCollection.AddBundling()` and `IApplicationBuilder.UseBundling(Action<IBundleConfigurator>)`
- `StyleTagHelper` — `<style-bundle name="...">` Razor tag helper
- `ScriptTagHelper` — `<script-bundle name="...">` Razor tag helper

### internal type hierarchy — net10.0

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

### request flow — net10.0

1. `AddBundling()` registers `StyleBundleProvider` and `ScriptBundleProvider` as singletons.
2. `UseBundling(configure)` runs the configure action (populating the providers), then calls `UseStaticFiles` with a `CompositeFileProvider` that layers the two bundle providers over `WebRootFileProvider`.
3. At startup, `BundleProvider<T>.Add()` resolves glob patterns via `Microsoft.Extensions.FileSystemGlobbing`, then in non-Development environments calls `Minify()` and hashes the result into `bundle.Version` (8-char lowercase hex SHA-256). Missing source files throw `FileNotFoundException`.
4. In non-Development environments, `WatchBundle()` registers `IChangeToken` watchers on each source file. Any change triggers `RebuildBundle()`, which re-minifies and re-hashes under `_rebuildLock` to serialize concurrent rebuilds. An `IOException` during rebuild is silently swallowed — the bundle retains its previous content until the next change.
5. Tag helpers use `Environments.Development` (not a custom string) to detect environment. `GetUrl()` appends `?v={bundle.Version}` to production bundle URLs for cache busting.
6. Minified content is served in-memory via `BundleFileInfo : IFileInfo` — no files are written to disk.

### public api surface — net48

Two types are public:

- `BundleCollectionExtensions` — `BundleCollection.AddStyleBundle(name, files...)` and `AddScriptBundle(name, files...)`. Registers bundles at `~/bundles/css/{name}` and `~/bundles/js/{name}` with NUglify transforms.
- `HtmlHelperExtensions` — `@Html.StyleBundle(name)` and `@Html.ScriptBundle(name)`. Wraps `Styles.Render` / `Scripts.Render` with the internal virtual path.

Two types are internal:

- `NUglifyStyleTransform : IBundleTransform` — replaces WebGrease's `CssMinify`
- `NUglifyScriptTransform : IBundleTransform` — replaces WebGrease's `JsMinify`

### minification

Uses **NUglify** (`Uglify.Css` / `Uglify.Js`) on both TFMs.

- net10.0: called at startup in `BundleProvider<T>.Minify()`, reading from `env.WebRootPath`. Also re-triggered by file watching (non-Development only).
- net48: called at request time by `System.Web.Optimization` via the `IBundleTransform` pipeline.

CSS files are preprocessed by `StyleBundleProvider.TransformFileContent()` before concatenation: relative `url()` references are rewritten to absolute paths so stylesheets from different directories compose correctly after bundling.

JS files are separated by `;\n` before concatenation (`ScriptBundleProvider.ConcatenationToken`) to guard against ASI hazards at file boundaries.

### tests

Tests live in `tests/DragonBundles.Tests/`. Internal types are exposed via `InternalsVisibleTo`. The project also multi-targets `net10.0;net48` using the same conditional compile pattern as the main library.

**net10.0 tests** (root of `tests/DragonBundles.Tests/`):
- `StyleBundleProviderTests` / `ScriptBundleProviderTests` — provider logic. Tests that write files use a per-test temp directory cleaned up via `IDisposable`.
- `StyleTagHelperTests` / `ScriptTagHelperTests` — tag helper HTML output in dev vs production. Use real `TagHelperContext`/`TagHelperOutput` — no mocking needed.
- `BundlingIntegrationTests` — end-to-end HTTP tests using `TestServer` via `BundlingTestFixture` (`IClassFixture`). Verifies bundles are served at the correct URLs with correct content types and minified content; also confirms static files still pass through the `CompositeFileProvider`.

**net48 tests** (`tests/DragonBundles.Tests/SystemWeb/`):
- `NUglifyTransformTests` — `IBundleTransform` implementations. Uses a real `BundleResponse` with `null` context (transforms don't access context).
- `BundleCollectionExtensionsTests` — verifies virtual path registration and NUglify transform wiring via `GetBundleFor()`. Uses a fresh `new BundleCollection()` per test (never `BundleTable.Bundles`).

net48 tests compile on Mac but only run on Windows. CI runs them on a `windows-latest` GitHub Actions runner.

### target frameworks

`net10.0;net48`. ASP.NET Core types come from `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (net10.0 only). System.Web types come from `Microsoft.AspNet.Web.Optimization` and `Microsoft.AspNet.Mvc` NuGet packages (net48 only). `Microsoft.NETFramework.ReferenceAssemblies` enables cross-compilation of the net48 target on Mac.
