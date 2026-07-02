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

- **`net8.0` / `net10.0`** — ASP.NET Core. Bundles CSS/JS at startup and serves them through static file middleware via a custom `IFileProvider`. Both TFMs compile from the same source.
- **`net48`** — Classic ASP.NET (System.Web). Integrates with `System.Web.Optimization` as a drop-in, replacing WebGrease with NUglify.

The runtimes have entirely separate source files. `src/DragonBundles/` contains the ASP.NET Core implementation (net8.0 + net10.0); `src/DragonBundles/SystemWeb/` contains the net48 implementation. MSBuild ItemGroup conditions in the `.csproj` select the right set per TFM.

### public api surface — net10.0

Only five types are public — everything else is `internal`:

- `IBundleConfigurator` — fluent interface for registering bundles (`AddStyleBundle`, `AddScriptBundle`)
- `BundlingExtensions` — `IServiceCollection.AddBundling(Action<BundlingOptions>?)` and `IApplicationBuilder.UseBundling(Action<IBundleConfigurator>)`
- `BundlingOptions` — global NUglify minification settings (`ScriptSettings` / `StyleSettings`), configured via `AddBundling`
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
3. At startup, `BundleProvider<T>.Add()` resolves glob patterns via `Microsoft.Extensions.FileSystemGlobbing`, then in non-Development environments calls `Minify()` and `UpdateHashes()`. The latter hashes the minified bytes once and derives both `bundle.Version` (8-char lowercase hex SHA-256, used for cache busting) and `bundle.Integrity` (`sha384-<base64>` SRI string). Missing source files throw `FileNotFoundException`.
4. In non-Development environments, `WatchBundle()` registers `IChangeToken` watchers on each source file. Any change triggers `RebuildBundle()`, which re-minifies and re-hashes under `_rebuildLock` to serialize concurrent rebuilds. An `IOException` during rebuild is silently swallowed — the bundle retains its previous content until the next change.
5. Tag helpers use `Environments.Development` (not a custom string) to detect environment. In production, `GetUrl()` appends `?v={bundle.Version}` for cache busting and the tag helpers stamp `integrity="sha384-..." crossorigin="anonymous"` from `GetIntegrity()`.
6. Minified content is served in-memory via `BundleFileInfo : IFileInfo` — no files are written to disk.

### public api surface — net48

Three types are public:

- `BundleCollectionExtensions` — `BundleCollection.AddStyleBundle(name, files...)` and `AddScriptBundle(name, files...)`, plus `ConfigureBundling(Action<BundlingOptions>)` for global NUglify settings (mirrors ASP.NET Core's `AddBundling`; options are held per-`BundleCollection` via a `ConditionalWeakTable` and read when bundles are registered). Registers bundles at `~/bundles/css/{name}` and `~/bundles/js/{name}` with NUglify transforms.
- `HtmlHelperExtensions` — `@Html.StyleBundle(name)` and `@Html.ScriptBundle(name)`. When optimizations are enabled, hand-builds the bundle tag with `integrity="sha384-..." crossorigin="anonymous" data-bundle="{name}"` (matching the ASP.NET Core tag helpers); when off, falls back to `Styles.Render` / `Scripts.Render` (per-file tags, no SRI).
- `BundlingOptions` — shared, TFM-neutral minification settings type (also public on net10.0), configured here via `ConfigureBundling`.

Internal types:

- `NUglifyStyleTransform : IBundleTransform` — replaces WebGrease's `CssMinify`
- `NUglifyScriptTransform : IBundleTransform` — replaces WebGrease's `JsMinify`; also generates the JS source map (see below)
- `SourceMapStore` — static in-memory map of bundle name → generated source-map JSON
- `SourceMapHandler : IHttpHandler` / `SourceMapRouteHandler : IRouteHandler` — serve the stored map at `bundles/js/{name}.min.js.map`

### minification

Uses **NUglify** (`Uglify.Css` / `Uglify.Js`) on both TFMs.

- net10.0: called at startup in `BundleProvider<T>.Minify()`, reading from `env.WebRootPath`. Also re-triggered by file watching (non-Development only).
- net48: called at request time by `System.Web.Optimization` via the `IBundleTransform` pipeline.

CSS files are preprocessed by `StyleBundleProvider.TransformFileContent()` before concatenation: relative `url()` references are rewritten to absolute paths so stylesheets from different directories compose correctly after bundling.

JS files are separated by `;\n` before concatenation (`ScriptBundleProvider.ConcatenationToken`) to guard against ASI hazards at file boundaries.

### JS source maps

Script bundles emit a V3 source map plus a trailing `//# sourceMappingURL={name}.min.js.map` comment on both TFMs. The map-generation logic (per-file minify loop, pre-minified passthrough with output-line tracking, single trailing `sourceMappingURL`) lives in the TFM-neutral `ScriptMapMinifier.Minify(...)`, sharing `DeferredSourceMap` — both files are compiled into the net48 target via explicit `Compile Include` (like `BundlingOptions.cs`).

- net10.0: `ScriptBundleProvider.Minify()` builds `(sourceUrl, content)` pairs from `env.WebRootPath` and calls the helper; the map is stored on `bundle.SourceMap` and served in-memory at `{name}.min.js.map`.
- net48: `NUglifyScriptTransform` rebuilds the pairs from `BundleResponse.Files` (via `BundleFile.ApplyTransforms()` + `IncludedVirtualPath`) — the transform is handed already-concatenated content, so per-file info must come from `Files` — calls the helper, and stashes the map in the static in-memory `SourceMapStore` keyed by bundle name. `AddScriptBundle` registers one `System.Web.Routing.Route` (inserted at index 0, once) mapping `bundles/js/{name}.min.js.map` to `SourceMapHandler`, which serves the stored map as `application/json` with non-immutable caching. When `BundleResponse.Files` is unavailable (e.g. a unit test setting `Content` directly), the transform falls back to minifying the combined content with no map.

### subresource integrity (SRI)

Both TFMs emit `integrity="sha384-<base64>" crossorigin="anonymous"` on production bundle tags. The `sha384-` computation lives in the TFM-neutral `SriHash.Compute(...)` (compiled into net48 via explicit `Compile Include`; uses `SHA384.HashData` on net8/net10 and `SHA384.Create().ComputeHash` on net48 behind `#if NET5_0_OR_GREATER` since the static API is .NET 5+).

- net10.0: `BundleProvider.UpdateHashes()` hashes `bundle.MinifiedContent` at startup into `bundle.Integrity`; tag helpers stamp it.
- net48: `HtmlHelperExtensions` hashes the served bytes at render time — `bundle.GenerateBundleResponse(context).Content`, which runs the full transform pipeline (so the hash covers the JS `sourceMappingURL` comment) and reuses the same server cache the bundle handler serves from. Hand-builds the tag with `SriHash.Compute(...)`. `BuildLinkTag` / `BuildScriptTag` are the tag formatters (host-free, unit-tested). Falls back to `Styles.Render` / `Scripts.Render` when `BundleTable.EnableOptimizations` is false or the bundle is unregistered.

### tests

Tests live in `tests/DragonBundles.Tests/`. Internal types are exposed via `InternalsVisibleTo`. The project also multi-targets `net8.0;net10.0;net48` using the same conditional compile pattern as the main library.

**net10.0 tests** (root of `tests/DragonBundles.Tests/`):
- `StyleBundleProviderTests` / `ScriptBundleProviderTests` — provider logic. Tests that write files use a per-test temp directory cleaned up via `IDisposable`.
- `StyleTagHelperTests` / `ScriptTagHelperTests` — tag helper HTML output in dev vs production. Use real `TagHelperContext`/`TagHelperOutput` — no mocking needed.
- `BundlingIntegrationTests` — end-to-end HTTP tests using `TestServer` via `BundlingTestFixture` (`IClassFixture`). Verifies bundles are served at the correct URLs with correct content types and minified content; also confirms static files still pass through the `CompositeFileProvider`.
- `ScriptMapMinifierTests` — host-independent contract for the shared `ScriptMapMinifier` (sourceMappingURL append, per-source listing, pre-minified passthrough, no-map when all inputs are pre-minified). Primary local guard for source-map behavior on both TFMs since the net48 transform's map path only runs on a live host.
- `SriHashTests` — host-independent contract for the shared `SriHash` (`sha384-<base64>`, matches an independent SHA-384, deterministic/content-sensitive). Guards the SRI algorithm for both TFMs.

**net48 tests** (`tests/DragonBundles.Tests/SystemWeb/`):
- `NUglifyTransformTests` — `IBundleTransform` implementations (these hit the `Files == null` fallback, so the map path is covered by `ScriptMapMinifierTests`, not here) plus `SourceMapStore` round-trip. Uses a real `BundleResponse` with `null` context (transforms don't access context).
- `BundleCollectionExtensionsTests` — verifies virtual path registration and NUglify transform wiring via `GetBundleFor()`. Uses a fresh `new BundleCollection()` per test (never `BundleTable.Bundles`).
- `SourceMapServingTests` — host-free coverage of the source-map handler (serve/404) and the route `AddScriptBundle` registers.
- `HtmlHelperExtensionsTests` — host-free coverage of the SRI tag format (`BuildLinkTag` / `BuildScriptTag`). The render-time integrity computation needs a live host and is verified on Windows.

net48 tests compile on Mac but only run on Windows. CI runs them on a `windows-latest` GitHub Actions runner.

### target frameworks

`net8.0;net10.0;net48`. The ASP.NET Core implementation compiles for both `net8.0` and `net10.0` from the same source; ASP.NET Core types come from the versionless `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (net8.0 + net10.0). System.Web types come from `Microsoft.AspNet.Web.Optimization` and `Microsoft.AspNet.Mvc` NuGet packages (net48 only). `Microsoft.NETFramework.ReferenceAssemblies` enables cross-compilation of the net48 target on Mac.
