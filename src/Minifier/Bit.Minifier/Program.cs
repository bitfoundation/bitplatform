using Bit.Minifier;

// usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive] [<assembly name>...]
// Without assembly names, every managed assembly in the directory is minified.
if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive] [<assembly name>...]");
    return 2;
}

string? map = null;
bool keepNullable = false, aggressive = false;
List<string> names = [];
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--map": map = args[++i]; break;
        case "--keep-nullable": keepNullable = true; break;
        case "--aggressive": aggressive = true; break;
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
catch (MinifierException e)
{
    // MSBuild's canonical format, so Exec reports it as a warning rather than plain output
    Console.WriteLine($"Bit.Minifier : warning BITMIN001: {e.Message} The assemblies were left unminified.");
    return 0;
}
