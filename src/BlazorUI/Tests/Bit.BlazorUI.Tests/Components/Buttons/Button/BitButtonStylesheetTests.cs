using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Buttons.Button;

/// <summary>
/// Pins the public --bit-Button-* variables against the stylesheet that reads them, which a bUnit render cannot see:
/// every variable the header documents is read with a fallback and never declared (so it keeps inheriting from :root
/// and the ancestors), and each one ranks below the parameter written on the button itself.
/// </summary>
[TestClass]
public partial class BitButtonStylesheetTests
{
    [TestMethod]
    public void BitButtonShouldReadEveryPublicVariableItDocumentsWithoutDeclaringIt()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();

        Assert.IsTrue(documented.Length > 0, "The stylesheet documents no public variable.");
        CollectionAssert.AreEquivalent(documented, read, "The variables the header documents are not the ones the stylesheet reads.");

        foreach (var name in documented)
        {
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
        }

        Assert.IsFalse(DeclaredVariable().IsMatch(SourceFiles.StripScssComments(stylesheet)), "A public --bit-Button-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitButtonShouldLetAParameterWinOverItsPublicVariable()
    {
        var stylesheet = ReadStylesheet();

        // An explicit Size publishes these, so they are read before the variable, which only restyles the medium
        // button an unset one stands for.
        StringAssert.Contains(stylesheet, "padding: var(--bit-btn-padding, var(--bit-Button-padding, #{$siz-ctrl-pad-y-md} #{$siz-ctrl-pad-x-md}));");
        StringAssert.Contains(stylesheet, "min-height: var(--bit-btn-min-height, var(--bit-Button-min-height, #{$siz-ctrl-md}));");
        StringAssert.Contains(stylesheet, "gap: var(--bit-btn-gap, var(--bit-Button-gap, #{spacing(1)}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-btn-prt-fontsize, var(--bit-Button-font-size, #{$tg-fs-sm}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-btn-sct-fontsize, var(--bit-Button-secondary-font-size, #{$tg-fs-xs}));");
        StringAssert.Contains(stylesheet, "font-size: var(--bit-btn-icn-size, var(--bit-Button-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(stylesheet, "var(--bit-btn-spn-size, var(--bit-Button-spinner-size, #{$siz-icon-md}))");
        StringAssert.Contains(stylesheet, "padding: var(--bit-btn-pad-ntx, var(--bit-Button-padding, #{$siz-ctrl-pad-y-md}));");

        // Rounded is a choice as well, so its full radius outranks the public radius.
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-btn-radius, var(--bit-Button-radius, #{$shp-radius-button}));");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-btn-rnd {"), "--bit-btn-radius: #{$shp-radius-full};");

        // So does an explicit Color, for every color it paints - the disabled and focus colors included.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-clr, var(--bit-Button-background, var(--bit-btn-dft-clr, #{$clr-pri})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-btn-clr-txt, var(--bit-Button-color, var(--bit-btn-dft-clr-txt, #{$clr-pri-text})));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-clr-hover, var(--bit-Button-hover-background, var(--bit-btn-dft-clr-hover, #{$clr-pri-hover})));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-clr-active, var(--bit-Button-active-background, var(--bit-btn-dft-clr-active, #{$clr-pri-active})));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-clr-dis, var(--bit-Button-disabled-background, var(--bit-btn-dft-clr-dis, #{$clr-pri-dis})));");
        StringAssert.Contains(stylesheet, "color: var(--bit-btn-clr-dis-text, var(--bit-Button-disabled-color, var(--bit-btn-dft-clr-dis-text, #{$clr-pri-dis-text})));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-btn-clr-focus, var(--bit-Button-focus-color, var(--bit-btn-dft-clr-focus))))");

        // What a Color does not paint stays the variable's: the transparent background of Outline and Text.
        var outline = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btn-otl {");
        StringAssert.Contains(outline, "background-color: var(--bit-Button-background, transparent);");

        // The default a host component gives its buttons (--bit-btn-dft-*) is the one private value read after them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Button-[a-z-]+, var\(--bit-btn-(?!dft-)"), "A public variable is read before the parameter it restyles the default of.");
        // Except the focus color, whose emptiness while nothing sets it is what hands the ring over to the global
        // --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
        Assert.IsFalse(Regex.IsMatch(SourceFiles.StripScssComments(stylesheet), @"var\(--bit-btn-(?!dft-clr-focus\))[a-z-]+\)"), "A value a parameter publishes is read with no fallback, which leaves it empty while the parameter is unset.");
    }

    [TestMethod]
    public void BitButtonShouldPublishItsColorSizeAndRoundedOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-btn {");

        // A button can sit in the child content or a template of another one, which must not inherit the outer button's
        // Color, Size or Rounded: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-btn-clr", "--bit-btn-clr-txt", "--bit-btn-clr-hover", "--bit-btn-clr-active", "--bit-btn-clr-focus",
                                         "--bit-btn-clr-dis", "--bit-btn-clr-dis-text", "--bit-btn-lbl-fontsize", "--bit-btn-prt-fontsize",
                                         "--bit-btn-sct-fontsize", "--bit-btn-icn-size", "--bit-btn-spn-size", "--bit-btn-min-height",
                                         "--bit-btn-icn-margintop", "--bit-btn-gap", "--bit-btn-pad-ntx", "--bit-btn-padding", "--bit-btn-radius" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "Button", "BitButton.scss");

    [GeneratedRegex(@"^//\s+(--bit-Button-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Button-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Button-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
