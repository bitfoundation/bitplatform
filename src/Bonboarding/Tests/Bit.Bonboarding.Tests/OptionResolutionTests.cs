using Bit.Bonboarding.Tests.Infrastructure;

namespace Bit.Bonboarding.Tests;

[TestClass]
public class OptionResolutionTests
{
    private static async Task<BonboardingEffectiveStepOptions> ResolveAsync(
        Action<BonboardingOptions>? global = null,
        Action<BonboardingTourDefinition>? tour = null,
        Action<BonboardingStepDefinition>? step = null,
        Action<FakeInterop>? browser = null)
    {
        await using var harness = new BonboardingHarness(global);
        harness.WithElements("#a");
        browser?.Invoke(harness.Interop);

        var definition = new BonboardingStepDefinition { Id = "a", Target = BonboardingStepTarget.Css("#a") };
        step?.Invoke(definition);

        var tourDefinition = new BonboardingTourDefinition { Id = "options", Steps = [definition] };
        tour?.Invoke(tourDefinition);

        var session = await harness.Service.StartAsync(tourDefinition);
        return session.CurrentOptions!;
    }

    [TestMethod]
    public async Task Falls_Back_To_The_Built_In_Defaults()
    {
        var options = await ResolveAsync();

        Assert.AreEqual(BonboardingDefaults.Placement, options.Placement);
        Assert.AreEqual(BonboardingDefaults.Offset, options.Offset);
        Assert.AreEqual(BonboardingDefaults.SpotlightPadding, options.SpotlightPadding);
        Assert.AreEqual(BonboardingDefaults.Interaction, options.Interaction);
        Assert.AreEqual(BonboardingDefaults.Progress, options.Progress);
        Assert.IsTrue(options.ShowNext);
    }

    [TestMethod]
    public async Task Global_Options_Beat_The_Defaults()
    {
        var options = await ResolveAsync(global: o => o.Placement = BonboardingPlacement.Right);

        Assert.AreEqual(BonboardingPlacement.Right, options.Placement);
    }

    [TestMethod]
    public async Task Tour_Options_Beat_Global_Options()
    {
        var options = await ResolveAsync(
            global: o => o.Placement = BonboardingPlacement.Right,
            tour: t => t.Placement = BonboardingPlacement.Left);

        Assert.AreEqual(BonboardingPlacement.Left, options.Placement);
    }

    [TestMethod]
    public async Task Step_Options_Beat_Everything()
    {
        var options = await ResolveAsync(
            global: o => o.Placement = BonboardingPlacement.Right,
            tour: t => t.Placement = BonboardingPlacement.Left,
            step: s => s.Placement = BonboardingPlacement.Top);

        Assert.AreEqual(BonboardingPlacement.Top, options.Placement);
    }

    [TestMethod]
    public async Task Popover_Classes_Accumulate_Down_The_Chain()
    {
        var options = await ResolveAsync(
            global: o => o.PopoverClass = "app-theme",
            tour: t => t.PopoverClass = "tour-theme",
            step: s => s.PopoverClass = "step-theme");

        Assert.AreEqual("app-theme tour-theme step-theme", options.PopoverClass);
    }

    [TestMethod]
    public async Task Reduced_Motion_Zeroes_The_Animation_Duration()
    {
        var options = await ResolveAsync(browser: b => b.Environment = new BonboardingBrowserEnvironment { ReducedMotion = true });

        Assert.IsTrue(options.ReducedMotion);
        Assert.AreEqual(TimeSpan.Zero, options.AnimationDuration);
    }

    [TestMethod]
    public async Task Reduced_Motion_Can_Be_Ignored_Deliberately()
    {
        var options = await ResolveAsync(
            tour: t => t.RespectReducedMotion = false,
            browser: b => b.Environment = new BonboardingBrowserEnvironment { ReducedMotion = true });

        Assert.IsFalse(options.ReducedMotion);
        Assert.AreEqual(BonboardingDefaults.AnimationDuration, options.AnimationDuration);
    }

    [TestMethod]
    public async Task Automatic_Direction_Follows_The_Document()
    {
        var rtl = await ResolveAsync(browser: b => b.Environment = new BonboardingBrowserEnvironment { RightToLeft = true });
        Assert.AreEqual(BonboardingDirection.RightToLeft, rtl.Direction);
        Assert.IsTrue(rtl.RightToLeft);

        var ltr = await ResolveAsync();
        Assert.AreEqual(BonboardingDirection.LeftToRight, ltr.Direction);
    }

    [TestMethod]
    public async Task An_Explicit_Direction_Overrides_The_Document()
    {
        var options = await ResolveAsync(
            tour: t => t.Direction = BonboardingDirection.LeftToRight,
            browser: b => b.Environment = new BonboardingBrowserEnvironment { RightToLeft = true });

        Assert.AreEqual(BonboardingDirection.LeftToRight, options.Direction);
    }

    [TestMethod]
    public async Task Labels_Come_From_The_Tour_When_It_Supplies_Them()
    {
        var options = await ResolveAsync(
            global: o => o.Labels = new BonboardingLabels { Next = "Global next" },
            tour: t => t.Labels = new BonboardingLabels { Next = "Weiter" });

        Assert.AreEqual("Weiter", options.Labels.Next);
    }

    [TestMethod]
    public async Task Labels_Fall_Back_To_The_Global_Set()
    {
        var options = await ResolveAsync(global: o => o.Labels = new BonboardingLabels { Next = "Continue" });

        Assert.AreEqual("Continue", options.Labels.Next);
    }
}

[TestClass]
public class GeometryTests
{
    [TestMethod]
    public void Rect_Exposes_Its_Edges_And_Centre()
    {
        var rect = new BonboardingRect(10, 20, 100, 50);

        Assert.AreEqual(10, rect.Left);
        Assert.AreEqual(20, rect.Top);
        Assert.AreEqual(110, rect.Right);
        Assert.AreEqual(70, rect.Bottom);
        Assert.AreEqual(60, rect.CenterX);
        Assert.AreEqual(45, rect.CenterY);
        Assert.AreEqual(5000, rect.Area);
    }

    [TestMethod]
    public void Inflate_Grows_On_Every_Side()
    {
        var rect = new BonboardingRect(10, 10, 20, 20).Inflate(5);

        Assert.AreEqual(new BonboardingRect(5, 5, 30, 30), rect);
    }

    [TestMethod]
    public void Intersect_Returns_Empty_For_Disjoint_Rectangles()
    {
        var a = new BonboardingRect(0, 0, 10, 10);
        var b = new BonboardingRect(20, 20, 10, 10);

        Assert.IsTrue(a.Intersect(b).IsEmpty);
        Assert.IsFalse(a.IntersectsWith(b));
    }

    [TestMethod]
    public void Intersect_Returns_The_Overlap()
    {
        var a = new BonboardingRect(0, 0, 20, 20);
        var b = new BonboardingRect(10, 10, 20, 20);

        Assert.AreEqual(new BonboardingRect(10, 10, 10, 10), a.Intersect(b));
    }

    [TestMethod]
    public void Union_Ignores_Empty_Rectangles()
    {
        var a = new BonboardingRect(0, 0, 10, 10);

        Assert.AreEqual(a, a.Union(BonboardingRect.Empty));
        Assert.AreEqual(a, BonboardingRect.Empty.Union(a));
    }

    [TestMethod]
    public void ApproximatelyEquals_Absorbs_Sub_Pixel_Jitter()
    {
        var a = new BonboardingRect(10, 10, 100, 50);
        var b = new BonboardingRect(10.2, 9.9, 100.1, 50);

        Assert.IsTrue(a.ApproximatelyEquals(b));
        Assert.IsFalse(a.ApproximatelyEquals(b with { Width = 102 }));
    }
}

[TestClass]
public class StepTargetTests
{
    [TestMethod]
    public void Converts_Implicitly_From_A_Selector_String()
    {
        BonboardingStepTarget target = "#create";

        Assert.AreEqual("#create", BonboardingTargetDescriptor.From(target).Selector);
        Assert.AreEqual(BonboardingTargetKind.Selector, BonboardingTargetDescriptor.From(target).Kind);
    }

    [TestMethod]
    public void An_Anchor_Escapes_Quotes_In_Its_Name()
    {
        var target = BonboardingStepTarget.Anchor("say \"hi\"");

        Assert.AreEqual("[data-bonboarding-anchor=\"say \\\"hi\\\"\"]", BonboardingTargetDescriptor.From(target).Selector);
    }

    [TestMethod]
    public void Equality_Is_Structural()
    {
        Assert.AreEqual(BonboardingStepTarget.Css("#a"), BonboardingStepTarget.Css("#a"));
        Assert.AreNotEqual(BonboardingStepTarget.Css("#a"), BonboardingStepTarget.Css("#b"));
        Assert.AreEqual(BonboardingStepTarget.Anchor("x"), BonboardingStepTarget.Anchor("x"));
        Assert.AreNotEqual<BonboardingStepTarget>(BonboardingStepTarget.Anchor("x"), BonboardingStepTarget.Css("x"));
        Assert.AreEqual(BonboardingStepTarget.None, BonboardingStepTarget.None);
    }

    [TestMethod]
    public async Task A_Dynamic_Target_That_Never_Settles_Degrades_To_None()
    {
        BonboardingStepTarget? recursive = null;
        recursive = BonboardingStepTarget.Dynamic(_ => recursive);

        var resolved = await recursive.ResolveAsync(null!);

        Assert.IsTrue(resolved.IsNone);
    }

    [TestMethod]
    public void Rejects_An_Empty_Selector()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BonboardingStepTarget.Css("  "));
        Assert.ThrowsExactly<ArgumentException>(() => BonboardingStepTarget.Anchor(""));
    }
}
