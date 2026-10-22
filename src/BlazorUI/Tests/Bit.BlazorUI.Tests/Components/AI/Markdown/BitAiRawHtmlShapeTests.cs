using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 25: raw HTML is recognised only when it is shaped like HTML.</summary>
[TestClass]
public class BitAiRawHtmlShapeTests
{
    private static readonly BitAiMarkdownOptions Html = new() { AllowRawHtml = true };

    [TestMethod]
    [DataRow("a < b > c")]
    [DataRow("x <3 you>")]
    [DataRow("<3>")]
    [DataRow("< div>")]
    [DataRow("<-->")]
    public void SomethingThatIsNotATagIsText(string source) =>
        Assert.IsEmpty(BitAiInlineParser.Parse(source, Html).OfType<BitAiRawHtmlInline>());

    [TestMethod]
    [DataRow("<b>x</b>", 2)]
    [DataRow("<br/>", 1)]
    [DataRow("<span class=\"x\">y</span>", 2)]
    [DataRow("<!-- note -->", 1)]
    [DataRow("<?php ?>", 1)]
    [DataRow("<my-element>", 1)]
    public void TagsCommentsAndDeclarationsAreHtml(string source, int count) =>
        Assert.AreEqual(count, BitAiInlineParser.Parse(source, Html).OfType<BitAiRawHtmlInline>().Count());

    [TestMethod]
    [DataRow("<div>\nx\n</div>")]
    [DataRow("<!-- a -->")]
    [DataRow("</div>")]
    [DataRow("<!DOCTYPE html>")]
    public void ABlockStartsWithATag(string source) =>
        Assert.IsExactInstanceOfType<BitAiHtmlBlock>(Assert.ContainsSingle(BitAiMarkdownParser.Parse(source, Html).Blocks));

    [TestMethod]
    [DataRow("< div")]
    [DataRow("<3 is less than 4")]
    [DataRow("<-- not a comment")]
    public void ABlockDoesNotStartWithSomethingThatOnlyLooksLikeOne(string source) =>
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(BitAiMarkdownParser.Parse(source, Html).Blocks));

    [TestMethod]
    public void ABlockLevelTagInterruptsAParagraphButAnInlineTagDoesNot()
    {
        Assert.AreEqual(2, BitAiMarkdownParser.Parse("text\n<div>\nx", Html).Blocks.Count);
        Assert.ContainsSingle(BitAiMarkdownParser.Parse("text\n<b>x</b>", Html).Blocks);
    }

    [TestMethod]
    public void NothingIsHtmlWhenRawHtmlIsOff()
    {
        Assert.IsEmpty(BitAiInlineParser.Parse("<b>x</b>").OfType<BitAiRawHtmlInline>());
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(BitAiMarkdownParser.Parse("<div>\nx\n</div>").Blocks));
    }
}
