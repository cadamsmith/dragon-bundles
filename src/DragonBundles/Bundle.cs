namespace DragonBundles;

abstract class Bundle(string name, List<string> sourceFiles)
{
    public string Name { get; set; } = name;
    public List<string> SourceFiles { get; set; } = sourceFiles;
    public string MinifiedContent { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
