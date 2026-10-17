namespace Bit.Bmotion.Tests.E2E;

/// <summary>BmotionSplitText: the split, its accessibility, the stagger and its completion.</summary>
[TestClass]
public sealed class SplitTextTests : BmotionPageTest
{
    protected override string Route => "splittext";

    [TestMethod]
    public async Task Chars_OneAnimatedUnitPerCharacter_WordsKeptTogether_TextIntact()
    {
        var container = Page.Locator(".chars");
        await Assertions.Expect(container.Locator(".unit")).ToHaveCountAsync(7); // "Hi" + "there", no unit for the space
        Assert.AreEqual("Hi there", await container.TextContentAsync());

        // Each word is one nowrap inline-block, so a line never breaks between two of its letters.
        var groups = await container.Locator(":scope > span[style*='nowrap']").CountAsync();
        Assert.AreEqual(2, groups);

        await ExpectEventAsync("chars:complete");
        foreach (var unit in await container.Locator(".unit").AllAsync())
            Assert.AreEqual("1", await unit.EvaluateAsync<string>("el => getComputedStyle(el).opacity"));
    }

    [TestMethod]
    public async Task Accessible_TheContainerReadsAsOneText_AndTheUnitsAreHidden()
    {
        var container = Page.Locator(".chars");
        await Assertions.Expect(container).ToHaveAttributeAsync("aria-label", "Hi there");
        await Assertions.Expect(container).ToHaveAttributeAsync("role", "text");
        await Assertions.Expect(container.Locator(":scope > span[aria-hidden='true']")).ToHaveCountAsync(2);

        var quiet = Page.Locator(".inaccessible");
        await Assertions.Expect(quiet).Not.ToHaveAttributeAsync("aria-label", new System.Text.RegularExpressions.Regex(".*"));
        await Assertions.Expect(quiet.Locator("[aria-hidden]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Words_OneUnitPerWord_InTheRequestedElement()
    {
        var container = Page.Locator("h2.words");
        await Assertions.Expect(container.Locator(".unit")).ToHaveCountAsync(3);
        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, (await container.Locator(".unit").AllTextContentsAsync()).ToArray());
        await ExpectEventAsync("words:complete");
    }

    [TestMethod]
    public async Task Lines_OneBlockUnitPerAuthoredLine()
    {
        var units = Page.Locator(".lines .unit");
        await Assertions.Expect(units).ToHaveCountAsync(2);
        Assert.AreEqual("block", await units.First.EvaluateAsync<string>("el => getComputedStyle(el).display"));
        CollectionAssert.AreEqual(new[] { "first line", "second line" }, (await units.AllTextContentsAsync()).ToArray());
    }

    [TestMethod]
    public async Task OnComplete_WaitsForTheUnitThatFinishesLast_EvenWhenTheCascadeRunsBackwards()
    {
        await ExpectEventAsync("backwards:complete");

        // The first character started 0.9s after the last one; it must be done by now too.
        var first = Page.Locator(".backwards .unit").First;
        Assert.AreEqual(1, Number(await first.EvaluateAsync<string>("el => getComputedStyle(el).opacity")), 0.01);
    }

    [TestMethod]
    public async Task WithoutATransition_TheSurroundingConfigsTransitionApplies()
    {
        await ClickAsync("go");
        await Page.WaitForTimeoutAsync(500);

        // 1.2s linear: still fading in after half a second, not done like the library's 0.3s default.
        var opacity = Number(await Page.Locator(".configured .unit").First.EvaluateAsync<string>("el => getComputedStyle(el).opacity"));
        Assert.IsTrue(opacity < 0.9, $"The config's transition was ignored: opacity {opacity} after 0.5s.");
        await ExpectEventAsync("configured:complete", timeout: 5_000);
    }
}
