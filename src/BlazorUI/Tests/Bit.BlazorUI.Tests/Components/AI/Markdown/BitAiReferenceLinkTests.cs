using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 24: link reference definitions and reference links.</summary>
[TestClass]
public class BitAiReferenceLinkTests
{
    [TestMethod]
    public void AFullReferenceResolvesAgainstADefinitionThatFollowsIt()
    {
        var document = BitAiMarkdownSpec.Parse("See [the docs][ref] now.\n\n[ref]: https://x.dev/docs \"Docs\"");

        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(document.Blocks));
        var link = paragraph.Inlines.OfType<BitAiLinkInline>().Single();
        Assert.AreEqual("https://x.dev/docs", link.Url);
        Assert.AreEqual("Docs", link.Title);
        Assert.AreEqual("the docs", BitAiMarkdownSpec.Text(link.Inlines));
    }

    [TestMethod]
    [DataRow("[ref][]\n\n[ref]: /u")]
    [DataRow("[ref]\n\n[ref]: /u")]
    [DataRow("[Ref]\n\n[ref]: /u")]
    [DataRow("[ref]: /u\n\n[ref]")]
    [DataRow("[ref]: </u>\n\n[ref]")]
    [DataRow("[ref]: /u 'Title'\n\n[ref]")]
    [DataRow("[ref]: /u (Title)\n\n[ref]")]
    [DataRow("[ref]:   /u   \n\n[ref]")]
    [DataRow("[  ref  ]\n\n[ref]: /u")]
    public void CollapsedAndShortcutReferencesResolveCaseInsensitively(string source)
    {
        var document = BitAiMarkdownSpec.Parse(source);
        var paragraph = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(document.Blocks));
        Assert.AreEqual("/u", paragraph.Inlines.OfType<BitAiLinkInline>().Single().Url);
    }

    [TestMethod]
    public void TheFirstDefinitionOfALabelWins()
    {
        var document = BitAiMarkdownSpec.Parse("[ref]: /a\n[ref]: /b\n\n[ref]");
        Assert.AreEqual("/a", Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(document.Blocks)).Inlines.OfType<BitAiLinkInline>().Single().Url);
    }

    [TestMethod]
    [DataRow("[text][missing]")]
    [DataRow("[text][missing]\n\n[other]: /u")]
    [DataRow("[text]")]
    public void AnUndefinedReferenceIsText(string source)
    {
        var document = BitAiMarkdownSpec.Parse(source);
        Assert.IsEmpty(Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]).Inlines.OfType<BitAiLinkInline>());
        Assert.AreEqual(source.Split("\n\n")[0], BitAiMarkdownSpec.Text(document.Blocks[0]));
    }

    [TestMethod]
    public void AFullReferenceWithAnUndefinedLabelDoesNotFallBackToAShortcut()
    {
        var document = BitAiMarkdownSpec.Parse("[ref][missing]\n\n[ref]: /u");
        Assert.IsEmpty(Assert.IsExactInstanceOfType<BitAiParagraphBlock>(document.Blocks[0]).Inlines.OfType<BitAiLinkInline>());
    }

    [TestMethod]
    public void ADefinitionIsNotABlockOfItsOwn()
    {
        var document = BitAiMarkdownSpec.Parse("[ref]: /u\n\ntext");
        Assert.AreEqual("text", BitAiMarkdownSpec.Text(Assert.ContainsSingle(document.Blocks)));
    }

    [TestMethod]
    public void ADefinitionCannotInterruptAParagraph()
    {
        var document = BitAiMarkdownSpec.Parse("Sources:\n[1]: https://x.dev");
        Assert.AreEqual("Sources:\n[1]: https://x.dev", BitAiMarkdownSpec.Text(Assert.ContainsSingle(document.Blocks)));
    }

    [TestMethod]
    public void ImagesMayUseReferences()
    {
        var document = BitAiMarkdownSpec.Parse("![alt][img]\n\n[img]: a.png \"A\"");
        var image = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(document.Blocks)).Inlines.OfType<BitAiImageInline>().Single();

        Assert.AreEqual("a.png", image.Url);
        Assert.AreEqual("alt", image.Alt);
        Assert.AreEqual("A", image.Title);
    }

    [TestMethod]
    public void ANumericLabelWithADefinitionIsALinkNotACitation()
    {
        var document = BitAiMarkdownSpec.Parse("see [1]\n\n[1]: https://x.dev");
        var inlines = Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(document.Blocks)).Inlines;

        Assert.IsEmpty(inlines.OfType<BitAiCitationMarkerInline>());
        Assert.AreEqual("https://x.dev", inlines.OfType<BitAiLinkInline>().Single().Url);
    }

    [TestMethod]
    [DataRow("[ref]: ")]
    [DataRow("[ref]: /u \"unterminated")]
    [DataRow("[ref]: /u extra words")]
    [DataRow("[]: /u")]
    [DataRow("[a [b]]: /u")]
    public void AMalformedDefinitionIsAParagraph(string source) =>
        Assert.IsExactInstanceOfType<BitAiParagraphBlock>(Assert.ContainsSingle(BitAiMarkdownSpec.Parse(source).Blocks));

    [TestMethod]
    public void TheIncrementalParserAgreesEvenWhenTheDefinitionArrivesLast() =>
        BitAiMarkdownSpec.AssertIncrementalMatches("Intro [a][x].\n\nMore [b][y] text.\n\n[x]: /x\n[y]: /y \"Y\"\n\nend");
}
