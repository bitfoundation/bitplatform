namespace Bit.Bmotion.Tests.E2E;

/// <summary>BmotionConfig: default transition, TransitionSpeed, ReduceMotion and colour space - with no OS preference.</summary>
[TestClass]
public sealed class ConfigTests : BmotionPageTest
{
    protected override string Route => "config";

    /// <summary>Every element must have finished initialising before #go changes its target.</summary>
    [TestInitialize]
    public async Task WaitUntilArmedAsync()
    {
        foreach (var name in new[] { "speed0", "speed4", "default", "reduced", "never", "os", "none", "srgb", "oklab" })
            await ExpectEventAsync($"armed:{name}");
    }

    [TestMethod]
    public async Task TransitionSpeedZero_MakesEveryAnimationInstant()
    {
        await ClickAsync("go");
        await WaitForStateAsync("#speed0", "s => Math.abs(s.x - 100) < 0.5", timeout: 500);
        await ExpectEventAsync("speed0:complete");
    }

    [TestMethod]
    public async Task TransitionSpeed_ScalesTheDuration()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#speed4", 900);

        // A 2s tween at 4x: done in about half a second.
        var arrived = samples.FirstOrDefault(s => Math.Abs(s.X - 100) < 0.5);
        Assert.IsNotNull(arrived, "The 4x tween hadn't finished after 900 ms.");
        Assert.IsTrue(arrived.Time is > 300 and < 800, $"Arrived after {arrived.Time:0} ms; expected about 500.");
    }

    [TestMethod]
    public async Task DefaultTransition_AppliesToElementsWithoutTheirOwn()
    {
        await ClickAsync("go");
        await Page.WaitForTimeoutAsync(700);

        // Linear over 1.5s: about half-way after 0.7s, not done like the library's 0.3s default.
        var midway = await ProbeAsync("#default-transition");
        Assert.IsTrue(midway.X is > 25 and < 75, $"Not following the config's transition: {midway}");
        await WaitForStateAsync("#default-transition", "s => Math.abs(s.x - 100) < 0.5");
    }

    [TestMethod]
    public async Task ReduceMotionTrue_SnapsTransforms_ButStillFadesOpacity()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#reduced", 600);

        Assert.IsTrue(Snapped(samples, 100), "The transform animated under reduced motion.");
        // The fade runs on the frame loop (a per-property transition the compositor can't take), so on
        // Blazor Server it lands at once along with everything else that needs the loop.
        if (FrameLoop)
            Assert.IsTrue(samples.Any(s => s.Opacity is > 0.25 and < 0.95), "Opacity should still fade under reduced motion.");
        await WaitForStateAsync("#reduced", "s => Math.abs(s.opacity - 1) < 0.01");
    }

    [TestMethod]
    public async Task WithoutAnOsPreference_ConfigsThatFollowTheOs_Animate()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#os-config", 400);

        Assert.IsTrue(samples.Any(s => s.X is > 1 and < 99), "Animation was reduced although the OS asks for nothing.");
    }

    [TestMethod]
    public async Task ColorSpaceOklab_InterpolatesThroughADifferentPathThanSrgb()
    {
        await ClickAsync("go");
        var srgb = SampleAsync("#srgb", 900);
        var oklab = await SampleAsync("#oklab", 900);

        static int Green(string rgb) => int.Parse(rgb.Split(',')[1].Trim());
        await WaitForStateAsync("#oklab", "s => s.backgroundColor === 'rgb(0, 0, 255)'");
        if (FrameLoop is false) return; // colours snap on Blazor Server

        // Red to blue in sRGB never picks up green; through OKLab it does.
        Assert.IsTrue((await srgb).All(s => Green(s.BackgroundColor) == 0), "sRGB interpolation picked up green.");
        Assert.IsTrue(oklab.Any(s => Green(s.BackgroundColor) > 0), "OKLab interpolation went through sRGB's path.");
    }
}

/// <summary>The same page with the OS asking for reduced motion (<c>prefers-reduced-motion: reduce</c>).</summary>
[TestClass]
public sealed class ConfigWithReducedMotionPreferenceTests : BmotionPageTest
{
    protected override string Route => "config";

    /// <summary>Every element must have finished initialising before #go changes its target.</summary>
    [TestInitialize]
    public async Task WaitUntilArmedAsync()
    {
        foreach (var name in new[] { "speed0", "speed4", "default", "reduced", "never", "os", "none", "srgb", "oklab" })
            await ExpectEventAsync($"armed:{name}");
    }

    protected override void ConfigureContext(BrowserNewContextOptions options) => options.ReducedMotion = ReducedMotion.Reduce;

    [TestMethod]
    public async Task AConfigWithoutReduceMotion_FollowsTheOsPreference()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#os-config", 400);

        Assert.IsTrue(Snapped(samples, 100), "The OS preference was ignored inside a BmotionConfig.");
    }

    [TestMethod]
    public async Task ReduceMotionFalse_AnimatesWhateverTheOsSays()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#never-reduced", 400);

        Assert.IsTrue(samples.Any(s => s.X is > 1 and < 99), "ReduceMotion=false did not animate.");
    }

    [TestMethod]
    public async Task OutsideAnyConfig_TheDefaultModeIgnoresTheOsPreference()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#no-config", 400);

        Assert.IsTrue(samples.Any(s => s.X is > 1 and < 99), "IgnoreUnlessConfigured reduced an element outside any BmotionConfig.");
    }
}
