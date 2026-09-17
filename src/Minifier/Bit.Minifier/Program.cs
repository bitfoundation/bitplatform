using Bit.Minifier;

// usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive | --super-aggressive] [<assembly name>...]
// Without assembly names, every managed assembly in the directory is minified.
const string Usage = "usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive | --super-aggressive] [<assembly name>...]";
if (args.Length < 1)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

string? map = null;
bool keepNullable = false, aggressive = false, superAggressive = false;
List<string> names = [];
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--map" when i + 1 < args.Length && args[i + 1].StartsWith("--", StringComparison.Ordinal) is false: map = args[++i]; break;
        case "--keep-nullable": keepNullable = true; break;
        case "--aggressive": aggressive = true; break;
        case "--super-aggressive": superAggressive = true; break;
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
    MapFile = map,
    KeepNullable = keepNullable,
    Aggressive = aggressive,
    SuperAggressive = superAggressive,
};

try
{
    var started = DateTime.UtcNow;
    var results = new AssemblyMinifier(options).Run();
    foreach (var result in results)
    {
        Console.WriteLine($"Bit.Minifier: {result.Name} {result.OriginalSize:N0} -> {result.MinifiedSize:N0} bytes ({result.RemovedAttributes:N0} attributes removed, {result.RenamedMembers:N0} names shortened)");
    }
    var before = results.Sum(r => r.OriginalSize);
    var after = results.Sum(r => r.MinifiedSize);
    Console.WriteLine($"Bit.Minifier: {results.Count} assemblies, {before:N0} -> {after:N0} bytes (-{before - after:N0}) in {(DateTime.UtcNow - started).TotalSeconds:N1}s");
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
