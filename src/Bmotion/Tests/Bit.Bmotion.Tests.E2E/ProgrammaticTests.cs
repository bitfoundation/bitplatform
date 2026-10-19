namespace Bit.Bmotion.Tests.E2E;

/// <summary>BmotionAnimateService, its playback controls and sequences, and a Bmotion's own instance methods.</summary>
[TestClass]
public sealed class ProgrammaticTests : BmotionPageTest
{
    protected override string Route => "programmatic";

    [TestMethod]
    public async Task BySelector_AnimatesEveryMatch_StaggeredInOrder()
    {
        await ClickAsync("selector");
        var first = SampleAsync("#prog-0", 900);
        var last = await SampleAsync("#prog-2", 900);

        double Start(IReadOnlyList<MotionState> samples) => samples.First(s => s.X > 1).Time;
        var gap = Start(last) - Start(await first);
        Assert.IsTrue(gap is > 450 and < 800, $"The third element started {gap:0} ms after the first; expected about 600.");

        await ExpectEventAsync("selector:done", timeout: 10_000);
        for (var i = 0; i < 3; i++)
            await WaitForStateAsync($"#prog-{i}", "s => Math.abs(s.x - 200) < 0.5");
    }

    [TestMethod]
    public async Task Controls_PauseHoldsTheAnimation_AndPlayResumesIt()
    {
        await ClickAsync("selector");
        await WaitForStateAsync("#prog-0", "s => s.x > 30");
        await ClickAsync("pause");
        // The pause lands a round trip after the click (on Blazor Server a real one): from then on it holds.
        var paused = await WaitForRestAsync("#prog-0");
        await Page.WaitForTimeoutAsync(400);

        Assert.IsTrue(paused.X < 190, $"Paused too late to tell: {paused}");
        Assert.AreEqual(paused.X, (await ProbeAsync("#prog-0")).X, 1, "The animation kept moving while paused.");
        await ExpectEventAsync("selector:done", 0);

        await ClickAsync("play");
        await ExpectEventAsync("selector:done", timeout: 10_000);
        await WaitForStateAsync("#prog-0", "s => Math.abs(s.x - 200) < 0.5");
    }

    [TestMethod]
    public async Task Controls_SetSpeed_PlaysFaster()
    {
        await ClickAsync("selector");
        await ClickAsync("speed");

        // 1.6s at normal speed (1s + 2 x 0.3s stagger); at 4x it is done in about 0.4s - well inside 1.3s even a
        // Blazor Server round trip or two after the click.
        await ExpectEventAsync("selector:done", timeout: 1_300);
        await WaitForStateAsync("#prog-2", "s => Math.abs(s.x - 200) < 0.5");
    }

    [TestMethod]
    public async Task Controls_Stop_LeavesEveryElementWhereItWas_AndSettlesTheAwait()
    {
        await ClickAsync("selector");
        await WaitForStateAsync("#prog-0", "s => s.x > 30");
        await ClickAsync("stop");
        await ExpectEventAsync("selector:done");

        var stopped = await WaitForRestAsync("#prog-0");
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(stopped.X is > 30 and < 200, $"Stop didn't leave the element mid-way: {stopped}");
        Assert.AreEqual(stopped.X, (await ProbeAsync("#prog-0")).X, 1, "The element kept moving after Stop.");
    }

    [TestMethod]
    public async Task Controls_Complete_JumpsEveryElementToItsTarget()
    {
        await ClickAsync("selector");
        await WaitForStateAsync("#prog-0", "s => s.x > 10");
        await ClickAsync("complete");

        await ExpectEventAsync("selector:done");
        for (var i = 0; i < 3; i++)
            await WaitForStateAsync($"#prog-{i}", "s => Math.abs(s.x - 200) < 0.5", timeout: 1_000);
    }

    [TestMethod]
    public async Task ByElementReference_AnimatesThatElement()
    {
        await ClickAsync("by-ref");
        await ExpectEventAsync("by-ref:done");
        await WaitForStateAsync("#by-ref-target", "s => Math.abs(s.x - 80) < 0.5 && Math.abs(s.opacity - 0.5) < 0.01");
    }

    [TestMethod]
    public async Task ARawNumber_IsAnimatedThroughTheCallback()
    {
        await ClickAsync("count");
        await ExpectEventAsync("count:done");
        await ExpectReadoutAsync("count-value", "100");
    }

    [TestMethod]
    public async Task ASecondAnimation_StartsWhereTheFirstLeftTheElement_AndKeepsItsOtherValues()
    {
        await ClickAsync("twice");
        await ExpectEventAsync("twice:first");
        var samples = await SampleAsync("#twice-target", 300);
        await ExpectEventAsync("twice:second");

        Assert.IsTrue(samples.All(s => s.X >= 99), $"The second animation restarted from 0: min x {samples.Min(s => s.X):0.#}");
        await WaitForStateAsync("#twice-target", "s => Math.abs(s.x - 200) < 0.5 && Math.abs(s.scaleX - 1.5) < 0.01");
    }

    [TestMethod]
    public async Task CancellingTheToken_StopsTheAnimationMidFlight()
    {
        await ClickAsync("cancel");
        await ExpectEventAsync("cancel:settled");

        var stopped = await WaitForRestAsync("#cancel-target");
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(stopped.X is > 20 and < 280, $"The cancelled animation didn't stop mid-way: {stopped}");
        Assert.AreEqual(stopped.X, (await ProbeAsync("#cancel-target")).X, 1, "The element kept moving after cancellation.");
    }

    [TestMethod]
    public async Task Sequence_RunsItsStepsAndResolvesWhenDone()
    {
        await ClickAsync("sequence");
        await ExpectEventAsync("sequence:done");

        await WaitForStateAsync("#seq-a", "s => Math.abs(s.x - 100) < 0.5 && Math.abs(s.y - 40) < 0.5");
        await WaitForStateAsync("#seq-b", "s => Math.abs(s.scaleX - 1.5) < 0.01");
    }

    [TestMethod]
    public async Task InstanceAnimate_AndPauseResume()
    {
        await ClickAsync("ref-animate");
        await WaitForStateAsync("#box", "s => s.x > 20");
        await ClickAsync("ref-pause");
        var paused = await WaitForRestAsync("#box");
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(paused.X < 115, $"Paused too late to tell: {paused}");
        Assert.AreEqual(paused.X, (await ProbeAsync("#box")).X, 1, "Pause() didn't hold the element.");

        await ClickAsync("ref-resume");
        await WaitForStateAsync("#box", "s => Math.abs(s.x - 120) < 0.5");
    }

    [TestMethod]
    public async Task InstanceStop_StopsOnlyTheNamedProperty()
    {
        await ClickAsync("ref-animate");
        await WaitForStateAsync("#box", "s => s.x > 20");
        await ClickAsync("ref-stop");

        var stopped = await WaitForRestAsync("#box");
        await Page.WaitForTimeoutAsync(300);
        Assert.AreEqual(stopped.X, (await ProbeAsync("#box")).X, 1);
        Assert.IsTrue(stopped.X < 115, $"Stopped too late to tell: {stopped}");
    }

    [TestMethod]
    public async Task InstanceSet_AndSetAsync_ApplyAtOnce()
    {
        await ClickAsync("ref-set");
        await WaitForStateAsync("#box", "s => Math.abs(s.y - 20) < 0.5", timeout: 2_000);

        await ClickAsync("ref-set-async");
        await WaitForStateAsync("#box", "s => Math.abs(s.opacity - 0.4) < 0.01", timeout: 2_000);
        // SetAsync writes individual declarations; it must not wipe the transform Set left.
        Assert.AreEqual(20, (await ProbeAsync("#box")).Y, 0.5);
    }

    [TestMethod]
    public async Task InstanceAnimate_CancelledMidFlight_Stops()
    {
        await ClickAsync("ref-cancel");
        await ExpectEventAsync("ref-cancel:cancelled");

        var stopped = await WaitForRestAsync("#box");
        await Page.WaitForTimeoutAsync(300);
        Assert.IsTrue(stopped.X is > 20 and < 280, $"The cancelled animation didn't stop mid-way: {stopped}");
        Assert.AreEqual(stopped.X, (await ProbeAsync("#box")).X, 1, "The element kept moving after cancellation.");
    }
}
