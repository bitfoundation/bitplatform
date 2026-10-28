using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiIncrementalRegressionTests
{
    private static void AssertMatchesOneShotAtEveryPrefix(string source, BitAiMarkdownOptions? options = null)
    {
        var parser = new BitAiIncrementalMarkdownParser(options);

        for (var length = 1; length <= source.Length; length++)
        {
            var prefix = source[..length];
            Assert.IsTrue(
                BitAiMarkdownTreeDump.Of(BitAiMarkdownParser.Parse(prefix, options, isComplete: false)) == BitAiMarkdownTreeDump.Of(parser.Parse(prefix, isComplete: false)),
                $"Mismatch at prefix {length}: {prefix}");
        }

        Assert.AreEqual(BitAiMarkdownTreeDump.Of(BitAiMarkdownParser.Parse(source, options)), BitAiMarkdownTreeDump.Of(parser.Parse(source, isComplete: true)));
    }

    [TestMethod]
    [DataRow("Intro.\n\n$$\na\n\nb\n$$\n\nafter\n\nend")]
    [DataRow("```a```\n```\ncode\n\nx\n```\n\nafter\n\nend")]
    [DataRow("```\ncode\n```  \n\nmiddle\n\n```\nmore\n\nstill code\n```\n\nend")]
    [DataRow("- a *b\n\nnext *c\n\n- d\n\n  e\n\nlast")]
    [DataRow("# Title\n\nSome **bold** text.\n\n> quote\n> more\n\n1. one\n2. two\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\nFinal.")]
    // A fence left open inside a list item ends with the item, so the column-zero fence after it
    // opens a new block rather than closing the first.
    [DataRow("- item\n  ```\n  code\n\ntext\n\n```\ninside\n\nstill inside\n```\n\nafter\n\nend")]
    [DataRow("1. step\n   ```bash\n   npm i\n\nNext.\n\n```\nnot closed by the list\n\nmore\n```\n\nend")]
    // A "$$" line in the middle of a paragraph is text, so the "$$" after it opens display math.
    [DataRow("text\n$$ not math\n\npara\n\n$$\nreal\n\nmath\n$$\n\nafter\n\nend")]
    [DataRow("- a\n* b\n\n+ c\n\n1. d\n2) e\n\nend")]
    public void TheDocumentMatchesAOneShotParseExactlyAtEveryPrefix(string source) =>
        AssertMatchesOneShotAtEveryPrefix(source);

    [TestMethod]
    public void AFenceHiddenInsideRawHtmlDoesNotConfuseTheBoundaries() =>
        AssertMatchesOneShotAtEveryPrefix(
            "<div>\n```\n</div>\n\ntext\n\n```\ncode\n\nmore code\n```\n\nend",
            new BitAiMarkdownOptions { AllowRawHtml = true });

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RandomDocumentsMatchAOneShotParseAtEveryPrefix(bool allowRawHtml)
    {
        string[] fragments =
        [
            "Plain *text* here.", "Two\nlines", "# Heading", "Setext\n---", "***", "- a\n- b", "- a\n\n- b",
            "1. one\n2. two", "10) ten", "- a\n\n  continued", "> quote\n> more", "> lazy\nline",
            "```js\nlet a;\n\nlet b;\n```", "~~~\ntilde\n\n~~~", "```inline``` text", "$$\nx\n\ny\n$$", "$$x$$",
            "Text:\n$$\nz\n$$", "| a | b |\n|---|---|\n| 1 | 2 |", "  indented", "-", "1.", "**bold", "- [x] done",
            // Fences left open inside list items, and column-zero fences that may or may not close them.
            "- a\n  ```\n  code", "- a\n  ```js\n  x\n\n  y", "1. step\n   ```bash\n   npm i\n\n   more", "```",
            "```\nz\n\nw\n```", "  ```\n  indented fence\n\n  body", "- a\n  ~~~\n  t", "- a\n```\nb",
            // "$$" lines inside paragraphs, at block starts, and inside list items.
            "text\n$$ not math\nmore", "para\n$$ x", "$$ x", "$$", "- item\n  $$\n  m", "end $$", "$$ a\n\nb $$",
            // Lists whose bullet or delimiter changes, and fences or math hidden inside other blocks.
            "* star\n+ plus\n- dash", "1. one\n2) two", "- a\n\n* b", "<div>\n```\n</div>", "<p>\n$$\n</p>", "> ```\n> q",
        ];

        var options = new BitAiMarkdownOptions { AllowRawHtml = allowRawHtml };
        var random = new Random(1234);
        for (var round = 0; round < 300; round++)
        {
            var parts = Enumerable.Range(0, random.Next(2, 7)).Select(_ => fragments[random.Next(fragments.Length)]);
            var source = string.Join(random.Next(3) == 0 ? "\n" : "\n\n", parts) + (random.Next(2) == 0 ? "\n" : string.Empty);
            AssertMatchesOneShotAtEveryPrefix(source, options);
        }
    }

    [TestMethod]
    public void BlocksAfterACodeBlockInsideAListStillBecomeStable()
    {
        const string source = "1. Install:\n   ```bash\n   npm i\n   ```\n\nFirst.\n\nSecond.\n\nThird.\n";
        var parser = new BitAiIncrementalMarkdownParser();

        for (var length = 1; length <= source.Length; length++) parser.Parse(source[..length]);

        Assert.AreEqual(3, parser.StableBlockCount);
    }

    [TestMethod]
    public void BoundariesAreFoundWhenTextArrivesACharacterAtATime()
    {
        var source = string.Concat(Enumerable.Range(0, 50).Select(i => $"Paragraph {i}.\n\n"));
        var parser = new BitAiIncrementalMarkdownParser();

        for (var length = 1; length <= source.Length; length++) parser.Parse(source[..length]);

        Assert.IsTrue(parser.StableBlockCount >= 49, $"Only {parser.StableBlockCount} blocks became stable.");
    }
}
