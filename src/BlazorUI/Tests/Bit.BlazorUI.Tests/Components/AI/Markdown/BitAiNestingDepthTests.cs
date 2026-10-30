using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 1: nesting is bounded by an option, and deep input is fast rather than fatal.</summary>
[TestClass, DoNotParallelize]
public class BitAiNestingDepthTests
{
    [TestMethod]
    public void FiveThousandNestedQuotesParseQuicklyAndStayWithinTheDefaultDepth()
    {
        var source = BitAiMarkdownSpec.Repeat("> ", 5000) + "x";
        BitAiMarkdownDocument document = null!;

        var elapsed = BitAiMarkdownSpec.Time(() => document = BitAiMarkdownSpec.Parse(source));

        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds:F0}ms.");
        Assert.IsInRange(1, 32, BitAiMarkdownSpec.BlockDepth(document.Blocks));
    }

    [TestMethod]
    public void FiveThousandNestedBracketsParseQuickly()
    {
        var source = BitAiMarkdownSpec.Repeat("[", 5000) + "a" + BitAiMarkdownSpec.Repeat("](u)", 5000);
        var elapsed = BitAiMarkdownSpec.Time(() => BitAiMarkdownSpec.Parse(source));
        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds:F0}ms.");
    }

    [TestMethod]
    public void FiveThousandNestedEmphasisSpansParseQuicklyAndStayWithinTheDefaultDepth()
    {
        var source = BitAiMarkdownSpec.Repeat("*a ", 5000) + "b" + BitAiMarkdownSpec.Repeat(" c*", 5000);
        List<BitAiMarkdownInline> inlines = null!;

        var elapsed = BitAiMarkdownSpec.Time(() => inlines = BitAiInlineParser.Parse(source));

        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds:F0}ms.");
        Assert.IsInRange(1, 32, BitAiMarkdownSpec.InlineDepth(inlines));
    }

    [TestMethod]
    public void DeeplyNestedListsParseQuicklyAndStayWithinTheDefaultDepth()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < 400; i++) builder.Append(' ', i * 2).Append("- a\n");
        var source = builder.ToString();
        BitAiMarkdownDocument document = null!;

        var elapsed = BitAiMarkdownSpec.Time(() => document = BitAiMarkdownSpec.Parse(source));

        Assert.IsTrue(elapsed < TimeSpan.FromSeconds(1), $"Took {elapsed.TotalMilliseconds:F0}ms.");
        Assert.IsInRange(1, 32, BitAiMarkdownSpec.BlockDepth(document.Blocks));
    }

    [TestMethod]
    public void MaxNestingDepthBoundsBlocksAndTurnsDeeperMarkersIntoText()
    {
        var options = new BitAiMarkdownOptions { MaxNestingDepth = 3 };
        var document = BitAiMarkdownSpec.Parse("> > > > > deep", options);

        Assert.AreEqual(3, BitAiMarkdownSpec.BlockDepth(document.Blocks));

        var innermost = document.Blocks[0];
        for (var i = 0; i < 3; i++) innermost = Assert.IsExactInstanceOfType<BitAiQuoteBlock>(innermost).Blocks[0];
        Assert.AreEqual("> > deep", BitAiMarkdownSpec.Text(innermost));
    }

    [TestMethod]
    public void MaxNestingDepthBoundsInlineSpansAndKeepsTheInnerOnes()
    {
        var options = new BitAiMarkdownOptions { MaxNestingDepth = 4 };
        var inlines = BitAiInlineParser.Parse(BitAiMarkdownSpec.Repeat("*a ", 50) + "b" + BitAiMarkdownSpec.Repeat(" c*", 50), options);

        Assert.AreEqual(4, BitAiMarkdownSpec.InlineDepth(inlines));
        Assert.StartsWith("*a *a ", BitAiMarkdownSpec.Text(inlines), StringComparison.Ordinal);
    }

    [TestMethod]
    public void BeyondTheLimitTheMarkersBecomeTextAndTheInnerSpansSurvive()
    {
        var options = new BitAiMarkdownOptions { MaxNestingDepth = 1 };
        Assert.AreEqual("{ParagraphBlock(\"*a \"strong(\"b\")\" c*\")}", BitAiMarkdownSpec.InlineDump("*a **b** c*", options));
    }
}
