using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.CitationMarker;

[TestClass]
public class BitAiCitationMarkerStylesheetTests
{
    private const string ForcedColors = "@media (forced-colors: active)";

    // Under forced colors the pointer would move the marker to the primary fill under its on-color - LinkText under a
    // Canvas number, which the backplate Chromium draws behind the text turns into Canvas on Canvas - so the forced block
    // sets the paint back and carries the state on the edge, in a system color, under the pointer and while pressed.
    [TestMethod,
        DataRow(".bit-aicmk:hover"),
        DataRow(".bit-aicmk:active")]
    public void UnderForcedColorsThePointerStateKeepsThePaintAtRestAndMarksTheEdge(string selector)
    {
        var rules = SourceFiles.GetScssRules(Read())
                               .Where(rule => rule.Ancestors.Contains(ForcedColors) && Selectors(rule).Contains(selector))
                               .ToArray();

        Assert.HasCount(1, rules, $"No single forced-colors rule for {selector}.");
        CollectionAssert.Contains(rules[0].Declarations.ToArray(), "--bit-aicmk-sfg: initial");
        CollectionAssert.Contains(rules[0].Declarations.ToArray(), "--bit-aicmk-sbg: initial");
        CollectionAssert.Contains(rules[0].Declarations.ToArray(), "border-color: Highlight");
    }

    // The disabled marker keeps its GrayText edge under the pointer: its rule follows the pointer one at the same weight.
    [TestMethod]
    public void UnderForcedColorsTheDisabledEdgeFollowsThePointerState()
    {
        var rules = SourceFiles.GetScssRules(Read()).Where(rule => rule.Ancestors.Contains(ForcedColors)).ToArray();

        var pointer = rules.Single(rule => Selectors(rule).Contains(".bit-aicmk:hover"));
        var disabled = rules.Single(rule => Selectors(rule).Contains(".bit-aicmk:disabled"));

        Assert.IsGreaterThan(pointer.Index, disabled.Index);
        CollectionAssert.Contains(disabled.Declarations.ToArray(), "border-color: GrayText");
    }

    private static string Read() => SourceFiles.Read("Bit.BlazorUI.AI", "Components", "CitationMarker", "BitAiCitationMarker.scss");

    private static List<string> Selectors(SourceFiles.ScssRule rule) =>
        SourceFiles.ResolveScssSelectors(rule.Ancestors.Where(a => a.StartsWith('@') is false).Append(rule.Header)).ToList();
}
