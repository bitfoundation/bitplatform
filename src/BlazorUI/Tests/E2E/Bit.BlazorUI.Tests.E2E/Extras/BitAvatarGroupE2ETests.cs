namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitAvatarGroup at /e2e/avatar-group in a real browser: the coins overlapping by a fifth of a coin, cut apart
/// by a ring, and overlapping the other way in right-to-left; side by side when spread; the avatars and the overflow
/// coin operated from the keyboard, a focused coin lifted over its neighbours; and the group checked by axe.
/// </summary>
[TestClass]
public class BitAvatarGroupE2ETests : E2ETestBase
{
    private const string Route = "/e2e/avatar-group";

    private ILocator Coins(string id) => Page.Locator($"#{id} > .bit-prs .bit-prs-cne");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private static void Near(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < 1.5, $"{what}: expected about {expected}, got {actual}.");
    }

    [TestMethod]
    [DataRow("avg", 32)]
    [DataRow("avg-72", 72)]
    public async Task TheCoins_OverlapByAFifthOfACoin_CutApartByARing(string group, int size)
    {
        await OpenAsync(Route);

        var coins = Coins(group);
        await Expect(coins).ToHaveCountAsync(4);

        // The overlap is a share of the coin of the size the group is drawn in.
        Near(size, (await coins.First.BoundingBoxAsync())!.Width, "the coin of the size");

        for (var i = 1; i < 4; i++)
        {
            var before = (await coins.Nth(i - 1).BoundingBoxAsync())!;
            var after = (await coins.Nth(i).BoundingBoxAsync())!;

            Near(before.Width, after.Width, "every coin, the overflow one included, is of the same size");
            Near(before.X + before.Width * 0.8, after.X, $"coin {i} starts four fifths into the one before it");
        }

        var shadow = await coins.Nth(1).EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        StringAssert.Contains(shadow, "2px", "the ring that cuts a coin out of the one it covers");
    }

    [TestMethod]
    public async Task ASpreadGroup_LaysTheCoinsOutSideBySide()
    {
        await OpenAsync(Route);

        var coins = Coins("avg-spr");

        for (var i = 1; i < 4; i++)
        {
            var before = (await coins.Nth(i - 1).BoundingBoxAsync())!;
            var after = (await coins.Nth(i).BoundingBoxAsync())!;

            Assert.IsTrue(after.X >= before.X + before.Width, $"coin {i} does not overlap the one before it.");
        }

        Assert.AreEqual("none", await coins.Nth(1).EvaluateAsync<string>("el => getComputedStyle(el).boxShadow"));
    }

    [TestMethod]
    public async Task ARightToLeftGroup_OverlapsTheOtherWay()
    {
        await OpenAsync(Route);

        var coins = Coins("avg-rtl");

        var first = (await coins.Nth(0).BoundingBoxAsync())!;
        var second = (await coins.Nth(1).BoundingBoxAsync())!;

        Near(first.X - first.Width * 0.8, second.X, "the second coin starts four fifths into the first, from its right");
    }

    [TestMethod]
    public async Task TheKeyboard_ReachesEveryAvatarAndTheOverflowCoin_LiftingTheFocusedOne()
    {
        await OpenAsync(Route);

        var buttons = Page.Locator("#avg button");
        await Expect(buttons).ToHaveCountAsync(4);

        await buttons.Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(buttons.Nth(1)).ToBeFocusedAsync();
        await Expect(buttons.Nth(1)).ToHaveAccessibleNameAsync("Aaron Reid, AR");

        // A focused coin comes over its neighbours, so its focus ring is not hidden under the next one.
        var zIndex = await buttons.Nth(1).EvaluateAsync<string>("el => getComputedStyle(el.closest('.bit-prs')).zIndex");
        Assert.AreEqual("1", zIndex);

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("avg-clicked")).ToHaveTextAsync("Aaron Reid");

        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(buttons.Nth(3)).ToBeFocusedAsync();
        await Expect(buttons.Nth(3)).ToHaveAccessibleNameAsync("2 more: Roko Kolar, Christian Bergqvist");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("avg-hidden")).ToHaveTextAsync("Roko Kolar,Christian Bergqvist");
    }

    [TestMethod]
    public async Task TheGroups_PassAxe()
    {
        await OpenAsync(Route);

        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Spread team" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Img, new() { Name = "2 more: Roko Kolar, Christian Bergqvist" }).First).ToBeVisibleAsync();

        await AssertNoAxeViolationsAsync("#avg");
        await AssertNoAxeViolationsAsync("#avg-spr");
        await AssertNoAxeViolationsAsync("#avg-rtl");
    }
}
