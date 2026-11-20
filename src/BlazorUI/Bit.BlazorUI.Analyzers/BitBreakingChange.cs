using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Bit.BlazorUI.Analyzers;

/// <summary>
/// One entry of BreakingChanges.json: a name of the packages' public API an app may still use, gone since a version.
/// </summary>
public sealed class BitBreakingChange
{
    /// <summary>
    /// <c>member</c>: a parameter or a property of the types in <see cref="On"/>, named <see cref="Name"/>. On a component
    /// it is matched in markup (an attribute, a <c>@bind-</c>, a child content tag) as well as in C#.
    /// <c>type</c>: a type named <see cref="Name"/>; with <see cref="Component"/> its tag is matched in markup too.
    /// </summary>
    public string Kind { get; }

    /// <summary>The simple name of the member or the type that is gone.</summary>
    public string Name { get; }

    /// <summary>For a member: the simple names of the types it was on. A type matches its subclasses too.</summary>
    public ImmutableArray<string> On { get; }

    /// <summary>For a type: whether it was a component, whose tag is matched in .razor files.</summary>
    public bool Component { get; }

    /// <summary>What to write instead, in a few words (<c>Disabled</c>, <c>BitPlacement</c>); empty when nothing replaces it.</summary>
    public string Use { get; }

    /// <summary>The fix, written for an app developer.</summary>
    public string Hint { get; }

    /// <summary>The version of the packages it is gone in.</summary>
    public string Since { get; }

    public BitBreakingChange(string kind, string name, ImmutableArray<string> on, bool component, string use, string hint, string since)
    {
        Kind = kind;
        Name = name;
        On = on;
        Component = component;
        Use = use;
        Hint = hint;
        Since = since;
    }

    public bool IsMember => Kind == "member";

    public bool IsType => Kind == "type";
}

/// <summary>
/// The changes of BreakingChanges.json, embedded into this assembly, indexed the ways the analyzer looks them up.
/// </summary>
public sealed class BitBreakingChanges
{
    public const string ResourceName = "Bit.BlazorUI.BreakingChanges.json";

    private static BitBreakingChanges? _default;

    /// <summary>The changes embedded into this assembly.</summary>
    public static BitBreakingChanges Default => _default ??= Load();

    public ImmutableArray<BitBreakingChange> All { get; }

    /// <summary>Members by their name, case ignored (Razor matches an attribute to a parameter regardless of case).</summary>
    public IReadOnlyDictionary<string, ImmutableArray<BitBreakingChange>> MembersByName { get; }

    /// <summary>Types and components by their name.</summary>
    public IReadOnlyDictionary<string, BitBreakingChange> TypesByName { get; }

    public BitBreakingChanges(IEnumerable<BitBreakingChange> changes)
    {
        All = changes.ToImmutableArray();
        MembersByName = All.Where(c => c.IsMember)
                           .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                           .ToDictionary(g => g.Key, g => g.ToImmutableArray(), StringComparer.OrdinalIgnoreCase);
        var types = new Dictionary<string, BitBreakingChange>(StringComparer.Ordinal);
        foreach (var change in All.Where(c => c.IsType)) types[change.Name] = change;
        TypesByName = types;
    }

    public static BitBreakingChanges Load()
    {
        using var stream = typeof(BitBreakingChanges).Assembly.GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"{ResourceName} is not embedded.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>Reads the changes out of the text of a BreakingChanges.json.</summary>
    public static BitBreakingChanges Parse(string json)
    {
        var root = BitJson.Parse(json) as Dictionary<string, object?> ?? throw new FormatException("BreakingChanges.json is not an object.");
        var list = new List<BitBreakingChange>();

        foreach (var item in root.TryGetValue("changes", out var c) && c is List<object?> l ? l : [])
        {
            if (item is not Dictionary<string, object?> o) throw new FormatException("A change is not an object.");

            string Str(string key) => o.TryGetValue(key, out var v) && v is string s ? s : string.Empty;

            var kind = Str("kind");
            if (kind is not ("member" or "type")) throw new FormatException($"Unknown kind '{kind}' of '{Str("name")}'.");

            var on = o.TryGetValue("on", out var onValue) && onValue is List<object?> onList
                         ? onList.OfType<string>().ToImmutableArray()
                         : ImmutableArray<string>.Empty;

            list.Add(new BitBreakingChange(kind, Str("name"), on, o.TryGetValue("component", out var comp) && comp is true,
                                           Str("use"), Str("hint"), Str("since")));
        }

        return new BitBreakingChanges(list);
    }
}

/// <summary>
/// A JSON reader just big enough for BreakingChanges.json: objects as dictionaries, arrays as lists, strings, numbers as
/// doubles, booleans and null. An analyzer cannot count on System.Text.Json being loadable in every compiler host.
/// </summary>
internal static class BitJson
{
    public static object? Parse(string text)
    {
        var i = 0;
        var value = ReadValue(text, ref i);
        SkipWhitespace(text, ref i);
        if (i != text.Length) throw Error(text, i, "end of the text");
        return value;
    }

    private static object? ReadValue(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length) throw Error(s, i, "a value");

        switch (s[i])
        {
            case '{':
                {
                    var obj = new Dictionary<string, object?>(StringComparer.Ordinal);
                    i++;
                    SkipWhitespace(s, ref i);
                    if (i < s.Length && s[i] == '}') { i++; return obj; }
                    while (true)
                    {
                        SkipWhitespace(s, ref i);
                        var key = ReadString(s, ref i);
                        SkipWhitespace(s, ref i);
                        Expect(s, ref i, ':');
                        obj[key] = ReadValue(s, ref i);
                        SkipWhitespace(s, ref i);
                        if (i < s.Length && s[i] == ',') { i++; continue; }
                        Expect(s, ref i, '}');
                        return obj;
                    }
                }
            case '[':
                {
                    var list = new List<object?>();
                    i++;
                    SkipWhitespace(s, ref i);
                    if (i < s.Length && s[i] == ']') { i++; return list; }
                    while (true)
                    {
                        list.Add(ReadValue(s, ref i));
                        SkipWhitespace(s, ref i);
                        if (i < s.Length && s[i] == ',') { i++; continue; }
                        Expect(s, ref i, ']');
                        return list;
                    }
                }
            case '"':
                return ReadString(s, ref i);
            case 't' when Matches(s, i, "true"):
                i += 4; return true;
            case 'f' when Matches(s, i, "false"):
                i += 5; return false;
            case 'n' when Matches(s, i, "null"):
                i += 4; return null;
            default:
                {
                    var start = i;
                    while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
                    if (start == i) throw Error(s, i, "a value");
                    return double.Parse(s.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
                }
        }
    }

    private static string ReadString(string s, ref int i)
    {
        Expect(s, ref i, '"');
        var sb = new System.Text.StringBuilder();
        while (i < s.Length)
        {
            var ch = s[i++];
            if (ch == '"') return sb.ToString();
            if (ch != '\\') { sb.Append(ch); continue; }
            if (i >= s.Length) break;
            var esc = s[i++];
            switch (esc)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u' when i + 4 <= s.Length:
                    sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                    i += 4;
                    break;
                default: throw Error(s, i - 1, "an escape sequence");
            }
        }
        throw Error(s, i, "the end of a string");
    }

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == '﻿')) i++;
    }

    private static void Expect(string s, ref int i, char ch)
    {
        if (i >= s.Length || s[i] != ch) throw Error(s, i, $"'{ch}'");
        i++;
    }

    private static bool Matches(string s, int i, string word) => string.CompareOrdinal(s, i, word, 0, word.Length) == 0;

    private static FormatException Error(string s, int i, string expected)
    {
        var line = 1;
        for (var k = 0; k < i && k < s.Length; k++) if (s[k] == '\n') line++;
        return new FormatException($"BreakingChanges.json, line {line}: expected {expected}.");
    }
}
