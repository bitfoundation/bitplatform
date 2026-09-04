using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var modules = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var literal in UserStringHeap.Read(assemblyPath))
        {
            if (TryGetModule(literal, out var module)) modules.Add(module);
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

        // Two things about a dependency, checked in the one place that can still say which line was wrong:
        // that it names a module at all, and that the module came earlier - the order the manifest is read in
        // is the order the chunks are concatenated in, so a dependency listed after its dependent would
        // produce a bundle whose pieces run before the ones they need.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in order)
        {
            foreach (var dependency in dependencies[name])
            {
                if (dependencies.ContainsKey(dependency) is false)
                    throw new InvalidDataException($"{manifestPath}: {name} depends on {dependency}, which is not a module.");

                if (seen.Contains(dependency) is false)
                    throw new InvalidDataException($"{manifestPath}: {name} depends on {dependency}, which it lists after itself; the manifest must be in dependency-first order.");
            }

            seen.Add(name);
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
    /// Turns the names a consumer wrote in their csproj into modules. Each one is either a module name as
    /// the manifest spells it (<c>clipboard</c>) or the name of a Bit.Butil class (<c>Clipboard</c>,
    /// or <c>Bit.Butil.Clipboard</c>) - a consumer thinks in the classes they inject, and a class is the
    /// safer thing to name anyway, since one class can need more than one module.
    /// </summary>
    /// <remarks>
    /// A name that is neither is an error rather than a shrug. MSBuild accepts a misspelled item silently,
    /// and the cost of ignoring one here is a module missing from a bundle - which surfaces in a browser,
    /// after publishing, as an API that does nothing.
    /// </remarks>
    /// <param name="names">What the consumer wrote.</param>
    /// <param name="manifest">The module manifest, which is what makes a name a module name.</param>
    /// <param name="types">The type map, when one could be built; without it only module names resolve.</param>
    /// <param name="unresolved">The names that matched nothing, in the order they were given.</param>
    public static SortedSet<string> ResolveNames(IEnumerable<string> names, ButilScriptManifest manifest, ButilTypeModules? types, out IReadOnlyList<string> unresolved)
    {
        var modules = new SortedSet<string>(StringComparer.Ordinal);
        var missing = new List<string>();

        foreach (var raw in names)
        {
            var name = (raw ?? string.Empty).Trim();
            if (name.Length == 0) continue;

            if (manifest.Dependencies.ContainsKey(name))
            {
                modules.Add(name);
                continue;
            }

            var fromType = types is null ? [] : types.ForFullName(name);
            if (fromType.Count == 0 && types is not null) fromType = types.ForName(name);

            if (fromType.Count == 0)
            {
                // A last, case-insensitive pass, so that a consumer who wrote the module in the casing of the
                // class (or the class in the casing of the module) is understood rather than corrected.
                var module = manifest.Order.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
                if (module is not null)
                {
                    modules.Add(module);
                    continue;
                }

                var type = types?.FullTypeNames.FirstOrDefault(candidate =>
                    string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(candidate.Substring(candidate.LastIndexOf('.') + 1), name, StringComparison.OrdinalIgnoreCase));

                fromType = type is null ? [] : types!.ForFullName(type);
            }

            if (fromType.Count == 0) missing.Add(name);
            else foreach (var module in fromType) modules.Add(module);
        }

        unresolved = missing;
        return modules;
    }

    /// <summary>
    /// Concatenates the chunks of the given modules, in the given order, into a bundle. Chunk files are
    /// <c>&lt;chunksDirectory&gt;/&lt;module&gt;.js</c>.
    /// </summary>
    public static void WriteBundle(string chunksDirectory, IEnumerable<string> modules, string outputPath)
    {
        var destination = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        // Assemble in memory, write it beside the destination and only then move it into place, so an
        // interrupted publish cannot leave a half-written bundle that a later incremental step would take
        // for a finished one.
        var bundle = new StringBuilder();
        foreach (var module in modules)
        {
            var chunk = Path.Combine(chunksDirectory, module + ".js");
            if (File.Exists(chunk) is false) throw new FileNotFoundException($"The Bit.Butil chunk for the '{module}' module is missing.", chunk);
            bundle.Append(File.ReadAllText(chunk));
        }

        var temporary = destination + ".tmp";
        try
        {
            File.WriteAllText(temporary, bundle.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(temporary, destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
