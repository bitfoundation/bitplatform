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
            StringAssert.Contains(stylesheet, $"var({name}, ", $"{name} is documented but never read with a fallback.");
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
            CollectionAssert.Contains(documented, name, $"{name} is read but not documented in the header.");
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

        CollectionAssert.AreEqual(documented, listed, "The demo page's CSS variables table and the stylesheet header disagree.");
    }

    [TestMethod]
    public void BitNavPanelShouldLetTheWidthParametersWinOverTheirPublicVariables()
    {
        var stylesheet = ReadStylesheet();

        // The Width and ToggledWidth parameters travel as the private --bit-npn-w / --bit-npn-tw, read ahead of the
        // public variables, which only restyle the default an unset parameter stands for.
        StringAssert.Contains(stylesheet, "width: var(--bit-npn-w, var(--bit-NavPanel-width, auto));");
        StringAssert.Contains(stylesheet, "max-width: var(--bit-npn-w, var(--bit-NavPanel-width, 100%));");
        StringAssert.Contains(stylesheet, "min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(12)}));");
        StringAssert.Contains(stylesheet, "min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(8)}));");
        StringAssert.Contains(stylesheet, "min-width: var(--bit-npn-tw, var(--bit-NavPanel-toggled-width, #{spacing(6)}));");

        Assert.IsFalse(Regex.IsMatch(stylesheet, @"var\(--bit-NavPanel-[a-z-]+, var\(--bit-npn-"), "A public variable is read before the parameter it restyles the default of.");
    }

    [TestMethod]
    public void BitNavPanelShouldStartItsPrivateWidthsOutUnsetOnEveryRoot()
    {
        var stylesheet = ReadStylesheet();

        // A panel nested in another one's content (a footer, a header) never takes the outer one's Width for its own.
        var list = SourceFiles.GetPrivatePropertyLists(stylesheet).Single(l => l.Name == "npn-private-properties");

        CollectionAssert.AreEqual(new[] { "w", "tw" }, list.Names.ToArray());

        var root = SourceFiles.GetScssDeclarations(stylesheet, "\n.bit-npn {");

        StringAssert.Contains(root, "--bit-npn-w: initial;");
        StringAssert.Contains(root, "--bit-npn-tw: initial;");
    }

    [TestMethod]
    public void BitNavPanelShapesShouldWeighNothing()
    {
        var stylesheet = ReadStylesheet();

        // The shape classes are wrapped in :where(), so the rules they select weigh what the bare .bit-npn rules
        // weigh and a Class an app restyles the panel with keeps winning over them.
        StringAssert.Contains(stylesheet, "&:where(.bit-npn-col) {\n        @include npn-column;");
        StringAssert.Contains(stylesheet, "&:where(.bit-npn-drw) {\n        @include npn-drawer;");
        StringAssert.Contains(stylesheet, "&:where(.bit-npn-#{$class}) {");

        // Until the browser has reported the screen, the breakpoint itself is the class, split at the default breakpoints.
        StringAssert.Contains(stylesheet, "bsm: $mq-sm-min,");
        StringAssert.Contains(stylesheet, "bmd: $mq-md-min,");
        StringAssert.Contains(stylesheet, "blg: $mq-lg-min,");
        StringAssert.Contains(stylesheet, "bxl: $mq-xl-min");
    }

    [TestMethod]
    public void BitNavPanelDrawerShouldDockToTheEdgeOfTheDirectionItIsLaidOutIn()
    {
        var drawer = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@mixin npn-drawer {");

        // The direction is the one the panel is laid out in (:dir), with the class an explicit Dir renders as the
        // fallback of an engine without :dir(); End turns it round, and both at once is back where it started.
        StringAssert.Contains(drawer, "&:is(.bit-rtl, :dir(rtl)),\n    &.bit-npn-end {\n        right: 0;\n        left: unset;");
        StringAssert.Contains(drawer, "&:is(.bit-rtl, :dir(rtl)).bit-npn-end {\n        left: 0;\n        right: unset;");
        StringAssert.Contains(drawer, "&:is(.bit-rtl, :dir(rtl)),\n        &.bit-npn-end {\n            transform: translateX(100%);");
        StringAssert.Contains(drawer, "&:is(.bit-rtl, :dir(rtl)).bit-npn-end {\n            transform: translateX(-100%);");

        // Every :dir(rtl) stands in the forgiving :is() with the class, never on its own.
        var stylesheet = ReadStylesheet();
        Assert.AreEqual(Regex.Matches(stylesheet, @":dir\(rtl\)").Count, Regex.Matches(stylesheet, @":is\(\.bit-rtl, :dir\(rtl\)\)").Count);
    }

    [TestMethod]
    public void BitNavPanelClosedDrawerShouldBeHiddenOutright()
    {
        var closed = SourceFiles.GetScssBlock(ReadStylesheet(), "\n    &.bit-npn-cls {");

        // A panel only moved off screen keeps its links in the tab order and the accessibility tree.
        StringAssert.Contains(closed, "visibility: hidden;");
        StringAssert.Contains(closed, "pointer-events: none;");
        StringAssert.Contains(closed, "transform: translateX(-100%);");
        // The visibility waits for the slide out, so the drawer is not cut off mid-way.
        StringAssert.Contains(closed, "visibility 0s linear $mot-duration");
    }

    [TestMethod]
    public void BitNavPanelShouldFollowTheThemeTokens()
    {
        var stylesheet = ReadStylesheet();

        // The drawer is a surface lifted over the page and moves with the popup easings; the column travels between
        // its two widths with the state easing, and a FitWidth panel jumps instead.
        StringAssert.Contains(stylesheet, "box-shadow: var(--bit-NavPanel-drawer-shadow, #{$box-shadow-sheet});");
        StringAssert.Contains(stylesheet, "transition: transform $mot-duration $mot-easing-decelerate, opacity $mot-duration-short $mot-easing-decelerate;");
        StringAssert.Contains(stylesheet, "transition: transform $mot-duration $mot-easing-accelerate, opacity $mot-duration-short $mot-easing-accelerate, visibility 0s linear $mot-duration;");
        StringAssert.Contains(stylesheet, "&:not(.bit-npn-fiw) {\n        transition: min-width $mot-duration $mot-easing, max-width $mot-duration $mot-easing;");
        StringAssert.Contains(stylesheet, "border-radius: var(--bit-NavPanel-radius, #{$shp-radius-sheet});");
        StringAssert.Contains(stylesheet, "z-index: $zindex-callout;");
        StringAssert.Contains(stylesheet, "font-size: $tg-fs-md;");
        StringAssert.Contains(stylesheet, "font-weight: $tg-fw-semibold;");

        Assert.IsFalse(stylesheet.Contains("cubic-bezier("), "A literal easing is written instead of a motion token.");
    }

    [TestMethod]
    public void BitNavPanelOverlayShouldReadItsOwnVariablesAndFollowTheDrawer()
    {
        var overlay = SourceFiles.GetScssBlock(ReadStylesheet(), "\n.bit-npn-ovl {");

        StringAssert.Contains(overlay, "background-color: var(--bit-NavPanel-overlay-background, #{$clr-bg-overlay});");
        StringAssert.Contains(overlay, "backdrop-filter: var(--bit-NavPanel-overlay-backdrop-filter, none);");
        StringAssert.Contains(overlay, "z-index: $zindex-overlay;");
        // Until the browser has answered, the overlay is taken away on the screens the column is on.
        StringAssert.Contains(overlay, "&.bit-npn-#{$class} {\n            @media screen and (min-width: #{$min}) {\n                display: none;");
    }

    [TestMethod]
    public void BitNavPanelShouldPaintTheAccentFromTheClassOfEveryColor()
    {
        var stylesheet = ReadStylesheet();

        foreach (var cssClass in _accentClasses)
        {
            StringAssert.Contains(stylesheet, $"\n.{cssClass} {{\n    --bit-npn-clr-bg: ", $"{cssClass} paints no accent.");
        }

        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-cnt {"), "background-color: var(--bit-npn-clr-bg);");
    }

    [TestMethod]
    public void BitNavPanelShouldKeepItsEdgeInForcedColors()
    {
        var forced = SourceFiles.GetScssBlock(ReadStylesheet(), "\n@media (forced-colors: active) {");

        // The background, the shadow and the overlay tint are all thrown away, so the rule along the edge that faces
        // the page is drawn instead - and on the other edge for a panel docked to the end.
        StringAssert.Contains(forced, "border-inline-end-width: $shp-border-width;");
        StringAssert.Contains(forced, "border-inline-end-color: CanvasText;");
        StringAssert.Contains(forced, "&.bit-npn-end {\n            border-inline-end-width: 0;\n            border-inline-start-width: $shp-border-width;\n            border-inline-start-color: CanvasText;");
    }

    [TestMethod]
    public void BitNavPanelShouldHideItsLiveRegionAndTheDrawersOnlyControls()
    {
        var stylesheet = ReadStylesheet();

        // The live region is read by a screen reader and never seen.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-lvr {"), "@include visually-hidden;");

        // The close button belongs to the drawer and the toggle button to the column: each is out of the layout where
        // the other is the control that belongs there.
        StringAssert.Contains(SourceFiles.GetScssBlock(stylesheet, "\n.bit-npn-cbw {"), "display: none;");

        var drawer = SourceFiles.GetScssBlock(stylesheet, "\n@mixin npn-drawer {");
        StringAssert.Contains(drawer, ".bit-npn-tbn {\n        display: none;");
        StringAssert.Contains(drawer, ".bit-npn-cbw {\n        display: contents;");
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
