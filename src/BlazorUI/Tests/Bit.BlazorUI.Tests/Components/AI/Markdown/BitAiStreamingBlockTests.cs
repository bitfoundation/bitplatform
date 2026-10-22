using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Findings 6, 7, 8: streaming marks only the last item open and agrees with a full parse.</summary>
[TestClass]
public class BitAiStreamingBlockTests
{
    [TestMethod]
    [DataRow("- a **b\n- c **d\n- e")]
    [DataRow("1. a **b\n2. c **d\n3. e")]
    public void OnlyTheLastListItemIsOpenWhileStreaming(string source)
    {
        var list = Assert.IsExactInstanceOfType<BitAiListBlock>(BitAiMarkdownSpec.Parse(source, complete: false).Blocks[0]);

        Assert.AreEqual(3, list.Items.Count);
        Assert.IsFalse(list.Items[0].Blocks[0].IsOpen);
        Assert.IsFalse(list.Items[1].Blocks[0].IsOpen);
        Assert.IsTrue(list.Items[2].Blocks[0].IsOpen);
        Assert.AreEqual("a **b", BitAiMarkdownSpec.Text(list.Items[0].Blocks[0]));
        Assert.AreEqual("c **d", BitAiMarkdownSpec.Text(list.Items[1].Blocks[0]));
    }

    [TestMethod]
    public void AClosingFenceWithTrailingWhitespaceClosesInBothParsers()
    {
        const string source = "```js\ncode\n```   \n\nparagraph\n\nmore";
        BitAiMarkdownSpec.AssertIncrementalMatches(source);

        var document = BitAiMarkdownSpec.Parse(source);
        Assert.AreEqual(3, document.Blocks.Count);
        Assert.IsFalse(document.Blocks[0].IsOpen);
    }

    [TestMethod]
    public void DisplayMathSpanningBlankLinesIsOneBlockInBothParsers()
    {
        const string source = "Intro.\n\n$$\na\n\nb\n$$\n\nafter";
        BitAiMarkdownSpec.AssertIncrementalMatches(source);

        var document = BitAiMarkdownSpec.Parse(source);
        Assert.AreEqual("a\n\nb", Assert.IsExactInstanceOfType<BitAiMathBlock>(document.Blocks[1]).Expression);
    }
}
