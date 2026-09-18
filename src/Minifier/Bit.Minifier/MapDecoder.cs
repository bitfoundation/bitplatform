using System.Text.RegularExpressions;

namespace Bit.Minifier;

/// <summary>
/// Reads bit-minifier.map the other way round: the short names of a logged stack trace, or of any text that
/// names them, become the names the source has.
///
/// The map is written as the assemblies are rewritten, so it lists old -> new, while a decoder needs new -> old,
/// which the entries only give together. A type's entry names the new leaf, not what the types it is nested in
/// became, and a member's entry names the type it belongs to by the name that type had when the member was
/// renamed: before its own rename, and after a generated name around it was shortened. So the types come first,
/// every type path is resolved to what it ended up as, and the members hang off that.
/// </summary>
internal sealed partial class MapDecoder
{
    /// <summary>What one minified assembly renamed, indexed by the names it is left with.</summary>
    private sealed class Index(string assembly)
    {
        public string Assembly { get; } = assembly;

        /// <summary>New parent path ("" for a top-level type) -> new leaf -> the leaf the source has.</summary>
        public Dictionary<string, Dictionary<string, string>> Types { get; } = [];

        /// <summary><c>new type path::new member name</c> -> the name the source has.</summary>
        public Dictionary<string, string> Members { get; } = [];

        /// <summary>New type path, or <c>type::method</c> -> new generic parameter name -> the name the source has.</summary>
        public Dictionary<string, Dictionary<string, string>> Generics { get; } = [];
    }

    /// <summary>One name of one frame, as one assembly reads it.</summary>
    private sealed record Resolution(Index Index, string Owner, string Original, bool Decoded);

    private readonly List<Index> indexes = [];

    public static MapDecoder Load(string path) => new(File.ReadLines(path));

    public MapDecoder(IEnumerable<string> lines)
    {
        // an assembly renames its members before its types, so nothing resolves while the lines are still coming in
        var entries = new Dictionary<string, List<Entry>>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length != 4) continue;
            if (entries.TryGetValue(parts[0], out var assembly) is false)
            {
                entries[parts[0]] = assembly = [];
                order.Add(parts[0]);
            }
            assembly.Add(new Entry(parts[1], parts[2], parts[3]));
        }
        foreach (var assembly in order) indexes.Add(Build(assembly, entries[assembly]));
    }

    private readonly record struct Entry(string Kind, string Original, string Name);

    private static Index Build(string assembly, List<Entry> entries)
    {
        var index = new Index(assembly);

        // a generated nested type can be renamed twice (<AddAsync>d__1 -> <a>d__1 -> _a), both times under the name
        // it has in the source: the last entry is the one it is left with
        var renamed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry.Kind == "T") renamed[entry.Original] = entry.Name;
        }

        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        // Where a type of the source ends up: every type around it resolved the same way, and the leaf this one was
        // renamed to. A segment no entry mentions is already the name it is left with - either nothing renamed it,
        // or the entry that did was written after the one being read.
        string Path(string original)
        {
            if (paths.TryGetValue(original, out var known)) return known;
            var nested = original.LastIndexOf('/');
            var path = nested < 0
                ? renamed.GetValueOrDefault(original, original)
                : Path(original[..nested]) + "/" + renamed.GetValueOrDefault(original, original[(nested + 1)..]);
            return paths[original] = path;
        }

        foreach (var (original, name) in renamed)
        {
            var nested = original.LastIndexOf('/');
            Leaves(index.Types, nested < 0 ? "" : Path(original[..nested]))[name] = nested < 0 ? original : original[(nested + 1)..];
        }

        // the members of a type renamed twice name it by the name it had in between, which nothing is left at
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (kind, original, name) in entries)
        {
            if (kind != "T") continue;
            var nested = original.LastIndexOf('/');
            var path = nested < 0 ? name : Path(original[..nested]) + "/" + name;
            if (path != Path(original)) aliases[path] = Path(original);
        }

        string Owner(string type) => aliases.GetValueOrDefault(Path(type)) ?? Path(type);

        foreach (var (kind, original, name) in entries)
        {
            if (kind is "M" or "F")
            {
                if (Split(original, "::") is not { } member) continue;
                index.Members.TryAdd(Owner(member.Left) + "::" + name, member.Right);
            }
            else if (kind == "G")
            {
                // owner<parameter>: the owner is a type, or a method that carries its new name already
                if (original.EndsWith('>') is false) continue;
                var open = Opening(original);
                if (open <= 0) continue;
                var owner = original[..open];
                var method = Split(owner, "::");
                Leaves(index.Generics, method is { } m ? Owner(m.Left) + "::" + m.Right : Owner(owner)).TryAdd(name, original[(open + 1)..^1]);
            }
        }
        return index;
    }

    private static Dictionary<string, string> Leaves(Dictionary<string, Dictionary<string, string>> map, string key)
        => map.TryGetValue(key, out var leaves) ? leaves : map[key] = [];

    private static (string Left, string Right)? Split(string text, string separator)
    {
        var at = text.LastIndexOf(separator, StringComparison.Ordinal);
        return at < 0 ? null : (text[..at], text[(at + separator.Length)..]);
    }

    /// <summary>The '&lt;' the last '&gt;' of a name closes, which the name of a generic parameter may itself hold.</summary>
    private static int Opening(string text)
    {
        var depth = 0;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            if (text[i] == '>') depth++;
            else if (text[i] == '<' && --depth == 0) return i;
        }
        return -1;
    }

    /// <summary>
    /// Every name of <paramref name="text"/> the map knows, as the source spells it. A name several assemblies
    /// were left with is decoded to the first of them and the others are named in brackets at the end of the line:
    /// a stack trace says which assembly a frame is in no more than it says what its names used to be.
    /// </summary>
    public string Decode(string text) => Lines().Replace(text, line => DecodeLine(line.Value));

    private string DecodeLine(string line)
    {
        List<string>? ambiguous = null;
        var decoded = Names().Replace(line, match =>
        {
            var name = match.Groups["name"].Value;
            var resolutions = Resolve(name);
            if (resolutions.Count == 0) return match.Value;
            if (resolutions.Count > 1)
            {
                (ambiguous ??= []).Add($"{name} is also {string.Join(" or ", resolutions.Skip(1).Select(r => $"{r.Original} ({r.Index.Assembly})"))}");
            }
            return resolutions[0].Original + Arguments(resolutions[0], match.Groups["arguments"]);
        });
        return ambiguous is null ? decoded : $"{decoded}  [Bit.Minifier: {string.Join("; ", ambiguous)}]";
    }

    /// <summary>The generic arguments a frame names, when they are the parameters of the method it names.</summary>
    private static string Arguments(Resolution resolution, Group arguments)
    {
        if (arguments.Success is false) return "";
        if (resolution.Index.Generics.TryGetValue(resolution.Owner, out var parameters) is false) return arguments.Value;
        return "[" + string.Join(",", arguments.Value[1..^1].Split(',').Select(a => parameters.GetValueOrDefault(a.Trim(), a))) + "]";
    }

    /// <summary>
    /// What <paramref name="name"/> stands for in every assembly that knows it: a type and one of its members, or,
    /// where a frame names no member, a type. Two assemblies that say the same thing count once - a type a
    /// forwarder leads to is renamed in both.
    /// </summary>
    private List<Resolution> Resolve(string name)
    {
        var results = new List<Resolution>();
        foreach (var index in indexes)
        {
            // the last part of a qualified name is a member before it is the type the rest of the name nests it in
            if (Split(name, ".") is { } split)
            {
                var (owner, member) = (Type(index, split.Left), split.Right);
                var original = index.Members.GetValueOrDefault(owner.Owner + "::" + member);
                if (owner.Decoded || original is not null)
                {
                    Add(owner with
                    {
                        Owner = owner.Owner + "::" + member,
                        Original = owner.Original + "." + (original ?? member),
                        Decoded = true,
                    });
                    continue;
                }
            }
            if (Type(index, name) is { Decoded: true } type) Add(type);
        }
        return results;

        void Add(Resolution resolution)
        {
            if (results.Any(r => r.Original == resolution.Original) is false) results.Add(resolution);
        }
    }

    /// <summary>
    /// <paramref name="name"/> read as a type path, one nested level at a time: a level the map doesn't know is a
    /// name that was kept, and only a path that holds a name that was not is a type this assembly renamed.
    /// </summary>
    private static Resolution Type(Index index, string name)
    {
        var path = "";
        var original = "";
        var decoded = false;
        foreach (var segment in name.Split('+'))
        {
            var leaf = index.Types.GetValueOrDefault(path)?.GetValueOrDefault(segment);
            decoded |= leaf is not null;
            original = original.Length == 0 ? leaf ?? segment : original + "+" + (leaf ?? segment);
            path = path.Length == 0 ? segment : path + "/" + segment;
        }
        return new Resolution(index, path, original, decoded);
    }

    /// <summary>
    /// A qualified name of a stack trace: <c>Namespace.Type+Nested.Method</c>, with the arity and the generated
    /// names metadata spells (<c>&lt;Main&gt;$</c>, <c>&lt;a&gt;d__3</c>), followed by the generic arguments when
    /// those are parameter names rather than types. A short name on its own is one too: super aggressive leaves a
    /// type whose namespace went with nothing but an <c>_a</c> to be named by.
    /// </summary>
    [GeneratedRegex(@"(?<![\w`<>$.+])(?<name>[\w`<>$]+(?:[.+][\w`<>$]+)+|_[A-Za-z]+(?:`\d+)?)(?![\w`<>$])(?<arguments>\[[A-Za-z_][\w`]*(?:, ?[A-Za-z_][\w`]*)*\])?")]
    private static partial Regex Names();

    /// <summary>A line without what ends it: whatever a log was written with is what it is read back with.</summary>
    [GeneratedRegex(@"[^\r\n]+")]
    private static partial Regex Lines();
}
