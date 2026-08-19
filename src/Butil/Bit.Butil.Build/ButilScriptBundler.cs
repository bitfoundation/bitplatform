using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Bit.Butil.Build;

/// <summary>
/// The publish-time counterpart of the C# trimming: works out which Bit.Butil JavaScript modules a trimmed
/// consumer still calls and assembles a bundle holding only those.
/// </summary>
/// <remarks>
/// The unit of trimming is a module - one <c>Scripts/*.ts</c> file, one <c>BitButil.&lt;module&gt;</c>
/// namespace - and the signal is the trimmed <c>Bit.Butil.dll</c> itself. Every JavaScript call the C# side
/// makes goes through a literal identifier <c>"BitButil.&lt;module&gt;.&lt;function&gt;"</c>, and the
/// trimmer rewrites the assembly's user-string heap to hold only the strings surviving method bodies still
/// reference. So the set of <c>BitButil.&lt;module&gt;.</c> prefixes left in that heap is exactly the set of
/// modules the app can still reach - static extension classes, internal interop helpers and services alike,
/// with nothing to annotate and nothing to keep in sync. Module-to-module dependencies (a manifest emitted by
/// Bit.Butil's own build from the TypeScript sources) close the set, and the bundle is the corresponding
/// chunks concatenated in dependency order - byte-for-byte what Bit.Butil ships as <c>bit-butil.js</c> when
/// every module is included.
/// <br/>
/// The methods are public and free of MSBuild types so the test projects can run the exact same computation
/// against a real trimmed assembly.
/// </remarks>
public static class ButilScriptBundler
{
    /// <summary>The identifier prefix every Bit.Butil interop call starts with.</summary>
    public const string IdentifierPrefix = "BitButil.";

    /// <summary>
    /// The modules whose <c>BitButil.&lt;module&gt;.</c> identifiers still exist in an assembly's user-string
    /// heap. On a trimmed assembly this is the set of modules the app can still call.
    /// </summary>
    public static SortedSet<string> ReadReferencedModules(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        var modules = new SortedSet<string>(StringComparer.Ordinal);
        // An empty #US heap is a single padding byte; the first real string sits at offset 1.
        if (metadata.GetHeapSize(HeapIndex.UserString) <= 1) return modules;

        var handle = MetadataTokens.UserStringHandle(1);
        while (handle.IsNil is false)
        {
            if (TryGetModule(metadata.GetUserString(handle), out var module)) modules.Add(module);
            handle = metadata.GetNextHandle(handle);
        }

        return modules;
    }

    /// <summary>
    /// Extracts the module from an interop identifier: <c>BitButil.clipboard.readText</c> is the
    /// <c>clipboard</c> module. False for anything that is not shaped like an interop identifier.
    /// </summary>
    public static bool TryGetModule(string identifier, out string module)
    {
        module = string.Empty;
        if (identifier is null || identifier.StartsWith(IdentifierPrefix, StringComparison.Ordinal) is false) return false;

        var start = IdentifierPrefix.Length;
        var end = identifier.IndexOf('.', start);
        if (end <= start) return false;

        for (var i = start; i < end; i++)
        {
            if (char.IsLetterOrDigit(identifier[i]) is false && identifier[i] != '_') return false;
        }

        module = identifier.Substring(start, end - start);
        return true;
    }

    /// <summary>
    /// Reads the dependency manifest Bit.Butil's build emits next to the chunks: one <c>name=dep1,dep2</c>
    /// line per module, already in dependency-first order.
    /// </summary>
    public static ButilScriptManifest ReadManifest(string manifestPath)
    {
        var order = new List<string>();
        var dependencies = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var raw in File.ReadAllLines(manifestPath))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            var separator = line.IndexOf('=');
            if (separator <= 0) throw new InvalidDataException($"Malformed line in {manifestPath}: '{line}'.");

            var name = line.Substring(0, separator);
            var deps = line.Substring(separator + 1).Split([','], StringSplitOptions.RemoveEmptyEntries);
            if (dependencies.ContainsKey(name)) throw new InvalidDataException($"{manifestPath} lists {name} twice.");

            order.Add(name);
            dependencies.Add(name, deps);
        }

        foreach (var pair in dependencies)
        {
            foreach (var dependency in pair.Value)
            {
                if (dependencies.ContainsKey(dependency) is false)
                    throw new InvalidDataException($"{manifestPath}: {pair.Key} depends on {dependency}, which is not a module.");
            }
        }

        return new ButilScriptManifest(order, dependencies);
    }

    /// <summary>
    /// Closes a set of directly referenced modules over the manifest's dependencies and returns the result in
    /// dependency-first order (the manifest's own order), which is the concatenation order of the bundle.
    /// Modules that are not in the manifest are ignored - and reported through <paramref name="unknown"/> -
    /// rather than failing, since an identifier the JavaScript never had is a bug for the interop-contract
    /// tests to catch, not for a consumer's publish to trip over.
    /// </summary>
    public static IReadOnlyList<string> Resolve(ButilScriptManifest manifest, IEnumerable<string> referenced, out IReadOnlyList<string> unknown)
    {
        var included = new HashSet<string>(StringComparer.Ordinal);
        var missing = new SortedSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();

        foreach (var module in referenced)
        {
            if (manifest.Dependencies.ContainsKey(module)) stack.Push(module);
            else missing.Add(module);
        }

        while (stack.Count > 0)
        {
            var module = stack.Pop();
            if (included.Add(module) is false) continue;

            foreach (var dependency in manifest.Dependencies[module]) stack.Push(dependency);
        }

        unknown = missing.ToArray();
        return manifest.Order.Where(included.Contains).ToArray();
    }

    /// <summary>
    /// Concatenates the chunks of the given modules, in the given order, into a bundle. Chunk files are
    /// <c>&lt;chunksDirectory&gt;/&lt;module&gt;.js</c>.
    /// </summary>
    public static void WriteBundle(string chunksDirectory, IEnumerable<string> modules, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

        // Assemble in memory and write once, so an interrupted publish cannot leave a half-written bundle
        // that a later incremental step would take for a finished one.
        var bundle = new StringBuilder();
        foreach (var module in modules)
        {
            var chunk = Path.Combine(chunksDirectory, module + ".js");
            if (File.Exists(chunk) is false) throw new FileNotFoundException($"The Bit.Butil chunk for the '{module}' module is missing.", chunk);
            bundle.Append(File.ReadAllText(chunk));
        }

        File.WriteAllText(outputPath, bundle.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

/// <summary>Module names in dependency-first order, and each module's direct dependencies.</summary>
public sealed class ButilScriptManifest(IReadOnlyList<string> order, IReadOnlyDictionary<string, string[]> dependencies)
{
    public IReadOnlyList<string> Order { get; } = order;

    public IReadOnlyDictionary<string, string[]> Dependencies { get; } = dependencies;
}
