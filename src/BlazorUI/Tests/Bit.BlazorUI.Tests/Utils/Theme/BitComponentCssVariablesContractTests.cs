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
