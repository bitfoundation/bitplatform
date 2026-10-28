using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiInlineParsingTests
{
    private static List<BitAiMarkdownInline> Parse(string source, bool complete = true) =>
        BitAiInlineParser.Parse(source, isComplete: complete);

    private static string Flatten(IEnumerable<BitAiMarkdownInline> inlines) =>
        string.Concat(inlines.Select(i => i switch
        {
            BitAiTextInline t => t.Text,
            BitAiEmphasisInline e => Flatten(e.Inlines),
            BitAiStrikethroughInline s => Flatten(s.Inlines),
            BitAiLinkInline l => Flatten(l.Inlines),
            BitAiCodeInline c => c.Code,
            BitAiLineBreakInline => " ",
            _ => string.Empty,
        }));

    [TestMethod]
    public void PlainTextIsOneRun()
    {
        var inlines = Parse("just text");
        Assert.AreEqual("just text", Assert.IsExactInstanceOfType<BitAiTextInline>(Assert.ContainsSingle(inlines)).Text);
    }

    [TestMethod]
    public void StrongAndOrdinaryEmphasisAreDistinguished()
    {
        var strong = Parse("**bold**").OfType<BitAiEmphasisInline>().Single();
        Assert.IsTrue(strong.IsStrong);

        var italic = Parse("*italic*").OfType<BitAiEmphasisInline>().Single();
        Assert.IsFalse(italic.IsStrong);
    }

    [TestMethod]
    public void EmphasisNestsAndKeepsSurroundingText()
    {
        var inlines = Parse("a **bold *and italic* end** z");

        Assert.AreEqual("a bold and italic end z", Flatten(inlines));
        var outer = inlines.OfType<BitAiEmphasisInline>().Single();
        Assert.IsTrue(outer.IsStrong);
        Assert.Contains(i => i is BitAiEmphasisInline { IsStrong: false }, outer.Inlines);
    }

    [TestMethod]
    public void UnderscoresInsideAWordAreNotEmphasis()
    {
        var inlines = Parse("snake_case_name");
        Assert.IsEmpty(inlines.OfType<BitAiEmphasisInline>());
    }

    [TestMethod]
    public void CodeSpansAreLiteralAndStripOneSurroundingSpace()
    {
        Assert.AreEqual("a * b", Parse("`a * b`").OfType<BitAiCodeInline>().Single().Code);
        Assert.AreEqual("`", Parse("`` ` ``").OfType<BitAiCodeInline>().Single().Code);
    }

    [TestMethod]
    public void EmphasisInsideACodeSpanIsNotParsed()
    {
        var inlines = Parse("`**not bold**`");
        Assert.IsEmpty(inlines.OfType<BitAiEmphasisInline>());
    }

    [TestMethod]
    public void StrikethroughUsesDoubleTildes()
    {
        var strike = Parse("~~gone~~").OfType<BitAiStrikethroughInline>().Single();
        Assert.AreEqual("gone", Flatten(strike.Inlines));
    }

    [TestMethod]
    public void LinksCaptureTextDestinationAndTitle()
    {
        var link = Parse("""[Docs](https://example.com "The docs")""").OfType<BitAiLinkInline>().Single();

        Assert.AreEqual("https://example.com", link.Url);
        Assert.AreEqual("The docs", link.Title);
        Assert.AreEqual("Docs", Flatten(link.Inlines));
    }

    [TestMethod]
    public void ImagesCaptureAltAndSource()
    {
        var image = Parse("![a cat](cat.png)").OfType<BitAiImageInline>().Single();

        Assert.AreEqual("cat.png", image.Url);
        Assert.AreEqual("a cat", image.Alt);
    }

    [TestMethod]
    public void BareUrlsBecomeLinksWhenAutoLinkingIsOn()
    {
        var link = Parse("see https://example.com/page for more").OfType<BitAiLinkInline>().Single();

        Assert.AreEqual("https://example.com/page", link.Url);
        Assert.IsTrue(link.IsAutoLink);
    }

    [TestMethod]
    public void TrailingPunctuationIsNotPartOfABareUrl()
    {
        var link = Parse("go to https://example.com.").OfType<BitAiLinkInline>().Single();
        Assert.AreEqual("https://example.com", link.Url);
    }

    [TestMethod]
    public void AngleAutoLinksIncludeMailTo()
    {
        Assert.AreEqual("mailto:a@b.com", Parse("<a@b.com>").OfType<BitAiLinkInline>().Single().Url);
        Assert.AreEqual("https://x.dev", Parse("<https://x.dev>").OfType<BitAiLinkInline>().Single().Url);
    }

    [TestMethod]
    public void BracketedNumbersBecomeCitationMarkers()
    {
        var marker = Parse("as reported [3] recently").OfType<BitAiCitationMarkerInline>().Single();
        Assert.AreEqual(3, marker.Number);
    }

    [TestMethod]
    public void ALinkIsNotMistakenForACitationMarker()
    {
        var inlines = Parse("[42](https://example.com)");
        Assert.IsEmpty(inlines.OfType<BitAiCitationMarkerInline>());
        Assert.ContainsSingle(inlines.OfType<BitAiLinkInline>());
    }

    [TestMethod]
    public void InlineMathIsCaptured()
    {
        Assert.AreEqual("x^2", Parse("the value $x^2$ here").OfType<BitAiMathInline>().Single().Expression);
    }

    [TestMethod]
    public void BackslashEscapesProduceLiteralCharacters()
    {
        Assert.AreEqual("*not emphasis*", Flatten(Parse("""\*not emphasis\*""")));
    }

    [TestMethod]
    public void TwoTrailingSpacesMakeAHardBreak()
    {
        Assert.IsTrue(Parse("one  \ntwo").OfType<BitAiLineBreakInline>().Single().IsHard);
        Assert.IsFalse(Parse("one\ntwo").OfType<BitAiLineBreakInline>().Single().IsHard);
    }

    [TestMethod]
    public void ADanglingDelimiterIsHiddenWhileStreamingAndLiteralWhenDone()
    {
        Assert.AreEqual("bold so far", Flatten(Parse("**bold so far", complete: false)));
        Assert.AreEqual("**bold so far", Flatten(Parse("**bold so far")));
    }

    [TestMethod]
    public void AnUnclosedCodeSpanIsHiddenWhileStreamingAndLiteralWhenDone()
    {
        Assert.AreEqual("half", Flatten(Parse("`half", complete: false)));
        Assert.AreEqual("`half", Flatten(Parse("`half")));
    }

    [TestMethod]
    public void EmphasisResolvesOnceTheClosingDelimiterArrives()
    {
        Assert.IsEmpty(Parse("**partial", complete: false).OfType<BitAiEmphasisInline>());
        Assert.ContainsSingle(Parse("**partial**", complete: false).OfType<BitAiEmphasisInline>());
    }
}
