using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.AccentColorSwitcher;

/// <summary>
/// Pins what the switcher's stylesheet promises the C# side and the demo page, which a bUnit render cannot see: the
/// swatch paints from the private --bit-acs-clr the markup writes (reset on the root so a nested instance never
/// inherits one), the neutral swatch's --bit-acs-ntr literal is the very preset the C# falls back to and is retuned
/// for Cupertino alone, and no public --bit-AccentColorSwitcher-* variable is read or declared - the demo page
/// documents none, so there is none to document.
/// </summary>
[TestClass]
public partial class BitAccentColorSwitcherStylesheetTests
{
    [TestMethod]
    public void BitAccentColorSwitcherShouldPaintTheSwatchFromThePrivateColorPropertyTheMarkupWrites()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.Contains("$acs-private-properties: clr;", stylesheet);
        Assert.Contains("--bit-acs-#{$name}: initial;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-acs {"));
        Assert.Contains("background-color: var(--bit-acs-clr);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-acs-swt {"));
        Assert.Contains("var(--bit-acs-clr)", SourceFiles.GetScssBlock(stylesheet, "\n.bit-acs-act {"));
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldPaintTheNeutralSwatchWithThePresetTheCSharpFallsBackTo()
    {
        // GetSwatchColor paints the neutral swatch as var(--bit-acs-ntr, BitAccentColorPresets.Blue); the stylesheet's
        // default of the property has to be that same blue, or a page loading the bundle shows another color than
        // one that does not.
        var root = SourceFiles.GetScssBlock(SourceFiles.StripScssComments(ReadStylesheet()), "\n.bit-acs {");

        var declared = Regex.Match(root, @"--bit-acs-ntr:\s*(#[0-9A-Fa-f]{6});").Groups[1].Value;

        Assert.AreEqual(BitAccentColorPresets.Blue, declared, StringComparer.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldRetuneTheNeutralSwatchForCupertinoAlone()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        // Matched on the prefix so the scheme suffix and a scoped preset both count.
        Assert.Contains("[bit-theme^=\"cupertino\"] & {", stylesheet);

        var retunes = Regex.Matches(stylesheet, @"--bit-acs-ntr:").Count;
        Assert.AreEqual(2, retunes, "The neutral color is the default plus the one packaged preset far enough away for the difference to show.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldNeitherReadNorDeclareAPublicVariable()
    {
        var stylesheet = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(PublicVariable().IsMatch(stylesheet),
            "A public --bit-AccentColorSwitcher-* variable has to be documented in the stylesheet header and on the demo page's componentCssVariables table.");
    }

    [TestMethod]
    public void BitAccentColorSwitcherShouldDimTheSwatchesOfADisabledSwitcherFromTheRootClass()
    {
        var disabled = SourceFiles.GetScssBlock(SourceFiles.StripScssComments(ReadStylesheet()), "\n.bit-acs.bit-dis {");

        Assert.Contains("opacity: $opa-dis;", disabled);
        Assert.Contains("cursor: default;", disabled);
    }

    private static string ReadStylesheet()
    {
        return SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "AccentColorSwitcher", "BitAccentColorSwitcher.scss");
    }

    [GeneratedRegex(@"--bit-AccentColorSwitcher-[a-z-]+")]
    private static partial Regex PublicVariable();
}
