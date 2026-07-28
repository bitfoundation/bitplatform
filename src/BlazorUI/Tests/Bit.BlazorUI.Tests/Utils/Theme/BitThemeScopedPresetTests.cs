using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Contract for scoped presets: any element carrying a <c>bit-theme</c> attribute re-themes its
/// subtree with the named packaged palette (an always-dark hero inside a light page, a per-tenant
/// region, …). This works because every scheme-bearing palette block declares each root selector
/// (<c>:root[bit-theme="X"]</c>) together with a same-specificity descendant twin
/// (<c>:root [bit-theme="X"]</c>), and the semantic alias tier re-declares its <c>var()</c>
/// references on scoped elements (a custom property's references are substituted at the element
/// that defines it, so an alias declared only on <c>:root</c> would freeze the document palette).
/// These tests pin both halves so a palette edit cannot silently drop the scoped support.
/// </summary>
[TestClass]
public sealed class BitThemeScopedPresetTests
{
    // The files whose values differ per scheme - the ones scoped presets depend on. The
    // scheme-agnostic files (shapes, typography, neutrals, motion) declare identical values for
    // every preset, so plain inheritance from :root already covers scoped regions there.
    private static readonly string[] SchemeBearingFiles =
    [
        Path.Combine("Fluent", "colors.fluent-light.scss"),
        Path.Combine("Fluent", "colors.fluent-dark.scss"),
        Path.Combine("Fluent", "shadows.fluent-light.scss"),
        Path.Combine("Fluent", "shadows.fluent-dark.scss"),
    ];

    private static readonly Regex RootPresetSelector = new(
        @":root\[bit-theme=""(?<name>[a-z0-9-]+)""\]",
        RegexOptions.Compiled);

    private static readonly Regex ScopedPresetSelector = new(
        @":root \[bit-theme=""(?<name>[a-z0-9-]+)""\]",
        RegexOptions.Compiled);

    private static string ReadStylesFile(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "theme-styles", relativePath);
        Assert.IsTrue(File.Exists(path), $"Missing {path}; ensure the library Styles folder is copied to output.");
        return File.ReadAllText(path);
    }

    [TestMethod]
    public void EveryRootPresetSelectorHasAScopedTwinInSchemeBearingFiles()
    {
        foreach (var file in SchemeBearingFiles)
        {
            var scss = ReadStylesFile(file);

            var rootNames = RootPresetSelector.Matches(scss)
                .Select(m => m.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            var scopedNames = ScopedPresetSelector.Matches(scss)
                .Select(m => m.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            Assert.IsTrue(rootNames.Length > 0, $"{file} declares no :root[bit-theme=…] palette selectors.");
            CollectionAssert.AreEqual(rootNames, scopedNames,
                $"{file}: every :root[bit-theme=\"X\"] selector must have a ':root [bit-theme=\"X\"]' descendant " +
                $"twin (and vice versa) so the palette is scopeable to a subtree. Root: [{string.Join(", ", rootNames)}] " +
                $"Scoped: [{string.Join(", ", scopedNames)}]");
        }
    }

    [TestMethod]
    public void SemanticAliasesAreReDeclaredOnScopedPresetElements()
    {
        var scss = ReadStylesFile("semantic-tokens.scss");

        Assert.IsTrue(scss.Contains(":root [bit-theme]", StringComparison.Ordinal),
            "semantic-tokens.scss must re-declare the alias tier on ':root [bit-theme]' (any scoped-preset " +
            "element). Without it, aliases inherited from :root carry the document palette's already-substituted " +
            "values and ignore the scoped region's re-themed primitives.");
    }

    [TestMethod]
    public void PackagedPresetNamesAreCoveredByScopedTwins()
    {
        // The BitThemePresets vocabulary (minus the "system" pseudo-preset, which never reaches the
        // bit-theme attribute as-is) must be scopeable: each name appears as a scoped twin in at
        // least one scheme-bearing palette file.
        var allScss = string.Join("\n", SchemeBearingFiles.Select(ReadStylesFile));

        var scopedNames = ScopedPresetSelector.Matches(allScss)
            .Select(m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var preset in new[] { BitThemePresets.Light, BitThemePresets.Dark, BitThemePresets.Fluent, BitThemePresets.FluentLight, BitThemePresets.FluentDark })
        {
            Assert.IsTrue(scopedNames.Contains(preset),
                $"Preset '{preset}' has no ':root [bit-theme=\"{preset}\"]' scoped selector in the scheme-bearing palette files.");
        }
    }
}
