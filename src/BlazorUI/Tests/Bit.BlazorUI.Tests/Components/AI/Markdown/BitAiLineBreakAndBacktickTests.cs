using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 24: soft breaks and dangling backticks.</summary>
[TestClass]
public class BitAiLineBreakAndBacktickTests
{
    [TestMethod]
    public void SoftBreaksAreSoftByDefaultAndHardWhenAsked()
    {
        Assert.IsFalse(BitAiInlineParser.Parse("a\nb").OfType<BitAiLineBreakInline>().Single().IsHard);
        Assert.IsTrue(BitAiInlineParser.Parse("a\nb", new BitAiMarkdownOptions { SoftBreaksAsHardBreaks = true }).OfType<BitAiLineBreakInline>().Single().IsHard);
        Assert.IsTrue(BitAiInlineParser.Parse("a  \nb").OfType<BitAiLineBreakInline>().Single().IsHard);
    }

    [TestMethod]
    public void TheOptionReachesParagraphsThroughTheBlockParser()
    {
        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(BitAiMarkdownSpec.Parse("a\nb", new BitAiMarkdownOptions { SoftBreaksAsHardBreaks = true }).Blocks[0]);
        Assert.IsTrue(paragraph.Inlines.OfType<BitAiLineBreakInline>().Single().IsHard);
    }

    [TestMethod]
    [DataRow("code `half", "code half")]
    [DataRow("`a` and `b", "a and b")]
    [DataRow("``still", "still")]
    public void AnUnmatchedBacktickRunIsHiddenWhileStreaming(string source, string expected) =>
        Assert.AreEqual(expected, BitAiMarkdownSpec.Text(BitAiInlineParser.Parse(source, isComplete: false)));

    [TestMethod]
    [DataRow("code `half", "{ParagraphBlock(\"code `half\")}")]
    [DataRow("`a` and `b", "{ParagraphBlock(code[a]\" and `b\")}")]
    public void AnUnmatchedBacktickRunIsLiteralOnceComplete(string source, string expected) =>
        Assert.AreEqual(expected, BitAiMarkdownSpec.InlineDump(source));

    [TestMethod]
    public void HidingCanBeTurnedOffForTheWholeDocument()
    {
        var options = new BitAiMarkdownOptions { HideDanglingDelimiters = false };
        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(BitAiMarkdownSpec.Parse("code `half **bold", options, complete: false).Blocks[0]);
        Assert.AreEqual("code `half **bold", BitAiMarkdownSpec.Text(paragraph.Inlines));
    }
}
