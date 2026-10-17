using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Build;

/// <summary>
/// Decides which pieces of the packages' scripts and stylesheets an app keeps, from what it can use: the types it can
/// reach, the interop identifiers its code (and the library code it reaches) names, and the names its csproj adds.
/// </summary>
/// <remarks>
/// Every rule here keeps more rather than less where it cannot tell: a piece left in costs an app some bytes it did not
/// need, a piece left out is a component drawn unstyled, or a <c>BitBlazorUI.x is undefined</c> in a browser, long after
/// the publish that caused it.
/// </remarks>
public sealed class AssetSelection
{
    /// <summary>What an interop identifier looks like: the namespace and a path of names under it.</summary>
    public static readonly Regex Identifier = new(@"^BitBlazorUI(\.[A-Za-z_$][A-Za-z0-9_$]*)+$", RegexOptions.CultureInvariant);

    private readonly IReadOnlyList<PackageAssets> _packages;
    private readonly Dictionary<string, List<ScriptModule>> _exports = new(StringComparer.Ordinal);

    public AssetSelection(IReadOnlyList<PackageAssets> packages)
    {
        _packages = packages;
        foreach (var module in packages.SelectMany(p => p.ScriptModules))
        {
            foreach (var export in module.Exports)
            {
                if (_exports.TryGetValue(export, out var list) is false) _exports[export] = list = [];
                list.Add(module);
            }
        }
    }

    /// <summary>The scripts kept, with every script they use; in no particular order (each bundle keeps its own).</summary>
    public HashSet<ScriptModule> Scripts { get; } = [];

    public HashSet<StyleChunk> Styles { get; } = [];

    /// <summary>The icon names kept, by the icon set's bundle.</summary>
    public HashSet<string> Icons { get; } = new(StringComparer.Ordinal);

    /// <summary>Identifiers that looked like interop calls but named nothing any script exports.</summary>
    public SortedSet<string> UnresolvedIdentifiers { get; } = new(StringComparer.Ordinal);

    /// <summary>The scripts an interop identifier calls into: "BitBlazorUI.Utils.isRtl" is Utils.isRtl if a script exports exactly that, else Utils.</summary>
    public IReadOnlyList<ScriptModule> ResolveIdentifier(string identifier)
    {
        if (Identifier.IsMatch(identifier) is false) return [];
        return ResolvePath(identifier.Substring("BitBlazorUI.".Length));
    }

    private IReadOnlyList<ScriptModule> ResolvePath(string path)
    {
        for (var key = path; key.Length > 0;)
        {
            if (_exports.TryGetValue(key, out var modules)) return modules;
            var dot = key.LastIndexOf('.');
            if (dot < 0) break;
            key = key.Substring(0, dot);
        }
        return [];
    }

    /// <summary>
    /// Keeps what the given types and texts reach, plus everything marked always: the scripts an interop identifier among
    /// the texts names, and the scripts whose markup one of the texts holds.
    /// </summary>
    /// <param name="presentTypeNames">The names (no namespace, no arity) of the library types the app can reach.</param>
    /// <param name="texts">The string literals of the code the app can run - its own and the library's it reaches - and
    /// the text of its static markup.</param>
    public void Select(ISet<string> presentTypeNames, IReadOnlyCollection<string> texts)
    {
        foreach (var module in _packages.SelectMany(p => p.ScriptModules))
        {
            if (module.Always || module.Owners.Any(o => IsPresent(o, presentTypeNames)) || module.Markup.Any(marker => texts.Any(t => t.IndexOf(marker, StringComparison.Ordinal) >= 0)))
            {
                KeepScript(module);
            }
        }

        foreach (var identifier in texts)
        {
            if (Identifier.IsMatch(identifier) is false) continue;

            var modules = ResolveIdentifier(identifier);
            if (modules.Count == 0) UnresolvedIdentifiers.Add(identifier);
            foreach (var module in modules) KeepScript(module);
        }

        foreach (var chunk in _packages.SelectMany(p => p.StyleChunks))
        {
            if (chunk.Always || chunk.Owners.Any(o => IsPresent(o, presentTypeNames))) Styles.Add(chunk);
        }
    }

    /// <summary>
    /// Whether an owner a piece names is among the types: by its name, or - for an owner ending in * - by the start of
    /// it, which is how a stylesheet names the parts of a component family (BitFc* of BitFullCalendar).
    /// </summary>
    public static bool IsPresent(string owner, ICollection<string> typeNames)
    {
        if (owner.EndsWith("*", StringComparison.Ordinal) is false) return typeNames.Contains(owner);

        var prefix = owner.Substring(0, owner.Length - 1);
        return typeNames.Any(name => name.StartsWith(prefix, StringComparison.Ordinal));
    }

    /// <summary>
    /// Keeps a script or a stylesheet a csproj names, by its name: a script module ("Callouts"), a component stylesheet
    /// ("BitButton") - or both, when the name is both. Returns false when the name is neither.
    /// </summary>
    public bool KeepByName(string name)
    {
        var found = false;
        foreach (var module in _packages.SelectMany(p => p.ScriptModules).Where(m => string.Equals(m.Name, name, StringComparison.Ordinal)))
        {
            KeepScript(module);
            found = true;
        }

        foreach (var chunk in _packages.SelectMany(p => p.StyleChunks).Where(c => string.Equals(c.Name, name, StringComparison.Ordinal)))
        {
            Styles.Add(chunk);
            found = true;
        }

        return found;
    }

    public void KeepScript(ScriptModule module)
    {
        var pending = new Stack<ScriptModule>();
        pending.Push(module);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (Scripts.Add(current) is false) continue;

            foreach (var name in current.Dependencies)
            {
                var dependency = current.Package.ScriptModules.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException($"{current.Id} depends on '{name}', which its package has no script of.");
                pending.Push(dependency);
            }

            foreach (var external in current.Externals)
            {
                var resolved = ResolvePath(external);
                // A path this package names in another package's namespace that no package of the app exports: the
                // other package is newer or older than this one. Said, since the script would fail where it calls it.
                if (resolved.Count == 0) UnresolvedIdentifiers.Add($"BitBlazorUI.{external} (used by {current.Id})");
                foreach (var dependency in resolved) pending.Push(dependency);
            }
        }
    }

    /// <summary>
    /// Keeps the glyphs whose names appear in the given strings - whole, or as one word of a longer one ("Add Delete"),
    /// or after the glyph class prefix ("bit-icon--Add").
    /// </summary>
    public void SelectIcons(IEnumerable<string> strings, IEnumerable<string> knownIcons, IEnumerable<string> classPrefixes)
    {
        var known = new HashSet<string>(knownIcons, StringComparer.Ordinal);
        var prefixes = classPrefixes.ToArray();

        foreach (var s in strings)
        {
            if (s.Length == 0) continue;
            if (known.Contains(s)) { Icons.Add(s); continue; }

            foreach (var word in s.Split([' ', '\t', '\n', '\r', ',', ';', '"', '\'', '.', ':', '(', ')', '[', ']', '{', '}', '<', '>', '/', '=', '#'], StringSplitOptions.RemoveEmptyEntries))
            {
                var name = word;
                foreach (var prefix in prefixes)
                {
                    if (name.StartsWith(prefix, StringComparison.Ordinal)) { name = name.Substring(prefix.Length); break; }
                }
                if (known.Contains(name)) Icons.Add(name);
            }
        }
    }
}
