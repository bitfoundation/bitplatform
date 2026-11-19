using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The names of the packages' API removed or renamed since 10.5.0, with what to write instead: what an agent upgrading
/// an app's packages works through, answered by <c>GetBitBlazorUIBreakingChanges</c>.
/// <para>
/// Nothing is written down twice: the list is Bit.BlazorUI.Analyzers' BreakingChanges.json, the very file the analyzer
/// that fails an app's build on these names (BITBC001 / BITBC002) is built from, embedded into this assembly by the
/// .csproj. So the fix an agent reads here is the fix the build error names, word for word, and a change added for the
/// analyzer is a change this server knows.
/// </para>
/// </summary>
public static class BlazorUIBreakingChanges
{
    private const string ResourceName = "BitBlazorUIBreakingChanges.json";

    public const string DocumentUrl = "https://github.com/bitfoundation/bitplatform/blob/develop/src/BlazorUI/BreakingChanges.md";

    public sealed record Change(string Kind, string Name, string[] On, bool Component, string Use, string Hint, string Since)
    {
        public bool IsMember => Kind == "member";

        public Version SinceVersion => ParseVersion(Since) ?? new Version(0, 0);
    }

    public static Change[] All { get; } = Load();

    /// <summary>The versions an app can be upgrading from: 10.5.0, and every one a change shipped in but the newest.</summary>
    public static string[] FromVersions { get; } =
        [.. All.Select(c => c.SinceVersion).Append(new Version(10, 5, 0)).Distinct().Order().SkipLast(1).Select(v => v.ToString(3))];

    /// <summary>
    /// The changes since <paramref name="fromVersion"/> (all of them when it is empty), narrowed to
    /// <paramref name="names"/> when given, as Markdown: one section per version, oldest first, the members and the
    /// types apart.
    /// </summary>
    public static string Get(string? fromVersion, string? names)
    {
        Version? from = null;
        if (string.IsNullOrWhiteSpace(fromVersion) is false)
        {
            from = ParseVersion(fromVersion);
            if (from is null)
                return $"'{fromVersion}' is not a version. Pass the version of the bit BlazorUI packages the app was on, e.g. '{FromVersions.First()}' or '{FromVersions.Last()}'.";
        }

        var wanted = (names ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var changes = All.Where(c => from is null || c.SinceVersion > from)
                         .Where(c => wanted.Length == 0 || wanted.Any(w => Matches(c, w)))
                         .ToArray();

        var sb = new StringBuilder();

        if (changes.Length == 0)
        {
            if (wanted.Length > 0)
            {
                sb.AppendLine($"None of {string.Join(", ", wanted.Select(w => $"'{w}'"))} is a name bit BlazorUI removed or renamed{(from is null ? " since 10.5.0" : $" after {from.ToString(3)}")}.");
                sb.AppendLine();
                sb.AppendLine("If the name is reported by the build as missing, look it up with GetBitBlazorUIComponent or GetBitBlazorUIType for what exists now. Behaviour, markup and CSS changes that no name shows are in " + DocumentUrl + ".");
                return sb.ToString();
            }

            return $"No name of bit BlazorUI's API was removed or renamed after {from!.ToString(3)}. Behaviour, markup and CSS changes are in {DocumentUrl}.";
        }

        sb.AppendLine(wanted.Length > 0
            ? $"# bit BlazorUI: {changes.Length} removed or renamed name(s) matching {string.Join(", ", wanted.Select(w => $"'{w}'"))}"
            : $"# bit BlazorUI: what to change after upgrading{(from is null ? " from 10.5.0 or later" : $" from {from.ToString(3)}")}");
        sb.AppendLine();
        sb.AppendLine("Each old name below is gone from the packages. Search the app for every one of them - in .razor markup (a component's tag, an attribute, a `@bind-` of it, its `...Changed` callback, a child content tag) and in C# (a type name, an object initializer, a member access) - and apply the fix. **Read the fix before replacing**: some renames invert the value (`IsEnabled=\"false\"` is `Disabled`), and some depend on the direction or the value written.");
        sb.AppendLine();
        sb.AppendLine("A removed parameter written on a component still compiles - the component renders it as an HTML attribute and it does nothing - and a removed component's tag renders as an unknown element. The packages' analyzer fails the build on what is left (errors BITBC001 and BITBC002, with the same fix as below) unless the project sets `<BitBlazorUIBreakingChangeAnalyzer>false</BitBlazorUIBreakingChangeAnalyzer>`; build after the edits and fix what it reports.");

        foreach (var version in changes.GroupBy(c => c.SinceVersion).OrderBy(g => g.Key))
        {
            sb.AppendLine();
            sb.AppendLine($"## Gone in {version.Key.ToString(3)}");

            var members = version.Where(c => c.IsMember).OrderBy(c => c.Name, StringComparer.Ordinal).ToArray();
            if (members.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Parameters and members");
                sb.AppendLine();
                sb.AppendLine("| Old name | On | Use | Fix |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var c in members)
                    sb.AppendLine($"| `{c.Name}` | {string.Join(", ", c.On)} | {Code(c.Use)} | {Cell(c.Hint)} |");
            }

            var types = version.Where(c => c.IsMember is false).OrderBy(c => c.Name, StringComparer.Ordinal).ToArray();
            if (types.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("### Components and types");
                sb.AppendLine();
                sb.AppendLine("| Old name | Kind | Use | Fix |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var c in types)
                    sb.AppendLine($"| `{c.Name}` | {(c.Component ? "component" : "type")} | {Code(c.Use)} | {Cell(c.Hint)} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"What changed without a name changing - markup, CSS classes, defaults, behaviour - is in {DocumentUrl}, newest version first. Read the sections of the versions above for the ones that apply to the app.");

        return sb.ToString();
    }

    private static bool Matches(Change change, string wanted)
    {
        // An error's text may be pasted as it is: "IsEnabled", "BitButton.IsEnabled", "<BitProModal>", "IsCheckedChanged".
        var name = wanted.Trim('<', '>', '/', ' ', '\'', '"', '`');
        var dot = name.LastIndexOf('.');
        string? on = null;
        if (dot > 0)
        {
            on = name[..dot];
            name = name[(dot + 1)..];
        }

        bool NameMatches(string n) => string.Equals(change.Name, n, StringComparison.OrdinalIgnoreCase);

        var nameMatches = NameMatches(name) || (name.EndsWith("Changed", StringComparison.Ordinal) && NameMatches(name[..^"Changed".Length]));
        if (nameMatches is false) return false;

        return on is null || change.IsMember is false || change.On.Contains(on, StringComparer.OrdinalIgnoreCase) || on.StartsWith("Bit", StringComparison.Ordinal) is false;
    }

    private static string Code(string use) => use.Length == 0 ? "-" : $"`{use}`";

    private static string Cell(string text) => text.Replace("|", "\\|").Replace("\n", " ");

    /// <summary>Reads '10.5.0', 'v-10.5.0', '10.5' and '11.0.0-pre-03' (as 11.0.0, the version its changes count for).</summary>
    public static Version? ParseVersion(string text)
    {
        var digits = new string(text.Trim().TrimStart('v', 'V', '-').TakeWhile(ch => char.IsDigit(ch) || ch == '.').ToArray()).TrimEnd('.');
        if (Version.TryParse(digits, out var version) is false) return null;
        return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
    }

    private static Change[] Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                           ?? throw new InvalidOperationException($"{ResourceName} is not embedded (see the .csproj).");
        using var document = JsonDocument.Parse(stream);

        return [.. document.RootElement.GetProperty("changes").EnumerateArray().Select(e => new Change(
            Kind: e.GetProperty("kind").GetString()!,
            Name: e.GetProperty("name").GetString()!,
            On: e.TryGetProperty("on", out var on) ? [.. on.EnumerateArray().Select(x => x.GetString()!)] : [],
            Component: e.TryGetProperty("component", out var component) && component.GetBoolean(),
            Use: e.TryGetProperty("use", out var use) ? use.GetString() ?? string.Empty : string.Empty,
            Hint: e.GetProperty("hint").GetString()!,
            Since: e.GetProperty("since").GetString()!))];
    }
}
