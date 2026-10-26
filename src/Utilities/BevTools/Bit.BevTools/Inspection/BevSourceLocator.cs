using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

[assembly: MetadataUpdateHandler(typeof(Bit.BevTools.BevSourceLocator))]

namespace Bit.BevTools;

/// <summary>Where code is written: a file of the build, a line and a column, from the assembly's portable PDB.</summary>
/// <param name="Path">The file as the build saw it: an absolute path on the machine that built the assembly.</param>
/// <param name="Line">One-based line.</param>
/// <param name="Column">One-based column.</param>
/// <param name="Url">The configured editor link for it (<see cref="BevToolsUiOptions.EditorUrlTemplate"/>), when one is configured.</param>
public sealed record BevSourceLocation(string Path, int Line, int Column, string? Url)
{
    /// <summary>
    /// The assembly was edited with hot reload since it was built: its symbols are the build's, so this line may have
    /// moved (the file is still the right one).
    /// </summary>
    public bool MayHaveMoved { get; init; }

    /// <summary>The file name, whichever platform built it: WebAssembly reads Windows paths from a PDB built on Windows.</summary>
    public string FileName => Path[(Path.LastIndexOfAny(['/', '\\']) + 1)..];

    public override string ToString() => Path + ":" + Line;
}

/// <summary>
/// Reads where components and methods are written from portable PDBs: the Razor compiler's <c>#line</c> directives make
/// the <c>.razor</c> file the document of <c>BuildRenderTree</c>, and a code-behind's methods point at the <c>.cs</c>.
/// Only what the symbols of this very build say is reported: a PDB beside the assembly must carry the id the assembly
/// was built with, an embedded one is the assembly's own, and on WebAssembly the PDB is the one the runtime's own
/// resource list names for this build. Without symbols (Release builds, PDBs not deployed) nothing is guessed.
/// </summary>
internal static partial class BevSourceLocator
{
    private sealed class Symbols(MetadataReaderProvider provider)
    {
        public MetadataReader Reader { get; } = provider.GetMetadataReader();

        // Kept with the reader: the reader reads from the provider's memory.
        public MetadataReaderProvider Provider { get; } = provider;
    }

    private static readonly ConditionalWeakTable<Assembly, Symbols?> Loaded = new();
    private static readonly ConcurrentDictionary<string, string> Unavailable = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Assembly, byte> Edited = new();
    private static readonly object Lock = new();

    /// <summary>
    /// Hot reload: the edited assemblies' symbols stay the build's, so their locations are marked as possibly moved.
    /// Called by the runtime through <see cref="MetadataUpdateHandlerAttribute"/>.
    /// </summary>
    internal static void ClearCache(Type[]? updatedTypes)
    {
        foreach (var assembly in updatedTypes is null ? AppDomain.CurrentDomain.GetAssemblies() : updatedTypes.Select(t => t.Assembly).Distinct())
        {
            Edited.TryAdd(assembly, 0);
        }
    }

    /// <summary>
    /// True when this assembly's symbols are not on disk and have to be handed over (<see cref="Provide"/>): WebAssembly,
    /// where the runtime lists the PDBs it serves. Elsewhere an assembly without a file (single-file publish, loaded
    /// from memory) has nothing to download.
    /// </summary>
    public static bool NeedsDownload(Assembly assembly) =>
        OperatingSystem.IsBrowser() && string.IsNullOrEmpty(SafeLocation(assembly)) && !assembly.IsDynamic && !Loaded.TryGetValue(assembly, out _) && !Unavailable.ContainsKey(assembly.FullName ?? "");

    /// <summary>Why this assembly has no symbols, when it is known not to have any.</summary>
    public static string? WhyUnavailable(Assembly assembly) => Unavailable.TryGetValue(assembly.FullName ?? "", out var why) ? why : null;

    /// <summary>
    /// Hands over the PDB the app serves for an assembly that has no file of its own (WebAssembly), fetched with the
    /// integrity hash the runtime's resource list gives for this build; null when the app lists none.
    /// </summary>
    public static void Provide(Assembly assembly, byte[]? pdb)
    {
        lock (Lock)
        {
            if (Loaded.TryGetValue(assembly, out _))
            {
                return;
            }

            if (pdb is not { Length: > 0 })
            {
                Unavailable[assembly.FullName ?? ""] = "The app serves no symbols (PDB) for " + assembly.GetName().Name + ": a Release build, or one published without them.";
                return;
            }

            try
            {
                Loaded.Add(assembly, new Symbols(MetadataReaderProvider.FromPortablePdbImage(System.Collections.Immutable.ImmutableArray.Create(pdb))));
            }
            catch (Exception ex)
            {
                Unavailable[assembly.FullName ?? ""] = "The symbols of " + assembly.GetName().Name + " could not be read: " + ex.GetType().Name + ".";
            }
        }
    }

    /// <summary>Where a method's body is written: its first sequence point (an async method's, in its state machine).</summary>
    public static BevSourceLocation? Of(MethodBase method, string? editorUrlTemplate)
    {
        if (SymbolsOf(method.Module.Assembly) is not { } symbols)
        {
            return null;
        }

        var body = StateMachineBody(method) ?? method;
        return FirstPoint(symbols.Reader, body) is { } point ? Location(point, editorUrlTemplate) with { MayHaveMoved = Edited.ContainsKey(method.Module.Assembly) } : null;
    }

    /// <summary>
    /// The statement at an IL offset of a method: the last visible sequence point at or before it. What a stack frame
    /// says when its text carries no "in file:line" - the runtime had no symbols loaded - but its method and offset are
    /// known. Null without symbols or without a sequence point there.
    /// </summary>
    public static BevSourceLocation? At(MethodBase method, int ilOffset, string? editorUrlTemplate)
    {
        if (ilOffset < 0 || SymbolsOf(method.Module.Assembly) is not { } symbols)
        {
            return null;
        }

        try
        {
            var reader = symbols.Reader;
            var definition = MetadataTokens.MethodDefinitionHandle(method.MetadataToken & 0x00FFFFFF);
            (string Path, int Line, int Column)? found = null;
            foreach (var point in reader.GetMethodDebugInformation(definition.ToDebugInformationHandle()).GetSequencePoints())
            {
                if (point.Offset > ilOffset)
                {
                    break;
                }

                if (!point.IsHidden && !point.Document.IsNil)
                {
                    found = (reader.GetString(reader.GetDocument(point.Document).Name), point.StartLine, point.StartColumn);
                }
            }

            return found is { } at ? Location(at, editorUrlTemplate) with { MayHaveMoved = Edited.ContainsKey(method.Module.Assembly) } : null;
        }
        catch
        {
            // A method the symbols do not describe (generated, or a token past their tables): no location.
            return null;
        }
    }

    /// <summary>
    /// The files a component type is written in, its markup first: the <c>.razor</c> file of <c>BuildRenderTree</c>, then
    /// a code-behind <c>.cs</c> when its methods are written elsewhere; each with the first line written there.
    /// </summary>
    public static IReadOnlyList<BevSourceLocation> Of(Type type, string? editorUrlTemplate)
    {
        if (SymbolsOf(type.Assembly) is not { } symbols)
        {
            return [];
        }

        var first = new Dictionary<string, (int Line, int Column)>(StringComparer.OrdinalIgnoreCase);
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Concat<MethodBase>(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)))
        {
            if (FirstPoint(symbols.Reader, StateMachineBody(method) ?? method) is not { } point)
            {
                continue;
            }

            if (!first.TryGetValue(point.Path, out var known) || point.Line < known.Line)
            {
                first[point.Path] = (point.Line, point.Column);
            }
        }

        // A .razor file is the component, so it opens at its top; its first sequence point is merely the first C#
        // expression in its markup. A code-behind file opens where the type's first member is written.
        return first
            .OrderBy(p => IsRazor(p.Key) ? 0 : 1)
            .ThenBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(p => Location(IsRazor(p.Key) ? (p.Key, 1, 1) : (p.Key, p.Value.Line, p.Value.Column), editorUrlTemplate) with { MayHaveMoved = Edited.ContainsKey(type.Assembly) })
            .ToArray();
    }

    private static bool IsRazor(string path) => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);

    /// <summary>The configured editor link: {path}, {line} and {column} filled in, the path with forward slashes and escaped.</summary>
    public static string? EditorUrl(string? template, string path, int line, int column)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var escaped = string.Join("/", path.Replace('\\', '/').Split('/').Select(segment => Uri.EscapeDataString(segment).Replace("%3A", ":", StringComparison.Ordinal)));
        var url = template.Replace("{path}", escaped, StringComparison.Ordinal)
            .Replace("{line}", line.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{column}", column.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

        // A link the panel renders: never one that runs script, whatever the configuration says. Judged on the final
        // address, with the browser's leniency in mind: it starts with a clean scheme ("java\tscript:" or a leading
        // control character is not one) that is not a script scheme - the same rule the companion applies.
        return SchemeRegex().Match(url) is { Success: true } scheme && scheme.Groups[1].Value.ToLowerInvariant() is not ("javascript" or "data" or "vbscript") ? url : null;
    }

    [GeneratedRegex("^([a-zA-Z][a-zA-Z0-9+.-]*):", RegexOptions.CultureInvariant)]
    private static partial Regex SchemeRegex();

    private static BevSourceLocation Location((string Path, int Line, int Column) point, string? template) =>
        new(point.Path, point.Line, point.Column, EditorUrl(template, point.Path, point.Line, point.Column));

    private static MethodBase? StateMachineBody(MethodBase method)
    {
        var stateMachine = method.GetCustomAttribute<StateMachineAttribute>(inherit: false)?.StateMachineType;
        return stateMachine?.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    }

    private static (string Path, int Line, int Column)? FirstPoint(MetadataReader reader, MethodBase method)
    {
        try
        {
            var definition = MetadataTokens.MethodDefinitionHandle(method.MetadataToken & 0x00FFFFFF);
            var information = reader.GetMethodDebugInformation(definition.ToDebugInformationHandle());
            foreach (var point in information.GetSequencePoints())
            {
                if (point.IsHidden || point.Document.IsNil)
                {
                    continue;
                }

                return (reader.GetString(reader.GetDocument(point.Document).Name), point.StartLine, point.StartColumn);
            }
        }
        catch
        {
            // A method the symbols do not describe (generated, or a token past their tables): no location.
        }

        return null;
    }

    private static string SafeLocation(Assembly assembly)
    {
        try
        {
            return assembly.IsDynamic ? "" : assembly.Location;
        }
        catch
        {
            return "";
        }
    }

    private static Symbols? SymbolsOf(Assembly assembly)
    {
        if (Loaded.TryGetValue(assembly, out var known))
        {
            return known;
        }

        var location = SafeLocation(assembly);
        if (string.IsNullOrEmpty(location))
        {
            // WebAssembly: nothing on disk; the panel downloads the PDB the runtime lists and hands it over. Elsewhere
            // the assembly was loaded without a file, and there is nothing to read.
            if (!OperatingSystem.IsBrowser() && !assembly.IsDynamic)
            {
                Unavailable.TryAdd(assembly.FullName ?? "", assembly.GetName().Name + " has no file on disk (a single-file publish, or loaded from memory), so its symbols cannot be read.");
            }

            return null;
        }

        lock (Lock)
        {
            if (Loaded.TryGetValue(assembly, out known))
            {
                return known;
            }

            known = ReadFromDisk(assembly, location, out var why);
            Loaded.AddOrUpdate(assembly, known);
            if (known is null)
            {
                Unavailable[assembly.FullName ?? ""] = why ?? "No symbols.";
            }

            return known;
        }
    }

    /// <summary>Whether a PDB carries the id the assembly was built with (its CodeView entry): a PDB of another build puts every line elsewhere.</summary>
    internal static bool IsBuildOf(MetadataReaderProvider provider, Guid expected) =>
        provider.GetMetadataReader().DebugMetadataHeader is { } header && new BlobContentId(header.Id).Guid == expected;

    /// <summary>Forgets hot reload edits (tests).</summary>
    internal static void ForgetEdits() => Edited.Clear();

    /// <summary>The embedded PDB, or the one beside the assembly or at the path the build recorded - only when its id is the assembly's.</summary>
    private static Symbols? ReadFromDisk(Assembly assembly, string location, out string? why)
    {
        why = null;
        try
        {
            using var stream = File.OpenRead(location);
            using var pe = new PEReader(stream);
            var entries = pe.ReadDebugDirectory();
            if (entries.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb) is { DataSize: > 0 } embedded)
            {
                return new Symbols(pe.ReadEmbeddedPortablePdbDebugDirectoryData(embedded));
            }

            if (entries.FirstOrDefault(e => e.Type == DebugDirectoryEntryType.CodeView) is not { DataSize: > 0 } codeView)
            {
                why = assembly.GetName().Name + " was built without symbols.";
                return null;
            }

            var expected = pe.ReadCodeViewDebugDirectoryData(codeView);
            string? unreadable = null;
            foreach (var candidate in new[] { Path.ChangeExtension(location, ".pdb"), expected.Path }.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(candidate) || !File.Exists(candidate))
                {
                    continue;
                }

                MetadataReaderProvider? provider = null;
                try
                {
                    provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(candidate));
                    if (IsBuildOf(provider, expected.Guid))
                    {
                        var symbols = new Symbols(provider);
                        provider = null;
                        return symbols;
                    }

                    // A PDB of another build of the same assembly would put every location on the wrong line.
                }
                catch (Exception ex)
                {
                    // Not a portable PDB, or a damaged one: the next candidate may still be right.
                    unreadable = ex.GetType().Name;
                }
                finally
                {
                    provider?.Dispose();
                }
            }

            why = unreadable is null
                ? "No symbols matching this build of " + assembly.GetName().Name + " were found beside it."
                : "The symbols beside " + assembly.GetName().Name + " could not be read (" + unreadable + "), and none other matching this build were found.";
            return null;
        }
        catch (Exception ex)
        {
            why = "The symbols of " + assembly.GetName().Name + " could not be read: " + ex.GetType().Name + ".";
            return null;
        }
    }
}
