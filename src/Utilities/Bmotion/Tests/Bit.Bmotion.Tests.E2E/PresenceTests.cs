namespace Bit.Bmotion.Tests.E2E;

/// <summary>Exit animations through BmotionAnimatePresence, BmotionPresenceSwitch and BmotionPresenceGroup.</summary>
[TestClass]
public sealed class PresenceTests : BmotionPageTest
{
    protected override string Route => "presence";

    /// <summary>Every presence child must have finished initialising before a test makes it leave.</summary>
    [TestInitialize]
    public async Task WaitUntilArmedAsync()
    {
        foreach (var name in new[] { "pop", "wait1", "sync1", "pg1", "pg2", "pg3" })
            await ExpectEventAsync($"armed:{name}");
        await ExpectEventAsync("sync:entered");
        for (var i = 1; i <= 3; i++) await ExpectEventAsync($"group:entered:{i}");
    }

    [TestMethod]
    public async Task AnimatePresence_KeepsTheElementWhileItExits_ThenRemovesIt()
    {
        await ExpectEventAsync("sync:entered");

        await ClickAsync("sync-toggle");
        var exiting = await SampleAsync("#sync", 200);
        Assert.IsTrue(exiting.Count > 0 && exiting.Any(s => s.Opacity is > 0.05 and < 0.95), "The element left without playing its exit.");
        Assert.IsTrue(exiting.Last().X > exiting.First().X, "The exit's x never moved.");

        await Assertions.Expect(Page.Locator("#sync")).ToHaveCountAsync(0);
        await ExpectEventAsync("sync:exited");
    }

    [TestMethod]
    public async Task AnimatePresence_ShownAgain_EntersAgain()
    {
        await ExpectEventAsync("sync:entered");
        await ClickAsync("sync-toggle");
        await ExpectEventAsync("sync:exited");

        await ClickAsync("sync-toggle");
        await Assertions.Expect(Page.Locator("#sync")).ToHaveCountAsync(1);
        await ExpectEventAsync("sync:entered", 2);
        await WaitForStateAsync("#sync", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");
    }

    [TestMethod]
    public async Task AnimatePresence_ReShownMidExit_ThenHiddenAgain_StillExitsAndRemoves()
    {
        await ExpectEventAsync("sync:entered");

        await ClickAsync("sync-toggle");
        await Page.WaitForTimeoutAsync(120);
        await ClickAsync("sync-toggle");
        await WaitForStateAsync("#sync", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");

        await ClickAsync("sync-toggle");
        await Assertions.Expect(Page.Locator("#sync")).ToHaveCountAsync(0);
        await ExpectEventAsync("sync:exited", 1);
    }

    [TestMethod]
    public async Task PopLayout_TakesTheExitingElementOutOfTheFlow_SoSiblingsMoveAtOnce()
    {
        var siblingBefore = (await ProbeAsync("#pop-sibling")).Top;

        await ClickAsync("pop-toggle");
        await WaitForStateAsync("#pop", "s => s.style.includes('position: absolute')");
        await WaitForStateAsync("#pop-sibling", $"s => s.top < {siblingBefore} - 10");
        Assert.AreEqual(1, await Page.Locator("#pop").CountAsync(), "The popped element should still be exiting.");

        await Assertions.Expect(Page.Locator("#pop")).ToHaveCountAsync(0);
        await ExpectEventAsync("pop:exited");
    }

    [TestMethod]
    public async Task PresenceSwitchWait_TheOldItemFinishesLeavingBeforeTheNewOneArrives()
    {
        await ClickAsync("switch-next");
        await Assertions.Expect(Page.Locator("#wait-2")).ToHaveCountAsync(1);
        await Assertions.Expect(Page.Locator("#wait-1")).ToHaveCountAsync(0);
        await ExpectEventAsync("wait:exited");

        // Never both at once: the samples of the container's children while switching again.
        await ClickAsync("switch-next");
        var counts = await Page.EvaluateAsync<int[]>("""
            async () => {
                const out = [];
                const end = performance.now() + 700;
                while (performance.now() < end) {
                    out.push(document.querySelectorAll('#switch-wait .page').length);
                    await new Promise(r => requestAnimationFrame(r));
                }
                return out;
            }
            """);
        Assert.IsTrue(counts.All(c => c <= 1), $"Wait mode showed two items at once: {string.Join(",", counts)}");
        await Assertions.Expect(Page.Locator("#wait-3")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task PresenceSwitchSync_TheOldAndNewItemsOverlap()
    {
        await ClickAsync("switch-next");
        await Assertions.Expect(Page.Locator("#switch-sync .page")).ToHaveCountAsync(2);
        await Assertions.Expect(Page.Locator("#sync-1")).ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator("#sync-2")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task PresenceGroup_AddedItemsEnter_AndRemovedItemsExitBeforeLeaving()
    {
        for (var i = 1; i <= 3; i++) await ExpectEventAsync($"group:entered:{i}");

        await ClickAsync("group-add");
        await ExpectEventAsync("group:entered:4");

        await ClickAsync("group-remove-first");
        var exiting = await SampleAsync("#g-1", 200);
        Assert.IsTrue(exiting.Any(s => s.Opacity is > 0.05 and < 0.95), "The removed item left without its exit.");
        await Assertions.Expect(Page.Locator("#g-1")).ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator("#group .item")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task PresenceGroup_OnExitComplete_FiresOncePerRemovedItem()
    {
        await ExpectEventAsync("group:entered:3");

        await ClickAsync("group-remove-first");
        await Assertions.Expect(Page.Locator("#g-1")).ToHaveCountAsync(0);
        await ClickAsync("group-remove-first");
        await Assertions.Expect(Page.Locator("#g-2")).ToHaveCountAsync(0);

        await Page.WaitForTimeoutAsync(300);
        await ExpectEventAsync("group:exited", 2);
    }

    [TestMethod]
    public async Task PresenceGroup_ReAddedMidExit_CancelsTheExit_AndCanBeRemovedAgain()
    {
        await ExpectEventAsync("group:entered:1");

        await ClickAsync("group-remove-first");
        await Page.WaitForTimeoutAsync(120);
        await ClickAsync("group-readd");
        await WaitForStateAsync("#g-1", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");
        await Page.WaitForTimeoutAsync(500);
        await Assertions.Expect(Page.Locator("#g-1")).ToHaveCountAsync(1);

        await ClickAsync("group-remove-first");
        await Assertions.Expect(Page.Locator("#g-1")).ToHaveCountAsync(0);
        await ExpectEventAsync("group:exited", 1);
    }

    [TestMethod]
    public async Task PresenceGroupPopLayout_SiblingsReflowAtOnce()
    {
        var secondBefore = (await ProbeAsync("#pg-2")).Top;

        await ClickAsync("popgroup-remove-first");
        await WaitForStateAsync("#pg-2", $"s => s.top < {secondBefore} - 10");
        Assert.AreEqual(1, await Page.Locator("#pg-1").CountAsync(), "The popped item should still be exiting.");

        await Assertions.Expect(Page.Locator("#pg-1")).ToHaveCountAsync(0);
        await ExpectEventAsync("popgroup:exited");
    }
}
