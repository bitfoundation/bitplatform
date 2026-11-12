using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Map;

/// <summary>
/// Pins the parts of the map that live in its stylesheet, which a bUnit render cannot see: the public CSS variables
/// (each read with a fallback and never declared, listed in the header of the stylesheet and in the table of the demo
/// page), the focus rings of its own controls, and the theme tokens it reads instead of literals.
/// </summary>
[TestClass]
public class BitMapStylesheetTests
{
    private static readonly string[] PublicVariables =
    [
        "--bit-Map-height",
        "--bit-Map-background",
        "--bit-Map-tile-filter",
        "--bit-Map-border",
        "--bit-Map-radius",
        "--bit-Map-focus-color",
        "--bit-Map-disabled-opacity",
        "--bit-Map-popup-background",
        "--bit-Map-popup-color",
        "--bit-Map-popup-radius",
        "--bit-Map-popup-shadow",
        "--bit-Map-popup-padding",
        "--bit-Map-popup-max-width",
        "--bit-Map-tooltip-background",
        "--bit-Map-tooltip-color",
        "--bit-Map-hint-background",
        "--bit-Map-hint-color",
        "--bit-Map-marker-color",
        "--bit-Map-vector-color",
        "--bit-Map-cluster-background",
        "--bit-Map-cluster-color",
        "--bit-Map-cluster-border-color",
    ];

    [TestMethod]
    public void BitMapShouldReadEveryPublicVariableWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(stylesheet, @"var\((--bit-Map-[a-z-]+)").Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.AreSequenceEqual(PublicVariables, read, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({variable}, [^)]"), $"{variable} is read without a fallback.");
            Assert.Contains($"//   {variable} ", stylesheet, $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitMapShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "Map", "BitMapDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Map-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        Assert.AreSequenceEqual(PublicVariables, listed, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    // The controls the map draws itself are drawn with the ring every control of the library uses (WCAG 2.4.7).
    [TestMethod,
        DataRow(".bit-map-popup-close"),
        DataRow(".bit-map-marker-table-action"),
        DataRow(".bit-map-ol-popup__close")]
    public void BitMapShouldDrawTheSharedFocusRingOnItsOwnControls(string selector)
    {
        var block = ReadBlock(selector);

        Assert.Contains("&:focus-visible {", block);
        Assert.Contains("@include focus-ring;", block);
    }

    // WCAG 2.5.8: the popup's close button sits right against its content, so it gets the 24px minimum target.
    [TestMethod]
    public void BitMapShouldGiveThePopupCloseButtonTheMinimumTargetSize()
    {
        var block = ReadBlock(".bit-map-popup-close");

        Assert.Contains("min-width: rem2(24px);", block);
        Assert.Contains("min-height: rem2(24px);", block);
    }

    // The anchor is moved by the point a provider projects, which is in pixels from the container's top-left corner in
    // every writing direction, so it starts there too: an inline-start origin is the right edge on a right-to-left map,
    // and puts the popup a map's width away from its marker (#13592). Every rule reaching the anchor is read declaration
    // by declaration: the one offset it may set is the physical top-left one, and a mixin, whose output the source does
    // not show, may not set it at all. Where the popup lands is BitDirResolvedBrowserTests' to check.
    [TestMethod]
    public void BitMapShouldAnchorThePopupToTheTopLeftCornerInEveryDirection()
    {
        var rules = SourceFiles.GetScssRules(ReadStylesheet())
                               .Where(r => r.Header.Contains(".bit-map-anchor") || r.Ancestors.Any(a => a.Contains(".bit-map-anchor")))
                               .ToList();

        var own = rules.Single(r => r.Header == ".bit-map-anchor" && r.Ancestors.Count == 0);
        CollectionAssert.IsSubsetOf(new[] { "position: absolute", "top: 0", "left: 0" }, own.Declarations.ToArray());

        foreach (var declaration in rules.SelectMany(r => r.Declarations))
        {
            Assert.IsFalse(declaration.StartsWith('@'), $"The anchor's origin is hidden behind a mixin or an at-rule: {declaration}");

            var property = declaration.Split(':')[0].Trim();
            if (Regex.IsMatch(property, @"^(inset(-.+)?|top|right|bottom|left)$") is false) continue;

            Assert.IsTrue(declaration is "top: 0" or "left: 0", $"The anchor is placed by another offset than its physical top-left corner: {declaration}");
        }
    }

    // A loader slows down under reduced motion rather than stopping, and the motion tokens are what do that - and what
    // ForceAnimation restores. A media query of the map's own would stop it outright and ignore ForceAnimation.
    [TestMethod]
    public void BitMapShouldTakeItsMotionFromTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        // The shared ring turns on the spinner tokens (BitSpinnerRingStylesheetTests pins the mixin itself).
        Assert.Contains("@include spinner-ring(", ReadBlock(".bit-map-status-spinner"));
        Assert.Contains("$mot-duration $mot-easing", ReadBlock(".bit-map-gesture-hint"));
        Assert.DoesNotContain("prefers-reduced-motion", stylesheet);
    }

    // A design-system decision is read off the theme, never typed into the component.
    [TestMethod]
    public void BitMapShouldNotHardCodeAColor()
    {
        var stylesheet = ReadStylesheet();

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"#[0-9a-f]{3,8}\b", RegexOptions.IgnoreCase), "A literal color is used instead of a theme token.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"rgba?\(", RegexOptions.IgnoreCase), "A literal color is used instead of a theme token.");
    }

    // The pins and the cluster bubbles are images and the shapes are drawn by the provider, so their colors are read off
    // a probe the script queries; the probe has to carry all five of them.
    [TestMethod]
    public void BitMapShouldResolveTheDrawnColorsOffItsProbe()
    {
        var block = ReadBlock(".bit-map-probe");

        Assert.Contains("color: var(--bit-Map-cluster-color, ", block);
        Assert.Contains("background-color: var(--bit-Map-cluster-background, ", block);
        Assert.Contains("border: 0 solid var(--bit-Map-cluster-border-color, ", block);
        Assert.Contains("fill: var(--bit-Map-marker-color, ", block);
        Assert.Contains("stroke: var(--bit-Map-vector-color, ", block);
    }



    // The tile filter is what turns a light basemap dark, so it must reach the raster tiles alone: on the whole canvas
    // it would invert the markers, the shapes and the cluster bubbles drawn on top of them too.
    [TestMethod]
    public void BitMapShouldFilterTheRasterTilesAlone()
    {
        var stylesheet = ReadStylesheet();

        Assert.Contains(".leaflet-tile-pane,\n    .bit-map-ol-tiles {\n        filter: var(--bit-Map-tile-filter, none);", stylesheet);
        Assert.ContainsSingle(Regex.Matches(stylesheet, @"var\(--bit-Map-tile-filter"));

        var openLayers = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Map", "Providers", "BitMapOpenLayers.ts");

        Assert.HasCount(3, Regex.Matches(openLayers, @"_tileClassName\b"), "Both the base layer and the tile overlays must render into the filtered canvas.");
        Assert.Contains("_tileClassName = 'ol-layer bit-map-ol-tiles'", openLayers);
    }

    // The script half of the probe: every provider draws a marker without an icon as the same themed pin, and resolves a
    // shape's colors - the theme's, or any CSS color the caller wrote - before its library sees them, so none of them
    // falls back to a fixed color of its own or misreads a color that is not hex.
    [TestMethod,
        DataRow("BitMapLeaflet.ts"),
        DataRow("BitMapGlBase.ts"),
        DataRow("BitMapOpenLayers.ts"),
        DataRow("BitMapArcGis.ts"),
        DataRow("BitMapAzureMaps.ts"),
        DataRow("BitMapCesium.ts")]
    public void BitMapProvidersShouldDrawInTheThemesColors(string file)
    {
        var script = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Map", "Providers", file);

        Assert.ContainsSingle(Regex.Matches(script, @"opts = BitMapHelpers\.withDefaultIcon\(id, opts\);"));
        Assert.HasCount(5, Regex.Matches(script, @"style = BitMapHelpers\.resolvePathStyle\(id, style\);"));
        Assert.IsFalse(script.Contains("hexToRgba", StringComparison.Ordinal), "A hex-only parser misreads every other CSS color.");
    }

    private static string ReadBlock(string selector) => SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} {{");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Map", "BitMap.scss");
}
