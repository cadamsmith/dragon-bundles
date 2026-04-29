namespace DragonBundles;

abstract class BundleProvider<T>(IWebHostEnvironment env, string bundleDirectory)
    : IFileProvider where T : Bundle
{
    readonly Dictionary<string, T> _bundles = [];
    readonly bool _isDevelopment = env.IsEnvironment(Environments.Development);
    readonly IFileProvider _webRootFileProvider = env.WebRootFileProvider;
    protected readonly string WebRootPath = env.WebRootPath;

    readonly Lock _rebuildLock = new();

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
            if (bundle.MinifiedContent.Length > 0)
            {
                bundle.Version = ComputeVersion(bundle.MinifiedContent);
            }
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
                if (bundle.MinifiedContent.Length > 0)
                {
                    bundle.Version = ComputeVersion(bundle.MinifiedContent);
                }
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

    static string ComputeVersion(string content)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    public List<string> GetSourceUrls(string name) =>
        _bundles.TryGetValue(name, out T? bundle) ? bundle.SourceFiles : [];

    public IFileInfo GetFileInfo(string subpath)
    {
        if (!subpath.StartsWith(bundleDirectory, StringComparison.Ordinal))
        {
            return new NotFoundFileInfo(subpath);
        }

        string name = subpath[bundleDirectory.Length..].Split('.')[0];
        return _bundles.TryGetValue(name, out T? bundle)
            ? new BundleFileInfo(bundle)
            : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) =>
        NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) =>
        NullChangeToken.Singleton;

    sealed class BundleFileInfo(T bundle) : IFileInfo
    {
        public bool Exists => true;
        public bool IsDirectory => false;
        public string Name => bundle.Name;
        public string? PhysicalPath => null;
        public DateTimeOffset LastModified => bundle.LastModified;
        public long Length => Encoding.UTF8.GetByteCount(bundle.MinifiedContent);
        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(bundle.MinifiedContent));
    }
}
