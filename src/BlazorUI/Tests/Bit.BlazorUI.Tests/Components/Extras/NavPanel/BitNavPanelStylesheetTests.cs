using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.NavPanel;

/// <summary>
/// Pins the public --bit-NavPanel-* variables and the rules of the stylesheet a bUnit render cannot see: every variable
/// the header documents is read with a fallback and listed on the demo page, none of them is ever declared (so they keep
/// inheriting from :root), the Width and ToggledWidth parameters win over the variables they restyle the default of
/// and start out unset on every root, the two shapes of the panel weigh nothing and read the direction the panel is
/// laid out in, and the drawer takes its elevation and its motion from the theme tokens.
/// </summary>
[TestClass]
public partial class BitNavPanelStylesheetTests
{
    // The classes RegisterCssClasses publishes for an Accent, which the stylesheet paints the container from.
    private static readonly string[] _accentClasses =
    [
        "bit-nav-apri", "bit-nav-asec", "bit-nav-ater", "bit-nav-ainf", "bit-nav-asuc", "bit-nav-awrn", "bit-nav-aswr", "bit-nav-aerr",
        "bit-nav-apbg", "bit-nav-asbg", "bit-nav-atbg", "bit-nav-apfg", "bit-nav-asfg", "bit-nav-atfg", "bit-nav-apbr", "bit-nav-asbr", "bit-nav-atbr",
    ];

    [TestMethod]
    public void BitNavPanelShouldReadEveryPublicVariableItDocuments()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);

        Assert.HasCount(14, documented, "The stylesheet does not document the fourteen public variables.");

        foreach (var name in documented)
        {
            Assert.Contains($"var({name}, ", stylesheet, $"{name} is documented but never read with a fallback.");
        }
    }

    [TestMethod]
    public void BitNavPanelShouldNotReadAPublicVariableItDoesNotDocument()
    {
        var stylesheet = ReadStylesheet();

        var documented = DocumentedVariables(stylesheet);
        var read = ReadVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct();

        foreach (var name in read)
        {
            Assert.Contains(name, documented, $"{name} is read but not documented in the header.");
        }
    }

    [TestMethod]
    public void BitNavPanelShouldNeverDeclareAPublicVariable()
    {
        var body = SourceFiles.StripScssComments(ReadStylesheet());

        Assert.IsFalse(DeclaredVariable().IsMatch(body), "A public --bit-NavPanel-* variable is declared, which stops it inheriting.");
    }

    [TestMethod]
    public void BitNavPanelDemoShouldListEveryPublicVariable()
    {
        var documented = DocumentedVariables(ReadStylesheet()).Order().ToArray();
        var listed = DemoVariable().Matches(ReadDemo()).Select(m => m.Groups[1].Value).Distinct().Order().ToArray();

        Assert.AreSequenceEqual(documented, listed, "The demo page's CSS variables table and the stylesheet header disagree.");
    }

    [TestMethod]
    public void BitNavPanelShouldLetTheWidthParametersWinOverTheirPublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // The Width and ToggledWidth parameters travel as the private --bit-npn-w / --bit-npn-tw, read ahead of the
        // public variables, which only restyle the default an unset parameter stands for.
        Assert.Contains("width: var(--bit-npn-w, var(--bit-NavPanel-width, auto));", stylesheet);
        Assert.Contains("max-width: var(--bit-npn-w, var(--bit-NavPanel-width, 100%));", stylesheet);
        Assert.Contains("min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(12)}));", stylesheet);
        Assert.Contains("min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(8)}));", stylesheet);
        Assert.Contains("min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(6)}));", stylesheet);

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-NavPanel-[a-z-]+, var\(--bit-npn-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNavPanelShouldStartItsPrivateWidthsOutUnsetOnEveryRoot()
    {
        var stylesheet = ReadStylesheet();

        // A panel nested in another one's content (a footer, a header) never takes the outer one's Width for its own.
        var list = SourceFiles.GetPrivatePropertyLists(stylesheet).Single(l => l.Name == "npn-private-properties");

        Assert.AreSequenceEqual(new[] { "w", "tw" }, list.Names.ToArray());

        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-npn {");

        Assert.Contains("--bit-npn-w: initial;", root);
        Assert.Contains("--bit-npn-tw: initial;", root);
    }

    [TestMethod]
    public void BitNavPanelShapesShouldWeighNothing()
    {
        var stylesheet = ReadStylesheet();

        // The shape classes are wrapped in :where(), so the rules they select weigh what the bare .bit-npn rules
        // weigh and a Class an app restyles the panel with keeps winning over them.
        Assert.Contains("&:where(.bit-npn-col) {\n        @include npn-column;", stylesheet);
        Assert.Contains("&:where(.bit-npn-drw) {\n        @include npn-drawer;", stylesheet);
        Assert.Contains("&:where(.bit-npn-#{$class}) {", stylesheet);

        // Until the browser has reported the screen, the breakpoint itself is the class, split at the default breakpoints.
        Assert.Contains("bsm: $mq-sm-min,", stylesheet);
        Assert.Contains("bmd: $mq-md-min,", stylesheet);
        Assert.Contains("blg: $mq-lg-min,", stylesheet);
        Assert.Contains("bxl: $mq-xl-min", stylesheet);
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldDockToTheEdgeOfTheDirectionItIsLaidOutIn()
    {
        var drawer = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@mixin npn-drawer {");

        // The direction is the one the panel is laid out in (:dir), with the class an explicit Dir renders as the
        // fallback of an engine without :dir(); End turns it round, and both at once is back where it started.
        Assert.Contains("&:is(.bit-rtl, :dir(rtl)),\n    &.bit-npn-end {\n        right: 0;\n        left: unset;", drawer);
        Assert.Contains("&:is(.bit-rtl, :dir(rtl)).bit-npn-end {\n        left: 0;\n        right: unset;", drawer);
        Assert.Contains("&:is(.bit-rtl, :dir(rtl)),\n        &.bit-npn-end {\n            transform: translateX(100%);", drawer);
        Assert.Contains("&:is(.bit-rtl, :dir(rtl)).bit-npn-end {\n            transform: translateX(-100%);", drawer);

        // Every :dir(rtl) stands in the forgiving :is() with the class, never on its own.
        var stylesheet = ReadStylesheet();
        Assert.HasCount(Regex.Matches(stylesheet, @":dir\(rtl\)").Count, Regex.Matches(stylesheet, @":is\(\.bit-rtl, :dir\(rtl\)\)"));
    }

    [TestMethod]
    public void BitNavPanelClosedDrawerShouldBeHiddenOutright()
    {
        var closed = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-npn-cls {");

        // A panel only moved off screen keeps its links in the tab order and the accessibility tree.
        Assert.Contains("visibility: hidden;", closed);
        Assert.Contains("pointer-events: none;", closed);
        Assert.Contains("transform: translateX(-100%);", closed);
        // The visibility waits for the slide out, so the drawer is not cut off mid-way.
        Assert.Contains("visibility 0s linear $mot-duration", closed);
    }

    [TestMethod]
    public void BitNavPanelShouldFollowTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        // The drawer is a surface lifted over the page and moves with the popup easings; the column travels between
        // its two widths with the state easing, and a FitWidth panel jumps instead.
        Assert.Contains("box-shadow: var(--bit-NavPanel-drawer-shadow, #{$box-shadow-sheet});", stylesheet);
        Assert.Contains("transition: transform $mot-duration $mot-easing-decelerate, opacity $mot-duration-short $mot-easing-decelerate;", stylesheet);
        Assert.Contains("transition: transform $mot-duration $mot-easing-accelerate, opacity $mot-duration-short $mot-easing-accelerate, visibility 0s linear $mot-duration;", stylesheet);
        Assert.Contains("&:not(.bit-npn-fiw) {\n        transition: min-width $mot-duration $mot-easing, max-width $mot-duration $mot-easing;", stylesheet);
        Assert.Contains("border-radius: var(--bit-NavPanel-radius, #{$shp-radius-sheet});", stylesheet);
        Assert.Contains("z-index: $zindex-callout;", stylesheet);
        Assert.Contains("font-size: $tg-fs-md;", stylesheet);
        Assert.Contains("font-weight: $tg-fw-semibold;", stylesheet);

        Assert.DoesNotContain("cubic-bezier(", stylesheet, "A literal easing is written instead of a motion token.");
    }

    [TestMethod]
    public void BitNavPanelOverlayShouldReadItsOwnVariablesAndFollowTheDrawer()
    {
        var overlay = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-npn-ovl {");

        Assert.Contains("background-color: var(--bit-NavPanel-overlay-background, #{$clr-bg-overlay});", overlay);
        Assert.Contains("backdrop-filter: var(--bit-NavPanel-overlay-backdrop-filter, none);", overlay);
        Assert.Contains("z-index: $zindex-overlay;", overlay);
        // Until the browser has answered, the overlay is taken away on the screens the column is on.
        Assert.Contains("&.bit-npn-#{$class} {\n            @media screen and (min-width: #{$min}) {\n                display: none;", overlay);
    }

    [TestMethod]
    public void BitNavPanelShouldPaintTheAccentFromTheClassOfEveryColor()
    {
        var stylesheet = ReadStylesheet();

        foreach (var cssClass in _accentClasses)
        {
            Assert.Contains($"\n.{cssClass} {{\n    --bit-npn-clr-bg: ", stylesheet, $"{cssClass} paints no accent.");
        }

        Assert.Contains("background-color: var(--bit-npn-clr-bg);", SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-cnt {"));
    }

    [TestMethod]
    public void BitNavPanelShouldKeepItsEdgeInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        // The background, the shadow and the overlay tint are all thrown away, so the rule along the edge that faces
        // the page is drawn instead - and on the other edge for a panel docked to the end.
        Assert.Contains("border-inline-end-width: $shp-border-width;", forced);
        Assert.Contains("border-inline-end-color: CanvasText;", forced);
        Assert.Contains("&.bit-npn-end {\n            border-inline-end-width: 0;\n            border-inline-start-width: $shp-border-width;\n            border-inline-start-color: CanvasText;", forced);
    }

    [TestMethod]
    public void BitNavPanelShouldHideItsLiveRegionAndTheDrawersOnlyControls()
    {
        var stylesheet = ReadStylesheet();

        // The live region is read by a screen reader and never seen.
        Assert.Contains("@include visually-hidden;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-lvr {"));

        // The close button belongs to the drawer and the toggle button to the column: each is out of the layout where
        // the other is the control that belongs there.
        Assert.Contains("display: none;", SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-cbw {"));

        var drawer = SourceFiles.GetScssBlock(stylesheet, "\n@mixin npn-drawer {");
        Assert.Contains(".bit-npn-tbn {\n        display: none;", drawer);
        Assert.Contains(".bit-npn-cbw {\n        display: contents;", drawer);
    }

    private static string[] DocumentedVariables(string stylesheet)
    {
        return DocumentedVariable().Matches(stylesheet).Select(m => m.Groups[1].Value).Distinct().ToArray();
    }

    private static string ReadStylesheet() => SourceFiles.ReadStylesheet("Bit.BlazorUI.Extras", "Components", "NavPanel", "BitNavPanel.scss");

    private static string ReadDemo() => SourceFiles.Read("Demo", "Client", "Bit.BlazorUI.Demo.Client.Core", "Pages", "Components", "Extras", "NavPanel", "BitNavPanelDemo.razor.cs");

    [GeneratedRegex(@"^//\s+(--bit-NavPanel-[a-z-]+)\s", RegexOptions.Multiline)]
    private static partial Regex DocumentedVariable();

    [GeneratedRegex(@"var\((--bit-NavPanel-[a-z-]+)[,)]")]
    private static partial Regex ReadVariable();

    [GeneratedRegex(@"(^|[;{\s])--bit-NavPanel-[a-z-]+\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclaredVariable();

    [GeneratedRegex(@"Name = ""(--bit-NavPanel-[a-z-]+)""")]
    private static partial Regex DemoVariable();
}
