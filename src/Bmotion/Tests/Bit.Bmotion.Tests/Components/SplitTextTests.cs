using Bit.Bmotion.Tests.TestInfra;
using Bunit;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmotionSplitText"/>: the splitting rules (which are pure string work) and
/// the rendered markup contract - stagger offsets, wrapping safety and the accessibility fallback.
/// </summary>
[TestClass]
public class SplitTextTests
{
    // ── Splitting ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Split_Words_KeepsWhitespaceAsGaps()
    {
        var chunks = BmotionTextSplitter.Split("hi  there", BmSplitBy.Words);

        Assert.AreEqual(3, chunks.Count);
        Assert.AreEqual("hi", chunks[0].Units.Single());
        Assert.IsTrue(chunks[1].IsGap);
        Assert.AreEqual("  ", chunks[1].Text);
        Assert.AreEqual("there", chunks[2].Units.Single());
        Assert.AreEqual(2, BmotionTextSplitter.CountUnits(chunks));
    }

    [TestMethod]
    public void Split_Chars_GroupsCharactersByWord()
    {
        var chunks = BmotionTextSplitter.Split("ab c", BmSplitBy.Chars);

        Assert.AreEqual(3, chunks.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, chunks[0].Units.ToArray());
        Assert.IsTrue(chunks[1].IsGap);
        CollectionAssert.AreEqual(new[] { "c" }, chunks[2].Units.ToArray());
    }

    [TestMethod]
    public void Split_Chars_KeepsGraphemeClustersIntact()
    {
        // A ZWJ emoji sequence and a combining accent must each stay one animatable character
        // rather than being torn into unrenderable UTF-16 halves.
        var chunks = BmotionTextSplitter.Split("👨‍👩‍👧é", BmSplitBy.Chars);

        var units = chunks.Single().Units;
        Assert.AreEqual(2, units.Count);
        Assert.AreEqual("👨‍👩‍👧", units[0]);
        Assert.AreEqual("é", units[1]);
    }

    [TestMethod]
    public void Split_Lines_SplitsOnAuthoredNewlines_NormalisingCrLf()
    {
        var chunks = BmotionTextSplitter.Split("one\r\ntwo\n\nthree", BmSplitBy.Lines);

        Assert.AreEqual(4, chunks.Count);
        Assert.AreEqual("one", chunks[0].Units.Single());
        Assert.AreEqual("two", chunks[1].Units.Single());
        Assert.IsTrue(chunks[2].IsGap);          // the blank line carries no animated unit
        Assert.AreEqual("three", chunks[3].Units.Single());
        Assert.AreEqual(3, BmotionTextSplitter.CountUnits(chunks));
    }

    [TestMethod]
    public void Split_EmptyText_ProducesNoChunks()
    {
        Assert.AreEqual(0, BmotionTextSplitter.Split(null, BmSplitBy.Chars).Count);
        Assert.AreEqual(0, BmotionTextSplitter.Split("", BmSplitBy.Words).Count);
    }

    // ── Rendering ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Renders_OneAnimatedUnitPerCharacter()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab c")
            .Add(p => p.UnitClass, "u")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        Assert.AreEqual(3, cut.FindAll("span.u").Count);
    }

    [TestMethod]
    public void Renders_OneAnimatedUnitPerWord_InWordsMode()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab c")
            .Add(p => p.By, BmSplitBy.Words)
            .Add(p => p.UnitClass, "u")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        Assert.AreEqual(2, cut.FindAll("span.u").Count);
    }

    [TestMethod]
    public void Units_AreInlineBlock_SoTransformsApply()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab")
            .Add(p => p.UnitClass, "u")
            .Add(p => p.Animate, Bm.To(x: 10)));

        foreach (var unit in cut.FindAll("span.u"))
            StringAssert.Contains(unit.GetAttribute("style"), "display:inline-block");
    }

    [TestMethod]
    public void CharsMode_WrapsEachWordInANowrapGroup_SoWordsNeverBreakMidWord()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab cd")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        var groups = cut.FindAll("span[style*='nowrap']");
        Assert.AreEqual(2, groups.Count);
    }

    [TestMethod]
    public void Accessible_LabelsTheWholeRunAndHidesTheSplitUnits()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab cd")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        var root = cut.Find("span[aria-label]");
        Assert.AreEqual("ab cd", root.GetAttribute("aria-label"));
        Assert.AreEqual("text", root.GetAttribute("role"));
        // Each word group hides its characters, so the sentence is announced once.
        Assert.AreEqual(2, cut.FindAll("span[aria-hidden='true']").Count);
    }

    [TestMethod]
    public void Accessible_False_OmitsTheAriaWiring()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "ab")
            .Add(p => p.Accessible, false)
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        Assert.AreEqual(0, cut.FindAll("[aria-label]").Count);
        Assert.AreEqual(0, cut.FindAll("[aria-hidden]").Count);
    }

    [TestMethod]
    public void EmptyText_RendersNothing()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps.Add(p => p.Text, ""));

        Assert.AreEqual(string.Empty, cut.Markup);
    }

    [TestMethod]
    public void ContainerPreservesWhitespace_SoTheTextLaysOutLikeTheOriginal()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "a b")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        StringAssert.Contains(cut.Find("span[aria-label]").GetAttribute("style"), "white-space:pre-wrap");
    }

    [TestMethod]
    public void CustomStyle_IsAppendedAfterTheWhitespaceRule_SoTheAuthorWins()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "a")
            .Add(p => p.Style, "color:red;")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        var style = cut.Find("span[aria-label]").GetAttribute("style")!;
        Assert.IsTrue(style.IndexOf("white-space", StringComparison.Ordinal)
                      < style.IndexOf("color", StringComparison.Ordinal));
    }

    [TestMethod]
    public void As_ChoosesTheContainerElement()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<BmotionSplitText>(ps => ps
            .Add(p => p.Text, "a")
            .Add(p => p.As, "h1")
            .Add(p => p.Animate, Bm.To(opacity: 1)));

        Assert.IsNotNull(cut.Find("h1"));
    }
}
