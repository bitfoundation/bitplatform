using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 11: entity and numeric character references are decoded in text.</summary>
[TestClass]
public class BitAiEntityTests
{
    [TestMethod]
    [DataRow("AT&amp;T", "AT&T")]
    [DataRow("&copy; 2024", "© 2024")]
    [DataRow("&#39;x&#39;", "'x'")]
    [DataRow("&#x27;&#X27;", "''")]
    [DataRow("&lt;b&gt;", "<b>")]
    [DataRow("&#0;", "�")]
    [DataRow("&#1114112;", "�")]
    [DataRow("&nbsp;", " ")]
    [DataRow("&hellip;", "…")]
    public void ValidReferencesAreDecoded(string source, string expected) =>
        Assert.AreEqual(expected, Assert.IsExactInstanceOfType<BitAiTextInline>(Assert.ContainsSingle(BitAiInlineParser.Parse(source))).Text);

    [TestMethod]
    [DataRow("&notanentity;")]
    [DataRow("&amp")]
    [DataRow("a & b")]
    [DataRow("&#;")]
    [DataRow("&#xZZ;")]
    [DataRow("&;")]
    [DataRow("& amp;")]
    public void AnythingElseStaysAsWritten(string source) =>
        Assert.AreEqual(source, BitAiMarkdownSpec.Text(BitAiInlineParser.Parse(source)));

    [TestMethod]
    public void ReferencesInsideCodeAreNotDecoded()
    {
        Assert.AreEqual("&amp;", BitAiInlineParser.Parse("`&amp;`").OfType<BitAiCodeInline>().Single().Code);
        Assert.AreEqual("&amp;", Assert.IsExactInstanceOfType<BitAiCodeBlock>(BitAiMarkdownSpec.Parse("```\n&amp;\n```").Blocks[0]).Code);
    }

    [TestMethod]
    public void ADecodedAngleBracketIsTextNotMarkup()
    {
        var inlines = BitAiInlineParser.Parse("&lt;script&gt;", new BitAiMarkdownOptions { AllowRawHtml = true });
        Assert.AreEqual("<script>", Assert.IsExactInstanceOfType<BitAiTextInline>(Assert.ContainsSingle(inlines)).Text);
    }
}
