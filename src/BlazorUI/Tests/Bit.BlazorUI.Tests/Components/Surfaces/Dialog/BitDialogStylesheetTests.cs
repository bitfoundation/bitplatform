using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Dialog;

/// <summary>
/// Pins the public --bit-Dialog-* custom properties and the global dialog tokens the stylesheet reads, which a
/// bUnit render cannot see: the public ones are read with a fallback and never declared, so they inherit from
/// :root, an ancestor or the Style of an instance, and the list at the head of the stylesheet names exactly the
/// ones it reads.
/// </summary>
[TestClass]
public class BitDialogStylesheetTests
{
    // Read off the stylesheet.
    private static readonly string[] _publicVariables =
    [
        "--bit-Dialog-z-index",
        "--bit-Dialog-margin",
        "--bit-Dialog-overlay-background",
        "--bit-Dialog-overlay-backdrop-filter",
        "--bit-Dialog-background",
        "--bit-Dialog-color",
        "--bit-Dialog-border-width",
        "--bit-Dialog-border-color",
        "--bit-Dialog-radius",
        "--bit-Dialog-shadow",
        "--bit-Dialog-padding",
        "--bit-Dialog-text-align",
        "--bit-Dialog-title-color",
        "--bit-Dialog-title-font-size",
        "--bit-Dialog-title-font-weight",
        "--bit-Dialog-subtitle-color",
        "--bit-Dialog-message-color",
        "--bit-Dialog-ok-button-background",
        "--bit-Dialog-ok-button-color",
        "--bit-Dialog-ok-button-border-color",
        "--bit-Dialog-ok-button-hover-background",
        "--bit-Dialog-ok-button-hover-color",
        "--bit-Dialog-ok-button-active-background",
        "--bit-Dialog-ok-button-active-color",
        "--bit-Dialog-ok-button-radius",
        "--bit-Dialog-cancel-button-background",
        "--bit-Dialog-cancel-button-color",
        "--bit-Dialog-cancel-button-border-color",
        "--bit-Dialog-cancel-button-hover-background",
        "--bit-Dialog-cancel-button-hover-color",
        "--bit-Dialog-cancel-button-active-background",
        "--bit-Dialog-cancel-button-active-color",
        "--bit-Dialog-cancel-button-radius",
    ];

    // The role colors an explicit Color publishes on the root, which every color read of the buttons ranks first.
    private static readonly string[] _roleColors = ["clr", "clr-txt", "clr-hover", "clr-active", "clr-focus", "clr-spn"];

    // Read off the style the C# side writes on the surface, since it only applies where no size was given.
    private const string MaxWidthVariable = "--bit-Dialog-max-width";

    [TestMethod]
    public void BitDialogStylesheetShouldReadEveryPublicVariableWithAFallback()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var name in _publicVariables)
        {
            Assert.IsTrue(Regex.IsMatch(rules, $@"var\({Regex.Escape(name)},\s*[^)\s]"), $"{name} is not read with a fallback.");
        }
    }

    [TestMethod]
    public void BitDialogStylesheetShouldNeverDeclareAPublicVariable()
    {
        var rules = GetRules(ReadStylesheet());

        Assert.IsFalse(Regex.IsMatch(rules, @"--bit-Dialog-[A-Za-z-]+\s*:"), "A public variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitDialogStylesheetShouldReadNoPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var read = Regex.Matches(GetRules(stylesheet), @"var\((--bit-Dialog-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();
        var documented = Regex.Matches(GetHeader(stylesheet), @"(--bit-Dialog-[A-Za-z-]+)").Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        CollectionAssert.AreEqual(_publicVariables.Order().ToArray(), read);
        CollectionAssert.AreEqual(read.Append(MaxWidthVariable).Order().ToArray(), documented);
    }

    [TestMethod]
    public void BitDialogTitleShouldTakeTheDialogTypographyTokens()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "ttl");

        StringAssert.Contains(rule, "font-size: var(--bit-Dialog-title-font-size, #{$tg-dialog-title-font-size});");
        StringAssert.Contains(rule, "font-weight: var(--bit-Dialog-title-font-weight, #{$tg-dialog-title-font-weight});");
        Assert.IsFalse(rule.Contains("$tg-fs-xl"), "The title is sized off the type ramp rather than the dialog title token every preset retunes.");
    }

    [TestMethod]
    public void BitDialogTextShouldFollowTheDialogTextAlignToken()
    {
        var rules = GetRules(ReadStylesheet());

        StringAssert.Contains(rules, "--bit-dlg-text-align: var(--bit-Dialog-text-align, #{$layout-dialog-text-align});");

        // Set on the title itself, since an application's element rule for h2 wins over an inherited alignment.
        foreach (var text in new[] { "ttl", "sub", "msg" })
        {
            StringAssert.Contains(GetRule(rules, text), "text-align: var(--bit-dlg-text-align);", $"The {text} does not follow the alignment.");
        }

        // The wrapper also holds a HeaderTemplate, whose content is the consumer's to lay out.
        Assert.IsFalse(GetRule(rules, "htc").Contains("text-align"), "A HeaderTemplate is re-aligned along with the title.");
    }

    [TestMethod]
    public void BitDialogCentredTitleShouldBeCentredOnTheSurfaceBesideTheCloseButton()
    {
        var rules = GetRules(ReadStylesheet());

        // The title and subtitle are given the close button's width back on their start side while centred, so
        // they are centred on the surface rather than on the room the button leaves beside them.
        var query = Regex.Match(rules, @"@container style\(--bit-dlg-text-align: center\) \{.*?\n\}", RegexOptions.Singleline).Value;

        StringAssert.Contains(query, ".bit-dlg-htc:not(:last-child) > .bit-dlg-ttl");
        StringAssert.Contains(query, ".bit-dlg-htc:not(:last-child) > .bit-dlg-sub");
        StringAssert.Contains(query, "padding-inline-start: $siz-ctrl-md;");
    }

    [TestMethod]
    public void BitDialogAbsolutePositionShouldNotReadThePublicZIndex()
    {
        // A value set on :root to restack the full-screen Dialogs must not lift every absolutely positioned one
        // out of the stacking of the area it is laid out in.
        var rule = GetRule(GetRules(ReadStylesheet()), "abs");

        StringAssert.Contains(rule, "z-index: auto;");
        Assert.IsFalse(rule.Contains("--bit-Dialog-z-index"), "An absolutely positioned Dialog reads the public z-index.");
    }

    [TestMethod]
    public void BitDialogTitleShouldSetWhatAnApplicationsHeadingRuleCouldReach()
    {
        var rule = GetRule(GetRules(ReadStylesheet()), "ttl");

        foreach (var declaration in new[] { "margin: 0;", "padding: 0;", "font-style: normal;", "font-family: inherit;",
                                            "letter-spacing: normal;", "text-transform: none;", "text-decoration: none;" })
        {
            StringAssert.Contains(rule, declaration);
        }
    }

    [TestMethod]
    public void BitDialogBandsShouldTakeTheSurfacesBackgroundAndCorners()
    {
        var rules = GetRules(ReadStylesheet());

        // A surface given a background or a radius of its own is not left with bands of the default in it.
        foreach (var band in new[] { "hdr", "bct", "ftr" })
        {
            var rule = GetRule(rules, band);

            StringAssert.Contains(rule, "background-color: var(--bit-dlg-bg);", $"The {band} band keeps a background of its own.");
            StringAssert.Contains(rule, "radius: var(--bit-dlg-radius);", $"The {band} band keeps corners of its own.");
        }
    }

    [TestMethod]
    public void BitDialogBusyOkButtonShouldStayUndimmedAndIgnoreHover()
    {
        var rules = GetRules(ReadStylesheet());

        // The spinner is the only sign the work is running, so the busy button is not dimmed like a disabled one.
        StringAssert.Contains(GetRule(rules, "bct"), "&[aria-busy=\"true\"] {\n            cursor: progress;\n        }");
        Assert.IsFalse(rules.Contains("&[aria-disabled=\"true\"] {"), "The busy Ok button is dimmed along with the spinner it holds.");
        StringAssert.Contains(GetRule(rules, "okb"), "&:hover:not(:disabled):not([aria-disabled=\"true\"])");
    }

    [TestMethod]
    public void BitDialogRootShouldLeaveTheRoleColorsUnsetUntilAColorIsGiven()
    {
        var stylesheet = ReadStylesheet();

        // Declared at the primary role on every root, they would win over the public button variables whether or not
        // a Color was asked for; unset, only the role class of an explicit Color publishes them.
        var names = SourceFiles.GetPrivatePropertyLists(SourceFiles.StripScssComments(stylesheet))
                               .Single(list => list.Name == "dlg-private-properties")
                               .Names;

        foreach (var name in _roleColors)
        {
            Assert.IsTrue(names.Contains(name), $"--bit-dlg-{name} is not reset on the root.");
        }

        var root = Regex.Match(GetRules(stylesheet), @"\n\.bit-dlg \{.*?\n\}", RegexOptions.Singleline).Value;
        Assert.IsFalse(Regex.IsMatch(root, @"--bit-dlg-clr[a-z-]*:\s*#\{"), "The root declares a role color at a default, which outranks the public button variables.");
    }

    [TestMethod]
    public void BitDialogButtonsShouldRankAnExplicitColorBeforeTheirPublicVariables()
    {
        var rules = GetRules(ReadStylesheet());

        foreach (var button in new[] { "okb", "cnb" })
        {
            var rule = GetRule(rules, button);

            // Every role color the button reads is read with the public chain as its fallback, never the other way round.
            Assert.IsFalse(Regex.IsMatch(rule, @"var\(--bit-Dialog-[a-z-]+,\s*var\(--bit-dlg-clr"), $"The {button} reads a public variable before the Color.");

            Assert.IsFalse(Regex.IsMatch(rule, @"var\(--bit-dlg-clr[a-z-]*\)"), $"The {button} reads a role color with no fallback, which an uncolored Dialog leaves unset.");
        }

        StringAssert.Contains(GetRule(rules, "okb"), "background-color: var(--bit-dlg-okb-bg, var(--bit-dlg-clr, #{$dlg-okb-background}));");
        StringAssert.Contains(rules, "$dlg-okb-background: var(--bit-Dialog-ok-button-background, #{$clr-pri});");
        StringAssert.Contains(GetRule(rules, "cnb"), "color: var(--bit-dlg-cnb-clr, var(--bit-dlg-clr, #{$dlg-cnb-color}));");
        StringAssert.Contains(rules, "$dlg-cnb-color: var(--bit-Dialog-cancel-button-color, #{$clr-pri});");

        // The transparent outline at rest is the Cancel button's recipe rather than the Dialog's color, so its public
        // background still applies under an explicit Color.
        StringAssert.Contains(GetRule(rules, "cnb"), "background-color: var(--bit-dlg-cnb-bg, var(--bit-Dialog-cancel-button-background, transparent));");
    }

    [TestMethod]
    public void BitDialogButtonStatesShouldFallBackOnlyWhereTheyStayReadable()
    {
        var rules = GetRules(ReadStylesheet());

        // The filled Ok button restyled at rest is not handed back the primary colors under the pointer or while pressed.
        StringAssert.Contains(rules, "$dlg-okb-hover-background: var(--bit-Dialog-ok-button-hover-background, var(--bit-Dialog-ok-button-background, #{$clr-pri-hover}));");
        StringAssert.Contains(rules, "$dlg-okb-active-background: var(--bit-Dialog-ok-button-active-background, var(--bit-Dialog-ok-button-hover-background, var(--bit-Dialog-ok-button-background, #{$clr-pri-active})));");

        // The Cancel button is an outline that fills under the pointer, so its states take the primary fill and text
        // until they are given values of their own: a color or background meant for the outline at rest would be
        // unreadable on that fill.
        StringAssert.Contains(rules, "$dlg-cnb-hover-background: var(--bit-Dialog-cancel-button-hover-background, #{$clr-pri-hover});");
        StringAssert.Contains(rules, "$dlg-cnb-hover-color: var(--bit-Dialog-cancel-button-hover-color, #{$clr-pri-text});");
        StringAssert.Contains(rules, "$dlg-cnb-active-background: var(--bit-Dialog-cancel-button-active-background, var(--bit-Dialog-cancel-button-hover-background, #{$clr-pri-active}));");
        StringAssert.Contains(rules, "$dlg-cnb-active-color: var(--bit-Dialog-cancel-button-active-color, var(--bit-Dialog-cancel-button-hover-color, #{$clr-pri-text}));");

        // The states move the private variables the rule at rest paints from, the role color of an explicit Color first.
        StringAssert.Contains(GetRule(rules, "okb"), "--bit-dlg-okb-bg: var(--bit-dlg-clr-hover, #{$dlg-okb-hover-background});");
        StringAssert.Contains(GetRule(rules, "cnb"), "--bit-dlg-cnb-bg: var(--bit-dlg-clr-active, #{$dlg-cnb-active-background});");
    }

    [TestMethod]
    public void BitDialogButtonsShouldTakeTheirRadiusAtTheWeightOfAClass()
    {
        var rules = GetRules(ReadStylesheet());

        // On the band's descendant rule it would outrank a single class passed through Classes.OkButton / CancelButton.
        Assert.IsFalse(GetRule(rules, "bct").Contains("border-radius: $shp-radius-button"), "The band sets the radius of its buttons.");
        StringAssert.Contains(GetRule(rules, "okb"), "border-radius: var(--bit-Dialog-ok-button-radius, #{$shp-radius-button});");
        StringAssert.Contains(GetRule(rules, "cnb"), "border-radius: var(--bit-Dialog-cancel-button-radius, #{$shp-radius-button});");
    }

    /// <summary>
    /// The stylesheet with its leading comment block cut off, so a name documented there is not mistaken for one read.
    /// </summary>
    private static string GetRules(string stylesheet) => stylesheet[stylesheet.IndexOf("\n.bit-dlg {")..];

    /// <summary>
    /// The body of the top-level rule of one class, up to the brace that closes it at the start of a line.
    /// </summary>
    private static string GetRule(string rules, string suffix) => Regex.Match(rules, $@"\n\.bit-dlg-{suffix} \{{.*?\n\}}", RegexOptions.Singleline).Value;

    private static string GetHeader(string stylesheet) => stylesheet[..stylesheet.IndexOf("\n.bit-dlg {")];

    private static string ReadStylesheet() => SourceFiles.Read("Bit.BlazorUI", "Components", "Surfaces", "Dialog", "BitDialog.scss");
}
