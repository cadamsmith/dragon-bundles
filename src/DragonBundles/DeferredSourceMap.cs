using NUglify.JavaScript;
using NUglify.JavaScript.Syntax;

namespace DragonBundles;

/// <summary>
/// Wraps NUglify's <see cref="V3SourceMap"/> so multiple source files can be minified into one
/// bundle sharing a single map. <see cref="EndFile"/> is deliberately a no-op: otherwise each
/// <c>Uglify.Js</c> call would emit its own <c>//# sourceMappingURL</c> comment mid-bundle,
/// which corrupts the combined output's line offsets. The comment is appended once by the caller
/// after the whole bundle is assembled.
/// </summary>
sealed class DeferredSourceMap(V3SourceMap inner) : ISourceMap
{
    public string Name => inner.Name;
    public string SourceRoot { get => inner.SourceRoot; set => inner.SourceRoot = value; }
    public bool SafeHeader { get => inner.SafeHeader; set => inner.SafeHeader = value; }

    public void StartPackage(string sourcePath, string mapPath) => inner.StartPackage(sourcePath, mapPath);
    public void EndPackage() => inner.EndPackage();

    public object StartSymbol(AstNode node, int startLine, int startColumn) =>
        inner.StartSymbol(node, startLine, startColumn);

    public void MarkSegment(AstNode node, int startLine, int startColumn, string name, SourceContext context) =>
        inner.MarkSegment(node, startLine, startColumn, name, context);

    public void EndSymbol(object symbol, int endLine, int endColumn, string parentContext) =>
        inner.EndSymbol(symbol, endLine, endColumn, parentContext);

    public void EndOutputRun(int lineNumber, int columnPosition) =>
        inner.EndOutputRun(lineNumber, columnPosition);

    public void NewLineInsertedInOutput() => inner.NewLineInsertedInOutput();

    // Deferred: suppress the per-file sourceMappingURL comment so combined offsets stay correct.
    public void EndFile(TextWriter writer, string newLine) { }

    public void Dispose() => inner.Dispose();
}
