using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins how the popups a narrow screen shows as panels make room for the safe areas of the screen edges they run to -
/// the status bar and the notch of a phone, its home indicator - and for the bars of an app shell, which paint over
/// them (#14549). One mixin of functions.scss names the insets, so every panel is inset by the same thing: the largest of
/// the safe area a page gives an app shell, the inset of the device and the maximum the device reports for it. Each
/// panel draws the top one as a transparent top border of its surface - painted by the background of that surface,
/// moving everything placed against the top of the box it borders, and left alone by any padding a consumer gives it -
/// and hands the bottom one to Callouts.position, which ends its list above it (or the panel itself, when it has no list
/// of its own: one coming down from the top edge reaches the bottom one once its content grows it that far), in a
/// registered length no other callout inherits. A sheet coming up from the bottom edge (a Responsive BitMenuButton) lies
/// against that edge whatever height it is given, so it draws the bottom one as a transparent bottom border instead.
/// </summary>
/// <remarks>
/// A panel pinned to the top edge with <c>top: 0</c> alone put the month header and the close buttons under an app
/// shell's top bar, which nothing the page renders can paint over, and a panel running to the bottom edge put the
/// last items of its list - or the actions of a picker grown that far - under the bottom one.
/// </remarks>
[TestClass]
public sealed class BitResponsivePanelSafeAreaContractTests
{
    // Every panel whose content the component lays out itself: the package, the stylesheet under its Components
    // folder, the class prefix, and the rule inside the lt-sm block of the responsive class that draws the band ("" for
    // that block itself, the callout).
    public static IEnumerable<object[]> Panels =>
    [
        ["Bit.BlazorUI", "Inputs/DatePicker/BitDatePicker.scss", "dtp", ".bit-dtp-cac"],
        ["Bit.BlazorUI", "Inputs/DateRangePicker/BitDateRangePicker.scss", "dtrp", ".bit-dtrp-cac"],
        ["Bit.BlazorUI", "Inputs/TimePicker/BitTimePicker.scss", "tpc", ".bit-tpc-cac"],
        ["Bit.BlazorUI", "Inputs/CircularTimePicker/BitCircularTimePicker.scss", "ctp", ".bit-ctp-tlb, .bit-ctp-chd"],
        ["Bit.BlazorUI", "Inputs/Dropdown/BitDropdown.scss", "drp", ""],
        ["Bit.BlazorUI.Extras", "TimeZonePicker/BitTimeZonePicker.scss", "tzp", ""],
        ["Bit.BlazorUI.Extras", "CountryPicker/BitCountryPicker.scss", "cnp", ""],
        ["Bit.BlazorUI.Extras", "PhoneInput/BitPhoneInput.scss", "phi", ""],
    ];

    // Every sheet whose content the component lays out itself that comes up from the bottom edge and is as tall as its
    // content, in the shape of Panels with no band: the package, the stylesheet under its Components folder and the class
    // prefix. A short one never reaches the top edge, so a band drawn there would only push its content down from its
    // own top; what it lies against is the bottom edge.
    public static IEnumerable<object[]> BottomSheets =>
    [
        ["Bit.BlazorUI", "Buttons/MenuButton/BitMenuButton.scss", "mnb", ""],
    ];

    // The panels that hold the consumer's own content, which the consumer pads through the public padding variable
    // of the callout rather than the component laying it out: the package and the stylesheet under its Components folder.
    private static readonly (string Project, string Path)[] ConsumerContentPanels =
    [
        ("Bit.BlazorUI", "Surfaces/Callout/BitCallout.scss"),
        ("Bit.BlazorUI", "Navs/DropMenu/BitDropMenu.scss"),
    ];

    // The absolutely positioned parts of the CircularTimePicker that are laid out against its dial rather than against
    // the container its band does not border (see APanelShouldKeepWhatItPlacesAgainstTheTopOfItsBoxBelowTheBand).
    private static readonly string[] CircularTimePickerDialParts = [".bit-ctp-pin", ".bit-ctp-ptr", ".bit-ctp-pth", ".bit-ctp-num", ".bit-ctp-clf::before"];

    [TestMethod]
    public void TheMixinShouldInsetEachEdgeByTheLargestOfWhatCanCoverIt()
    {
        var functions = SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI", "Styles", "functions.scss"));
        var mixin = Normalize(SourceFiles.GetScssBlock(functions, "@mixin responsive-panel-safe-area(", "The responsive-panel-safe-area mixin is missing."));

        foreach (var edge in new[] { "top", "bottom" })
        {
            StringAssert.Contains(mixin, $"$device-{edge}: (env(safe-area-inset-{edge}, 0px), env(safe-area-max-inset-{edge}, 0px))",
                $"The device insets the {edge} edge defaults to are not the inset of the device and the maximum it reports for it.");
        }

        StringAssert.Contains(mixin, "--bit-#{$prefix}-sa-top: max(var(--bit-AppShell-safe-area-top, 0px), #{$device-top});",
            "The top edge is not inset by the largest of the safe area of an app shell and the insets of the device.");
        StringAssert.Contains(mixin, "--bit-callout-sa-bottom: max(var(--bit-AppShell-safe-area-bottom, 0px), #{$device-bottom});",
            "The bottom edge is not inset by the largest of the safe area of an app shell and the insets of the device.");
    }

    [TestMethod]
    public void TheBottomSafeAreaShouldBeARegisteredLengthThatIsNotInherited()
    {
        var general = SourceFiles.StripScssComments(SourceFiles.Read("Bit.BlazorUI", "Styles", "general.scss"));
        var property = Normalize(SourceFiles.GetScssBlock(general, "@property --bit-callout-sa-bottom", "--bit-callout-sa-bottom is not registered."));

        // A length is what Callouts.position reads resolved, env() included; and only the panel that names it has it.
        StringAssert.Contains(property, "syntax: \"<length>\";");
        StringAssert.Contains(property, "inherits: false;");
        StringAssert.Contains(property, "initial-value: 0px;");
    }

    [TestMethod]
    public void CalloutsShouldEndTheListOfAPanelAboveTheBottomSafeAreaUnlessTheKeyboardCoversIt()
    {
        var position = SourceFiles.Read("Bit.BlazorUI", "Scripts", "Callouts.ts");

        StringAssert.Contains(position, "getPropertyValue('--bit-callout-sa-bottom')",
            "Callouts.position does not read the safe area a panel names for the bottom of the screen.");
        Assert.IsTrue(Regex.IsMatch(position, @"keyboardUp\s*\?\s*0\s*:"),
            "Callouts.position keeps the bottom safe area while the on-screen keyboard covers the bottom edge.");
        Assert.IsTrue(Regex.IsMatch(position, @"bottomGap\s*=\s*Math\.max\(10,\s*bottomSafeArea\(\)\)"),
            "Callouts.position does not end the list of a panel above the bottom safe area.");
        // A panel coming down from the top edge has no list of its own, and its content can still grow it down to the
        // bottom one, where the actions of a picker would sit under the home indicator.
        Assert.IsTrue(Regex.IsMatch(position, @"callout\.style\.maxHeight\s*=\s*\(scrollContainerId\s*\?\s*visualHeight\s*:\s*Math\.max\(0,\s*visualHeight\s*-\s*bottomSafeArea\(\)\)\)"),
            "Callouts.position does not end a panel with no list of its own above the bottom safe area.");
        Assert.IsFalse(position.Contains("calloutStyle.paddingBottom", StringComparison.Ordinal),
            "Callouts.position reads the padding of the callout, which a consumer pads a callout of its own with.");
    }

    [TestMethod]
    [DynamicData(nameof(Panels))]
    [DynamicData(nameof(BottomSheets))]
    public void APanelShouldNameTheSafeAreasOfTheScreenThroughTheMixin(string project, string path, string prefix, string band)
    {
        var panel = GetPanel(GetRules(project, path), prefix);
        var include = panel.Declarations.Select(Normalize).SingleOrDefault(d => d.StartsWith("@include responsive-panel-safe-area(", StringComparison.Ordinal));

        Assert.IsNotNull(include, $"{path} does not name the safe areas of its panel through responsive-panel-safe-area.");
        Assert.IsTrue(Regex.IsMatch(include, $@"^@include responsive-panel-safe-area\({Regex.Escape(prefix)}\s*[,)]"), $"{path} names the safe areas under another prefix.");

        // The Extras package publishes the insets of the device in variables an app adjusts them for the whole package with.
        if (project == "Bit.BlazorUI.Extras")
        {
            StringAssert.Contains(include, "$device-top: ($bit-env-inset-top, $bit-env-max-inset-top)", $"{path} reads the top inset past the variables of the package.");
            StringAssert.Contains(include, "$device-bottom: ($bit-env-inset-bottom, $bit-env-max-inset-bottom)", $"{path} reads the bottom inset past the variables of the package.");
        }

        // Read once, in the mixin, so a later fix to what the inset is made of reaches every panel.
        var code = SourceFiles.StripScssComments(SourceFiles.Read([project, "Components", .. path.Split('/')]));
        Assert.IsFalse(code.Contains("env(safe-area-", StringComparison.Ordinal), $"{path} reads a safe area of the device itself.");
    }

    [TestMethod]
    [DynamicData(nameof(Panels))]
    public void APanelShouldDrawTheTopSafeAreaAsATransparentBorderOfItsSurface(string project, string path, string prefix, string band)
    {
        var rules = GetRules(project, path);
        var panel = GetPanel(rules, prefix);
        var surface = band.Length == 0
            ? panel
            : rules.SingleOrDefault(r => Normalize(r.Header) == band && r.Ancestors.SequenceEqual([$".bit-{prefix}-res", "@include lt-sm"]));

        Assert.IsNotNull(surface, $"{band} is not declared in the lt-sm block of .bit-{prefix}-res in {path}.");
        CollectionAssert.Contains(surface.Declarations.Select(Normalize).ToList(), $"border-top: var(--bit-{prefix}-sa-top) solid transparent",
            $"{path} does not draw the safe area at the top as the border of the surface of its panel.");

        // A padding is replaced by any a consumer writes on the element, and moves nothing placed against its box.
        var padded = rules.Where(r => r.Ancestors.Take(2).SequenceEqual([$".bit-{prefix}-res", "@include lt-sm"]) || r == panel)
                          .SelectMany(r => r.Declarations)
                          .Where(d => d.StartsWith("padding", StringComparison.Ordinal) && d.Contains($"--bit-{prefix}-sa-", StringComparison.Ordinal))
                          .ToArray();

        Assert.AreEqual(0, padded.Length, $"{path} pads its panel by a safe area: {string.Join("; ", padded)}");
    }

    [TestMethod]
    [DynamicData(nameof(BottomSheets))]
    public void ABottomSheetShouldDrawTheBottomSafeAreaAsATransparentBorderOfItsSurface(string project, string path, string prefix, string band)
    {
        var rules = GetRules(project, path);
        var declarations = GetPanel(rules, prefix).Declarations.Select(Normalize).ToList();

        Assert.AreEqual(string.Empty, band, $"{path} is a bottom sheet with a band at its top.");

        // A sheet that lies against the bottom edge is not moved off it by the shorter height Callouts.position gives it
        // there, so it keeps its last item above the safe area itself - as a border, for the reasons the band of the
        // other panels is one.
        CollectionAssert.Contains(declarations, "bottom: 0", $"{path} does not lie its sheet against the bottom edge.");
        CollectionAssert.Contains(declarations, "border-bottom: var(--bit-callout-sa-bottom) solid transparent",
            $"{path} does not draw the safe area at the bottom as the border of its sheet.");

        var padded = rules.Where(r => r.Ancestors.Take(2).SequenceEqual([$".bit-{prefix}-res", "@include lt-sm"]))
                          .SelectMany(r => r.Declarations)
                          .Concat(declarations)
                          .Where(d => d.StartsWith("padding", StringComparison.Ordinal) && d.Contains("-sa-", StringComparison.Ordinal))
                          .ToArray();

        Assert.AreEqual(0, padded.Length, $"{path} pads its sheet by a safe area: {string.Join("; ", padded)}");
    }

    [TestMethod]
    [DynamicData(nameof(Panels))]
    public void APanelShouldKeepWhatItPlacesAgainstTheTopOfItsBoxBelowTheBand(string project, string path, string prefix, string band)
    {
        var rules = GetRules(project, path);

        if (band.Length == 0)
        {
            // The band borders the callout, which is fixed, so it is the box every absolutely positioned part of the panel
            // is placed against - and the padding box of that one starts below its border.
            var callout = rules.SingleOrDefault(r => r.Ancestors.Count == 0 && r.Header == $".bit-{prefix}-cal" && r.Declarations.Contains("position: fixed"));
            Assert.IsNotNull(callout, $".bit-{prefix}-cal of {path} is not the fixed box the parts of its panel are placed against.");
            return;
        }

        if (band == $".bit-{prefix}-cac")
        {
            // The band borders the container, which is the box the parts inside it are placed against.
            var container = rules.SingleOrDefault(r => r.Ancestors.Count == 0 && r.Header == band && r.Declarations.Contains("position: relative"));
            Assert.IsNotNull(container, $"{band} of {path} is not the box the parts of its panel are placed against.");
            return;
        }

        // The band borders the first part of the container instead, so whatever is placed against the container is not
        // moved by it. Every absolutely positioned part of the stylesheet, at any depth, is either one laid out against
        // the dial or one the panel moves down by the safe area itself.
        Assert.AreEqual("ctp", prefix, $"{path} draws its band on a part no rule of this test accounts for.");
        Assert.IsTrue(rules.Any(r => r.Ancestors.Count == 0 && r.Header == ".bit-ctp-clk" && r.Declarations.Contains("position: relative")),
            "The dial is not the box its parts are placed against.");

        var parts = rules.Where(r => r.Declarations.Contains("position: absolute")).Select(r => r.Header).Distinct().ToArray();
        var unaccounted = parts.Except(CircularTimePickerDialParts).Except([".bit-ctp-cbn"]).ToArray();

        Assert.AreEqual(0, unaccounted.Length, $"{path} places parts absolutely that may sit under the safe area: {string.Join(", ", unaccounted)}");

        var close = rules.SingleOrDefault(r => r.Header == ".bit-ctp-cbn" && r.Ancestors.SequenceEqual([".bit-ctp-res", "@include lt-sm"]));
        Assert.IsNotNull(close, "The close button of the panel is not restyled for it.");
        CollectionAssert.Contains(close.Declarations.ToList(), "top: var(--bit-ctp-sa-top)", "The close button is laid over the corner of the container, under the safe area there.");
    }

    [TestMethod]
    public void EveryResponsivePanelShouldBeCovered()
    {
        // Keyed by the package and the path under its Components folder, so a stylesheet that only shares the name of a
        // covered one is still caught.
        var covered = Panels.Concat(BottomSheets).Select(p => Key((string)p[0], (string)p[1]))
                            .Concat(ConsumerContentPanels.Select(p => Key(p.Project, p.Path)))
                            .ToHashSet(StringComparer.Ordinal);

        var panels = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.AI" }
            .SelectMany(project =>
            {
                var components = SourceFiles.GetDirectory(project, "Components");

                return Directory.EnumerateFiles(components, "*.scss", SearchOption.AllDirectories)
                                .Where(file => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Contains("@include responsive-panel-closed;", StringComparison.Ordinal))
                                .Select(file => Key(project, Path.GetRelativePath(components, file)));
            })
            .ToArray();

        Assert.IsTrue(panels.Length >= covered.Count, $"Only {panels.Length} stylesheets draw a responsive panel; the scan no longer finds them.");

        var missing = panels.Where(key => covered.Contains(key) is false).ToArray();

        Assert.AreEqual(0, missing.Length,
            "A stylesheet draws a responsive panel that is not pinned here; make it name the safe areas of the edges of the " +
            "screen it runs to through responsive-panel-safe-area and add it to Panels: " + string.Join(", ", missing));

        static string Key(string project, string path) => $"{project}/{path.Replace('\\', '/')}";
    }

    private static IReadOnlyList<SourceFiles.ScssRule> GetRules(string project, string path)
    {
        return SourceFiles.GetScssRules(SourceFiles.ReadStylesheet([project, "Components", .. path.Split('/')]));
    }

    // The rules of the panel, which every responsive class declares inside its lt-sm block.
    private static SourceFiles.ScssRule GetPanel(IReadOnlyList<SourceFiles.ScssRule> rules, string prefix)
    {
        var panel = rules.SingleOrDefault(r => r.Header == "@include lt-sm" && r.Ancestors.SequenceEqual([$".bit-{prefix}-res"]));

        Assert.IsNotNull(panel, $"The lt-sm block of .bit-{prefix}-res was not found.");

        return panel;
    }

    // A statement or a header with every run of white space - the line breaks of one written over several lines among
    // them - read as one space.
    private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();
}
