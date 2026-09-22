using Bit.Minifier;

// usage: Bit.Minifier <directory> [--map <file>] [--own <a;b>] [--keep-nullable] [--aggressive] [<assembly name>...]
//        Bit.Minifier --decode <map file> [<stack trace file>]
// Without assembly names, every managed assembly in the directory is minified. --own names the app's own
// assemblies, which keep their names. --aggressive renames public names too. --decode reads a stack trace (from
// the file, or from standard input) and writes it back with the names the source has.
const string Usage = """
    usage: Bit.Minifier <directory> [--map <file>] [--own <a;b>] [--keep-nullable] [--aggressive] [<assembly name>...]
           Bit.Minifier --decode <map file> [<stack trace file>]
    """;

if (args is ["--decode", ..]) return Decode.Run(Usage, args);

if (args.Length < 1 || args[0].StartsWith("--", StringComparison.Ordinal))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

string? map = null;
bool keepNullable = false, aggressive = false;
List<string> names = [], own = [];
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--map" when i + 1 < args.Length && args[i + 1].StartsWith("--", StringComparison.Ordinal) is false: map = args[++i]; break;
        // semicolon-separated, the way MSBuild writes an item list
        case "--own" when i + 1 < args.Length && args[i + 1].StartsWith("--", StringComparison.Ordinal) is false:
            own.AddRange(args[++i].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            break;
        case "--keep-nullable": keepNullable = true; break;
        case "--aggressive": aggressive = true; break;
        // a mistyped switch would otherwise be taken for an assembly name, and nothing would be minified
        case var unknown when unknown.StartsWith("--", StringComparison.Ordinal):
            Console.Error.WriteLine(Usage);
            return 2;
        default: names.Add(args[i]); break;
    }
}

var options = new MinifierOptions
{
    Directory = Path.GetFullPath(args[0]),
    Assemblies = names.Count > 0 ? names : null,
    OwnAssemblies = own,
    MapFile = map,
    KeepNullable = keepNullable,
    Aggressive = aggressive,
};

try
{
    var started = DateTime.UtcNow;
    var minifier = new AssemblyMinifier(options);
    var results = minifier.Run();
    if (results.Count > 0) Report.Write(results, DateTime.UtcNow - started, options.Aggressive);
    // an assembly minified around rather than minified is worth a warning of its own: the publish is fine, and a
    // release that quietly stopped shrinking what it used to shrink is what nobody would otherwise notice
    if (minifier.Skipped.Count > 0)
    {
        Console.WriteLine($"Bit.Minifier : warning BITMIN001: left unminified: {string.Join("; ", minifier.Skipped).ReplaceLineEndings(" ")}");
    }
    return 0;
}
catch (Exception e)
{
    // MSBuild's canonical format, so Exec reports it as a warning rather than plain output. Nothing reaches the
    // folder before every assembly is written, and a failure while replacing them puts the originals back.
    var message = e is MinifierException ? e.Message : $"{e.GetType().Name}: {e.Message}";
    if (e is not MinifierException { FolderUntouched: false }) message += " The assemblies were left unminified.";
    Console.WriteLine($"Bit.Minifier : warning BITMIN001: {message.ReplaceLineEndings(" ")}");
    if (e is not MinifierException) Console.Error.WriteLine(e);
    return 0;
}
