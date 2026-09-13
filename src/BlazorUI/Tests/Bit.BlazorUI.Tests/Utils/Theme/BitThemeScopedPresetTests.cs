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

    // The same contract for the presets that ship with Bit.BlazorUI.Extras. Their token files are
    // listed too: unlike the core Fluent ones they are NOT scheme-agnostic-by-inheritance, because
    // an element carrying bit-theme="material" inherits the DOCUMENT's Fluent values for everything
    // the preset does not re-declare on that very element.
    private static readonly string[] ExtraPresetFiles =
    [
        Path.Combine("Fluent2", "colors.fluent2-light.scss"),
        Path.Combine("Fluent2", "colors.fluent2-dark.scss"),
        Path.Combine("Fluent2", "shadows.fluent2-light.scss"),
        Path.Combine("Fluent2", "shadows.fluent2-dark.scss"),
        Path.Combine("Fluent2", "tokens.fluent2.scss"),
        Path.Combine("Material", "colors.material-light.scss"),
        Path.Combine("Material", "colors.material-dark.scss"),
        Path.Combine("Material", "tokens.material.scss"),
        Path.Combine("Cupertino", "colors.cupertino-light.scss"),
        Path.Combine("Cupertino", "colors.cupertino-dark.scss"),
        Path.Combine("Cupertino", "tokens.cupertino.scss"),
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

    [TestMethod]
    public void EveryRootPresetSelectorHasAScopedTwinInTheExtraPresetBundles()
    {
        foreach (var file in ExtraPresetFiles)
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

            Assert.IsTrue(rootNames.Length > 0, $"{file} declares no :root[bit-theme=…] preset selectors.");
            CollectionAssert.AreEqual(rootNames, scopedNames,
                $"{file}: every :root[bit-theme=\"X\"] selector must have a ':root [bit-theme=\"X\"]' descendant " +
                $"twin (and vice versa) so the preset is scopeable to a subtree. Root: [{string.Join(", ", rootNames)}] " +
                $"Scoped: [{string.Join(", ", scopedNames)}]");
        }
    }

    [TestMethod]
    public void PackagedExtraPresetNamesAreCoveredByScopedTwins()
    {
        var allScss = string.Join("\n", ExtraPresetFiles.Select(ReadStylesFile));

        var scopedNames = ScopedPresetSelector.Matches(allScss)
            .Select(m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var preset in new[]
        {
            BitExtraThemePresets.Fluent2, BitExtraThemePresets.Fluent2Light, BitExtraThemePresets.Fluent2Dark,
            BitExtraThemePresets.Material, BitExtraThemePresets.MaterialLight, BitExtraThemePresets.MaterialDark,
            BitExtraThemePresets.Cupertino, BitExtraThemePresets.CupertinoLight, BitExtraThemePresets.CupertinoDark,
        })
        {
            Assert.IsTrue(scopedNames.Contains(preset),
                $"Preset '{preset}' has no ':root [bit-theme=\"{preset}\"]' scoped selector in the Extras preset bundles.");
        }
    }

    [TestMethod]
    public void MotionAliasesAreReDeclaredOnScopedPresetElements()
    {
        // The plain --bit-mot-* tokens alias the -full ones, and a preset retunes only the -full
        // literals. Declared on :root alone the aliases would freeze the DOCUMENT's durations inside
        // an element carrying bit-theme="material", whose own -full tokens say something else. The
        // twin is wrapped in :where() so it adds no specificity: the reduced-motion query must still
        // win on source order, and .bit-fam (the ForceAnimation opt-out) must still outrank both.
        var scss = ReadStylesFile(Path.Combine("Fluent", "motion.fluent.scss"));

        var twins = Regex.Matches(scss, @":where\(:root \[bit-theme\]\)").Count;
        Assert.AreEqual(2, twins,
            "motion.fluent.scss must carry the ':where(:root [bit-theme])' scoped twin twice - once on the " +
            "block that aliases the -full tokens, and once inside the prefers-reduced-motion query so the " +
            "collapse reaches a scoped preset region as well.");

        var reducedMotion = scss[scss.IndexOf("prefers-reduced-motion", StringComparison.Ordinal)..];
        Assert.IsTrue(reducedMotion.Contains(":where(:root [bit-theme])", StringComparison.Ordinal),
            "The prefers-reduced-motion block must carry the scoped twin too, or a scoped preset region " +
            "would keep its unreduced durations.");
    }

    [TestMethod]
    public void FocusRingCompositeIsReDeclaredOnScopedPresetElements()
    {
        // --bit-shd-focus-ring composes four other tokens, so it is substituted where it is DECLARED.
        // On the palette selectors alone, a region carrying a preset those selectors do not name (any
        // Extras preset) would draw the ring's inner separator in the document's background color
        // rather than in its own - a white halo inside a scoped dark region.
        var scss = ReadStylesFile(Path.Combine("Fluent", "shapes.fluent.scss"));

        var declaration = scss.IndexOf("--bit-shd-focus-ring:", StringComparison.Ordinal);
        Assert.IsTrue(declaration > 0, "shapes.fluent.scss no longer declares --bit-shd-focus-ring.");

        var block = scss[..declaration];
        var selector = block[(block.LastIndexOf('}') + 1)..];
        Assert.IsTrue(selector.Contains(":root [bit-theme]", StringComparison.Ordinal),
            "The block declaring --bit-shd-focus-ring must carry the ':root [bit-theme]' scoped twin so the " +
            "composite is re-substituted against a scoped preset region's own palette.");
    }
}
