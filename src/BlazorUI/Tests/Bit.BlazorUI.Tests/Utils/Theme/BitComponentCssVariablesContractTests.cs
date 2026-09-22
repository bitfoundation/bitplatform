using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Guards the per-component CSS variable tier (<c>--bit-&lt;cmp&gt;-*</c>) against colliding with
/// the global theme token tier.
/// </summary>
[TestClass]
public sealed class BitComponentCssVariablesContractTests
{
    private static readonly Regex CssVarDeclaration = new(
        @"^\s*(--bit-[a-zA-Z0-9-]+)\s*:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // The public tier: --bit-<Component>-<part>, told apart from the private --bit-<abbr>-* one and from
    // the global tokens by the capital its component name starts with. Only reads count - the whole point
    // of the tier is that a component never declares one, so that a value set anywhere above it inherits.
    private static readonly Regex PublicVarRead = new(
        @"var\(\s*(--bit-(?<component>[A-Z][A-Za-z0-9]*)-[a-z0-9-]+)",
        RegexOptions.Compiled);

    // One row of the demo page's componentCssVariables table, which is the whole source of what the site
    // and the MCP server say about this tier - there is no type behind it to read the names off.
    private static readonly Regex DocumentedVar = new(
        @"Name\s*=\s*""(--bit-[A-Za-z0-9-]+)""",
        RegexOptions.Compiled);

    [TestMethod]
    public void ComponentVariablesNeverShadowGlobalTokens()
    {
        // A component redeclaring a global token (e.g. --bit-clr-pri) would silently re-scope
        // theming for its subtree. The component tier must stay disjoint from the theme tier.
        var theme = new BitTheme();
        BitThemeTestGraph.FillStringLeavesWithSentinels(theme);
        var globalTokens = BitThemeUtilities.ToCssVariables(theme).Keys.ToHashSet(StringComparer.Ordinal);

        var offenders = CollectDeclarations(GetComponentStylesDirectory())
            .SelectMany(kv => kv.Value.Where(globalTokens.Contains).Select(v => $"{kv.Key}: {v}"))
            .OrderBy(o => o, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"Component styles redeclare global theme tokens: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// The public <c>--bit-&lt;Component&gt;-*</c> variables a component reads have no type behind them, so the
    /// demo page's <c>componentCssVariables</c> table is the only thing that says they exist - to the site, to
    /// the MCP server and to anyone reading the docs. This pins the two to each other in both directions.
    /// </summary>
    [TestMethod]
    public void EveryPublicVariableIsBothReadAndDocumented()
    {
        var read = CollectPublicReads(GetComponentStylesDirectory());
        var documented = CollectDocumented(GetDemoPagesDirectory());

        var undocumented = read.Keys.Except(documented.Keys)
            .Select(v => $"{v} (read by {read[v]}, named in no componentCssVariables table)");

        var unread = documented.Keys.Except(read.Keys)
            .Select(v => $"{v} (documented by {documented[v]}, read by no stylesheet)");

        var offenders = undocumented.Concat(unread).OrderBy(o => o, StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders,
            $"The public CSS variable tier and the demo tables have drifted apart: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// A public variable is named after the component that reads it, so the one place it is set says which
    /// component it re-skins. A name that does not match the stylesheet reading it is a typo or a leak.
    /// </summary>
    [TestMethod]
    public void EveryPublicVariableIsNamedAfterItsComponent()
    {
        var offenders = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var file in EnumerateStyles(GetComponentStylesDirectory()))
        {
            var component = Path.GetFileNameWithoutExtension(file);

            foreach (Match match in PublicVarRead.Matches(File.ReadAllText(file)))
            {
                if ($"Bit{match.Groups["component"].Value}" == component) continue;

                offenders.Add($"{component} reads {match.Groups[1].Value}");
            }
        }

        CollectionAssert.AreEqual(Array.Empty<string>(), offenders.ToArray(),
            $"Public CSS variables read outside the component they are named for: {string.Join(", ", offenders)}");
    }

    private static string GetTestSourceDirectory([CallerFilePath] string thisFile = "")
    {
        var dir = Path.GetDirectoryName(thisFile);
        Assert.IsTrue(dir is not null && Directory.Exists(dir),
            $"The test source directory ({dir}) is not available; this contract test must run from a source checkout.");
        return dir!;
    }

    private static string GetComponentStylesDirectory()
    {
        var dir = Path.GetFullPath(Path.Combine(GetTestSourceDirectory(), "..", "..", "..", "..", "Bit.BlazorUI", "Components"));
        Assert.IsTrue(Directory.Exists(dir), $"Missing {dir}.");
        return dir;
    }

    private static string GetDemoPagesDirectory()
    {
        var dir = Path.GetFullPath(Path.Combine(GetTestSourceDirectory(), "..", "..", "..", "..",
            "Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components"));
        Assert.IsTrue(Directory.Exists(dir), $"Missing {dir}.");
        return dir;
    }

    private static IEnumerable<string> EnumerateStyles(string stylesDir)
        => Directory.EnumerateFiles(stylesDir, "*.scss", SearchOption.AllDirectories);

    /// <summary>Every public variable a component stylesheet reads, against the file that reads it.</summary>
    private static SortedDictionary<string, string> CollectPublicReads(string stylesDir)
    {
        var byVariable = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in EnumerateStyles(stylesDir))
        {
            var component = Path.GetFileNameWithoutExtension(file);

            foreach (Match match in PublicVarRead.Matches(File.ReadAllText(file)))
            {
                byVariable[match.Groups[1].Value] = component;
            }
        }

        return byVariable;
    }

    /// <summary>Every variable a demo page's componentCssVariables table names, against the page naming it.</summary>
    private static SortedDictionary<string, string> CollectDocumented(string demoPagesDir)
    {
        var byVariable = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(demoPagesDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);

            // The table is a field of the page, so only what follows its declaration is read - every other
            // mention of a variable on a demo page is a worked example setting one, which documents nothing.
            var start = text.IndexOf("componentCssVariables", StringComparison.Ordinal);
            if (start < 0) continue;

            foreach (Match match in DocumentedVar.Matches(text, start))
            {
                byVariable[match.Groups[1].Value] = Path.GetFileName(file);
            }
        }

        return byVariable;
    }

    private static SortedDictionary<string, SortedSet<string>> CollectDeclarations(string stylesDir)
    {
        var byComponent = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(stylesDir, "*.scss", SearchOption.AllDirectories))
        {
            var component = Path.GetFileNameWithoutExtension(file);
            var names = CssVarDeclaration.Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value);

            foreach (var name in names)
            {
                if (byComponent.TryGetValue(component, out var set) is false)
                {
                    byComponent[component] = set = new SortedSet<string>(StringComparer.Ordinal);
                }

                set.Add(name);
            }
        }

        return byComponent;
    }
}
