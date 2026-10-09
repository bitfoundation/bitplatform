using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.Persona;

/// <summary>
/// Pins the public --bit-Persona-* custom properties, which a bUnit render cannot see: they are read off the
/// stylesheet with a fallback and never declared there, so they inherit from :root, an ancestor or the Style of an
/// instance, and the list at the head of the stylesheet names exactly the ones it reads.
/// </summary>
[TestClass]
public class BitPersonaStylesheetTests
{
    private static readonly string[] _publicVariables =
    [
        "--bit-Persona-surface-color",
        "--bit-Persona-gap",
        "--bit-Persona-primary-color",
        "--bit-Persona-primary-font-weight",
        "--bit-Persona-secondary-color",
        "--bit-Persona-disabled-color",
        "--bit-Persona-coin-background",
        "--bit-Persona-coin-color",
        "--bit-Persona-coin-radius",
        "--bit-Persona-overlay-background",
        "--bit-Persona-overlay-color",
        "--bit-Persona-focus-color",
        "--bit-Persona-action-background",
        "--bit-Persona-action-color",
        "--bit-Persona-action-hover-background",
        "--bit-Persona-presence-border-color",
        "--bit-Persona-presence-icon-color",
        "--bit-Persona-presence-online-color",
        "--bit-Persona-presence-away-color",
        "--bit-Persona-presence-busy-color",
        "--bit-Persona-presence-dnd-color",
        "--bit-Persona-presence-out-of-office-color",
        "--bit-Persona-presence-unknown-color",
        "--bit-Persona-presence-offline-color",
        "--bit-Persona-presence-blocked-color",
        "--bit-Persona-ring-color",
        "--bit-Persona-ring-width",
        "--bit-Persona-ring-gap",
        "--bit-Persona-ring-gap-color",
        "--bit-Persona-active-shadow",
        "--bit-Persona-inactive-opacity",
        "--bit-Persona-inactive-scale",
    ];

    [TestMethod]
    public void BitPersonaStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitPersonaStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Persona-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitPersonaStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Persona-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Persona-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read, documented);
    }

    [TestMethod]
    public void BitPersonaSecondaryRowsShouldReadQuieterThanTheName()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "color: var(--bit-Persona-secondary-color, #{$clr-fg-sec});");
        StringAssert.Contains(rules, "color: var(--bit-Persona-primary-color, #{$clr-fg-pri});");
    }

    [TestMethod]
    public void BitPersonaVerticalLayoutShouldStackTheCoinOverTheDetails()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, ".bit-prs-vrt {\n    --bit-prs-flex-direction: column;");
        StringAssert.Contains(rules, "--bit-prs-flex-direction: column-reverse;");
    }

    [TestMethod]
    public void BitPersonaHollowPresenceDotsShouldBeCutInThePageColor()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(GetRule(rules, "off"), "background-color: var(--bit-prs-surface);");
        StringAssert.Contains(GetRule(rules, "blk"), "background-color: var(--bit-prs-surface);");
        Assert.IsFalse(rules.Contains("$clr-ntr-white"), "A presence dot is painted white, which glares on a dark scheme.");
    }

    [TestMethod]
    public void BitPersonaSurfaceColorShouldFeedEveryCutoutOfThePersona()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "--bit-prs-surface: var(--bit-Persona-surface-color, #{$clr-bg-pri});");

        // The active ring's gap is cut in the surface unless a gap color of its own is given.
        Assert.AreEqual(2, Regex.Matches(rules, Regex.Escape("var(--bit-Persona-ring-gap-color, var(--bit-prs-surface))")).Count);
        Assert.IsFalse(rules.Contains("var(--bit-Persona-ring-gap-color, #{$clr-bg-pri})"), "The ring gap ignores the surface color.");
    }

    [TestMethod]
    public void BitPersonaFilledCoinShouldClearItsEdgeUnderAPicture()
    {
        var rules = GetRules(ReadStylesheet());

        var rule = Regex.Match(rules, @"\n\.bit-prs-cph\.bit-prs-fil \{.*?\n\}", RegexOptions.Singleline).Value;

        StringAssert.Contains(rule, "border-color: transparent;");
        StringAssert.Contains(rule, "background-clip: padding-box;");
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldOfferAPointerTargetOfAtLeast24Px()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "abt");

        // Spacing(2.5) is 20px on the Size24 coin, so the hit area is grown past the button to the WCAG 2.5.8 minimum.
        StringAssert.Contains(rule, "&::before {");
        StringAssert.Contains(rule, "width: max(100%, #{rem2(24px)});");
        StringAssert.Contains(rule, "height: max(100%, #{rem2(24px)});");
    }

    [TestMethod,
        DataRow("onl", "$clr-suc-text"),
        DataRow("awy", "$clr-wrn-text"),
        DataRow("dnd", "$clr-err-text"),
        DataRow("bsy", "$clr-err-text"),
        DataRow("oof", "$clr-swr-text"),
        DataRow("unk", "$clr-bg-pri")
    ]
    public void BitPersonaPresenceGlyphShouldTakeTheTextColorPairedWithItsFill(string status, string token)
    {
        var rule = GetRule(GetRules(ReadStylesheet()), status);

        StringAssert.Contains(rule, $"color: var(--bit-Persona-presence-icon-color, #{{{token}}});");
    }

    [TestMethod,
        DataRow("off"),
        DataRow("blk")
    ]
    public void BitPersonaHollowPresenceGlyphShouldTakeTheStrokeColor(string status)
    {
        var rule = GetRule(GetRules(ReadStylesheet()), status);

        StringAssert.Contains(rule, "color: var(--bit-Persona-presence-icon-color, var(--bit-prs-presence-clr));");
        StringAssert.Contains(rule, "border: $shp-border-width-thick $shp-border-style var(--bit-prs-presence-clr);");
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldBeRevealedByHoveringTheCoinAndCarryItsOwnSurface()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, ".bit-prs-cin:hover > .bit-prs-abt {\n        opacity: 1;");

        var rule = GetRule(rules, "abt");

        StringAssert.Contains(rule, "background-color: var(--bit-Persona-action-background, #{$clr-bg-pri});");
        Assert.IsFalse(rule.Contains("transparent"), "The action button is hidden by painting it transparent, which leaves no surface under it once shown.");

        // A custom surface is kept on hover rather than snapped back to the light default under a glyph paired with it.
        StringAssert.Contains(rule, "background-color: var(--bit-Persona-action-hover-background, var(--bit-Persona-action-background, #{$clr-bg-pri-hover}));");
    }

    [TestMethod]
    public void BitPersonaPresenceDotShouldStackAboveTheActionButton()
    {
        var rules = GetRules(ReadStylesheet());

        static int ZIndex(string rule) => int.Parse(Regex.Match(rule, @"\n    z-index: (\d+);").Groups[1].Value);

        Assert.IsTrue(ZIndex(GetRule(rules, "pre")) > ZIndex(GetRule(rules, "abt")), "The opaque action button would hide the presence dot where the two overlap.");
    }

    [TestMethod]
    public void BitPersonaShouldLetAParameterWinOverItsPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        // An explicit CoinColor (or the one AutoCoinColor picks) publishes the pair, so it is read before the variables,
        // which only restyle the Info coin an unset one stands for - the Active ring included, which is the coin's color.
        StringAssert.Contains(rules, "--bit-prs-coin-bg: var(--bit-prs-coin-clr-bg, var(--bit-Persona-coin-background, #{$clr-inf}));");
        StringAssert.Contains(rules, "--bit-prs-coin-clr: var(--bit-prs-coin-clr-txt, var(--bit-Persona-coin-color, #{$clr-inf-text}));");
        Assert.AreEqual(2, Regex.Matches(rules, Regex.Escape("var(--bit-prs-coin-clr-bg, var(--bit-Persona-ring-color, var(--bit-prs-coin-bg)))")).Count);

        // So does an explicit Shape (or Squared), and an explicit Pill has a class of its own to say so.
        Assert.AreEqual(2, Regex.Matches(rules, Regex.Escape("border-radius: var(--bit-prs-coin-radius, var(--bit-Persona-coin-radius, 50%));")).Count);
        StringAssert.Contains(GetRule(rules, "cir"), "--bit-prs-coin-radius: 50%;");

        // So does an explicit Size, which alone hands over what its size class publishes for the gap and the active
        // ring; an unset one is laid out as Size48, whose values come after the variables.
        StringAssert.Contains(rules, "gap: var(--bit-prs-gap, var(--bit-Persona-gap, var(--bit-prs-sz-gap)));");
        Assert.AreEqual(5, Regex.Matches(rules, Regex.Escape("var(--bit-prs-ring-gap, var(--bit-Persona-ring-gap, var(--bit-prs-sz-ring-gap)))")).Count);
        Assert.AreEqual(2, Regex.Matches(rules, Regex.Escape("var(--bit-prs-ring-width, var(--bit-Persona-ring-width, var(--bit-prs-sz-ring-width)))")).Count);
        var explicitSize = GetRule(rules, "ssz");
        StringAssert.Contains(explicitSize, "--bit-prs-gap: var(--bit-prs-sz-gap);");
        StringAssert.Contains(explicitSize, "--bit-prs-ring-gap: var(--bit-prs-sz-ring-gap);");
        StringAssert.Contains(explicitSize, "--bit-prs-ring-width: var(--bit-prs-sz-ring-width);");
        Assert.IsFalse(Regex.IsMatch(GetRule(rules, "s48"), @"--bit-prs-(gap|ring-gap|ring-width):"), "A size class hands its gap over as a choice.");

        Assert.IsFalse(Regex.IsMatch(rules, @"var\(--bit-Persona-[a-z-]+, var\(--bit-prs-(coin-clr-bg|coin-clr-txt|coin-radius|gap|ring-gap|ring-width)\)"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitPersonaShouldPublishItsCoinColorAndShapeOnlyWhereTheyAreSet()
    {
        var rules = GetRules(ReadStylesheet());

        // A persona can sit in the template of another one, which must not inherit the outer persona's coin color or
        // shape: each root starts the values those classes publish out unset, and the classes - declared further down
        // at the same weight - still win on the root that carries them.
        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-coin-radius: initial;");
        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-gap: initial;");
        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-ring-gap: initial;");
        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-ring-width: initial;");
        Assert.IsTrue(rules.IndexOf("\n.bit-prs-s120 {") < rules.IndexOf("\n.bit-prs-ssz {"), "The explicit size comes before a size class it reads.");

        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-coin-clr-bg: initial;");
        StringAssert.Contains(SourceFiles.GetScssBlock(rules, "\n.bit-prs {"), "--bit-prs-coin-clr-txt: initial;");
        Assert.IsTrue(rules.IndexOf("\n.bit-prs {") < rules.IndexOf("\n.bit-prs-pri {"), "A color class comes before the reset it has to win over.");
        Assert.IsTrue(rules.IndexOf("\n.bit-prs {") < rules.IndexOf("\n.bit-prs-cir {"), "A shape class comes before the reset it has to win over.");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-prs {")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a
    /// line - the braces of an interpolation inside it are not the end of it.
    /// </summary>
    private static string GetRule(string rules, string suffix) => Regex.Match(rules, $@"\n\.bit-prs-{suffix} \{{.*?\n\}}", RegexOptions.Singleline).Value;

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-prs {")];

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI", "Components", "Notifications", "Persona", "BitPersona.scss");
}
