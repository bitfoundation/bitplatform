using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// Pins how the popups a narrow screen shows as panels make room for the safe areas of the screen edges they run to -
/// the status bar and the notch of a phone, its home indicator - which an app shell's bars paint over (#14549): each
/// names the inset of every such edge as a private variable read off env(), pads that edge by it with its surface
/// painted behind, and moves a close button placed against that edge down by it, so its header, its close button and
/// its list stay clear of what covers them.
/// </summary>
/// <remarks>
/// A panel pinned to the top edge with <c>top: 0</c> alone put the month header and the close buttons under an app
/// shell's top bar, which nothing the page renders can paint over, and a panel running to the bottom edge put the
/// last items of its list under the bottom one.
/// </remarks>
[TestClass]
public sealed class BitResponsivePanelSafeAreaContractTests
{
    // Every panel whose content the component lays out itself: the package, the stylesheet under its Components
    // folder, the class prefix and the edges of the screen the panel runs to.
    public static IEnumerable<object[]> Panels =>
    [
        ["Bit.BlazorUI", "Inputs/DatePicker/BitDatePicker.scss", "dtp", "top"],
        ["Bit.BlazorUI", "Inputs/DateRangePicker/BitDateRangePicker.scss", "dtrp", "top"],
        ["Bit.BlazorUI", "Inputs/TimePicker/BitTimePicker.scss", "tpc", "top"],
        ["Bit.BlazorUI", "Inputs/CircularTimePicker/BitCircularTimePicker.scss", "ctp", "top"],
        ["Bit.BlazorUI", "Inputs/Dropdown/BitDropdown.scss", "drp", "top,bottom"],
        ["Bit.BlazorUI.Extras", "TimeZonePicker/BitTimeZonePicker.scss", "tzp", "top,bottom"],
        ["Bit.BlazorUI.Extras", "CountryPicker/BitCountryPicker.scss", "cnp", "top,bottom"],
        ["Bit.BlazorUI.Extras", "PhoneInput/BitPhoneInput.scss", "phi", "top,bottom"],
    ];

    // The panels that hold the consumer's own content, which the consumer pads through the public padding variable
    // of the callout rather than the component laying it out.
    private static readonly string[] ConsumerContentPanels = ["BitCallout.scss", "BitDropMenu.scss"];

    [TestMethod]
    [DynamicData(nameof(Panels))]
    public void APanelShouldPadTheEdgesItRunsToByTheirSafeArea(string project, string path, string prefix, string edges)
    {
        var panel = GetPanel(GetRules(project, path), prefix);

        foreach (var edge in edges.Split(','))
        {
            CollectionAssert.Contains(panel.Declarations.ToList(), $"--bit-{prefix}-sa-{edge}: env(safe-area-inset-{edge}, 0px)",
                $"{path} does not name the safe area of the {edge} edge its panel runs to.");

            Assert.IsTrue(panel.Declarations.Any(d => d.StartsWith("padding", StringComparison.Ordinal) && d.Contains($"var(--bit-{prefix}-sa-{edge})", StringComparison.Ordinal)),
                $"{path} does not pad the {edge} edge of its panel by the safe area there.");
        }
    }

    [TestMethod]
    [DynamicData(nameof(Panels))]
    public void APanelShouldPaintItsSurfaceBehindTheRoomItMakes(string project, string path, string prefix, string edges)
    {
        var rules = GetRules(project, path);
        var panel = GetPanel(rules, prefix);

        // The padding is the callout's own, so it is the callout that has to be painted, whether as a panel or always.
        var painted = rules.Where(r => r == panel || (r.Ancestors.Count == 0 && r.Header == $".bit-{prefix}-cal"))
                           .SelectMany(r => r.Declarations)
                           .Any(d => d.StartsWith("background-color:", StringComparison.Ordinal) || d.StartsWith("background:", StringComparison.Ordinal));

        Assert.IsTrue(painted, $"{path} leaves the room its panel makes for the safe area unpainted, so the page shows through it.");
    }

    [TestMethod]
    [DynamicData(nameof(Panels))]
    public void APanelShouldMoveWhatItPlacesAgainstItsTopEdgeDownByTheSafeArea(string project, string path, string prefix, string edges)
    {
        var rules = GetRules(project, path);
        string[] ancestors = [$".bit-{prefix}-res", "@include lt-sm"];

        // An absolutely positioned part is placed against the padding box, which the padding does not move.
        foreach (var part in rules.Where(r => r.Ancestors.SequenceEqual(ancestors) && r.Declarations.Contains("position: absolute")))
        {
            foreach (var top in part.Declarations.Where(d => d.StartsWith("top:", StringComparison.Ordinal)))
            {
                StringAssert.Contains(top, $"var(--bit-{prefix}-sa-top)", $"{part.Header} of {path} is placed against the top edge of the panel, under the safe area there.");
            }
        }
    }

    [TestMethod]
    public void EveryResponsivePanelShouldBeCovered()
    {
        var covered = Panels.Select(p => Path.GetFileName((string)p[1])).Concat(ConsumerContentPanels).ToHashSet(StringComparer.Ordinal);

        var panels = new[] { "Bit.BlazorUI", "Bit.BlazorUI.Extras", "Bit.BlazorUI.AI" }
            .SelectMany(project => Directory.EnumerateFiles(SourceFiles.GetDirectory(project, "Components"), "*.scss", SearchOption.AllDirectories))
            .Where(file => SourceFiles.StripScssComments(SourceFiles.ReadFullPath(file)).Contains("@include responsive-panel-closed;", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.IsTrue(panels.Length >= covered.Count, $"Only {panels.Length} stylesheets draw a responsive panel; the scan no longer finds them.");

        var missing = panels.Where(name => covered.Contains(name!) is false).ToArray();

        Assert.AreEqual(0, missing.Length,
            "A stylesheet draws a responsive panel that is not pinned here; make it pad the edges of the screen it runs to by " +
            "their safe area and add it to Panels: " + string.Join(", ", missing));
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
}
