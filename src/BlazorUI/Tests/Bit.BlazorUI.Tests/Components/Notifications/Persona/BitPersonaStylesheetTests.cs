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
            // The focus color alone is read without a fallback, on purpose: its absence is what hands the ring over
            // to the global --bit-shd-focus-ring (see BitFocusRingStylesheetTests).
            if (name == "--bit-Persona-focus-color")
            {
                Assert.Contains($"var({name})", rules, $"{name} is not read without a fallback.");
                continue;
            }

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

        Assert.AreSequenceEqual(_publicVariables.Order().ToArray(), read);
        Assert.AreSequenceEqual(read, documented);
    }

    [TestMethod]
    public void BitPersonaSecondaryRowsShouldReadQuieterThanTheName()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.Contains("color: var(--bit-Persona-secondary-color, #{$clr-fg-sec});", rules);
        Assert.Contains("color: var(--bit-Persona-primary-color, #{$clr-fg-pri});", rules);
    }

    [TestMethod]
    public void BitPersonaVerticalLayoutShouldStackTheCoinOverTheDetails()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.Contains(".bit-prs-vrt {\n    --bit-prs-flex-direction: column;", rules);
        Assert.Contains("--bit-prs-flex-direction: column-reverse;", rules);
    }

    [TestMethod]
    public void BitPersonaHollowPresenceDotsShouldBeCutInThePageColor()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.Contains("background-color: var(--bit-prs-surface);", GetRule(rules, "off"));
        Assert.Contains("background-color: var(--bit-prs-surface);", GetRule(rules, "blk"));
        Assert.DoesNotContain("$clr-ntr-white", rules, "A presence dot is painted white, which glares on a dark scheme.");
    }

    [TestMethod]
    public void BitPersonaSurfaceColorShouldFeedEveryCutoutOfThePersona()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.Contains("--bit-prs-surface: var(--bit-Persona-surface-color, #{$clr-bg-pri});", rules);

        // The active ring's gap is cut in the surface unless a gap color of its own is given.
        Assert.HasCount(2, Regex.Matches(rules, Regex.Escape("var(--bit-Persona-ring-gap-color, var(--bit-prs-surface))")));
        Assert.DoesNotContain("var(--bit-Persona-ring-gap-color, #{$clr-bg-pri})", rules, "The ring gap ignores the surface color.");
    }

    [TestMethod]
    public void BitPersonaFilledCoinShouldClearItsEdgeUnderAPicture()
    {
        var rules = GetRules(ReadStylesheet());

        var rule = Regex.Match(rules, @"\n\.bit-prs-cph\.bit-prs-fil \{.*?\n\}", RegexOptions.Singleline).Value;

        Assert.Contains("border-color: transparent;", rule);
        Assert.Contains("background-clip: padding-box;", rule);
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldOfferAPointerTargetOfAtLeast24Px()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "abt");

        // Spacing(2.5) is 20px on the Size24 coin, so the hit area is grown past the button to the WCAG 2.5.8 minimum.
        Assert.Contains("&::before {", rule);
        Assert.Contains("width: max(100%, #{rem2(24px)});", rule);
        Assert.Contains("height: max(100%, #{rem2(24px)});", rule);
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

        Assert.Contains($"color: var(--bit-Persona-presence-icon-color, #{{{token}}});", rule);
    }

    [TestMethod,
        DataRow("off"),
        DataRow("blk")
    ]
    public void BitPersonaHollowPresenceGlyphShouldTakeTheStrokeColor(string status)
    {
        var rule = GetRule(GetRules(ReadStylesheet()), status);

        Assert.Contains("color: var(--bit-Persona-presence-icon-color, var(--bit-prs-presence-clr));", rule);
        Assert.Contains("border: $shp-border-width-thick $shp-border-style var(--bit-prs-presence-clr);", rule);
    }

    [TestMethod]
    public void BitPersonaActionButtonShouldBeRevealedByHoveringTheCoinAndCarryItsOwnSurface()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.Contains(".bit-prs-cin:hover > .bit-prs-abt {\n        opacity: 1;", rules);

        var rule = GetRule(rules, "abt");

        Assert.Contains("background-color: var(--bit-prs-abt-sbg, var(--bit-Persona-action-background, #{$clr-bg-pri}));", rule);
        Assert.DoesNotContain("transparent", rule, "The action button is hidden by painting it transparent, which leaves no surface under it once shown.");

        // A custom surface is kept on hover rather than snapped back to the light default under a glyph paired with it;
        // the hover only moves the variable the rule at rest paints from, so a Classes paint is not outranked by it.
        Assert.Contains("--bit-prs-abt-sbg: var(--bit-Persona-action-hover-background, var(--bit-Persona-action-background, #{$clr-bg-pri-hover}));", rule);
    }

    [TestMethod]
    public void BitPersonaPresenceDotShouldStackAboveTheActionButton()
    {
        var rules = GetRules(ReadStylesheet());

        static int ZIndex(string rule) => int.Parse(Regex.Match(rule, @"\n    z-index: (\d+);").Groups[1].Value);

        Assert.IsGreaterThan(ZIndex(GetRule(rules, "abt")), ZIndex(GetRule(rules, "pre")), "The opaque action button would hide the presence dot where the two overlap.");
    }

    [TestMethod]
    public void BitPersonaShouldLetAParameterWinOverItsPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        // An explicit CoinColor (or the one AutoCoinColor picks) publishes the pair, so it is read before the variables,
        // which only restyle the Info coin an unset one stands for - the Active ring included, which is the coin's color.
        Assert.Contains("--bit-prs-coin-bg: var(--bit-prs-coin-clr-bg, var(--bit-Persona-coin-background, #{$clr-inf}));", rules);
        Assert.Contains("--bit-prs-coin-clr: var(--bit-prs-coin-clr-txt, var(--bit-Persona-coin-color, #{$clr-inf-text}));", rules);
        Assert.HasCount(2, Regex.Matches(rules, Regex.Escape("var(--bit-prs-coin-clr-bg, var(--bit-Persona-ring-color, var(--bit-prs-coin-bg)))")));

        // So does an explicit Shape (or Squared), and an explicit Pill has a class of its own to say so.
        Assert.HasCount(2, Regex.Matches(rules, Regex.Escape("border-radius: var(--bit-prs-coin-radius, var(--bit-Persona-coin-radius, 50%));")));
        Assert.Contains("--bit-prs-coin-radius: 50%;", GetRule(rules, "cir"));

        // So does an explicit Size, which alone hands over what its size class publishes for the gap and the active
        // ring; an unset one is laid out as Size48, whose values come after the variables.
        Assert.Contains("gap: var(--bit-prs-gap, var(--bit-Persona-gap, var(--bit-prs-sz-gap)));", rules);
        Assert.HasCount(5, Regex.Matches(rules, Regex.Escape("var(--bit-prs-ring-gap, var(--bit-Persona-ring-gap, var(--bit-prs-sz-ring-gap)))")));
        Assert.HasCount(2, Regex.Matches(rules, Regex.Escape("var(--bit-prs-ring-width, var(--bit-Persona-ring-width, var(--bit-prs-sz-ring-width)))")));
        var explicitSize = GetRule(rules, "ssz");
        Assert.Contains("--bit-prs-gap: var(--bit-prs-sz-gap);", explicitSize);
        Assert.Contains("--bit-prs-ring-gap: var(--bit-prs-sz-ring-gap);", explicitSize);
        Assert.Contains("--bit-prs-ring-width: var(--bit-prs-sz-ring-width);", explicitSize);
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
        Assert.Contains("--bit-prs-coin-radius: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.Contains("--bit-prs-gap: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.Contains("--bit-prs-ring-gap: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.Contains("--bit-prs-ring-width: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.IsLessThan(rules.IndexOf("\n.bit-prs-ssz {"), rules.IndexOf("\n.bit-prs-s120 {"), "The explicit size comes before a size class it reads.");

        Assert.Contains("--bit-prs-coin-clr-bg: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.Contains("--bit-prs-coin-clr-txt: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.Contains("--bit-prs-coin-clr-fg: initial;", SourceFiles.GetScssBlock(rules, "\n.bit-prs {"));
        Assert.IsLessThan(rules.IndexOf("\n.bit-prs-pri {"), rules.IndexOf("\n.bit-prs {"), "The reset comes before the color class that has to win over it.");
        Assert.IsLessThan(rules.IndexOf("\n.bit-prs-cir {"), rules.IndexOf("\n.bit-prs {"), "The reset comes before the shape class that has to win over it.");
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

    [TestMethod]
    public void BitPersonaShouldDrawTheInitialsOfAnUnfilledCoinInTheRoleTextColor()
    {
        var rules = GetRules(ReadStylesheet());

        // The initials of an Outline or Text coin sit on the page rather than on the coin's fill, so they are drawn in the
        // role's text color (role-text): a secondary or a warning fill is under 3:1 as text on white, and a background role's
        // fill is the page itself. The ring keeps the fill; an unset CoinColor stands for Info.
        Assert.Contains("\n    --bit-prs-coin-fg: var(--bit-prs-coin-clr-fg, var(--bit-Persona-coin-background, #{$clr-inf-fg}));", rules);
        Assert.Contains("color: var(--bit-prs-coin-fg);", GetRule(rules, "otl"));
        Assert.Contains("border-color: var(--bit-prs-coin-bg);", GetRule(rules, "otl"));
        Assert.Contains("color: var(--bit-prs-coin-fg);", GetRule(rules, "txt"));

        // The role classes are written out by hand, each with the text color role-text gives its role.
        foreach (var (role, text) in new[] { ("pri", "pri"), ("sec", "sec-fg"), ("ter", "ter-fg"), ("inf", "inf-fg"), ("suc", "suc-fg"), ("wrn", "wrn-fg"),
                                             ("swr", "swr-fg"), ("err", "err-fg"), ("pbg", "fg-pri"), ("sbg", "fg-pri"), ("tbg", "fg-pri"), ("pfg", "fg-pri"),
                                             ("sfg", "fg-sec"), ("tfg", "fg-ter"), ("pbr", "fg-pri"), ("sbr", "fg-pri"), ("tbr", "fg-pri") })
        {
            Assert.Contains($"--bit-prs-coin-clr-fg: #{{$clr-{text}}};", GetRule(rules, role));
        }
    }
}
