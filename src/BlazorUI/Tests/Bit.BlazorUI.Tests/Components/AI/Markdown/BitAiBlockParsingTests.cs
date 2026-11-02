using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiBlockParsingTests
{
    private static BitAiMarkdownDocument Parse(string source, bool complete = true) =>
        BitAiMarkdownParser.Parse(source, isComplete: complete);

    [TestMethod]
    public void ParagraphsAreSeparatedByBlankLines()
    {
        var document = Parse("first line\nstill first\n\nsecond");

        Assert.AreEqual(2, document.Blocks.Count);
        foreach (var b in document.Blocks)
        {
            Assert.IsExactInstanceOfType<BitAiParagraphBlock>(b);
        }
    }

    [TestMethod]
    [DataRow("# One", 1)]
    [DataRow("### Three", 3)]
    [DataRow("###### Six", 6)]
    public void AtxHeadingsCarryTheirLevel(string source, int level)
    {
        var heading = Assert.IsExactInstanceOfType<BitAiHeadingBlock>(Parse(source).Blocks[0]);
        Assert.AreEqual(level, heading.Level);
    }

    [TestMethod]
    public void SevenHashesIsAParagraphNotAHeading()
    {
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Parse("####### too many").Blocks[0]);
    }

    [TestMethod]
    public void HeadingsGetAnchorIdentifiers()
    {
        var heading = Assert.IsExactInstanceOfType<BitAiHeadingBlock>(Parse("## Getting Started Quickly").Blocks[0]);
        Assert.AreEqual("bit-ai-h-getting-started-quickly", heading.Id);
    }

    [TestMethod]
    public void SetextHeadingsAreRecognised()
    {
        var heading = Assert.IsExactInstanceOfType<BitAiHeadingBlock>(Parse("Title\n=====").Blocks[0]);
        Assert.AreEqual(1, heading.Level);
    }

    [TestMethod]
    public void FencedCodeKeepsItsLanguageAndBody()
    {
        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(Parse("```csharp\nvar x = 1;\nvar y = 2;\n```").Blocks[0]);

        Assert.AreEqual("csharp", code.Language);
        Assert.AreEqual("var x = 1;\nvar y = 2;", code.Code);
        Assert.IsFalse(code.IsOpen);
    }

    [TestMethod]
    public void AnUnclosedFenceIsOpenWhileStreamingAndClosedWhenDone()
    {
        Assert.IsTrue(Assert.IsExactInstanceOfType<BitAiCodeBlock>(Parse("```js\nlet a =", complete: false).Blocks[0]).IsOpen);
        Assert.IsFalse(Assert.IsExactInstanceOfType<BitAiCodeBlock>(Parse("```js\nlet a =").Blocks[0]).IsOpen);
    }

    [TestMethod]
    public void BlankLinesInsideAFenceDoNotEndIt()
    {
        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(Parse("```\nline one\n\nline two\n```").Blocks[0]);
        Assert.AreEqual("line one\n\nline two", code.Code);
    }

    [TestMethod]
    public void MarkdownInsideAFenceIsNotParsed()
    {
        var document = Parse("```\n# not a heading\n- not a list\n```");
        Assert.ContainsSingle(document.Blocks);
        Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[0]);
    }

    [TestMethod]
    public void BulletListsCollectTheirItems()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("- one\n- two\n- three").Blocks[0]);

        Assert.IsFalse(list.IsOrdered);
        Assert.AreEqual(3, list.Items.Count);
    }

    [TestMethod]
    public void OrderedListsKeepTheirStartingNumber()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("3. three\n4. four").Blocks[0]);

        Assert.IsTrue(list.IsOrdered);
        Assert.AreEqual(3, list.Start);
        Assert.AreEqual(2, list.Items.Count);
    }

    [TestMethod]
    public void TaskListItemsCarryTheirCheckedState()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("- [x] done\n- [ ] todo").Blocks[0]);

        Assert.IsTrue(list.Items[0].IsChecked);
        Assert.IsFalse(list.Items[1].IsChecked);
    }

    [TestMethod]
    public void NestedListsBecomeNestedBlocks()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("- outer\n  - inner\n- outer two").Blocks[0]);

        Assert.AreEqual(2, list.Items.Count);
        Assert.Contains(b => b is BitAiListBlock, list.Items[0].Blocks);
    }

    [TestMethod]
    public void BlockQuotesNestTheirContent()
    {
        var quote = Assert.IsExactInstanceOfType<BitAiQuoteBlock>(Parse("> quoted\n> more").Blocks[0]);
        Assert.ContainsSingle(quote.Blocks);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(quote.Blocks[0]);
    }

    [TestMethod]
    public void TablesParseHeadersRowsAndAlignment()
    {
        var table = Assert.IsExactInstanceOfType<BitAiTableBlock>(
            Parse("| Name | Qty |\n| :--- | ---: |\n| Apple | 3 |\n| Pear | 12 |").Blocks[0]);

        Assert.AreEqual(2, table.Header.Count);
        Assert.AreEqual(2, table.Rows.Count);
        Assert.AreEqual(BitAiColumnAlignment.Left, table.Alignments[0]);
        Assert.AreEqual(BitAiColumnAlignment.Right, table.Alignments[1]);
    }

    [TestMethod]
    public void ATableWithoutADelimiterRowStaysAParagraph()
    {
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Parse("| a | b |\n| c | d |").Blocks[0]);
    }

    [TestMethod]
    [DataRow("---")]
    [DataRow("***")]
    [DataRow("___")]
    [DataRow("- - -")]
    public void ThematicBreaksAreRecognised(string source) =>
        Assert.IsExactInstanceOfType<BitAiThematicBreakBlock>(Parse(source).Blocks[0]);

    [TestMethod]
    public void DisplayMathIsItsOwnBlock()
    {
        var math = Assert.IsExactInstanceOfType<BitAiMathBlock>(Parse("$$\nE = mc^2\n$$").Blocks[0]);
        Assert.AreEqual("E = mc^2", math.Expression);
    }

    [TestMethod]
    public void RawHtmlIsCapturedAsHtmlOnlyWhenAllowed()
    {
        var allowed = BitAiMarkdownParser.Parse("<div>hi</div>", new BitAiMarkdownOptions { AllowRawHtml = true });
        Assert.IsExactInstanceOfType<BitAiHtmlBlock>(allowed.Blocks[0]);

        var escaped = BitAiMarkdownParser.Parse("<div>hi</div>");
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(escaped.Blocks[0]);
    }

    [TestMethod]
    public void TheLastParagraphIsOpenWhileStreaming()
    {
        var streaming = Parse("done\n\nstill writ", complete: false);
        Assert.IsFalse(streaming.Blocks[0].IsOpen);
        Assert.IsTrue(streaming.Blocks[1].IsOpen);
    }

    [TestMethod]
    public void AMixedDocumentKeepsEveryBlockInOrder()
    {
        var document = Parse("# Title\n\nText.\n\n```js\ncode\n```\n\n- a\n- b\n\n> quote");

        Assert.HasCount(5, document.Blocks);
        Assert.IsExactInstanceOfType<BitAiHeadingBlock>(document.Blocks[0]);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[1]);
        Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[2]);
        Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[3]);
        Assert.IsExactInstanceOfType<BitAiQuoteBlock>(document.Blocks[4]);
    }
}
