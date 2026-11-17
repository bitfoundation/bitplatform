using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins the order src/BlazorUI/CLAUDE.md sets for a component's parameters: declared in one run sorted by name with case
/// ignored, and listed in the same order in the parameter tables of the demo pages.
/// </summary>
/// <remarks>
/// Both read the source tree. A type's run is read per file - a class split over partial files keeps one run in each -
/// and a type declared inside another starts a run of its own. Every failure names the file and the two parameters
/// out of order, so it can be fixed from the message alone.
/// </remarks>
[TestClass]
public partial class BitParameterOrderContractTests
{
    /// <summary>The folders of the packages whose components are read, relative to src/BlazorUI (Bit.BlazorUI.AI holds all
    /// the AI packages).</summary>
    private static readonly string[] PackageFolders =
        ["Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.Legacy", "Bit.BlazorUI.Assets", "Bit.BlazorUI.AI"];

    [TestMethod]
    public void EveryComponentShouldDeclareItsParametersInAlphabeticalOrder()
    {
        var files = PackageFolders.SelectMany(p => EnumerateSources(Path.Combine(SourceFiles.Root, p), "*.cs", "*.razor")).ToArray();

        var runs = files.SelectMany(f => ReadParameterRuns(f).Select(r => (File: f, Run: r))).ToArray();

        Assert.IsGreaterThan(200, runs.Length, $"Only {runs.Length} components with parameters were found.");

        var offenders = runs.Select(r => FirstOutOfOrder(r.Run) is { } pair ? $"{Relative(r.File)}: {pair.Before} before {pair.After}" : null)
                            .OfType<string>()
                            .ToList();

        Assert.IsEmpty(offenders, "A component declares its parameters out of alphabetical order (case ignored); move each to its place:" +
                                  Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void EveryDemoPageShouldListTheParametersInAlphabeticalOrder()
    {
        var client = SourceFiles.GetDirectory("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core");

        var lists = EnumerateSources(client, "*.cs").SelectMany(f => ReadParameterLists(f).Select(l => (File: f, l.Field, l.Names))).ToArray();

        Assert.IsGreaterThan(150, lists.Length, $"Only {lists.Length} parameter lists were found in the demo pages.");

        var offenders = lists.Select(l => FirstOutOfOrder(l.Names) is { } pair ? $"{Relative(l.File)} ({l.Field}): {pair.Before} before {pair.After}" : null)
                             .OfType<string>()
                             .ToList();

        Assert.IsEmpty(offenders, "A demo page lists the parameters out of alphabetical order (case ignored); move each row to its place:" +
                                  Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void TheReadingShouldFollowTheTypesAndTheAttributes()
    {
        string[] source =
        [
            "public partial class BitFoo",
            "{",
            "    [CascadingParameter] public BitFooParams? CascadingParameters { get; set; }",
            "    [Parameter] public bool Alpha { get; set; }",
            "    [Parameter, ResetClassBuilder]",
            "    public RenderFragment<BitFoo<T>>? Beta { get; set; }",
            "    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> Gamma { get; set; } = [];",
            "    [Parameter]",
            "    public int? Delta",
            "    {",
            "        get => _delta;",
            "    }",
            "    internal sealed class Nested",
            "    {",
            "        [Parameter] public bool Zeta { get; set; }",
            "    }",
        ];

        var runs = ReadParameterRuns(source).ToArray();

        Assert.HasCount(2, runs);
        CollectionAssert.AreEqual(new[] { "Alpha", "Beta", "Gamma", "Delta" }, runs[0].ToArray());
        CollectionAssert.AreEqual(new[] { "Zeta" }, runs[1].ToArray());

        Assert.AreEqual(("Gamma", "Delta"), FirstOutOfOrder(runs[0]));
        Assert.IsNull(FirstOutOfOrder(["Items", "ItemSize", "ItemsProvider", "OnChange", "OnClick"]));
    }

    private static IEnumerable<string> EnumerateSources(string folder, params string[] patterns) =>
        patterns.SelectMany(p => Directory.EnumerateFiles(folder, p, SearchOption.AllDirectories))
                .Where(f => f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) is var parts &&
                            parts.Contains("obj") is false && parts.Contains("bin") is false && parts.Contains("node_modules") is false)
                .Order(StringComparer.Ordinal);

    private static IEnumerable<List<string>> ReadParameterRuns(string file) =>
        ReadParameterRuns(SourceFiles.ReadFullPath(file).Split('\n'));

    // The [Parameter]s of each type a file declares, in their order. An attribute line whose list names Parameter (not
    // CascadingParameter) marks the next property declared, on the same line or a later one.
    private static IEnumerable<List<string>> ReadParameterRuns(string[] lines)
    {
        var run = new List<string>();
        var pending = false;

        foreach (var line in lines)
        {
            if (TypeDeclaration().IsMatch(line))
            {
                if (run.Count > 0) yield return run;
                run = [];
                pending = false;
                continue;
            }

            if (ParameterAttribute().IsMatch(line)) pending = true;

            if (pending && PropertyName().Match(line) is { Success: true } property)
            {
                run.Add(property.Groups["name"].Value);
                pending = false;
            }
        }

        if (run.Count > 0) yield return run;
    }

    // Every List<ComponentParameter> field named ...Parameters (componentParameters, DemoPage's _componentBaseParameters),
    // with the Name of each of its rows in order.
    private static IEnumerable<(string Field, List<string> Names)> ReadParameterLists(string file)
    {
        var source = SourceFiles.ReadFullPath(file);

        foreach (Match field in ParameterListField().Matches(source))
        {
            var open = source.IndexOfAny(['[', '{'], field.Index + field.Length);
            var close = MatchingClose(source, open);
            var body = source[open..close];

            yield return (field.Groups["field"].Value, RowName().Matches(body).Select(m => m.Groups["name"].Value).ToList());
        }
    }

    private static int MatchingClose(string source, int open)
    {
        var depth = 0;
        var inString = false;

        for (var i = open; i < source.Length; i++)
        {
            var c = source[i];

            if (inString)
            {
                if (c == '\\') i++;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c is '[' or '{' or '(') depth++;
            else if (c is ']' or '}' or ')' && --depth == 0) return i;
        }

        throw new InvalidOperationException("The list has no end.");
    }

    private static (string Before, string After)? FirstOutOfOrder(IReadOnlyList<string> names)
    {
        for (var i = 1; i < names.Count; i++)
        {
            if (StringComparer.OrdinalIgnoreCase.Compare(names[i - 1], names[i]) > 0) return (names[i - 1], names[i]);
        }

        return null;
    }

    private static string Relative(string path) => Path.GetRelativePath(SourceFiles.Root, path).Replace('\\', '/');

    [GeneratedRegex(@"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|file|readonly)\s+)*(?:class|record|struct|interface)\s+\w+")]
    private static partial Regex TypeDeclaration();

    // [Parameter], [Parameter(...)] or Parameter in a list ([Parameter, ResetClassBuilder], [EditorRequired, Parameter]).
    [GeneratedRegex(@"^\s*\[(?:[^\]]*?,\s*)?Parameter(?:Attribute)?\s*(?:\(|,|\])")]
    private static partial Regex ParameterAttribute();

    [GeneratedRegex(@"\bpublic\s+(?:(?:new|virtual|override|required|static)\s+)*[^=;(]+?\s(?<name>\w+)\s*(?:\{|=>|$)")]
    private static partial Regex PropertyName();

    [GeneratedRegex(@"List<ComponentParameter>\s+(?<field>\w*Parameters)\s*=")]
    private static partial Regex ParameterListField();

    [GeneratedRegex(@"^\s*Name\s*=\s*""(?<name>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex RowName();
}
