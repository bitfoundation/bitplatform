using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 24: indented code blocks.</summary>
[TestClass]
public class BitAiIndentedCodeTests
{
    [TestMethod]
    public void FourSpacesAfterABlankLineOpenACodeBlock()
    {
        var document = BitAiMarkdownSpec.Parse("para\n\n    code here\n    more\n\nafter");

        Assert.AreEqual(3, document.Blocks.Count);
        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[1]);
        Assert.IsFalse(code.IsFenced);
        Assert.IsNull(code.Language);
        Assert.AreEqual("code here\nmore", code.Code);
        Assert.AreEqual("after", BitAiMarkdownSpec.Text(document.Blocks[2]));
    }

    [TestMethod]
    public void BlankLinesInsideAreKeptAndTrailingOnesAreNot()
    {
        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("    a\n\n    b\n\n\n").Blocks));
        Assert.AreEqual("a\n\nb", code.Code);
    }

    [TestMethod]
    public void ATabCountsAsFourColumns() =>
        Assert.AreEqual("code", Assert.IsExactInstanceOfType<BitAiCodeBlock>(BitAiMarkdownSpec.Parse("\tcode").Blocks[0]).Code);

    [TestMethod]
    public void IndentedCodeCannotInterruptAParagraph()
    {
        var document = BitAiMarkdownSpec.Parse("para\n    not code");
        Assert.AreEqual("para\nnot code", BitAiMarkdownSpec.Text(Assert.ContainsSingle(document.Blocks)));
    }

    [TestMethod]
    public void IndentedCodeWorksInsideAListItem()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("- item\n\n      code").Blocks));
        var item = Assert.ContainsSingle(list.Items);

        Assert.AreEqual(2, item.Blocks.Count);
        Assert.AreEqual("code", Assert.IsExactInstanceOfType<BitAiCodeBlock>(item.Blocks[1]).Code);
    }

    [TestMethod]
    public void MarkdownInsideIndentedCodeIsNotParsed() =>
        Assert.AreEqual("# not a heading", Assert.IsExactInstanceOfType<BitAiCodeBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("    # not a heading").Blocks)).Code);

    [TestMethod]
    public void AnIndentedBlockIsOpenWhileStreaming()
    {
        Assert.IsTrue(BitAiMarkdownSpec.Parse("    code", complete: false).Blocks[0].IsOpen);
        Assert.IsFalse(BitAiMarkdownSpec.Parse("    code\n\nafter", complete: false).Blocks[0].IsOpen);
    }

    [TestMethod]
    public void TheIncrementalParserAgrees() =>
        BitAiMarkdownSpec.AssertIncrementalMatches("para\n\n    code\n\n    more\n\nafter\n\n    again\n\nend");
}
