# 🐉 dragon-bundles: contributing

## prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- On macOS/Linux: net48 compiles via `Microsoft.NETFramework.ReferenceAssemblies` but net48 tests only run on Windows

## build

```bash
dotnet build
```

## test

```bash
dotnet test                                            # all tests (net10.0 only on non-Windows)
dotnet test --framework net10.0                        # net10.0 explicitly
dotnet test --filter "FullyQualifiedName~StyleBundle"  # single test class
```

net48 tests run in CI on a `windows-latest` runner. If you're on macOS or Linux, only the net10.0 tests will execute locally.

## code style

Format is enforced by `dotnet format`. CI will reject unformatted code.

```bash
dotnet format          # fix
dotnet format --verify-no-changes  # check only
```

A pre-commit hook runs the check automatically. Enable it once after cloning:

```bash
git config core.hooksPath .githooks
```

## project layout

```
src/DragonBundles/              # library (net10.0 + net48)
  SystemWeb/                    # net48-only source files
tests/DragonBundles.Tests/      # tests (net10.0 + net48)
  SystemWeb/                    # net48-only tests
```

The two TFMs share a single `.csproj` with MSBuild `Condition` guards — net10.0 uses ASP.NET Core, net48 uses `System.Web.Optimization`.

## submitting changes

1. Fork the repository and create a branch off `main`.
2. Make your changes and add or update tests as needed.
3. Run `dotnet build` and `dotnet test --framework net10.0` locally before pushing.
4. Open a pull request against `main`.
