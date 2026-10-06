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

        CollectionAssert.AreEquivalent(PublicVariables, read);

        foreach (var variable in PublicVariables)
        {
            Assert.IsFalse(Regex.IsMatch(stylesheet, $@"^\s*{variable}\s*:", RegexOptions.Multiline), $"{variable} is declared, so it no longer inherits.");
            Assert.IsTrue(Regex.IsMatch(stylesheet, $@"var\({variable}, [^)]"), $"{variable} is read without a fallback.");
            StringAssert.Contains(stylesheet, $"//   {variable} ", $"{variable} is missing from the header of the stylesheet.");
        }
    }

    [TestMethod]
    public void BitMapShouldListEveryPublicVariableOnItsDemoPage()
    {
        var demo = SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "Map", "BitMapDemo.razor.cs");

        var listed = Regex.Matches(demo, @"Name = ""(--bit-Map-[a-z-]+)""").Select(m => m.Groups[1].Value).ToArray();

        CollectionAssert.AreEquivalent(PublicVariables, listed);
    }

    // The controls the map draws itself are drawn with the ring every control of the library uses (WCAG 2.4.7).
    [TestMethod,
        DataRow(".bit-map-popup-close"),
        DataRow(".bit-map-marker-table-action"),
        DataRow(".bit-map-ol-popup__close")]
    public void BitMapShouldDrawTheSharedFocusRingOnItsOwnControls(string selector)
    {
        var block = ReadBlock(selector);

        StringAssert.Contains(block, "&:focus-visible {");
        StringAssert.Contains(block, "@include focus-ring;");
    }

    // WCAG 2.5.8: the popup's close button sits right against its content, so it gets the 24px minimum target.
    [TestMethod]
    public void BitMapShouldGiveThePopupCloseButtonTheMinimumTargetSize()
    {
        var block = ReadBlock(".bit-map-popup-close");

        StringAssert.Contains(block, "min-width: rem2(24px);");
        StringAssert.Contains(block, "min-height: rem2(24px);");
    }

    // A loader slows down under reduced motion rather than stopping, and the motion tokens are what do that - and what
    // ForceAnimation restores. A media query of the map's own would stop it outright and ignore ForceAnimation.
    [TestMethod]
    public void BitMapShouldTakeItsMotionFromTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(ReadBlock(".bit-map-status-spinner"), "animation: bit-map-spin $mot-duration-spinner $mot-easing-spinner infinite;");
        StringAssert.Contains(ReadBlock(".bit-map-gesture-hint"), "$mot-duration $mot-easing");
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

        StringAssert.Contains(block, "color: var(--bit-Map-cluster-color, ");
        StringAssert.Contains(block, "background-color: var(--bit-Map-cluster-background, ");
        StringAssert.Contains(block, "border: 0 solid var(--bit-Map-cluster-border-color, ");
        StringAssert.Contains(block, "fill: var(--bit-Map-marker-color, ");
        StringAssert.Contains(block, "stroke: var(--bit-Map-vector-color, ");
    }



    // The tile filter is what turns a light basemap dark, so it must reach the raster tiles alone: on the whole canvas
    // it would invert the markers, the shapes and the cluster bubbles drawn on top of them too.
    [TestMethod]
    public void BitMapShouldFilterTheRasterTilesAlone()
    {
        var stylesheet = ReadStylesheet();

        StringAssert.Contains(stylesheet, ".leaflet-tile-pane,\n    .bit-map-ol-tiles {\n        filter: var(--bit-Map-tile-filter, none);");
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"var\(--bit-Map-tile-filter").Count);

        var openLayers = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Map", "Providers", "BitMapOpenLayers.ts");

        Assert.AreEqual(3, Regex.Matches(openLayers, @"_tileClassName\b").Count, "Both the base layer and the tile overlays must render into the filtered canvas.");
        StringAssert.Contains(openLayers, "_tileClassName = 'ol-layer bit-map-ol-tiles'");
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

        Assert.AreEqual(1, Regex.Matches(script, @"opts = BitMapHelpers\.withDefaultIcon\(id, opts\);").Count);
        Assert.AreEqual(5, Regex.Matches(script, @"style = BitMapHelpers\.resolvePathStyle\(id, style\);").Count);
        Assert.IsFalse(script.Contains("hexToRgba", StringComparison.Ordinal), "A hex-only parser misreads every other CSS color.");
    }

    private static string ReadBlock(string selector) => SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} {{");

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "Map", "BitMap.scss");
}
