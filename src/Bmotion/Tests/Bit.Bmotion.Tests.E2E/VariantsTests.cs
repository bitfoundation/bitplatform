namespace Bit.Bmotion.Tests.E2E;

/// <summary>Variant labels cascading to children: stagger, orchestration, dynamic variants and opting out.</summary>
[TestClass]
public sealed class VariantsTests : BmotionPageTest
{
    protected override string Route => "variants";

    /// <summary>
    /// Toggles the label once the mount cascade has finished: the page logs each completion with the label
    /// current when it arrives, so a mount completion arriving after the click would read as the new label.
    /// </summary>
    private async Task ToggleAsync()
    {
        foreach (var settled in new[] { "item3:hidden", "last0:hidden", "before-child:hidden", "after-parent:hidden", "list:hidden" })
            await ExpectEventAsync(settled);
        await ClickAsync("toggle");
    }

    [TestMethod]
    public async Task InitialState_PutsTheWholeTreeAtTheHiddenVariant()
    {
        for (var i = 0; i < 4; i++)
            await WaitForStateAsync($"#item{i}", "s => Math.abs(s.opacity) < 0.01 && Math.abs(s.x + 30) < 0.5");
        await WaitForStateAsync("#list", "s => Math.abs(s.opacity - 0.5) < 0.01");
    }

    [TestMethod]
    public async Task AChildInheritingTheInitialVariant_StaysAtItThroughItsStaggerDelay()
    {
        // At mount the cascade plays "hidden" to "hidden" with the last child 0.75s into it: it must show
        // nothing but the hidden variant meanwhile, never the identity values it was never given.
        var samples = await SampleAsync("#item3", 500);

        Assert.IsTrue(samples.All(s => s.Opacity < 0.01 && Math.Abs(s.X + 30) < 0.5),
            $"The child showed something other than its initial variant: {samples.First(s => s.Opacity >= 0.01 || Math.Abs(s.X + 30) >= 0.5)}");
    }

    [TestMethod]
    public async Task ChangingTheLabel_CascadesToEveryChild_InStaggerOrder()
    {
        await ToggleAsync();

        for (var i = 0; i < 4; i++)
            await ExpectEventAsync($"item{i}:visible");
        var log = await LoggedAsync();
        var order = Enumerable.Range(0, 4).Select(i => Array.IndexOf(log, $"item{i}:visible")).ToArray();
        CollectionAssert.AreEqual(order.Order().ToArray(), order, $"Children finished out of stagger order: {string.Join(", ", log)}");

        for (var i = 0; i < 4; i++)
            await WaitForStateAsync($"#item{i}", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");
        await ExpectEventAsync("list:visible");
    }

    [TestMethod]
    public async Task Stagger_DelaysEachChildByItsSlot()
    {
        await ToggleAsync();
        var first = SampleAsync("#item0", 1_400);
        var last = await SampleAsync("#item3", 1_400);

        // 0.25s apart: the last child starts three slots (0.75s) after the first.
        double Start(IReadOnlyList<MotionState> samples) => samples.First(s => s.Opacity > 0.05).Time;
        var gap = Start(last) - Start(await first);
        Assert.IsTrue(gap is > 550 and < 950, $"The last child started {gap:0} ms after the first; expected about 750.");
    }

    [TestMethod]
    public async Task StaggerFromLast_RunsTheCascadeBackwards()
    {
        await ToggleAsync();

        await ExpectEventAsync("last0:visible");
        var log = await LoggedAsync();
        Assert.IsTrue(Array.IndexOf(log, "last2:visible") < Array.IndexOf(log, "last1:visible"), string.Join(", ", log));
        Assert.IsTrue(Array.IndexOf(log, "last1:visible") < Array.IndexOf(log, "last0:visible"), string.Join(", ", log));
    }

    [TestMethod]
    public async Task BeforeChildren_TheParentFinishesBeforeItsChildrenStart()
    {
        await ToggleAsync();

        await ExpectEventAsync("before-child:visible");
        var log = await LoggedAsync();
        Assert.IsTrue(Array.IndexOf(log, "before-parent:visible") >= 0 &&
                      Array.IndexOf(log, "before-parent:visible") < Array.IndexOf(log, "before-child:visible"), string.Join(", ", log));
    }

    [TestMethod]
    public async Task AfterChildren_TheParentWaitsForItsChildren()
    {
        await ToggleAsync();

        await ExpectEventAsync("after-parent:visible");
        var log = await LoggedAsync();
        Assert.IsTrue(Array.IndexOf(log, "after-child:visible") >= 0 &&
                      Array.IndexOf(log, "after-child:visible") < Array.IndexOf(log, "after-parent:visible"), string.Join(", ", log));
    }

    [TestMethod]
    public async Task DynamicVariants_ResolveFromEachChildsCustom()
    {
        await ToggleAsync();

        for (var i = 1; i <= 3; i++)
            await WaitForStateAsync($"#custom{i}", $"s => Math.abs(s.x - {10 * i}) < 0.5");
    }

    [TestMethod]
    public async Task InheritFalse_IgnoresTheCascade()
    {
        await ToggleAsync();
        await WaitForStateAsync("#custom3", "s => Math.abs(s.x - 30) < 0.5");

        var optedOut = await ProbeAsync("#opted-out");
        Assert.AreEqual(0, optedOut.Opacity, 0.01, "An element with Inherit=false followed the parent's label.");
    }

    [TestMethod]
    public async Task TogglingBack_ReturnsEveryChildToTheHiddenVariant()
    {
        await ToggleAsync();
        await ExpectEventAsync("item3:visible");
        await ClickAsync("toggle");

        for (var i = 0; i < 4; i++)
            await WaitForStateAsync($"#item{i}", "s => Math.abs(s.opacity) < 0.01 && Math.abs(s.x + 30) < 0.5");
    }

    [TestMethod]
    public async Task HoverEndOnAStaggeredChild_RevertsWithoutReplayingItsStaggerDelay()
    {
        await ToggleAsync();
        await ExpectEventAsync("item3:visible");

        await HoverAsync("#item3");
        await WaitForStateAsync("#item3", "s => Math.abs(s.scaleX - 1.3) < 0.01");
        await Page.Mouse.MoveAsync(1, 1);

        // Its slot in the cascade is 0.75s in; the hover-out must not wait that long.
        await WaitForStateAsync("#item3", "s => Math.abs(s.scaleX - 1) < 0.01", timeout: 600);
    }
}
