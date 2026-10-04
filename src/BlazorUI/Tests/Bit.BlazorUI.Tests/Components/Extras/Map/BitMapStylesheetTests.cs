using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        "--bit-Map-background",
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
        var demo = ReadFile("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "Map", "BitMapDemo.razor.cs");

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

    // The cluster bubbles are images, so their colors are read off a probe the clustering layer queries; the probe has
    // to carry all three of them.
    [TestMethod]
    public void BitMapShouldResolveTheClusterColorsOffItsProbe()
    {
        var block = ReadBlock(".bit-map-cluster-probe");

        StringAssert.Contains(block, "color: var(--bit-Map-cluster-color, ");
        StringAssert.Contains(block, "background-color: var(--bit-Map-cluster-background, ");
        StringAssert.Contains(block, "border: 0 solid var(--bit-Map-cluster-border-color, ");
    }



    private static string ReadBlock(string selector)
    {
        var stylesheet = ReadStylesheet();

        var start = stylesheet.IndexOf($"\n{selector} {{", StringComparison.Ordinal);

        Assert.IsTrue(start >= 0, $"{selector} has no rule of its own.");

        var block = stylesheet[start..];

        return block[..block.IndexOf("\n}", StringComparison.Ordinal)];
    }

    private static string ReadStylesheet() => ReadFile("Bit.BlazorUI.Extras", "Components", "Map", "BitMap.scss");

    private static string ReadFile(params string[] segments) => ReadFileFrom(segments);

    private static string ReadFileFrom(string[] segments, [CallerFilePath] string thisFile = "")
    {
        var path = Path.GetFullPath(Path.Combine([Path.GetDirectoryName(thisFile)!, "..", "..", "..", "..", "..", .. segments]));

        Assert.IsTrue(File.Exists(path), $"Missing {path}.");

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
