namespace DragonBundles;

sealed class NUglifyScriptTransform(string bundleName, CodeSettings? settings = null) : IBundleTransform
{
    // Match the ASP.NET Core target's ASI-safe token so both runtimes concatenate identically.
    static readonly string _concatenationToken = ";" + Environment.NewLine;

    readonly CodeSettings _settings = settings ?? new CodeSettings();

    public void Process(BundleContext context, BundleResponse response)
    {
        List<(string, string)>? files = ReadFiles(response);

        if (files is null || files.Count == 0)
        {
            // No per-file information available (e.g. a unit test that sets Content directly with
            // no Files). Minify the already-concatenated content; there is nothing to map.
            response.Content = Uglify.Js(response.Content, _settings).Code;
        }
        else
        {
            (string content, string? map) =
                ScriptMapMinifier.Minify(bundleName, files, _settings, _concatenationToken);

            response.Content = content;
            if (map is not null)
            {
                SourceMapStore.Set(bundleName, map);
            }
        }

        response.ContentType = "text/javascript";
    }

    // Re-reads each included file (applying any item transforms) alongside its virtual path, so the
    // bundle can be re-minified per file with source-map offsets — System.Web hands the transform
    // the already-concatenated blob, which has lost the file boundaries a map needs.
    static List<(string, string)>? ReadFiles(BundleResponse response)
    {
        if (response.Files is null)
        {
            return null;
        }

        List<(string, string)> files = [];
        foreach (BundleFile file in response.Files)
        {
            // IncludedVirtualPath is app-relative (e.g. "~/Scripts/app.js"); "~" is meaningless to a
            // browser, so make it web-absolute ("/Scripts/app.js") — matching the ASP.NET Core map's
            // source style — so devtools can actually fetch the original file from static content.
            string sourceUrl = VirtualPathUtility.ToAbsolute(file.IncludedVirtualPath);
            files.Add((sourceUrl, file.ApplyTransforms()));
        }

        return files;
    }
}
