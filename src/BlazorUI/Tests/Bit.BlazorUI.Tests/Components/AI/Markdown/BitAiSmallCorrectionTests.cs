using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Findings 14, 15, 17, 18, 19: smaller parsing corrections.</summary>
[TestClass]
public class BitAiSmallCorrectionTests
{
    [TestMethod]
    [DataRow("## Using C#", "Using C#")]
    [DataRow("## C# and F# ##", "C# and F#")]
    [DataRow("# Title #", "Title")]
    public void OnlyASpaceSeparatedClosingSequenceIsStripped(string source, string expected) =>
        Assert.AreEqual(expected, BitAiMarkdownSpec.Text(BitAiMarkdownSpec.Parse(source).Blocks[0]));

    [TestMethod]
    [DataRow("-\titem")]
    [DataRow("1.\titem")]
    [DataRow("-   item")]
    public void TheMarkerContentStartsAfterTheFirstSpaceOrTab(string source)
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(BitAiMarkdownSpec.Parse(source).Blocks[0]);
        Assert.AreEqual("item", BitAiMarkdownSpec.Text(list.Items[0].Blocks[0]));
    }

    [TestMethod]
    [DataRow("arr[0] and x[12]")]
    [DataRow("a[1]")]
    [DataRow("[text][1]")]
    public void ABracketAfterAWordOrLabelIsNotACitation(string source) =>
        Assert.IsEmpty(BitAiInlineParser.Parse(source).OfType<BitAiCitationMarkerInline>());

    [TestMethod]
    [DataRow("see [1] and [2]", 2)]
    [DataRow("see [1][2] here", 2)]
    [DataRow("see [1][2][3].", 3)]
    [DataRow("(see [4])", 1)]
    public void CitationMarkersAreRecognisedAfterSpacesPunctuationAndOtherMarkers(string source, int count) =>
        Assert.AreEqual(count, BitAiInlineParser.Parse(source).OfType<BitAiCitationMarkerInline>().Count());

    [TestMethod]
    [DataRow("see https://en.wikipedia.org/wiki/Foo_(bar) now", "https://en.wikipedia.org/wiki/Foo_(bar)")]
    [DataRow("(see https://x.dev/a)", "https://x.dev/a")]
    [DataRow("https://x.dev/a(b)(c))", "https://x.dev/a(b)(c)")]
    public void BareUrlsBalanceTheirParentheses(string source, string url) =>
        Assert.AreEqual(url, BitAiInlineParser.Parse(source).OfType<BitAiLinkInline>().Single().Url);

    [TestMethod]
    [DataRow("[a](<http://x y>)", "http://x y", null)]
    [DataRow("[a](http://x \"title\")", "http://x", "title")]
    [DataRow("[a](http://x 'title')", "http://x", "title")]
    [DataRow("[a](<http://x> \"t\")", "http://x", "t")]
    public void LinkDestinationsMayBeBracketedAndTitled(string source, string url, string? title)
    {
        var link = Assert.IsExactInstanceOfType<BitAiLinkInline>(Assert.ContainsSingle(BitAiInlineParser.Parse(source)));
        Assert.AreEqual(url, link.Url);
        Assert.AreEqual(title, link.Title);
    }
}
