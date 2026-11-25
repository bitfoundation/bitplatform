using System;
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
        // The hover and press states set the variables the rules at rest paint from (--bit-btn-sfg / -sbg / -sbr), never
        // the colors themselves, so the state's value is read ahead of the rest value it stands in for.
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-sbg, var(--bit-btn-clr, var(--bit-Button-fill-background, var(--bit-Button-background, var(--bit-btn-dft-clr, #{$clr-pri})))));");
        StringAssert.Contains(stylesheet, "color: var(--bit-btn-sfg, var(--bit-btn-clr-txt, var(--bit-Button-fill-color, var(--bit-Button-color, var(--bit-btn-dft-clr-txt, #{$clr-pri-text})))));");
        StringAssert.Contains(stylesheet, "--bit-btn-sbg: var(--bit-btn-clr-hover, var(--bit-Button-fill-hover-background, var(--bit-Button-hover-background, var(--bit-btn-dft-clr-hover, #{$clr-pri-hover}))));");
        StringAssert.Contains(stylesheet, "--bit-btn-sbg: var(--bit-btn-clr-active, var(--bit-Button-fill-active-background, var(--bit-Button-active-background, var(--bit-btn-dft-clr-active, #{$clr-pri-active}))));");
        StringAssert.Contains(stylesheet, "background-color: var(--bit-btn-clr-dis, var(--bit-Button-fill-disabled-background, var(--bit-Button-disabled-background, var(--bit-btn-dft-clr-dis, #{$clr-pri-dis}))));");
        StringAssert.Contains(stylesheet, "color: var(--bit-btn-clr-dis-text, var(--bit-Button-fill-disabled-color, var(--bit-Button-disabled-color, var(--bit-btn-dft-clr-dis-text, #{$clr-pri-dis-text}))));");
        StringAssert.Contains(stylesheet, "focus-ring-own(var(--bit-btn-clr-focus, var(--bit-Button-focus-color, var(--bit-btn-dft-clr-focus))))");

        // What a Color does not paint stays the variable's: the transparent background of Outline and Text.
        var outline = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btn-otl {");
        StringAssert.Contains(outline, "background-color: var(--bit-btn-sbg, var(--bit-Button-outline-background, var(--bit-Button-background, transparent)));");

        // The default a host component gives its buttons (--bit-btn-dft-*) is the one private value read after them.
        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-Button-[a-z-]+, var\(--bit-btn-(?!dft-)"), "A public variable is read before the parameter it restyles the default of.");
        // Except the focus color, whose emptiness while nothing sets it is what hands the ring over to the global
        // --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
        Assert.IsFalse(Regex.IsMatch(SourceFiles.StripScssComments(stylesheet), @"var\(--bit-btn-(?!dft-clr-focus\))[a-z-]+\)"), "A value a parameter publishes is read with no fallback, which leaves it empty while the parameter is unset.");
    }

    [TestMethod,
        DataRow("fill", ".bit-btn-fil"),
        DataRow("outline", ".bit-btn-otl"),
        DataRow("text", ".bit-btn-txt")]
    public void BitButtonShouldReadTheVariablesOfItsVariantAheadOfTheSharedOnes(string variant, string selector)
    {
        var stylesheet = ReadStylesheet();
        var block = SourceFiles.GetScssBlock(stylesheet, $"\n{selector} {{");

        // Each variant reads a tier of its own ahead of every shared color variable, so a theme can set one look apart
        // from the other two on :root - an outline button's neutral hairline without a ring around every filled one.
        foreach (var name in new[] { "color", "background", "border-color",
                                     "hover-color", "hover-background", "hover-border-color",
                                     "active-color", "active-background", "active-border-color",
                                     "disabled-color", "disabled-background", "disabled-border-color" })
        {
            StringAssert.Contains(block, $"var(--bit-Button-{variant}-{name}, var(--bit-Button-{name}, ",
                                  $"{selector} does not read --bit-Button-{variant}-{name} ahead of --bit-Button-{name}.");
        }

        // A variant reads a shared color variable only right behind its own twin, and never the tier of another variant.
        Assert.IsFalse(Regex.IsMatch(block, $@"(?<!var\(--bit-Button-{variant}-[a-z-]+, )var\(--bit-Button-(?:hover-|active-|disabled-)?(?:color|background|border-color)\b"),
                       $"{selector} reads a shared color variable without its own tier ahead of it.");
        Assert.IsFalse(Regex.IsMatch(block, $@"var\(--bit-Button-(?!{variant}-)(?:fill|outline|text)-"),
                       $"{selector} reads the variables of another variant.");
    }

    [TestMethod]
    public void BitButtonShouldPublishItsColorSizeAndRoundedOnlyWhereTheyAreSet()
    {
        var root = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-btn {");

        // A button can sit in the child content or a template of another one, which must not inherit the outer button's
        // Color, Size or Rounded: each root starts the values those classes publish out unset, and the classes - declared
        // further down at the same weight - still win on the root that carries them.
        foreach (var property in new[] { "--bit-btn-clr", "--bit-btn-clr-fg", "--bit-btn-clr-txt", "--bit-btn-clr-hover", "--bit-btn-clr-active", "--bit-btn-clr-focus",
                                         "--bit-btn-clr-dis", "--bit-btn-clr-dis-text", "--bit-btn-lbl-fontsize", "--bit-btn-prt-fontsize",
                                         "--bit-btn-sct-fontsize", "--bit-btn-icn-size", "--bit-btn-spn-size", "--bit-btn-min-height",
                                         "--bit-btn-icn-margintop", "--bit-btn-gap", "--bit-btn-pad-ntx", "--bit-btn-padding", "--bit-btn-radius",
                                         // The colors the hover and press states move, which a nested button must not inherit either.
                                         "--bit-btn-sfg", "--bit-btn-sbg", "--bit-btn-sbr" })
        {
            StringAssert.Contains(root, $"{property}: initial;");
        }
    }

    [TestMethod,
        DataRow(".bit-btn-otl", "outline"),
        DataRow(".bit-btn-txt", "text")]
    public void BitButtonShouldDrawTheLabelOfAnUnfilledVariantInTheRoleTextColor(string selector, string variant)
    {
        var stylesheet = ReadStylesheet();

        // The label of an Outline or Text button sits on the page rather than on the role's fill, so it is drawn in the
        // role's text color: a secondary or a warning main is picked to fill a surface and is under 3:1 as text on white.
        StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, $"\n{selector} {{"),
                              $"color: var(--bit-btn-sfg, var(--bit-btn-clr-fg, var(--bit-Button-{variant}-color, var(--bit-Button-color, var(--bit-btn-dft-clr-fg, #{{$clr-pri}})))));");

        // Only the label: the outline keeps the role's main, which is what keeps the button reading as that role.
        StringAssert.Contains(stylesheet, "border-color: var(--bit-btn-sbr, var(--bit-btn-clr, var(--bit-Button-outline-border-color, ");

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-btn-#{$role} {"), "--bit-btn-clr-fg: #{role-text($role, $tokens)};");
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n    .bit-btn-dft-#{$role} {"), "--bit-btn-dft-clr-fg: #{role-text($role, $tokens)};");
    }

    [TestMethod]
    public void BitButtonShouldKeepItsLabelReadableInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "@media (forced-colors: active) {");

        // The forced palette fills the primary role with LinkText under a Canvas label, and Chromium draws a Canvas
        // backplate behind every line of text: a filled button is painted as a native one instead, a pair that reads
        // with the backplate and without it.
        var filled = SourceFiles.GetScssDeclarations(forced, ".bit-btn-fil:not(.bit-dis) {");
        StringAssert.Contains(filled, "color: ButtonText;");
        StringAssert.Contains(filled, "background-color: ButtonFace;");
        StringAssert.Contains(filled, "border-color: ButtonText;");

        // The pointer states would move every variant back to that pair, so their variables are set back.
        foreach (var state in new[] { ".bit-btn-fil:not(.bit-dis, .bit-btn-lnc):hover", ".bit-btn-otl:not(.bit-dis, .bit-btn-lnc):active",
                                      ".bit-btn-txt:not(.bit-dis, .bit-btn-lnc):hover", ".bit-btn.bit-btn-fxc:not(.bit-dis):active" })
        {
            StringAssert.Contains(forced, state);
        }

        StringAssert.Contains(forced, "--bit-btn-sfg: initial;");
        StringAssert.Contains(forced, "--bit-btn-sbg: initial;");
    }

    [TestMethod,
        DataRow(".bit-btn-fil.bit-btn-fxc:not(.bit-dis)", "var(--bit-btn-clr-txt, var(--bit-Button-fill-color, var(--bit-Button-color, "),
        DataRow(".bit-btn-otl.bit-btn-fxc:not(.bit-dis)", "var(--bit-btn-clr-txt, var(--bit-Button-outline-hover-color, var(--bit-Button-hover-color, "),
        DataRow(".bit-btn-txt.bit-btn-fxc:not(.bit-dis)", "var(--bit-btn-clr-txt, var(--bit-Button-text-hover-color, var(--bit-Button-hover-color, ")]
    public void BitButtonShouldFixTheOnColorOfItsVariant(string selector, string expected)
    {
        // FixedColor wears the color that stays readable on the role fill the hover and press paint: a Fill button's
        // resting foreground, an Outline or Text button's hover foreground - never the resting foreground of those two,
        // which is the role's own color and would vanish on that fill.
        var block = SourceFiles.GetScssBlock(ReadStylesheet(), $"\n{selector} {{");

        StringAssert.Contains(block, expected);
        Assert.IsFalse(Regex.IsMatch(block, @"var\(--bit-Button-(outline|text)-color\b"), $"{selector} fixes the resting foreground of its variant.");
    }

    [TestMethod]
    public void BitButtonShouldDrawAnIconTemplateAtTheSizeOfTheIcon()
    {
        var stylesheet = ReadStylesheet();

        // The box takes the icon size as its font size (an explicit Size still ahead of the variable), so markup sized
        // in em matches the glyph it stands in for, and it never shrinks out of the row, as the glyph does not.
        var box = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btn-ict {");

        StringAssert.Contains(box, "font-size: var(--bit-btn-icn-size, var(--bit-Button-icon-size, #{$siz-icon-md}));");
        StringAssert.Contains(box, "flex-shrink: 0;");

        // A compound button drops it onto the line of the primary text, as it does the glyph and the image.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-btn-hsc:not(.bit-btn-ntx) {"), ".bit-btn-ict");

        // An svg given no size gets the icon's square, wherever it sits in the template, from rules that weigh nothing so
        // an app's own rules win; a side given without a viewBox to derive the other from gets the icon size for that one.
        var unsized = SourceFiles.GetScssDeclarations(stylesheet, "\n:where(.bit-btn-ict svg:not([width]):not([height]):not(svg svg)) {");

        StringAssert.Contains(unsized, "width: 1em;");
        StringAssert.Contains(unsized, "height: 1em;");

        var widthOnly = SourceFiles.GetScssDeclarations(stylesheet, "\n:where(.bit-btn-ict svg[width]:not([height]):not([viewBox]):not(svg svg)) {");

        StringAssert.Contains(widthOnly, "height: 1em;");
        Assert.DoesNotContain("width:", widthOnly, "The width the svg was given is kept.");

        var heightOnly = SourceFiles.GetScssDeclarations(stylesheet, "\n:where(.bit-btn-ict svg[height]:not([width]):not([viewBox]):not(svg svg)) {");

        StringAssert.Contains(heightOnly, "width: 1em;");
        Assert.DoesNotContain("height:", heightOnly, "The height the svg was given is kept.");
    }

    [TestMethod]
    public void BitButtonShouldMoveItsShadowAndItsPlaceUnderThePointer()
    {
        var stylesheet = ReadStylesheet();

        // The root and the floating buttons paint from the variables the states move, so a class the app hands the root
        // keeps its shadow and its place under the pointer; a nested button never inherits the outer one's state.
        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btn {");
        StringAssert.Contains(root, "box-shadow: var(--bit-btn-shd, var(--bit-Button-shadow, none));");
        StringAssert.Contains(root, "translate: var(--bit-btn-trn, none);");
        StringAssert.Contains(root, "translate $mot-duration-short $mot-easing;");
        Assert.IsFalse(Regex.IsMatch(root, @"transition:[^;]*box-shadow"), "The box-shadow is transitioned, which would fade the focus ring in.");

        foreach (var selector in new[] { "\n.bit-btn-fab {", "\n.bit-btn-ffx {" })
        {
            StringAssert.Contains(SourceFiles.GetScssDeclarations(stylesheet, selector), "box-shadow: var(--bit-btn-shd, var(--bit-Button-float-shadow, #{$box-shadow-card}));");
        }

        // Neither variable is a variant's, so the states that read them qualify no variant: the hover shadow falls back to
        // the resting shadow, and to the floating one for a floating button, and the lift is a length that is nothing
        // unless it is set and never below zero - the way a card lifts.
        StringAssert.Contains(stylesheet, "$btn-lift: max(0px, var(--bit-Button-hover-lift, none));");
        var hover = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-btn:not(.bit-dis, .bit-btn-lnc):hover {");
        StringAssert.Contains(hover, "--bit-btn-shd: var(--bit-Button-hover-shadow, var(--bit-Button-shadow, none));");
        StringAssert.Contains(hover, "--bit-btn-trn: 0 calc(-1 * #{$btn-lift});");

        var floatHover = SourceFiles.GetScssBlock(stylesheet, "\n    .bit-btn-fab:not(.bit-dis, .bit-btn-lnc):hover,\n    .bit-btn-ffx:not(.bit-dis, .bit-btn-lnc):hover {");
        StringAssert.Contains(floatHover, "--bit-btn-shd: var(--bit-Button-hover-shadow, var(--bit-Button-float-shadow, #{$box-shadow-card}));");

        // The room the lift leaves under the button stays the button's, so a pointer resting just above its bottom edge is
        // not left behind by the lift, which would end the hover and bring the button down under it, over and over.
        var band = SourceFiles.GetScssDeclarations(stylesheet, "\n    .bit-btn:not(.bit-dis, .bit-btn-lnc):hover::before {");
        StringAssert.Contains(band, "position: absolute;");
        StringAssert.Contains(band, "top: 100%;");
        StringAssert.Contains(band, "height: #{$btn-lift};");

        // A press settles the shadow alone, in a rule that follows both hover ones at the same weight: a lifted button keeps
        // its place, so a press near its top edge is not left outside it by the time it is released.
        var press = "\n.bit-btn:not(.bit-dis, .bit-btn-lnc):active {\n    --bit-btn-shd: initial;\n}";
        StringAssert.Contains(stylesheet, press);
        Assert.IsTrue(stylesheet.IndexOf(press, StringComparison.Ordinal) > stylesheet.IndexOf(".bit-btn-ffx:not(.bit-dis, .bit-btn-lnc):hover", StringComparison.Ordinal),
                      "The press is declared ahead of the hover it has to win over.");
        Assert.IsFalse(Regex.IsMatch(stylesheet, @":active[^{]*\{[^}]*--bit-btn-trn"), "A press moves the lifted button out from under the pointer.");

        foreach (var property in new[] { "--bit-btn-shd", "--bit-btn-trn", "--bit-btn-sov", "--bit-btn-grd" })
        {
            StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-btn {"), $"{property}: initial;");
        }
    }

    [TestMethod]
    public void BitButtonShouldPaintAGradientOverTheFillAndWashItUnderThePointer()
    {
        var stylesheet = ReadStylesheet();
        var gradient = SourceFiles.GetScssBlock(stylesheet, "\n.bit-btn-grd {");

        // The gradient is laid over the background color from the border box, under a transparent border, with a layer
        // of one color on top that the pointer states move.
        var declarations = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-btn-grd {");
        StringAssert.Contains(declarations, "border-color: transparent;");
        StringAssert.Contains(declarations, "background-origin: border-box;");
        StringAssert.Contains(declarations, "background-image: linear-gradient(var(--bit-btn-sov, transparent), var(--bit-btn-sov, transparent)), var(--bit-btn-grd, none);");

        // The wash is the button's own foreground, deeper under the press than under the pointer.
        StringAssert.Contains(gradient, "&:not(.bit-dis, .bit-btn-lnc):hover {\n            --bit-btn-sov: color-mix(in srgb, currentcolor 10%, transparent);");
        StringAssert.Contains(gradient, "&:not(.bit-dis, .bit-btn-lnc):active {\n        --bit-btn-sov: color-mix(in srgb, currentcolor 20%, transparent);");

        // A disabled button keeps the disabled fill of its variant, and so does a forced-colors palette.
        StringAssert.Contains(SourceFiles.GetScssBlock(gradient, "&.bit-dis {"), "background-image: none;");
        var forced = SourceFiles.GetScssBlock(stylesheet, "@media (forced-colors: active) {");
        StringAssert.Contains(SourceFiles.GetScssBlock(forced, ".bit-btn-grd {"), "background-image: none;");

        // The rule follows the variants, so its transparent border wins over the Fill variant's at the same weight, while
        // the disabled border of the variant still outranks it.
        Assert.IsTrue(stylesheet.IndexOf("\n.bit-btn-grd {", StringComparison.Ordinal) > stylesheet.IndexOf("\n.bit-btn-fil {", StringComparison.Ordinal),
                      "The gradient rule is declared ahead of the Fill variant it overrides.");
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Buttons", "Button", "BitButton.scss");

    [GeneratedRegex(@"^//\s+(--bit-Button-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-Button-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-Button-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();
}
