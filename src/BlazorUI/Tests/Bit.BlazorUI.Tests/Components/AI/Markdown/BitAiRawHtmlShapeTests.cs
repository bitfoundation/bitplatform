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
    [DataRow("<b>x</b>", 1)]
    [DataRow("<br/>", 1)]
    [DataRow("<span class=\"x\">y</span>", 1)]
    [DataRow("<!-- note -->", 1)]
    [DataRow("<?php ?>", 1)]
    [DataRow("<my-element>", 1)]
    [DataRow("<b>x", 1)]
    [DataRow("x</b>", 1)]
    public void TagsCommentsAndDeclarationsAreHtml(string source, int count) =>
        Assert.AreEqual(count, BitAiInlineParser.Parse(source, Html).OfType<BitAiRawHtmlInline>().Count());

    // A view writes each inline of raw HTML as markup of its own, which the browser parses on its own: an opening tag
    // alone would be closed on the spot, and the text meant to be inside the element would follow it.
    [TestMethod]
    public void AnElementClosedInTheSameTextIsOneInlineWithItsContent()
    {
        var inlines = BitAiInlineParser.Parse("Press <kbd>Ctrl</kbd> + <KBD>K</kbd> to search.", Html);

        CollectionAssert.AreEqual(
            new[] { "<kbd>Ctrl</kbd>", "<KBD>K</kbd>" },
            inlines.OfType<BitAiRawHtmlInline>().Select(h => h.Html).ToArray());
        Assert.AreEqual("Press  +  to search.", string.Concat(inlines.OfType<BitAiTextInline>().Select(t => t.Text)));
    }

    [TestMethod]
    public void AnElementKeepsTheElementsOfItsNameNestedInIt()
    {
        var html = Assert.ContainsSingle(BitAiInlineParser.Parse("<span>a <span>b</span> c</span> after", Html).OfType<BitAiRawHtmlInline>());

        Assert.AreEqual("<span>a <span>b</span> c</span>", html.Html);
    }

    [TestMethod]
    public void ATagTheTextDoesNotCloseStaysATagOfItsOwn()
    {
        // A void element, a self-closing tag and an element whose closing tag has not arrived (yet) close nothing.
        CollectionAssert.AreEqual(
            new[] { "<br>", "<img src=\"x\"/>", "<kbd>" },
            BitAiInlineParser.Parse("a<br>b <img src=\"x\"/> <kbd>Ct", Html).OfType<BitAiRawHtmlInline>().Select(h => h.Html).ToArray());

        // An escaped one is text, and does not take the closing tag from the element around it.
        CollectionAssert.AreEqual(
            new[] { "<b>x \\<b> y</b>" },
            BitAiInlineParser.Parse("<b>x \\<b> y</b>", Html).OfType<BitAiRawHtmlInline>().Select(h => h.Html).ToArray());
    }

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
