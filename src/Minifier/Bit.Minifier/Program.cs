using Bit.Minifier;

// usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive | --super-aggressive] [<assembly name>...]
//        Bit.Minifier --decode <map file> [<stack trace file>]
// Without assembly names, every managed assembly in the directory is minified. --decode reads a stack trace (from
// the file, or from standard input) and writes it back with the names the source has.
const string Usage = """
    usage: Bit.Minifier <directory> [--map <file>] [--keep-nullable] [--aggressive | --super-aggressive] [<assembly name>...]
           Bit.Minifier --decode <map file> [<stack trace file>]
    """;

if (args is ["--decode", var mapFile, ..])
{
    if (args.Length > 3)
    {
        Console.Error.WriteLine(Usage);
        return 2;
    }
    if (File.Exists(mapFile) is false)
    {
        Console.Error.WriteLine($"Bit.Minifier: no map at {Path.GetFullPath(mapFile)}. It is written next to the assemblies a publish minifies, as obj/<configuration>/<tfm>/bit-minifier.map, and only kept until the next clean.");
        return 2;
    }
    if (args.Length == 3 && File.Exists(args[2]) is false)
    {
        Console.Error.WriteLine($"Bit.Minifier: no stack trace at {Path.GetFullPath(args[2])}.");
        return 2;
    }
    var trace = args.Length == 3 ? File.ReadAllText(args[2]) : Console.In.ReadToEnd();
    // the trace is written back the way it came in, with whatever ends its lines
    var decoded = MapDecoder.Load(mapFile).Decode(trace);
    Console.Out.Write(decoded);
    if (decoded.EndsWith('\n') is false) Console.Out.WriteLine();
    return 0;
}

if (args.Length < 1 || args[0].StartsWith("--", StringComparison.Ordinal))
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
