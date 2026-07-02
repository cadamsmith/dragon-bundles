using System.Collections.Concurrent;

namespace DragonBundles;

/// <summary>
/// Holds the most recently generated JS source map for each bundle, keyed by bundle name.
/// The request-time <see cref="NUglifyScriptTransform"/> writes here whenever it minifies; the
/// <see cref="SourceMapHandler"/> route reads from here. Kept in memory (no disk) to match the
/// ASP.NET Core target. A map is written before the browser can learn its URL (which only appears
/// in the <c>sourceMappingURL</c> comment of the already-served bundle), so a lookup can never
/// race ahead of the store.
/// </summary>
static class SourceMapStore
{
    static readonly ConcurrentDictionary<string, string> _maps = new(StringComparer.Ordinal);

    public static void Set(string bundleName, string map) => _maps[bundleName] = map;

    public static bool TryGet(string bundleName, out string map) => _maps.TryGetValue(bundleName, out map);
}
