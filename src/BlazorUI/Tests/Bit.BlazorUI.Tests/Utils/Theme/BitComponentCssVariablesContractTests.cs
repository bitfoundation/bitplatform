using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Pins the per-component CSS variable vocabulary (<c>--bit-&lt;cmp&gt;-*</c>) as stable public
/// API. The checked-in inventory (<c>component-css-variables.md</c>, next to this test) is regenerated
/// here from the component SCSS sources and compared verbatim: adding, renaming, or removing a
/// component variable fails this test once and rewrites the inventory in place - reviewing and
/// committing that diff is the documented signal that the override surface changed for consumers.
/// </summary>
[TestClass]
public sealed class BitComponentCssVariablesContractTests
{
    private static readonly Regex CssVarDeclaration = new(
        @"^\s*(--bit-[a-zA-Z0-9-]+)\s*:",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [TestMethod]
    public void CheckedInInventoryMatchesTheComponentStyles()
    {
        var generated = GenerateInventory(GetComponentStylesDirectory());

        var inventoryPath = Path.Combine(GetTestSourceDirectory(), "component-css-variables.md");
        Assert.IsTrue(File.Exists(inventoryPath), $"Missing {inventoryPath}.");

        var checkedIn = Normalize(File.ReadAllText(inventoryPath));

        if (!string.Equals(checkedIn, generated, StringComparison.Ordinal))
        {
            RefreshInventory(inventoryPath, generated);

            Assert.Fail(
                "The component CSS variable inventory had drifted from the SCSS sources and has been " +
                $"refreshed in place ({inventoryPath}). These names are documented public API: if the " +
                "change is intentional, review the diff, commit it and note the change in the release " +
                "notes; otherwise revert the SCSS rename.");
        }
    }

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

    private static void RefreshInventory(string inventoryPath, string content)
    {
        // The three target frameworks run this test concurrently over the same checkout; write via a
        // unique temp file and treat losing the race to a sibling (writing identical content) as success.
        var tempPath = $"{inventoryPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, inventoryPath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
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

    private static string GenerateInventory(string stylesDir)
    {
        var byComponent = CollectDeclarations(stylesDir);

        var builder = new StringBuilder();
        builder.Append("# Component CSS variables\n\n");
        builder.Append("<!-- GENERATED FILE - do not edit by hand. Regenerated and verified by\n");
        builder.Append("     BitComponentCssVariablesContractTests; on an intentional change, run that test once - it\n");
        builder.Append("     rewrites this file in place - then review the diff and commit it. -->\n\n");
        builder.Append("Every bit BlazorUI component exposes its themable knobs as `--bit-<cmp>-*` CSS custom\n");
        builder.Append("properties, assigned on the component root by its role/variant classes and consumed by its\n");
        builder.Append("internal selectors. They are **stable public API**: override them from app CSS\n");
        builder.Append("(`.bit-btn { --bit-btn-clr: hotpink; }`) or per instance via the `Style` parameter\n");
        builder.Append("(`Style=\"--bit-btn-clr: hotpink\"`), and rely on this contract-tested inventory when upgrading.\n\n");

        var total = byComponent.Sum(kv => kv.Value.Count);
        builder.Append(FormattableString.Invariant($"{byComponent.Count} components, {total} variables.\n"));

        foreach (var (component, names) in byComponent)
        {
            builder.Append(FormattableString.Invariant($"\n## {component}\n\n"));
            foreach (var name in names)
            {
                builder.Append(FormattableString.Invariant($"- `{name}`\n"));
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
