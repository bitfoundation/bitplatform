using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.JsonView;

[TestClass]
public class BitAiJsonViewStylesheetTests
{
    // Only the turn of an open container animates: it is the transform, and the mirror of a right-to-left tree is the
    // scale property, which is not transitioned - so a tree changing direction flips its chevrons at once. The scale
    // applies after the transform, so an open chevron turned down still points down once mirrored.
    [TestMethod]
    public void TheChevronTransitionsItsTurnAndMirrorsARightToLeftTreeWithoutATransition()
    {
        var rules = SourceFiles.GetScssRules(Read());

        var chevron = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aijsv-tic");
        CollectionAssert.Contains(chevron.Declarations.ToArray(), "transition: transform $mot-duration-short $mot-easing");

        var open = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aijsv-top");
        CollectionAssert.AreEqual(new[] { "transform: rotate(90deg)" }, open.Declarations.ToArray());

        var mirrors = rules.Where(rule => Selectors(rule).Any(s => s.Contains(".bit-aijsv-tic") && (s.Contains(":dir(rtl)") || s.Contains(".bit-rtl"))))
                           .ToArray();

        Assert.HasCount(2, mirrors, "The :dir() rule and its fallback for an engine without it.");

        foreach (var mirror in mirrors)
        {
            CollectionAssert.AreEqual(new[] { "scale: -1 1" }, mirror.Declarations.ToArray(), mirror.Header);
        }
    }

    private static string Read() => SourceFiles.Read("Bit.BlazorUI.AI", "Components", "JsonView", "BitAiJsonView.scss");

    private static List<string> Selectors(SourceFiles.ScssRule rule) =>
        SourceFiles.ResolveScssSelectors(rule.Ancestors.Where(a => a.StartsWith('@') is false).Append(rule.Header)).ToList();
}
