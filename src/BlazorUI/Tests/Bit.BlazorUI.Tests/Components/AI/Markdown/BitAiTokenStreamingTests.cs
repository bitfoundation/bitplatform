using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 3: the stable prefix grows while text arrives a few characters at a time.</summary>
[TestClass]
public class BitAiTokenStreamingTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    public void TheStablePrefixGrowsAsParagraphsStreamInSmallChunks(int chunk)
    {
        var source = string.Concat(Enumerable.Range(0, 50).Select(i => $"Paragraph {i} has some **bold** words and a [link](https://x.dev/{i}).\n\n"));
        var parser = new BitAiIncrementalMarkdownParser();
        var previous = 0;
        var growths = 0;

        for (var length = chunk; length < source.Length; length += chunk)
        {
            parser.Parse(source[..length], isComplete: false);
            Assert.IsTrue(parser.StableLength >= previous, "The stable prefix must never shrink while the text only grows.");
            if (parser.StableLength > previous) growths++;
            previous = parser.StableLength;
        }

        var final = parser.Parse(source, isComplete: true);

        Assert.IsTrue(growths >= 40, $"The stable prefix grew only {growths} times over 50 paragraphs.");
        Assert.IsTrue(parser.StableBlockCount >= 49, $"Only {parser.StableBlockCount} blocks became stable.");
        Assert.AreEqual(BitAiMarkdownSpec.Dump(BitAiMarkdownParser.Parse(source)), BitAiMarkdownSpec.Dump(final));
    }
}
