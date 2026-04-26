using System.Text;

namespace DragonBundles;

public abstract class BundleProvider<T>(IWebHostEnvironment env, string bundleDirectory)
    : IFileProvider where T : Bundle
{
    readonly Dictionary<string, T> _bundles = [];
    readonly bool _isDevelopment = env.IsEnvironment(Environments.Development);
    protected readonly string WebRootPath = env.WebRootPath;

    protected abstract string Extension { get; }
    protected abstract T Create(string name, List<string> sourceFiles);
    public abstract void Minify(T bundle);

    public void Add(string name, params string[] files)
    {
        T bundle = Create(name, ResolveSourceUrls(files));

        if (!_isDevelopment)
        {
            Minify(bundle);
            if (bundle.MinifiedContent.Length > 0)
                bundle.Version = ComputeVersion(bundle.MinifiedContent);
        }

        _bundles[name] = bundle;
    }

    public string GetUrl(string name)
    {
        string baseUrl = $"{bundleDirectory}{name}.min.{Extension}";
        return _bundles.TryGetValue(name, out T? bundle) && bundle.Version.Length > 0
            ? $"{baseUrl}?v={bundle.Version}"
            : baseUrl;
    }

    List<string> ResolveSourceUrls(string[] patterns)
    {
        var result = new List<string>();
        foreach (string pattern in patterns)
        {
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                result.Add(pattern);
                continue;
            }

            var matcher = new Matcher();
            matcher.AddInclude(pattern.TrimStart('/'));
            PatternMatchingResult matchResult = matcher.Execute(
                new DirectoryInfoWrapper(new DirectoryInfo(WebRootPath)));
            foreach (FilePatternMatch file in matchResult.Files.OrderBy(f => f.Path))
                result.Add("/" + file.Path.Replace('\\', '/'));
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
        if (!subpath.StartsWith(bundleDirectory))
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
        static readonly Encoding _encoding = Encoding.UTF8;

        public bool Exists => true;
        public bool IsDirectory => false;
        public string Name => bundle.Name;
        public string? PhysicalPath => null;
        public DateTimeOffset LastModified => bundle.LastModified;
        public long Length => _encoding.GetByteCount(bundle.MinifiedContent);
        public Stream CreateReadStream() => new MemoryStream(_encoding.GetBytes(bundle.MinifiedContent));
    }
}
