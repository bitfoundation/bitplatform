using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 20: heading identifiers are prefixed and unique within a document.</summary>
[TestClass]
public class BitAiHeadingIdTests
{
    private static string? Id(BitAiMarkdownBlock block) => Assert.IsExactInstanceOfType<BitAiHeadingBlock>(block).Id;

    [TestMethod]
    public void IdentifiersCarryAPrefixSoTheyCannotClobberThePage() =>
        Assert.AreEqual("bit-ai-h-hello-world", Id(BitAiMarkdownSpec.Parse("# Hello World").Blocks[0]));

    [TestMethod]
    public void AnEmptyHeadingStillGetsAnIdentifier() =>
        Assert.AreEqual("bit-ai-h-section", Id(BitAiMarkdownSpec.Parse("#").Blocks[0]));

    [TestMethod]
    public void RepeatedHeadingsAreNumbered()
    {
        var document = BitAiMarkdownSpec.Parse("## A\n\n## A\n\n## A\n\n## A 2");
        CollectionAssert.AreEqual(new[] { "bit-ai-h-a", "bit-ai-h-a-2", "bit-ai-h-a-3", "bit-ai-h-a-2-2" }, document.Blocks.Select(Id).ToArray());
    }

    [TestMethod]
    public void HeadingsInsideQuotesAndListsShareTheDocumentNamespace()
    {
        var document = BitAiMarkdownSpec.Parse("> ## A\n\n- ## A\n\n## A");

        Assert.AreEqual("bit-ai-h-a", Id(Assert.IsExactInstanceOfType<BitAiQuoteBlock>(document.Blocks[0]).Blocks[0]));
        Assert.AreEqual("bit-ai-h-a-2", Id(Assert.IsExactInstanceOfType<BitAiListBlock>(document.Blocks[1]).Items[0].Blocks[0]));
        Assert.AreEqual("bit-ai-h-a-3", Id(document.Blocks[2]));
    }

    [TestMethod]
    public void SetextHeadingsAreNumberedToo()
    {
        var document = BitAiMarkdownSpec.Parse("A\n===\n\nA\n---");
        CollectionAssert.AreEqual(new[] { "bit-ai-h-a", "bit-ai-h-a-2" }, document.Blocks.Select(Id).ToArray());
    }

    [TestMethod]
    public void TheIncrementalParserNumbersHeadingsAcrossTheStablePrefix() =>
        BitAiMarkdownSpec.AssertIncrementalMatches("## A\n\ntext\n\n## A\n\nmore\n\n## A\n\nend");

    [TestMethod]
    public void IdentifiersCanBeTurnedOff() =>
        Assert.IsNull(Id(BitAiMarkdownSpec.Parse("# A", new BitAiMarkdownOptions { HeadingIds = false }).Blocks[0]));
}
