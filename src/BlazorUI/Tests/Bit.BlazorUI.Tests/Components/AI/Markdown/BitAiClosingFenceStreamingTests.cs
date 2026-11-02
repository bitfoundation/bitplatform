using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>
/// The closing fence of a code block arrives a character at a time while a reply streams: the last line of a block still
/// being written that can only be the fence on its way ("``" of "```") is never drawn as code, and is code again the moment
/// it turns into something else, a line follows it, or the stream ends.
/// </summary>
[TestClass]
public class BitAiClosingFenceStreamingTests
{
    private static BitAiCodeBlock OnlyCode(BitAiMarkdownDocument document)
    {
        Assert.HasCount(1, document.Blocks);
        return Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[0]);
    }

    [TestMethod]
    [DataRow("```cs\nvar total = items.Sum(i => i.Price);\n`")]
    [DataRow("```cs\nvar total = items.Sum(i => i.Price);\n``")]
    [DataRow("```cs\nvar total = items.Sum(i => i.Price);\n``  ")]
    [DataRow("```cs\nvar total = items.Sum(i => i.Price);\n   ``")]
    [DataRow("````cs\nvar total = items.Sum(i => i.Price);\n```")]
    [DataRow("~~~cs\nvar total = items.Sum(i => i.Price);\n~~")]
    public void AHalfWrittenClosingFenceIsHeldBackWhileTheBlockStreams(string source)
    {
        var code = OnlyCode(BitAiMarkdownSpec.Parse(source, complete: false));

        Assert.AreEqual("var total = items.Sum(i => i.Price);", code.Code);
        Assert.AreEqual("cs", code.Language);
        Assert.IsTrue(code.IsOpen);
    }

    [TestMethod]
    public void AFenceHeldBackRightAfterItsOpeningLeavesTheBlockEmpty()
    {
        var code = OnlyCode(BitAiMarkdownSpec.Parse("```\n``", complete: false));

        Assert.AreEqual(string.Empty, code.Code);
        Assert.IsTrue(code.IsOpen);
    }

    [TestMethod]
    public void TheHeldBackLineClosesTheBlockOnceTheFenceCompletes()
    {
        var code = OnlyCode(BitAiMarkdownSpec.Parse("```cs\nvar x = 1;\n```", complete: false));

        Assert.AreEqual("var x = 1;", code.Code);
        Assert.IsFalse(code.IsOpen);
    }

    [TestMethod]
    [DataRow("```\nx\n``y", "x\n``y")]
    [DataRow("```\nx\n`` y", "x\n`` y")]
    [DataRow("```\nx\n~~", "x\n~~")]
    [DataRow("~~~\nx\n``", "x\n``")]
    [DataRow("```\nx\n``\n", "x\n``\n")]
    [DataRow("```\nx\n``\ny", "x\n``\ny")]
    public void ALineThatCanNoLongerBeTheFenceIsCode(string source, string expected)
    {
        var code = OnlyCode(BitAiMarkdownSpec.Parse(source, complete: false));

        Assert.AreEqual(expected, code.Code);
        Assert.IsTrue(code.IsOpen);
    }

    [TestMethod]
    public void AtTheEndOfTheStreamTheLineIsCodeAsWritten()
    {
        var code = OnlyCode(BitAiMarkdownSpec.Parse("```cs\nvar x = 1;\n``"));

        Assert.AreEqual("var x = 1;\n``", code.Code);
        Assert.IsFalse(code.IsOpen);
    }

    [TestMethod]
    public void ABlockInsideAQuoteHoldsItsFenceBackToo()
    {
        var quote = Assert.IsExactInstanceOfType<BitAiQuoteBlock>(BitAiMarkdownSpec.Parse("> ```\n> x\n> ``", complete: false).Blocks[0]);

        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(quote.Blocks[0]);
        Assert.AreEqual("x", code.Code);
        Assert.IsTrue(code.IsOpen);
    }

    [TestMethod]
    public void ABlockInsideAListItemHoldsItsFenceBackToo()
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(BitAiMarkdownSpec.Parse("- ```\n  x\n  ``", complete: false).Blocks[0]);

        var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(list.Items[0].Blocks[0]);
        Assert.AreEqual("x", code.Code);
        Assert.IsTrue(code.IsOpen);
    }

    [TestMethod]
    public void AQuoteThatHasEndedShowsItsLastLineAsCode()
    {
        // The quote ends before the stream does, so its last line is not the last line of the stream: it is final.
        var document = BitAiMarkdownSpec.Parse("> ```\n> x\n> ``\n\nafter", complete: false);

        var quote = Assert.IsExactInstanceOfType<BitAiQuoteBlock>(document.Blocks[0]);
        Assert.AreEqual("x\n``", Assert.IsExactInstanceOfType<BitAiCodeBlock>(quote.Blocks[0]).Code);
    }

    [TestMethod]
    public void TheIncrementalParserHoldsTheFenceBackAtEveryStep()
    {
        const string source = "Intro.\n\n```cs\nvar total = items.Sum(i => i.Price);\n```\n\nAfter.";
        BitAiMarkdownSpec.AssertIncrementalMatches(source);

        var parser = new BitAiIncrementalMarkdownParser();
        var fenceStart = source.LastIndexOf("```", System.StringComparison.Ordinal);

        for (var length = fenceStart + 1; length < fenceStart + 3; length++)
        {
            var document = parser.Parse(source[..length]);
            var code = Assert.IsExactInstanceOfType<BitAiCodeBlock>(document.Blocks[^1]);

            Assert.AreEqual("var total = items.Sum(i => i.Price);", code.Code, $"At {source[fenceStart..length]}");
            Assert.IsTrue(code.IsOpen);
        }

        Assert.IsFalse(Assert.IsExactInstanceOfType<BitAiCodeBlock>(parser.Parse(source[..(fenceStart + 3)]).Blocks[^1]).IsOpen);
    }
}
