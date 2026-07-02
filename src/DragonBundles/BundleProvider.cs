namespace DragonBundles;

abstract class BundleProvider<T>(IWebHostEnvironment env, string bundleDirectory)
    : IFileProvider where T : Bundle
{
    readonly Dictionary<string, T> _bundles = [];
    readonly bool _isDevelopment = env.IsEnvironment(Environments.Development);
    readonly IFileProvider _webRootFileProvider = env.WebRootFileProvider;
    protected readonly string WebRootPath = env.WebRootPath;

    readonly object _rebuildLock = new();

    protected abstract string Extension { get; }
    protected virtual string ConcatenationToken => Environment.NewLine;
    protected abstract T Create(string name, List<string> sourceFiles);
    public abstract void Minify(T bundle);

    public void Add(string name, params string[] files)
    {
        T bundle = Create(name, ResolveSourceUrls(files));

        if (!_isDevelopment)
        {
            Minify(bundle);
            UpdateHashes(bundle);
            WatchBundle(bundle);
        }

        _bundles[name] = bundle;
    }

    void WatchBundle(T bundle)
    {
        List<IChangeToken> tokens = [.. bundle.SourceFiles
            .Select(f => _webRootFileProvider.Watch(f.TrimStart('/')))];
        CompositeChangeToken composite = new(tokens);
        composite.RegisterChangeCallback(_ => RebuildBundle(bundle), null);
    }

    void RebuildBundle(T bundle)
    {
        WatchBundle(bundle);
        try
        {
            lock (_rebuildLock)
            {
                Minify(bundle);
                UpdateHashes(bundle);
            }
        }
        catch (IOException)
        {
            // Source file may be mid-write; bundle retains previous content until the next change.
        }
    }

    public string GetUrl(string name)
    {
        string baseUrl = $"{bundleDirectory}{name}.min.{Extension}";
        return _bundles.TryGetValue(name, out T? bundle) && bundle.Version.Length > 0
            ? $"{baseUrl}?v={bundle.Version}"
            : baseUrl;
    }

    protected virtual string TransformFileContent(string content, string sourceUrl) => content;

    protected string ReadSourceFile(string bundleName, string sourceUrl)
    {
        string path = Path.Combine(WebRootPath, sourceUrl.TrimStart('/'));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Bundle '{bundleName}': source file '{sourceUrl}' not found.", path);
        }
        return TransformFileContent(File.ReadAllText(path), sourceUrl);
    }

    protected string ReadSourceFiles(Bundle bundle) =>
        string.Join(ConcatenationToken, bundle.SourceFiles.Select(f => ReadSourceFile(bundle.Name, f)));

    List<string> ResolveSourceUrls(string[] patterns)
    {
        List<string> result = [];
        foreach (string pattern in patterns)
        {
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                result.Add(pattern);
                continue;
            }

            Matcher matcher = new();
            matcher.AddInclude(pattern.TrimStart('/'));
            PatternMatchingResult matchResult = matcher.Execute(
                new DirectoryInfoWrapper(new DirectoryInfo(WebRootPath)));
            result.AddRange(matchResult.Files
                .OrderBy(f => f.Path)
                .Select(f => "/" + f.Path.Replace('\\', '/'))
                .Where(f => !f.EndsWith(".map", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains(".min.", StringComparison.OrdinalIgnoreCase)));
        }
        return result;
    }

    static void UpdateHashes(Bundle bundle)
    {
        if (bundle.MinifiedContent.Length == 0)
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(bundle.MinifiedContent);
        bundle.Version = Convert.ToHexString(SHA256.HashData(bytes))[..8].ToLowerInvariant();
        bundle.Integrity = "sha384-" + Convert.ToBase64String(SHA384.HashData(bytes));
    }

    public List<string> GetSourceUrls(string name) =>
        _bundles.TryGetValue(name, out T? bundle) ? bundle.SourceFiles : [];

    public string GetIntegrity(string name) =>
        _bundles.TryGetValue(name, out T? bundle) ? bundle.Integrity : string.Empty;

    public IFileInfo GetFileInfo(string subpath)
    {
        if (!subpath.StartsWith(bundleDirectory, StringComparison.Ordinal))
        {
            return new NotFoundFileInfo(subpath);
        }

        // Requests are "{name}.min.{ext}" (the bundle) or "{name}.min.{ext}.map" (its source map).
        // Strip the known suffix rather than splitting on '.', so names with dots (e.g. "jquery.ui")
        // resolve correctly.
        string fileName = subpath[bundleDirectory.Length..];
        string bundleSuffix = $".min.{Extension}";
        string mapSuffix = $"{bundleSuffix}.map";

        bool isMap = fileName.EndsWith(mapSuffix, StringComparison.Ordinal);
        string? name = isMap
            ? fileName[..^mapSuffix.Length]
            : fileName.EndsWith(bundleSuffix, StringComparison.Ordinal) ? fileName[..^bundleSuffix.Length] : null;

        if (name is null || !_bundles.TryGetValue(name, out T? bundle))
        {
            return new NotFoundFileInfo(subpath);
        }

        if (isMap)
        {
            return bundle.SourceMap.Length > 0
                ? new BundleFileInfo(bundle.Name, bundle.SourceMap, bundle.LastModified)
                : new NotFoundFileInfo(subpath);
        }

        return new BundleFileInfo(bundle.Name, bundle.MinifiedContent, bundle.LastModified);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) =>
        NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) =>
        NullChangeToken.Singleton;

    sealed class BundleFileInfo(string name, string content, DateTimeOffset lastModified) : IFileInfo
    {
        public bool Exists => true;
        public bool IsDirectory => false;
        public string Name => name;
        public string? PhysicalPath => null;
        public DateTimeOffset LastModified => lastModified;
        public long Length => Encoding.UTF8.GetByteCount(content);
        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
