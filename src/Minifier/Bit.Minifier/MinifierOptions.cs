namespace Bit.Minifier;

internal sealed class MinifierOptions
{
    /// <summary>The bit libraries: known to be safe for every rule, so they get all of them.</summary>
    public static readonly IReadOnlyList<string> BitAssemblies = ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.Butil", "Bit.Bswup", "Bit.Brouter"];

    /// <summary>The folder holding the trimmed assemblies (ILLink's output) and everything they reference.</summary>
    public required string Directory { get; init; }

    /// <summary>Simple names of the assemblies to minify; null minifies every managed assembly in <see cref="Directory"/>.</summary>
    public IReadOnlyList<string>? Assemblies { get; init; }

    /// <summary>
    /// Assemblies that also get the rules other libraries may object to: renamed backing fields and no
    /// [CompilerGenerated] on fields. The rest keep both, since EF Core finds backing fields by name and
    /// Newtonsoft.Json skips fields by that attribute.
    /// </summary>
    public IReadOnlyList<string> FullyMinified { get; init; } = BitAssemblies;

    /// <summary>Where to write the old-name -> new-name map, for reading minified stack traces.</summary>
    public string? MapFile { get; init; }

    /// <summary>
    /// Keeps the nullable metadata, which only NullabilityInfoContext (and EF Core through it) reads at runtime.
    /// Kept in every assembly but the fully minified ones anyway when EF Core is part of the app.
    /// </summary>
    public bool KeepNullable { get; init; }

    /// <summary>
    /// Everything that can go, goes (see <see cref="AggressiveMinifier"/>). Implies the full treatment for
    /// every assembly, whatever <see cref="FullyMinified"/> says - unless EF Core is part of the app, which may
    /// map the types of any of them.
    /// </summary>
    public bool Aggressive { get => aggressive || SuperAggressive; init => aggressive = value; }

    /// <summary>
    /// Experimental: <see cref="Aggressive"/>, and public names go too - public types and their namespaces,
    /// non-virtual methods and property accessors, fields, parameter and generic parameter names - in every
    /// assembly that only minified assemblies of the folder reference. Only for apps nothing reaches by a public
    /// name from outside the client, or through reflection over names no string literal mentions.
    /// </summary>
    public bool SuperAggressive { get; init; }

    private readonly bool aggressive;
}

internal sealed record MinifiedAssembly(string Name, long OriginalSize, long MinifiedSize, int RemovedAttributes, int RenamedMembers);

internal sealed class MinifierException(string message, bool folderUntouched = true) : Exception(message)
{
    /// <summary>Whether the folder is still exactly as ILLink left it.</summary>
    public bool FolderUntouched { get; } = folderUntouched;
}
