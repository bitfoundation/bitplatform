using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Every construct, in random combination, must parse identically incrementally and in one shot.</summary>
[TestClass]
public class BitAiIncrementalPropertyTests
{
    public static readonly string[] Fragments =
    [
        "Plain *text* here.", "Two\nlines", "# Heading", "## A", "## A\n\n## A", "Setext\n---", "***", "- a\n- b", "- a\n\n- b",
        "1. one\n2. two", "10) ten", "- a\n\n  continued", "> quote\n> more", "> lazy\nline", "- a\n---", "- a\n - b\n\n  text",
        "```js\nlet a;\n\nlet b;\n```", "~~~\ntilde\n\n~~~", "```inline``` text", "$$\nx\n\ny\n$$", "$$x$$", "- a\n$$\nm\n$$",
        "Text:\n$$\nz\n$$", "| a | b |\n|---|---|\n| 1 | 2 |", "- a\n| x | y |\n|--|--|\n| 1 | 2 |", "  indented", "    code", "para\n\n    code\n\n    more",
        "-", "1.", "**bold", "- [x] done", "**bold **text", "***both*** after", "*a **b** c*", "costs $5 and $10", "AT&amp;T &copy; &#39;",
        "arr[0] [1][2] and [3]", "a\\\nb", "`half", "`a` and `b", "- a\n  ```\n  code", "```\nz\n\nw\n```", "- a\n```\nb",
        "text\n$$ not math\nmore", "$$", "end $$", "* star\n+ plus\n- dash", "1. one\n2) two", "<div>\n```\n</div>", "<b>x</b> a < b", "> ```\n> q",
        "[text][ref]", "[ref]: https://x.dev \"T\"", "[ref][]", "[ref]", "![alt][img]", "[img]: a.png", "x[^1]", "[^1]: note\n    more\n\n    para",
        "y[^a] z[^1]", "[^a]: a note", "Sources:\n[1]: https://x.dev", "see [1]", "[1]: /one",
    ];

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public void RandomDocumentsMatchAOneShotParseAtEveryPrefix(bool allowRawHtml, bool hardBreaks)
    {
        var options = new BitAiMarkdownOptions { AllowRawHtml = allowRawHtml, SoftBreaksAsHardBreaks = hardBreaks };
        var random = new Random(4321);

        for (var round = 0; round < 250; round++)
        {
            var parts = Enumerable.Range(0, random.Next(2, 7)).Select(_ => Fragments[random.Next(Fragments.Length)]);
            var source = string.Join(random.Next(3) == 0 ? "\n" : "\n\n", parts) + (random.Next(2) == 0 ? "\n" : string.Empty);
            BitAiMarkdownSpec.AssertIncrementalMatches(source, options);
        }
    }

    [TestMethod]
    public void EveryFragmentOnItsOwnMatchesAOneShotParse()
    {
        foreach (var fragment in Fragments) BitAiMarkdownSpec.AssertIncrementalMatches(fragment);
        foreach (var fragment in Fragments) BitAiMarkdownSpec.AssertIncrementalMatches(fragment, new BitAiMarkdownOptions { AllowRawHtml = true });
    }
}
