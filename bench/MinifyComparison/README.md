# MinifyComparison

Compares **NUglify** (the minifier DragonBundles uses) against **WebGrease** (the minifier
`System.Web.Optimization` ships and that DragonBundles replaces) on a corpus of classic and
modern CSS/JS.

This is a standalone diagnostic — it is intentionally **not** part of `DragonBundles.slnx`, so
`dotnet build`/`dotnet test`/CI ignore it. WebGrease is a .NET Framework package; it loads on
net10 via the compatibility shim (the `NU1701` warning is expected and suppressed). "WebGrease"
here is its bundled `Microsoft.Ajax.Utilities` engine — the same engine `System.Web.Optimization`
uses to minify.

Versions: the harness pins **WebGrease 1.6.0** (the latest published). Note that
`Microsoft.AspNet.Web.Optimization` 1.1.3 — the package DragonBundles' net48 target references —
depends on **WebGrease 1.5.2**, so that's the version replaced in practice. Both fail identically
on modern syntax, so the result below holds for either.

## Run

```bash
cd bench/MinifyComparison
dotnet run -c Release
```

## Result

```
file             orig   WebGrease                 NUglify
------------------------------------------------------------------------------
classic.css       195      113 B  -42.1%    0.51ms   106 B  -45.6%    0.43ms
classic.js        319      184 B  -42.3%    0.66ms   184 B  -42.3%    0.72ms
modern.css        352   FAILED (8 err)               275 B  -21.9%    0.09ms
modern.js         465   FAILED (threw)               304 B  -34.6%    0.24ms
```

## Takeaway

- On **classic** CSS/JS the two are neck-and-neck (NUglify is a fork of the same AjaxMin engine),
  with NUglify a touch smaller on CSS.
- On **modern** syntax WebGrease **fails outright** — it can't parse ES2020+ JS (private fields,
  arrow functions, optional chaining `?.`, nullish `??`, spread) or modern CSS (custom properties,
  `calc()`, `clamp()`, `:is()`, grid). NUglify handles all of it.

That compatibility gap — not raw ratio — is why DragonBundles replaces WebGrease.

The corpus lives in `corpus/`; add files there (`.css`/`.js`) to extend the comparison.