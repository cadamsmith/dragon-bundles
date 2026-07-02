using System.Diagnostics;
using Microsoft.Ajax.Utilities;
using NUglify;

// Compares NUglify (DragonBundles) vs WebGrease's minifier (System.Web.Optimization's engine)
// on a corpus of classic and modern CSS/JS. Reports size reduction and — the headline — which
// engine successfully parses modern (ES2020+ / modern CSS) syntax.

const int timingRuns = 50;
string corpus = Path.Combine(AppContext.BaseDirectory, "corpus");

static string Cell(Result r)
{
    if (!r.Ok)
    {
        string reason = r.Errors < 0 ? "threw" : $"{r.Errors} err";
        return $"FAILED ({reason})";
    }
    double reduction = r.Original == 0 ? 0 : 100.0 * (r.Original - r.Size) / r.Original;
    return $"{r.Size,6} B  -{reduction,4:0.0}%  {r.Ms,6:0.00}ms";
}

Console.WriteLine();
Console.WriteLine("NUglify (DragonBundles)  vs  WebGrease (System.Web.Optimization)");
Console.WriteLine(new string('=', 78));
Console.WriteLine($"{"file",-14}{"orig",7}   {"WebGrease",-26}{"NUglify",-26}");
Console.WriteLine(new string('-', 78));

foreach (string path in Directory.GetFiles(corpus).OrderBy(p => p))
{
    string name = Path.GetFileName(path);
    bool isCss = name.EndsWith(".css", StringComparison.OrdinalIgnoreCase);
    string source = File.ReadAllText(path);

    Result wg = RunWebGrease(source, isCss);
    Result nu = RunNUglify(source, isCss);

    Console.WriteLine($"{name,-14}{source.Length,7}   {Cell(wg),-26}{Cell(nu),-26}");
}

Console.WriteLine(new string('-', 78));
Console.WriteLine("size = minified bytes; -% = reduction vs original; ms = mean over " + timingRuns + " runs");
Console.WriteLine("FAILED = engine could not parse the input (errors or exception).");
Console.WriteLine();
return;

Result RunWebGrease(string source, bool isCss)
{
    Minifier minifier = new();
    Stopwatch sw = Stopwatch.StartNew();
    string output;
    bool threw = false;
    try
    {
        output = isCss ? minifier.MinifyStyleSheet(source) : minifier.MinifyJavaScript(source);
    }
    catch
    {
        threw = true;
        output = string.Empty;
    }
    for (int i = 0; i < timingRuns && !threw; i++)
    {
        _ = isCss ? new Minifier().MinifyStyleSheet(source) : new Minifier().MinifyJavaScript(source);
    }
    sw.Stop();

    int errors = minifier.ErrorList.Count;
    bool ok = !threw && errors == 0 && !string.IsNullOrEmpty(output);
    return new Result(source.Length, output?.Length ?? 0, ok, threw ? -1 : errors, sw.Elapsed.TotalMilliseconds / timingRuns);
}

Result RunNUglify(string source, bool isCss)
{
    Stopwatch sw = Stopwatch.StartNew();
    UglifyResult result = isCss ? Uglify.Css(source) : Uglify.Js(source);
    for (int i = 0; i < timingRuns; i++)
    {
        _ = isCss ? Uglify.Css(source) : Uglify.Js(source);
    }
    sw.Stop();

    int errors = result.Errors?.Count ?? 0;
    bool ok = !result.HasErrors && !string.IsNullOrEmpty(result.Code);
    return new Result(source.Length, result.Code?.Length ?? 0, ok, errors, sw.Elapsed.TotalMilliseconds / timingRuns);
}

sealed record Result(int Original, int Size, bool Ok, int Errors, double Ms);
