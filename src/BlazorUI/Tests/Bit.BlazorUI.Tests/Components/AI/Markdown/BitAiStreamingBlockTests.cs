using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Findings 6, 7, 8: streaming marks only the last item open and agrees with a full parse.</summary>
[TestClass]
public class BitAiStreamingBlockTests
{
    [TestMethod]
    [DataRow("- a **b\n- c **d\n- e")]
    [DataRow("1. a **b\n2. c **d\n3. e")]
    public void OnlyTheLastListItemIsOpenWhileStreaming(string source)
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(BitAiMarkdownSpec.Parse(source, complete: false).Blocks[0]);

        Assert.AreEqual(3, list.Items.Count);
        Assert.IsFalse(list.Items[0].Blocks[0].IsOpen);
        Assert.IsFalse(list.Items[1].Blocks[0].IsOpen);
        Assert.IsTrue(list.Items[2].Blocks[0].IsOpen);
        Assert.AreEqual("a **b", BitAiMarkdownSpec.Text(list.Items[0].Blocks[0]));
        Assert.AreEqual("c **d", BitAiMarkdownSpec.Text(list.Items[1].Blocks[0]));
    }

    [TestMethod]
    public void AClosingFenceWithTrailingWhitespaceClosesInBothParsers()
    {
        const string source = "```js\ncode\n```   \n\nparagraph\n\nmore";
        BitAiMarkdownSpec.AssertIncrementalMatches(source);

        var document = BitAiMarkdownSpec.Parse(source);
        Assert.AreEqual(3, document.Blocks.Count);
        Assert.IsFalse(document.Blocks[0].IsOpen);
    }

    // A heading is one line, so only the line the source ends on is still being written; while it is, a run of asterisks
    // or a backtick whose partner has not arrived is hidden, as it is in an open paragraph.
    [TestMethod]
    public void AHeadingStillBeingWrittenIsOpenAndHidesItsDanglingDelimiters()
    {
        var open = Assert.IsExactInstanceOfType<BitAiHeadingBlock>(BitAiMarkdownSpec.Parse("## Render **mo", complete: false).Blocks[0]);

        Assert.IsTrue(open.IsOpen);
        Assert.AreEqual("Render mo", BitAiMarkdownSpec.Text(open));
        Assert.AreEqual("Use Sta", BitAiMarkdownSpec.Text(BitAiMarkdownSpec.Parse("# Use `Sta", complete: false).Blocks[0]));

        var done = BitAiMarkdownSpec.Parse("## Render **mo\n", complete: false).Blocks[0];
        Assert.IsFalse(done.IsOpen);
        Assert.AreEqual("Render **mo", BitAiMarkdownSpec.Text(done));

        Assert.IsFalse(BitAiMarkdownSpec.Parse("## Render **mo").Blocks[0].IsOpen);
        BitAiMarkdownSpec.AssertIncrementalMatches("Intro.\n\n## Render **modes** compared\n\nText.");
    }

    // The header row of a table is a paragraph line until the delimiter row under it arrives: shown as text meanwhile, it
    // would flash its pipes and then be taken back into the table.
    [TestMethod]
    [DataRow("| Mode | Runs")]
    [DataRow("| Mode | Runs |\n")]
    [DataRow("| Mode | Runs |\n|")]
    [DataRow("| Mode | Runs |\n| :")]
    public void AHalfArrivedTableHeaderIsHeldBack(string source) =>
        Assert.IsEmpty(BitAiMarkdownSpec.Parse(source, complete: false).Blocks);

    [TestMethod]
    public void OnlyTheTrailingPipeLinesOfAParagraphAreHeldBackWhileATableMayStillFollow()
    {
        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(BitAiMarkdownSpec.Parse("Compare them:\n| Mode | Runs |", complete: false).Blocks[0]);
        Assert.AreEqual("Compare them:", BitAiMarkdownSpec.Text(paragraph));

        // Once the line after it is not the delimiter row - or nothing more is coming, or tables are off - it is text.
        Assert.AreEqual("|x| is the absolute value.", BitAiMarkdownSpec.Text(BitAiMarkdownSpec.Parse("|x| is the absolute value.\n\n", complete: false).Blocks[0]));
        Assert.AreEqual("|x| is the absolute value.", BitAiMarkdownSpec.Text(BitAiMarkdownSpec.Parse("|x| is the absolute value.").Blocks[0]));
        Assert.IsNotEmpty(BitAiMarkdownSpec.Parse("| a |", new BitAiMarkdownOptions { Tables = false }, complete: false).Blocks);
        Assert.IsNotEmpty(BitAiMarkdownSpec.Parse("| a |", new BitAiMarkdownOptions { HideDanglingDelimiters = false }, complete: false).Blocks);

        var table = Assert.IsExactInstanceOfType<BitAiTableBlock>(BitAiMarkdownSpec.Parse("| Mode | Runs |\n|---|---|\n| Server", complete: false).Blocks[0]);
        Assert.HasCount(2, table.Header);
        BitAiMarkdownSpec.AssertIncrementalMatches("Compare them:\n\n| Mode | Runs |\n|---|---|\n| Server | here |\n\nDone.");
    }

    // A delimiter row still being written can stop on the colon its last cell starts with (":---" on its way): refused,
    // the table would turn back into a header row in waiting, held back, and blink out for that one delta.
    [TestMethod]
    public void ADelimiterRowStoppingOnTheColonOfItsLastCellKeepsTheTable()
    {
        var table = Assert.IsExactInstanceOfType<BitAiTableBlock>(BitAiMarkdownSpec.Parse("| a | b | c |\n| --- | --- | :", complete: false).Blocks[0]);
        Assert.HasCount(3, table.Header);
        CollectionAssert.AreEqual(new[] { BitAiColumnAlignment.None, BitAiColumnAlignment.None, BitAiColumnAlignment.Left }, table.Alignments);

        // So is the header row under a paragraph line, which the table interrupts.
        var document = BitAiMarkdownSpec.Parse("Compare them:\n| a | b |\n| --- | :", complete: false);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]);
        Assert.IsExactInstanceOfType<BitAiTableBlock>(document.Blocks[1]);

        // Only while the source is still writing that very row: a finished one, or one a row has followed, is no
        // delimiter row with a lone colon in it.
        Assert.IsNotInstanceOfType<BitAiTableBlock>(BitAiMarkdownSpec.Parse("| a | b | c |\n| --- | --- | :").Blocks[0]);
        Assert.IsNotInstanceOfType<BitAiTableBlock>(BitAiMarkdownSpec.Parse("| a | b |\n| --- | :\n| 1 | 2 |\n\nDone.", complete: false).Blocks[0]);

        BitAiMarkdownSpec.AssertIncrementalMatches("Intro.\n\n| a | b | c |\n| --- | --- | :---: |\n| 1 | 2 | 3 |\n\nDone.");
    }

    // Whatever the text so far ends with - a blank line, a closed fence - more may follow it, which a view reads off the
    // document to draw its caret.
    [TestMethod]
    public void TheDocumentIsOpenWhileMoreSourceIsExpected()
    {
        Assert.IsTrue(BitAiMarkdownSpec.Parse("Done.\n\n", complete: false).IsOpen);
        Assert.IsTrue(BitAiMarkdownParser.Parse(string.Empty, isComplete: false).IsOpen);
        Assert.IsFalse(BitAiMarkdownSpec.Parse("Done.\n\n").IsOpen);

        var parser = new BitAiIncrementalMarkdownParser();
        Assert.IsTrue(parser.Parse("First.\n\nSecond.\n\n", isComplete: false).IsOpen);
        Assert.IsFalse(parser.Parse("First.\n\nSecond.\n\nThird.", isComplete: true).IsOpen);
    }

    [TestMethod]
    public void DisplayMathSpanningBlankLinesIsOneBlockInBothParsers()
    {
        const string source = "Intro.\n\n$$\na\n\nb\n$$\n\nafter";
        BitAiMarkdownSpec.AssertIncrementalMatches(source);

        var document = BitAiMarkdownSpec.Parse(source);
        Assert.AreEqual("a\n\nb", Assert.IsExactInstanceOfType<BitAiMathBlock>(document.Blocks[1]).Expression);
    }
}
