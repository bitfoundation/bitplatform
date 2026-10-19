using System.Text.RegularExpressions;

namespace Bit.Bmotion.Tests.E2E;

/// <summary>Motion values bound to elements, the View Transitions wrapper, the inspector, and the served bridge.</summary>
[TestClass]
public sealed class MiscTests : BmotionPageTest
{
    protected override string Route => "misc";

    [TestMethod]
    public async Task AMotionValue_IsWrittenToItsElementWithoutARender_AndDerivedValuesFollow()
    {
        await ClickAsync("mv-set");

        await WaitForStateAsync("#mv-x", "s => Math.abs(s.x - 100) < 0.5");
        await WaitForStateAsync("#mv-opacity", "s => Math.abs(s.opacity - 0.6) < 0.01");
        await ExpectReadoutAsync("mv-value", "100");
        await ExpectReadoutAsync("mv-label", "100px");
    }

    [TestMethod]
    public async Task AnimatingAMotionValue_MovesEverythingBoundToIt()
    {
        await ClickAsync("mv-animate");
        var samples = await SampleAsync("#mv-x", 300);
        await ExpectEventAsync("mv:animated");

        await WaitForStateAsync("#mv-x", "s => Math.abs(s.x - 200) < 0.5");
        await WaitForStateAsync("#mv-opacity", "s => Math.abs(s.opacity - 1) < 0.01");
        if (FrameLoop)
            Assert.IsTrue(samples.Any(s => s.X is > 5 and < 195), "The value jumped instead of animating.");
    }

    [TestMethod]
    public async Task ASpringFollower_TrailsItsSource_AndSettlesOnIt()
    {
        await ClickAsync("mv-set");
        var samples = await SampleAsync("#mv-spring", 1_500);

        await WaitForStateAsync("#mv-spring", "s => Math.abs(s.x - 100) < 1", timeout: 10_000);
        if (FrameLoop)
        {
            Assert.IsTrue(samples.Any(s => s.X is > 5 and < 95), "The follower jumped instead of springing.");
            Assert.IsTrue(samples.Max(s => s.X) > 101, "An underdamped follower should overshoot.");
        }
    }

    [TestMethod]
    public async Task ATemplate_ComposesItsInputsIntoACssString()
    {
        await ClickAsync("mv-blur");
        await Assertions.Expect(Page.Locator("#mv-filter")).ToHaveCSSAsync("filter", "blur(4px)");
    }

    [TestMethod]
    public async Task ViewTransition_RunsTheUpdateInsideTheBrowsersTransition()
    {
        await ClickAsync("vt");

        // Chromium has the API, so the update ran inside document.startViewTransition.
        await ExpectEventAsync("vt:native");
        await Assertions.Expect(Page.Locator("#vt-target")).ToHaveTextAsync("after");
    }

    [TestMethod]
    public async Task Inspector_ListsTheRegisteredElements()
    {
        await Assertions.Expect(Page.Locator(".bm-insp-count")).ToContainTextAsync(new Regex(@"^[1-9]\d* el"));

        // Open by default; rows appear on its next poll - every Bmotion on the page is one.
        await Assertions.Expect(Page.Locator(".bm-insp-el").Nth(3)).ToBeVisibleAsync();

        // The bar collapses it, and opens it again.
        await Page.Locator(".bm-insp-bar").ClickAsync();
        await Assertions.Expect(Page.Locator(".bm-insp-el")).ToHaveCountAsync(0);
        await Page.Locator(".bm-insp-bar").ClickAsync();
        await Assertions.Expect(Page.Locator(".bm-insp-el").Nth(3)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task TheServedBridge_ExportsEveryFunctionTheInteropCalls()
    {
        // The identifiers BmotionInterop invokes on the module, from its source, against the bundle the host
        // actually serves (minified in Release) - a rename on either side, or an export the bundler dropped,
        // fails here instead of as a silent no-op in an app.
        var source = await File.ReadAllTextAsync(RepoLayout.InteropSource());
        var invoked = Regex.Matches(source, @"Invoke(?:Void)?Async(?:<[^>]+>)?\(\s*""([A-Za-z_][A-Za-z0-9_]*)""")
            .Select(m => m.Groups[1].Value)
            .Where(name => name is not "import")
            .Distinct()
            .ToArray();
        Assert.IsTrue(invoked.Length > 20, $"Found only {invoked.Length} invoked identifiers - did the interop's shape change?");

        var exported = await Page.EvaluateAsync<string[]>(
            "async () => Object.keys(await import('./_content/Bit.Bmotion/bit-bmotion.js'))");

        CollectionAssert.IsSubsetOf(invoked, exported, $"Missing exports: {string.Join(", ", invoked.Except(exported))}");
    }
}
