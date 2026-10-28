using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Findings 12, 13, 16: lazy continuation lines never start a block.</summary>
[TestClass]
public class BitAiLazyContinuationTests
{
    [TestMethod]
    public void AThematicBreakAfterAListItemEndsTheList()
    {
        var document = BitAiMarkdownSpec.Parse("- last\n---\nNext");

        Assert.HasCount(3, document.Blocks);
        Assert.ContainsSingle(Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]).Items);
        Assert.IsExactInstanceOfType<BitAiThematicBreakBlock>(document.Blocks[1]);
        Assert.AreEqual("Next", BitAiMarkdownSpec.Text(document.Blocks[2]));
    }

    [TestMethod]
    public void DisplayMathAfterAListItemEndsTheList()
    {
        var document = BitAiMarkdownSpec.Parse("- a\n$$\nx\n$$");

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.AreEqual("x", Assert.IsExactInstanceOfType<BitAiMathBlock>(document.Blocks[1]).Expression);
    }

    [TestMethod]
    public void ATableAfterAListItemEndsTheList()
    {
        var document = BitAiMarkdownSpec.Parse("- a\n| x | y |\n|---|---|\n| 1 | 2 |");

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.IsExactInstanceOfType<BitAiTableBlock>(document.Blocks[1]);
    }

    [TestMethod]
    public void AnHtmlBlockAfterAListItemEndsTheList()
    {
        var document = BitAiMarkdownSpec.Parse("- a\n<div>\nx\n</div>", new BitAiMarkdownOptions { AllowRawHtml = true });

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.IsExactInstanceOfType<BitAiHtmlBlock>(document.Blocks[1]);
    }

    [TestMethod]
    public void PlainTextStillContinuesAListItemLazily()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("- a\nb").Blocks));
        Assert.AreEqual("a\nb", BitAiMarkdownSpec.Text(list.Items[0].Blocks[0]));
    }

    [TestMethod]
    public void TextAfterABlankLineThatNoItemCanClaimIsKeptAsAParagraph()
    {
        var document = BitAiMarkdownSpec.Parse("- a\n - b\n\n  text");

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.AreEqual(2, Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]).Items.Count);
        Assert.AreEqual("text", BitAiMarkdownSpec.Text(document.Blocks[1]));
    }

    [TestMethod]
    public void IndentedTextAfterABlankLineContinuesTheItem()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("- a\n\n  continued\n\n  more").Blocks));
        var item = Assert.ContainsSingle(list.Items);

        CollectionAssert.AreEqual(new[] { "a", "continued", "more" }, item.Blocks.Select(BitAiMarkdownSpec.Text).ToArray());
    }

    [TestMethod]
    public void SeveralBlankLinesBetweenItemsKeepOneList()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse("- a\n\n\n- b").Blocks));
        Assert.AreEqual(2, list.Items.Count);
        Assert.IsTrue(list.IsLoose);
    }

    [TestMethod]
    [DataRow("> quote\n# Heading", typeof(BitAiHeadingBlock))]
    [DataRow("> quote\n```\ncode\n```", typeof(BitAiCodeBlock))]
    [DataRow("> quote\n---", typeof(BitAiThematicBreakBlock))]
    [DataRow("> quote\n$$\nx\n$$", typeof(BitAiMathBlock))]
    public void ABlockStartIsNotALazyQuoteContinuation(string source, Type following)
    {
        var document = BitAiMarkdownSpec.Parse(source);

        Assert.AreEqual(2, document.Blocks.Count);
        Assert.IsExactInstanceOfType<BitAiQuoteBlock>(document.Blocks[0]);
        Assert.IsExactInstanceOfType(document.Blocks[1], following);
    }
}
