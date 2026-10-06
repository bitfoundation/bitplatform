using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Card;

/// <summary>
/// Pins the public --bit-Card-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read somewhere with a fallback, none of them is ever declared (so they keep
/// inheriting from :root and the ancestors), and nothing is read that the header does not document.
/// </summary>
[TestClass]
public partial class BitCardStylesheetTests
{
    [TestMethod]
    public void BitCardShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitCardShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitCardShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-Card-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitCardShouldLetAParameterAndTheDisabledStateWinOverThePublicColors()
    {
        var stylesheet = ReadStylesheet();

        // The disabled colors first, then what a parameter asked for, then the public variable, then the theme - all
        // read by the one rule painting the root, so a Classes.Root class still competes with it at equal weight.
        StringAssert.Contains(stylesheet, "color: var(--bit-crd-dis-txt, var(--bit-crd-fg, var(--bit-Card-color, ");
        // The surface and the shadow chains are the shared $crd-bg / $crd-shd the states read as well.
        StringAssert.Contains(stylesheet, "$crd-bg: var(--bit-crd-bg, var(--bit-Card-background, ");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-crd-dis-bg, #{$crd-bg});");
        StringAssert.Contains(stylesheet, "border-color: var(--bit-crd-dis-brd, var(--bit-crd-brd-clr, var(--bit-Card-border-color, ");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-crd-radius, var(--bit-Card-radius, ");
        StringAssert.Contains(stylesheet, "$crd-shd: var(--bit-crd-shd, var(--bit-Card-shadow, ");
        StringAssert.Contains(stylesheet, "box-shadow: $crd-shd;");
    }

    [TestMethod]
    public void BitCardShouldKeepItsOwnLookAwayFromACardNestedInIt()
    {
        var stylesheet = ReadStylesheet();

        var root = SourceFiles.GetScssBlock(stylesheet, "\n.bit-crd {");

        // Every private variable a parameter class or an inline style sets on the root would otherwise be inherited by a
        // card nested inside it - an Outlined card would hand its rule to every card it holds, a Fill card its text color.
        var setByAClass = PrivateDeclaration().Matches(SourceFiles.StripScssComments(stylesheet))
                                              .Select(m => m.Groups[1].Value)
                                              .Distinct()
                                              .Where(name => name is not ("--bit-crd-pad" or "--bit-crd-gap" or "--bit-crd-htx-gap"
                                                                      or "--bit-crd-ttl-fontsize" or "--bit-crd-sub-fontsize"
                                                                      or "--bit-crd-icn-fontsize")) // set by the size class every card wears
                                              .Where(name => name.StartsWith("--bit-crd-clr") is false) // only read where the same class sets them
                                              .ToArray();

        Assert.IsTrue(setByAClass.Length > 0, "No private variable is set by a class.");

        foreach (var name in setByAClass)
        {
            StringAssert.Contains(root, $"{name}: initial;", $"{name} is not reset on the root, so a nested card inherits it.");
        }

        foreach (var name in new[] { "--bit-crd-img-height", "--bit-crd-img-position", "--bit-crd-cvr-width", "--bit-crd-cvr-ratio" })
        {
            StringAssert.Contains(root, $"{name}: initial;", $"{name} is not reset on the root, so a nested card inherits it.");
        }
    }

    [TestMethod]
    public void BitCardShouldLiftToTheThemeHoverElevation()
    {
        var stylesheet = ReadStylesheet();

        // The lift is a design-system decision (Fluent depth8, Material L2, none under Cupertino), so it comes from the
        // theme rather than from a fixed step of the numbered ramp - which under Fluent is the resting depth itself.
        StringAssert.Contains(stylesheet, "var(--bit-Card-hover-shadow, #{$box-shadow-card-hover})");
        Assert.IsFalse(stylesheet.Contains("$box-shadow-8"), "The hover lift is a fixed step of the numbered ramp.");

        // A pressed card settles back to where it rests rather than to a fixed shadow, so a flat card never rises on press.
        StringAssert.Contains(stylesheet, "var(--bit-crd-shd-act, var(--bit-Card-active-shadow, var(--bit-Card-shadow, #{$box-shadow-card})))");
    }

    [TestMethod,
        DataRow("sm"),
        DataRow("md"),
        DataRow("lg")]
    public void BitCardShouldReadItsInsetFromTheTheme(string size)
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, $"\n.bit-crd-{size} {{");

        // The inset is a design-system decision (Fluent 2 8/12/16px), so a preset re-pads every card through the theme.
        StringAssert.Contains(block, $"--bit-crd-pad: var(--bit-Card-padding, #{{$spa-card-{size}}});");
    }

    [TestMethod]
    public void BitCardShouldTintTheTextVariantRatherThanPaintItInTheLightShade()
    {
        var stylesheet = ReadStylesheet();

        // The light shade of a role is a pastel in a dark scheme, which leaves the words in the role color unreadable on it.
        // The tint is the theme's own wash, laid over the surface rather than replacing it, so a Background still shows.
        Assert.IsFalse(stylesheet.Contains("role($tokens, light)"), "A card surface is painted in the role's light shade.");
        StringAssert.Contains(stylesheet, "--bit-crd-clr-tint: #{role($tokens, tint)};");
        StringAssert.Contains(Block(stylesheet, ".bit-crd-vtx"), "background-image: linear-gradient(var(--bit-crd-clr-tint), var(--bit-crd-clr-tint));");
    }

    [TestMethod,
        DataRow(".bit-crd-vot"),
        DataRow(".bit-crd-vtx")]
    public void BitCardShouldWriteAnUnfilledVariantInTheReadableShadeOfItsRole(string variant)
    {
        var stylesheet = ReadStylesheet();

        // A role's main color is picked to be a fill - a warning amber is under 2:1 on white - so the words of a card that
        // is not filled read the role's foreground shade, which the theme keeps readable on the page.
        StringAssert.Contains(stylesheet, "--bit-crd-clr-fg: #{role($tokens, fg)};");
        StringAssert.Contains(Block(stylesheet, variant), "--bit-crd-fg: var(--bit-crd-clr-fg);");
    }

    [TestMethod]
    public void BitCardShouldShadeACardThatIsAControlUnderThePointerAndThePress()
    {
        var stylesheet = ReadStylesheet();

        // A lift alone is lost on a flat card and absent under a design system with no shadows, so a control also shades
        // its surface: in its role's own shades when it is filled, otherwise the public variable, otherwise a wash of the
        // text color over whatever the card rests on.
        StringAssert.Contains(stylesheet, "$crd-bg: var(--bit-crd-bg, var(--bit-Card-background, #{$clr-bg-sec}));");
        StringAssert.Contains(stylesheet, "$crd-bg-hover: var(--bit-crd-bg-hov, var(--bit-Card-hover-background, color-mix(in srgb, currentcolor 5%, #{$crd-bg})));");
        StringAssert.Contains(stylesheet, "$crd-bg-active: var(--bit-crd-bg-act, var(--bit-Card-active-background, color-mix(in srgb, currentcolor 10%, #{$crd-bg})));");

        var fill = Block(stylesheet, ".bit-crd-vfl");
        StringAssert.Contains(fill, "--bit-crd-bg-hov: var(--bit-crd-clr-hover);");
        StringAssert.Contains(fill, "--bit-crd-bg-act: var(--bit-crd-clr-active);");

        // Only a control is shaded: a Hoverable card reacts to the pointer without being one, and only lifts.
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"var\(--bit-Card-hover-background,").Count);
        StringAssert.Contains(stylesheet, "    .bit-crd-int:hover {\n        background-color: $crd-bg-hover;");
    }

    [TestMethod]
    public void BitCardShouldReadItsSurfaceChainsFromOnePlace()
    {
        var stylesheet = ReadStylesheet();

        // The resting surface is written once and read by the root and by every state, so a change to the default
        // surface cannot leave hover and press shading a different color from the card at rest.
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"var\(--bit-crd-bg, ").Count, "The resting surface chain is written more than once.");
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"var\(--bit-crd-shd, ").Count, "The resting shadow chain is written more than once.");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-crd-dis-bg, #{$crd-bg});");
        StringAssert.Contains(stylesheet, "box-shadow: $crd-shd;");
    }

    [TestMethod]
    public void BitCardShouldOnlyBePressedThroughItsOwnControl()
    {
        var stylesheet = ReadStylesheet();

        // A card that is a button is pressed through its root, in a rule that needs no :has, so a browser that cannot
        // read :has keeps the press of a root that is the control rather than dropping it with the rest of a list.
        StringAssert.Contains(stylesheet, "\n.bit-crd-btn:active {\n    box-shadow: $crd-shd-active;\n    background-color: $crd-bg-active;\n}");

        // A linked card is pressed through its anchor alone, since :active on the root would also be the press of a
        // control in its header or its footer.
        StringAssert.Contains(stylesheet, "\n.bit-crd-int:has(> .bit-crd-lnk:active) {\n    box-shadow: $crd-shd-active;");
        Assert.IsFalse(stylesheet.Contains("\n.bit-crd-int:active"), "A linked card is pressed by any control inside it.");

        // ...and pressing one of the controls in the slots of a card that is a button leaves the card where it is.
        StringAssert.Contains(stylesheet, "\n.bit-crd-btn:active:has(> .bit-crd-fac:active, > .bit-crd-mai > .bit-crd-hdr > .bit-crd-act:active, > .bit-crd-mai > .bit-crd-ftr:active) {\n    box-shadow: $crd-shd;");
    }

    [TestMethod]
    public void BitCardStretchedButtonShouldCoverTheCardAndLetThePointerThrough()
    {
        var stylesheet = ReadStylesheet();

        // A button whose width is auto fits its content instead of stretching between its insets, and this one has
        // none, so the size is spelled out or the focus ring drawn inside it collapses to nothing.
        var overlay = Block(stylesheet, ".bit-crd-lnk");
        StringAssert.Contains(overlay, "inset: 0;");
        StringAssert.Contains(overlay, "width: 100%;");
        StringAssert.Contains(overlay, "height: 100%;");

        // The root answers a click anywhere on the card, so the button is left to the keyboard and lets the pointer
        // reach the body: its controls, its text and its scrollbar. The anchor of a linked card keeps the pointer.
        StringAssert.Contains(Block(stylesheet, "button.bit-crd-lnk"), "pointer-events: none;");
    }

    [TestMethod]
    public void BitCardShouldOnlyPaintTheScrimOverAnOverlaidCover()
    {
        var stylesheet = ReadStylesheet();

        var block = SourceFiles.GetScssBlock(stylesheet, "\n.bit-crd-ovl {");

        StringAssert.Contains(block, "background: var(--bit-Card-scrim, none);");
        Assert.AreEqual(1, Regex.Matches(stylesheet, @"--bit-Card-scrim,").Count, "The scrim is painted outside the overlaid cover.");
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string Block(string stylesheet, string selector)
    {
        return SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");
    }

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Card", "BitCard.scss");

    [GeneratedRegex(@"^//\s+(--bit-Card-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Card-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Card-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    // A private variable given a value other than the reset, anywhere in the rules.
    [GeneratedRegex(@"^\s*(--bit-crd-[a-z-]+):\s*(?!initial;)", RegexOptions.Multiline)]
    private static partial Regex PrivateDeclaration();
}
