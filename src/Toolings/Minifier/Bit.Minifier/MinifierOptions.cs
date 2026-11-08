using System.IO.Enumeration;

namespace Bit.Minifier;

internal sealed class MinifierOptions
{
    /// <summary>
    /// The bit libraries: packages an app installs, wherever this repository builds them from source. Patterns
    /// (see <see cref="Matches"/>), so a new package is one without anything to add here: every Bit.* assembly
    /// but the ones this repository publishes as an app's own code - its demos, samples, tests and websites.
    /// </summary>
    public static readonly IReadOnlyList<string> BitAssemblies =
    [
        "Bit.*",
        "!*.Demo", "!*.Demo.*", "!*Sample*", "!*.Tests", "!*.Tests.*", "!*.TestKit", "!*.Benchmarks", "!Bit.Websites.*"
    ];

    /// <summary>
    /// Whether <paramref name="name"/> is one of <paramref name="patterns"/>, ignoring case. A pattern is an assembly
    /// name that may have * for any run of characters and ? for any one; one that starts with ! takes back what the
    /// others match, whatever their order.
    /// </summary>
    public static bool Matches(IReadOnlyList<string> patterns, string name)
    {
        var included = false;
        foreach (var pattern in patterns)
        {
            if (pattern.StartsWith('!'))
            {
                if (FileSystemName.MatchesSimpleExpression(pattern.AsSpan(1), name)) return false;
            }
            else
            {
                included = included || FileSystemName.MatchesSimpleExpression(pattern, name);
            }
        }
        return included;
    }

    /// <summary>The folder holding the trimmed assemblies (ILLink's output) and everything they reference.</summary>
    public required string Directory { get; init; }

    /// <summary>Simple names of the assemblies to minify; null minifies every managed assembly in <see cref="Directory"/>.</summary>
    public IReadOnlyList<string>? Assemblies { get; init; }

    /// <summary>
    /// Libraries an app consumes rather than writes, whatever <see cref="OwnAssemblies"/> says: built from
    /// their own source tree they are still the packages this app installs, so they are minified like any
    /// other library. Patterns, as <see cref="Matches"/> reads them.
    /// </summary>
    public IReadOnlyList<string> FullyMinified { get; init; } = BitAssemblies;

    /// <summary>
    /// The app's own assemblies: the ones its developer wrote, which the publish knows by their being project
    /// references rather than packages. Their names never change, whatever the level, so an exception they log
    /// names the types and methods the source has and points at the code that has to be read. Their attributes
    /// still go, and the references they hold into renamed assemblies still follow.
    /// </summary>
    public IReadOnlyList<string> OwnAssemblies { get; init; } = [];

    /// <summary>Where to write the old-name -> new-name map, for reading minified stack traces.</summary>
    public string? MapFile { get; init; }

    /// <summary>
    /// Keeps the nullable metadata, which only NullabilityInfoContext reads at runtime. The publish says so
    /// through NullabilityInfoContextSupport, the switch .NET already has for it.
    /// </summary>
    public bool KeepNullable { get; init; }

    /// <summary>
    /// Public names go too - public types and their namespaces, non-virtual methods and property accessors,
    /// fields, parameter and generic parameter names - in every assembly that only minified assemblies of the
    /// folder reference. Only for apps nothing reaches by a public name from outside the client, or through
    /// reflection over names no string literal mentions. Without it, every non-public name still goes.
    /// </summary>
    public bool Aggressive { get; init; }
}

internal sealed record MinifiedAssembly(string Name, long OriginalSize, long MinifiedSize, int RemovedAttributes, int RenamedMembers);

internal sealed class MinifierException(string message, bool folderUntouched = true) : Exception(message)
{
    /// <summary>Whether the folder is still exactly as ILLink left it.</summary>
    public bool FolderUntouched { get; } = folderUntouched;
}
