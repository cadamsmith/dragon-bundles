namespace DragonBundles;

sealed partial class StyleBundleProvider(IWebHostEnvironment env) : BundleProvider<StyleBundle>(env, "/bundles/css/")
{
    [GeneratedRegex(@"url\(\s*(['""]?)([^'""\)\s]+)\1\s*\)")]
    private static partial Regex UrlPattern();

    protected override string Extension => "css";
    protected override StyleBundle Create(string name, List<string> sourceFiles) => new(name, sourceFiles);

    public override void Minify(StyleBundle bundle)
    {
        bundle.MinifiedContent = string.Join(ConcatenationToken, bundle.SourceFiles.Select(f =>
        {
            string content = ReadSourceFile(bundle.Name, f);
            return f.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase)
                ? content
                : Uglify.Css(content).Code;
        }));
        bundle.LastModified = DateTime.UtcNow;
    }

    protected override string TransformFileContent(string content, string sourceUrl)
    {
        string directory = sourceUrl[..sourceUrl.LastIndexOf('/')] + "/";
        return UrlPattern().Replace(content, m =>
        {
            string quote = m.Groups[1].Value;
            string url = m.Groups[2].Value;
            return $"url({quote}{RebaseUrl(url, directory)}{quote})";
        });
    }

    static string RebaseUrl(string url, string sourceDirectory)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith('/') || url.StartsWith('#') || url.Contains(':'))
        {
            return url;
        }

        return NormalizePath(sourceDirectory + url);
    }

    static string NormalizePath(string path)
    {
        List<string> segments = [];
        foreach (string segment in path.Split('/'))
        {
            if (segment == "..")
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }
            }
            else if (segment != ".")
            {
                segments.Add(segment);
            }
        }
        return string.Join('/', segments);
    }
}
