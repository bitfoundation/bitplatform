using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.ToolCallView;

[TestClass]
public class BitAiToolCallViewStylesheetTests
{
    // Only the turn of an open card animates: it is the transform, and the mirror of right-to-left text is the scale
    // property, which is not transitioned - so a card changing direction flips its chevron at once. The scale applies
    // after the transform, so the open chevron turned down still points down once mirrored.
    [TestMethod]
    public void TheChevronTransitionsItsTurnAndMirrorsRightToLeftTextWithoutATransition()
    {
        var rules = SourceFiles.GetScssRules(SourceFiles.Read("Bit.BlazorUI.AI", "Components", "ToolCallView", "BitAiToolCallView.scss"));

        var chevron = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aitcv-chv");
        CollectionAssert.Contains(chevron.Declarations.ToArray(), "transition: transform $mot-duration $mot-easing");

        var open = rules.Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aitcv-opn > .bit-aitcv-hdr > .bit-aitcv-chv");
        CollectionAssert.AreEqual(new[] { "transform: rotate(90deg)" }, open.Declarations.ToArray());

        var mirror = rules.Single(rule => rule.Header.StartsWith(".bit-aitcv-chv:is(:dir(rtl)"));
        CollectionAssert.AreEqual(new[] { "scale: -1 1" }, mirror.Declarations.ToArray());
    }
}
