using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Markdown;

/// <summary>Finding 5: emphasis follows the CommonMark delimiter algorithm, with one leniency.</summary>
[TestClass]
public class BitAiEmphasisSpecTests
{
    [TestMethod]
    [DataRow("*a **b** c*", "{ParagraphBlock(em(\"a \"strong(\"b\")\" c\"))}")]
    [DataRow("***bold italic*** after", "{ParagraphBlock(em(strong(\"bold italic\"))\" after\")}")]
    [DataRow("**a *b* c**", "{ParagraphBlock(strong(\"a \"em(\"b\")\" c\"))}")]
    [DataRow("*foo**bar**baz*", "{ParagraphBlock(em(\"foo\"strong(\"bar\")\"baz\"))}")]
    [DataRow("foo***bar***baz", "{ParagraphBlock(\"foo\"em(strong(\"bar\"))\"baz\")}")]
    [DataRow("**foo*bar*baz**", "{ParagraphBlock(strong(\"foo\"em(\"bar\")\"baz\"))}")]
    public void NestedEmphasisFollowsCommonMark(string source, string expected) =>
        Assert.AreEqual(expected, BitAiMarkdownSpec.InlineDump(source));

    [TestMethod]
    // The second run cannot close by the letter of the specification, since a space precedes
    // it, but nothing else claims it and a reader sees bold text.
    [DataRow("**bold **text", "{ParagraphBlock(strong(\"bold\")\" text\")}")]
    [DataRow("*it *text", "{ParagraphBlock(em(\"it\")\" text\")}")]
    [DataRow("say **bold **text and **more**", "{ParagraphBlock(\"say \"strong(\"bold\")\" text and \"strong(\"more\"))}")]
    // A run with whitespace on both sides is neither an opener nor a closer, and stays text.
    [DataRow("say **bold ** and more", "{ParagraphBlock(\"say **bold ** and more\")}")]
    // Where a run can close properly, the specification wins.
    [DataRow("**a **b**", "{ParagraphBlock(\"**a \"strong(\"b\"))}")]
    // Runs of different sizes are not paired leniently.
    [DataRow("**bold *text", "{ParagraphBlock(\"**bold *text\")}")]
    public void AWhitespacePrecededRunClosesWhenNothingElseClaimsIt(string source, string expected) =>
        Assert.AreEqual(expected, BitAiMarkdownSpec.InlineDump(source));

    [TestMethod]
    public void TheLenientCloserAlsoAppliesWhileStreaming() =>
        Assert.AreEqual("{ParagraphBlock(strong(\"bold\")\" text\")}", BitAiMarkdownSpec.InlineDump("**bold **text", complete: false));
}
