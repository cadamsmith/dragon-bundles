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
        T bundle = Create(name, [.. files]);

        if (!_isDevelopment)
            Minify(bundle);

        _bundles[name] = bundle;
    }

    public string GetUrl(string name) =>
        $"{bundleDirectory}{name}.min.{Extension}";

    public List<string> GetSourceUrls(string name) =>
        _bundles.TryGetValue(name, out T? bundle) ? bundle.SourceFiles : [];

    public IFileInfo GetFileInfo(string subpath)
    {
        if (!subpath.StartsWith(bundleDirectory))
            return new NotFoundFileInfo(subpath);

        string name = subpath[bundleDirectory.Length..].Split('.')[0];
        return _bundles.TryGetValue(name, out T? bundle)
            ? new BundleFileInfo(bundle)
            : new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath) =>
        NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) =>
        NullChangeToken.Singleton;

    private sealed class BundleFileInfo(T bundle) : IFileInfo
    {
        static readonly Encoding _enc = Encoding.UTF8;

        public bool Exists => true;
        public bool IsDirectory => false;
        public string Name => bundle.Name;
        public string? PhysicalPath => null;
        public DateTimeOffset LastModified => bundle.LastModified;
        public long Length => _enc.GetByteCount(bundle.MinifiedContent);
        public Stream CreateReadStream() => new MemoryStream(_enc.GetBytes(bundle.MinifiedContent));
    }
}
