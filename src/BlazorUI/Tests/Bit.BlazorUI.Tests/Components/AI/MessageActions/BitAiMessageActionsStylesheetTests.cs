using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.MessageActions;

[TestClass]
public class BitAiMessageActionsStylesheetTests
{
    // The empty read-aloud status is a visually hidden live region, so it is absolutely positioned against the nearest
    // positioned ancestor - usually the chat, outside the box the transcript scrolls in. Left where it would have been
    // laid out, the status of an answer below the fold would hang past the chat and give whatever scrolls around it a
    // scrollbar of its own (#14995). Pinned to the start corner of that ancestor, with the mixin's negative margin taken
    // back, its pixel is inside it whatever ancestor it is and whichever way it reads.
    [TestMethod]
    public void TheEmptyReadAloudStatusIsPinnedInsideTheCornerOfItsContainingBlock()
    {
        var rule = GetRules().Single(rule => rule.Header == "&:empty" && rule.Ancestors.SequenceEqual([".bit-aimac-ras"]));

        CollectionAssert.AreEqual(new[] { "@include visually-hidden", "margin: 0", "inset-block-start: 0", "inset-inline-start: 0" },
                                  rule.Declarations.ToArray());
    }

    // Positioning the row instead would paint it, its metadata included, over every earlier positioned element without a
    // z-index that overlaps it - an app's sticky date header in the transcript - which it used to pass under.
    [TestMethod]
    public void TheRowIsNotPositioned()
    {
        var root = GetRules().Single(rule => rule.Ancestors.Count == 0 && rule.Header == ".bit-aimac");

        Assert.IsFalse(root.Declarations.Any(d => d.StartsWith("position", StringComparison.Ordinal)), string.Join("; ", root.Declarations));
    }

    private static IReadOnlyList<SourceFiles.ScssRule> GetRules() =>
        SourceFiles.GetScssRules(SourceFiles.Read("Bit.BlazorUI.AI", "Components", "MessageActions", "BitAiMessageActions.scss"));
}
