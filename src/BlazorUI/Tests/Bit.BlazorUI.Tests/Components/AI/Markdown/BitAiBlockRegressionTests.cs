using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiBlockRegressionTests
{
    private static BitAiMarkdownDocument Parse(string source, bool complete = true) =>
        BitAiMarkdownParser.Parse(source, isComplete: complete);

    private static string Text(IEnumerable<BitAiMarkdownInline> inlines) =>
        string.Concat(inlines.Select(i => i switch
        {
            BitAiTextInline t => t.Text,
            BitAiEmphasisInline e => Text(e.Inlines),
            BitAiLinkInline l => Text(l.Inlines),
            BitAiCodeInline c => c.Code,
            BitAiLineBreakInline => "\n",
            _ => string.Empty,
        }));

    [TestMethod]
    [DataRow("$$$")]
    [DataRow("$$$\n")]
    [DataRow("> $$$")]
    public void ThreeDollarSignsDoNotThrow(string source) => Parse(source);

    [TestMethod]
    public void ContentOnTheOpeningMathLineIsKept()
    {
        var math = Assert.IsExactInstanceOfType<BitAiMathBlock>(Parse("$$ a + b\n= c\n$$").Blocks[0]);
        Assert.AreEqual("a + b\n= c", math.Expression);
    }

    [TestMethod]
    public void DisplayMathInterruptsAParagraph()
    {
        var document = Parse("The formula:\n$$\nE = mc^2\n$$\nafter");

        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]);
        Assert.AreEqual("E = mc^2", Assert.IsExactInstanceOfType<BitAiMathBlock>(document.Blocks[1]).Expression);
    }

    [TestMethod]
    [DataRow("## Using C#", "Using C#")]
    [DataRow("# F# and C#", "F# and C#")]
    [DataRow("# Title ##", "Title")]
    [DataRow("# Title #####   ", "Title")]
    [DataRow("# #", "")]
    public void OnlyASpaceSeparatedClosingSequenceIsStrippedFromAHeading(string source, string expected)
    {
        var heading = Assert.IsExactInstanceOfType<BitAiHeadingBlock>(Parse(source).Blocks[0]);
        Assert.AreEqual(expected, Text(heading.Inlines));
    }

    [TestMethod]
    public void AClosingFenceMayHaveTrailingWhitespace()
    {
        var document = Parse("```\ncode\n```   \n\nafter");

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.AreEqual("code", Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[0]).Code);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[1]);
    }

    [TestMethod]
    public void ATabAfterAListMarkerKeepsTheItemText()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("-\tfirst\n-\tsecond").Blocks[0]);
        Assert.AreEqual("first", Text(Assert.IsExactInstanceOfType<BitAiParagraphBlock>(list.Items[0].Blocks[0]).Inlines));
    }

    [TestMethod]
    public void TabIndentedContinuationLinesAreNotTruncated()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Parse("- Run:\n\t```go\n\tfunc main() {\n\t\tfmt.Println()\n\t}\n\t```").Blocks[0]);
        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(list.Items[0].Blocks[1]);

        Assert.AreEqual("go", code.Language);
        Assert.Contains("func main() {", code.Code);
        Assert.Contains("fmt.Println()", code.Code);
    }

    [TestMethod]
    [DataRow("text\n***\nmore")]
    [DataRow("text\n___\nmore")]
    public void AThematicBreakInterruptsAParagraph(string source)
    {
        var document = Parse(source);
        Assert.HasCount(3, document.Blocks);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]);
        Assert.IsExactInstanceOfType<BitAiThematicBreakBlock>(document.Blocks[1]);
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[2]);
    }

    [TestMethod]
    public void TwoMarkersAreNotAThematicBreak()
    {
        Assert.IsNotInstanceOfType<BitAiThematicBreakBlock>(Parse("- -").Blocks[0]);
    }

    [TestMethod]
    [DataRow("- a\n---")]
    [DataRow("- a\n***")]
    [DataRow("- a\n* * *")]
    public void AThematicBreakEndsAList(string source)
    {
        var document = Parse(source);
        Assert.AreEqual(2, document.Blocks.Count);
        Assert.ContainsSingle(Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]).Items);
        Assert.IsExactInstanceOfType<BitAiThematicBreakBlock>(document.Blocks[1]);
    }

    [TestMethod]
    public void ABlankLineBeforeADifferentKindOfListDoesNotMakeTheFirstLoose()
    {
        var document = Parse("- a\n- b\n\n1. c");
        Assert.IsFalse(Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]).IsLoose);
    }

    [TestMethod]
    [DataRow("> note\n# Heading")]
    [DataRow("> note\n```\ncode\n```")]
    [DataRow("> note\n- item")]
    public void ABlockStartIsNotALazyQuoteContinuation(string source)
    {
        var document = Parse(source);
        Assert.AreEqual(2, document.Blocks.Count);
        Assert.ContainsSingle(Assert.IsExactInstanceOfType<BitAiQuoteBlock>(document.Blocks[0]).Blocks);
    }

    [TestMethod]
    public void AFinishedListIsNotOpenInsideWhileStreaming()
    {
        var document = Parse("- a *b\n\nnext", complete: false);
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]);
        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(list.Items[0].Blocks[0]);

        Assert.IsFalse(paragraph.IsOpen);
        Assert.AreEqual("a *b", Text(paragraph.Inlines));
    }

    [TestMethod]
    [DataRow('>', 1)]
    [DataRow('>', 2)]
    public void DeeplyNestedBlocksDoNotOverflowTheStack(char marker, int width)
    {
        var line = string.Concat(Enumerable.Repeat(marker + new string(' ', width - 1), 50_000)) + "x";
        Assert.IsNotEmpty(Parse(line).Blocks);
    }

    [TestMethod]
    public void DeeplyNestedListMarkersDoNotOverflowTheStack()
    {
        var line = string.Concat(Enumerable.Repeat("1. ", 50_000)) + "x";
        Assert.IsNotEmpty(Parse(line).Blocks);
    }

    [TestMethod]
    [DataRow("- a\n* b\n+ c", 3)]
    [DataRow("- a\n- b\n* c", 2)]
    [DataRow("- a\n\n* b", 2)]
    [DataRow("1. a\n2) b", 2)]
    [DataRow("1) a\n\n2. b", 2)]
    [DataRow("- a\n1. b", 2)]
    public void ChangingTheBulletOrDelimiterStartsANewList(string source, int lists)
    {
        var document = Parse(source);

        Assert.AreEqual(lists, document.Blocks.Count);
        foreach (var b in document.Blocks)
        {
            Assert.IsFalse(Assert.IsExactInstanceOfType<BitAiListBlock>(b).IsLoose);
        }
    }

    [TestMethod]
    public void AListKeepsItsItemsWhenTheBulletStaysTheSame()
    {
        Assert.AreEqual(3, Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(Parse("* a\n* b\n\n* c").Blocks)).Items.Count);
        Assert.AreEqual(2, Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(Parse("1) a\n2) b").Blocks)).Items.Count);
    }
}
