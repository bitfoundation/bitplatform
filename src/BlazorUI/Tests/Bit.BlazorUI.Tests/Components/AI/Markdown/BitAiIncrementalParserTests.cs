using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

[TestClass]
public class BitAiIncrementalParserTests
{
    [TestMethod]
    public void TheDocumentMatchesAOneShotParseAtEveryPrefix()
    {
        const string source =
            "# Title\n\nSome **bold** text.\n\n```csharp\nvar x = 1;\n```\n\n- one\n- two\n\nFinal words.";

        var parser = new BitAiIncrementalMarkdownParser();

        for (var length = 1; length <= source.Length; length++)
        {
            var prefix = source[..length];
            var incremental = parser.Parse(prefix, isComplete: false);
            var oneShot = BitAiMarkdownParser.Parse(prefix, isComplete: false);

            Assert.AreEqual(oneShot.Blocks.Count, incremental.Blocks.Count);
            for (var i = 0; i < oneShot.Blocks.Count; i++)
            {
                Assert.AreEqual(oneShot.Blocks[i].GetType(), incremental.Blocks[i].GetType());
            }
        }
    }

    [TestMethod]
    public void CompletedBlocksAreReusedRatherThanReparsed()
    {
        var parser = new BitAiIncrementalMarkdownParser();
        parser.Parse("First paragraph.\n\nSecond paragraph.\n\nthird\n", isComplete: false);

        Assert.IsTrue(parser.StableLength > 0, "Text before the last confirmed boundary should become stable.");
        Assert.AreEqual(2, parser.StableBlockCount);
    }

    [TestMethod]
    public void NothingBecomesStableInsideAnOpenFence()
    {
        var parser = new BitAiIncrementalMarkdownParser();
        parser.Parse("```js\nline one\n\nline two\n", isComplete: false);

        Assert.AreEqual(0, parser.StableLength);
    }

    [TestMethod]
    public void ABlankLineInsideALooseListIsNotAStableBoundary()
    {
        var parser = new BitAiIncrementalMarkdownParser();
        var document = parser.Parse("- one\n\n- two\n\n- three\n", isComplete: false);

        Assert.ContainsSingle(document.Blocks);
        Assert.AreEqual(3, Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[0]).Items.Count);
    }

    [TestMethod]
    public void RewritingTheSourceStartsOver()
    {
        var parser = new BitAiIncrementalMarkdownParser();
        parser.Parse("First.\n\nSecond.\n\n", isComplete: false);
        Assert.IsTrue(parser.StableLength > 0);

        var document = parser.Parse("Completely different.", isComplete: false);

        Assert.AreEqual(0, parser.StableLength);
        Assert.ContainsSingle(document.Blocks);
    }

    [TestMethod]
    public void IncrementalParsingStaysCheapOnALongDocument()
    {
        var source = string.Concat(Enumerable.Range(0, 400)
            .Select(i => $"Paragraph number {i} with a reasonable amount of text in it.\n\n"));

        var incremental = new BitAiIncrementalMarkdownParser();
        var stopwatch = Stopwatch.StartNew();
        for (var length = 200; length <= source.Length; length += 200) incremental.Parse(source[..length], false);
        var incrementalTime = stopwatch.Elapsed;

        stopwatch.Restart();
        for (var length = 200; length <= source.Length; length += 200) BitAiMarkdownParser.Parse(source[..length], null, false);
        var oneShotTime = stopwatch.Elapsed;

        Assert.IsTrue(
            incrementalTime < oneShotTime,
            $"Incremental parsing took {incrementalTime.TotalMilliseconds:F1}ms " +
            $"against {oneShotTime.TotalMilliseconds:F1}ms for repeated full parses.");
    }
}
