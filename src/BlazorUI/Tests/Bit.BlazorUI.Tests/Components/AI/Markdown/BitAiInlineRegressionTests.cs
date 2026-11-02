using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiInlineRegressionTests
{
    private static List<BitAiMarkdownInline> Parse(string source, bool complete = true) =>
        BitAiInlineParser.Parse(source, isComplete: complete);

    private static string Dump(string source, bool complete = true) =>
        BitAiMarkdownTreeDump.Of(Doc(Parse(source, complete)));

    private static BitAiMarkdownDocument Doc(List<BitAiMarkdownInline> inlines)
    {
        var document = new BitAiMarkdownDocument();
        var paragraph = new BitAiParagraphBlock();
        paragraph.Inlines.AddRange(inlines);
        document.Blocks.Add(paragraph);
        return document;
    }

    [TestMethod]
    [DataRow("***both***", "{ParagraphBlock(em(strong(\"both\")))}")]
    [DataRow("a ***b*** c", "{ParagraphBlock(\"a \"em(strong(\"b\"))\" c\")}")]
    [DataRow("*a **b** c*", "{ParagraphBlock(em(\"a \"strong(\"b\")\" c\"))}")]
    [DataRow("**a *b* c**", "{ParagraphBlock(strong(\"a \"em(\"b\")\" c\"))}")]
    [DataRow("*foo**bar**baz*", "{ParagraphBlock(em(\"foo\"strong(\"bar\")\"baz\"))}")]
    [DataRow("**2*3 is six**", "{ParagraphBlock(strong(\"2*3 is six\"))}")]
    [DataRow("2*3*4", "{ParagraphBlock(\"2\"em(\"3\")\"4\")}")]
    [DataRow("**bold*", "{ParagraphBlock(\"*\"em(\"bold\"))}")]
    [DataRow("*it**", "{ParagraphBlock(em(\"it\")\"*\")}")]
    [DataRow("****", "{ParagraphBlock(\"****\")}")]
    [DataRow("**a **b**", "{ParagraphBlock(\"**a \"strong(\"b\"))}")]
    [DataRow("**bold** and *it*", "{ParagraphBlock(strong(\"bold\")\" and \"em(\"it\"))}")]
    [DataRow("snake_case_name", "{ParagraphBlock(\"snake_case_name\")}")]
    [DataRow("_under_ score", "{ParagraphBlock(em(\"under\")\" score\")}")]
    public void EmphasisRunsAreMatchedSensibly(string source, string expected) =>
        Assert.AreEqual(expected, Dump(source));

    [TestMethod]
    [DataRow("**")]
    [DataRow("**bold*")]
    [DataRow("***bold**")]
    [DataRow("text **")]
    public void APartialCloserIsHiddenWhileStreaming(string source) =>
        Assert.DoesNotContain("*", Dump(source, complete: false));

    [TestMethod]
    public void ContentInsideAClosedSpanIsNotTreatedAsStreaming()
    {
        Assert.AreEqual("{ParagraphBlock(strong(\"2*3 is six\")\" and more\")}", Dump("**2*3 is six** and more", complete: false));
        Assert.AreEqual("{ParagraphBlock(a[u](\"a *b\"))}", Dump("[a *b](u)", complete: false));
    }

    [TestMethod]
    public void AnUnmatchedBacktickRunIsLiteralAsAWhole()
    {
        Assert.IsEmpty(Parse("``foo`").OfType<BitAiCodeInline>());
        Assert.AreEqual("{ParagraphBlock(\"``foo\"code[and]\"x`\")}", Dump("``foo` and `x`"));
    }

    [TestMethod]
    [DataRow("It costs $5 and $10 today.")]
    [DataRow("Between $5 and $10.")]
    [DataRow("$a $")]
    public void DollarAmountsAreNotMath(string source) =>
        Assert.IsEmpty(Parse(source).OfType<BitAiMathInline>());

    [TestMethod]
    public void AnAngleAutoLinkWithAnAtSignAndASchemeIsNotMailTo()
    {
        Assert.AreEqual("https://medium.com/@user", Parse("<https://medium.com/@user>").OfType<BitAiLinkInline>().Single().Url);
        Assert.AreEqual("mailto:a@b.com", Parse("<a@b.com>").OfType<BitAiLinkInline>().Single().Url);
        Assert.IsEmpty(Parse("<a\nb:c>").OfType<BitAiLinkInline>());
    }

    [TestMethod]
    [DataRow("see https://en.wikipedia.org/wiki/Foo_(bar) now", "https://en.wikipedia.org/wiki/Foo_(bar)")]
    [DataRow("(see https://en.wikipedia.org/wiki/Foo_(bar))", "https://en.wikipedia.org/wiki/Foo_(bar)")]
    [DataRow("(https://example.com)", "https://example.com")]
    [DataRow("(https://example.com).", "https://example.com")]
    public void BareUrlsKeepBalancedParentheses(string source, string expected) =>
        Assert.AreEqual(expected, Parse(source).OfType<BitAiLinkInline>().Single().Url);

    [TestMethod]
    public void ALinkWhoseTextIsAUrlDoesNotNestALink()
    {
        var link = Parse("[https://example.com](https://example.com)").OfType<BitAiLinkInline>().Single();
        Assert.IsEmpty(link.Inlines.OfType<BitAiLinkInline>());
        Assert.AreEqual("https://example.com", Assert.IsExactInstanceOfType<BitAiTextInline>(Assert.ContainsSingle(link.Inlines)).Text);
    }

    [TestMethod]
    public void DeeplyNestedInlinesDoNotOverflowTheStack()
    {
        var links = string.Concat(Enumerable.Repeat("[", 30_000)) + "x" + string.Concat(Enumerable.Repeat("](u)", 30_000));
        Assert.IsNotEmpty(Parse(links));
    }

    [TestMethod]
    // A link cannot contain a link, so the innermost one wins and the outer brackets are text.
    [DataRow("[a [b](c) d](e)", "{ParagraphBlock(\"[a \"a[c](\"b\")\" d](e)\")}")]
    // Links bind more tightly than emphasis, and code spans more tightly than either.
    [DataRow("*[a*](u)", "{ParagraphBlock(\"*\"a[u](\"a*\"))}")]
    [DataRow("[`](u)`", "{ParagraphBlock(\"[\"code[](u)])}")]
    [DataRow("**`a**`**", "{ParagraphBlock(strong(code[a**]))}")]
    // An image may sit inside a link, as badges do.
    [DataRow("[![build](b.svg)](u)", "{ParagraphBlock(a[u](ImageInline))}")]
    // Autolinks inside a link's text stay text rather than nesting an anchor in an anchor.
    [DataRow("[see <https://x.dev>](u)", "{ParagraphBlock(a[u](\"see <https://x.dev>\"))}")]
    [DataRow("[go to www.x.dev](u)", "{ParagraphBlock(a[u](\"go to www.x.dev\"))}")]
    // A bare URL does not swallow the delimiters around it.
    [DataRow("**https://x.dev**", "{ParagraphBlock(strong(a[https://x.dev](\"https://x.dev\")))}")]
    [DataRow("_see https://x.dev/a_b_", "{ParagraphBlock(em(\"see \"a[https://x.dev/a_b](\"https://x.dev/a_b\")))}")]
    // Strikethrough pairs runs of exactly two tildes, so a run of three is text.
    [DataRow("~~gone~~ and ~~~kept~~~", "{ParagraphBlock(del(\"gone\")\" and ~~~kept~~~\")}")]
    [DataRow("**a ~~b** c~~", "{ParagraphBlock(strong(\"a ~~b\")\" c~~\")}")]
    public void InlineSpansFollowCommonMarkPrecedence(string source, string expected) =>
        Assert.AreEqual(expected, Dump(source));

    [TestMethod]
    // What remains of an opener may still close, so it is hidden too.
    [DataRow("***a* b", "{ParagraphBlock(em(\"a\")\" b\")}")]
    [DataRow("a *b c", "{ParagraphBlock(\"a b c\")}")]
    [DataRow("~~gone so far", "{ParagraphBlock(\"gone so far\")}")]
    [DataRow("almost ~", "{ParagraphBlock(\"almost \")}")]
    [DataRow("_a__", "{ParagraphBlock(em(\"a\"))}")]
    // An underscore ending a word is far more likely the middle of snake_case than a delimiter.
    [DataRow("snake_", "{ParagraphBlock(\"snake_\")}")]
    // Delimiters inside a closed span are literal.
    [DataRow("~~a *b~~ c", "{ParagraphBlock(del(\"a *b\")\" c\")}")]
    [DataRow("[*a](u) *b", "{ParagraphBlock(a[u](\"*a\")\" b\")}")]
    public void UnmatchedDelimitersAreHiddenOnlyOutsideClosedSpansWhileStreaming(string source, string expected) =>
        Assert.AreEqual(expected, Dump(source, complete: false));

    [TestMethod]
    public void UnmatchedDelimitersAreLiteralOnceComplete()
    {
        Assert.AreEqual("{ParagraphBlock(\"**\"em(\"a\")\" b\")}", Dump("***a* b"));
        Assert.AreEqual("{ParagraphBlock(\"~~gone so far\")}", Dump("~~gone so far"));
        Assert.AreEqual("{ParagraphBlock(\"almost ~\")}", Dump("almost ~"));
    }

    [TestMethod]
    [DataRow("\\é", "\\é")]
    [DataRow("\\a", "\\a")]
    [DataRow("\\1", "\\1")]
    [DataRow("\\—", "\\—")]
    [DataRow("\\«quote»", "\\«quote»")]
    [DataRow("\\→", "\\→")]
    [DataRow("\\\\", "\\")]
    [DataRow("\\*not em\\*", "*not em*")]
    public void OnlyAsciiPunctuationCanBeEscaped(string source, string expected) =>
        Assert.AreEqual($"{{ParagraphBlock(\"{expected}\")}}", Dump(source));

    [TestMethod]
    public void EveryAsciiPunctuationCharacterCanBeEscaped()
    {
        foreach (var c in "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~")
        {
            Assert.AreEqual($"{{ParagraphBlock(\"{c}\")}}", Dump("\\" + c));
        }
    }

    [TestMethod]
    [DataRow("[a](u \"t\")", "u", "t")]
    [DataRow("[a](u 't')", "u", "t")]
    [DataRow("[a](u (t))", "u", "t")]
    [DataRow("[a](u\n  \"t\"  )", "u", "t")]
    [DataRow("[a](<my file.md> 't')", "my file.md", "t")]
    [DataRow("[a](u \"say \\\"hi\\\"\")", "u", "say \"hi\"")]
    [DataRow("[a](u 'it\\'s')", "u", "it's")]
    [DataRow("[a](https://x.dev/it's)", "https://x.dev/it's", null)]
    [DataRow("[a](https://en.wikipedia.org/wiki/Foo_(bar))", "https://en.wikipedia.org/wiki/Foo_(bar)", null)]
    [DataRow("[a](Q3 summary.pdf)", "Q3 summary.pdf", null)]
    [DataRow("[a](u\\)v)", "u)v", null)]
    [DataRow("[a](u\\é)", "u\\é", null)]
    [DataRow("[a]()", "", null)]
    public void LinkDestinationsAndTitlesFollowCommonMark(string source, string url, string? title)
    {
        var link = Assert.IsExactInstanceOfType<BitAiLinkInline>(Assert.ContainsSingle(Parse(source)));

        Assert.AreEqual(url, link.Url);
        Assert.AreEqual(title, link.Title);
    }

    [TestMethod]
    [DataRow("![cat](c.png 'A cat')", "c.png", "A cat")]
    [DataRow("![cat](c.png (A cat))", "c.png", "A cat")]
    public void ImageTitlesFollowCommonMark(string source, string url, string title)
    {
        var image = Assert.IsExactInstanceOfType<BitAiImageInline>(Assert.ContainsSingle(Parse(source)));

        Assert.AreEqual(url, image.Url);
        Assert.AreEqual("cat", image.Alt);
        Assert.AreEqual(title, image.Title);
    }

    [TestMethod]
    [DataRow("[a](u \"t\" x)")]
    [DataRow("[a](foo (bar) baz)")]
    [DataRow("[a](u\n\nv)")]
    [DataRow("[a](u (t)")]
    [DataRow("[a](<u)")]
    public void AMalformedLinkTailIsText(string source) =>
        Assert.IsEmpty(Parse(source).OfType<BitAiLinkInline>());
}
