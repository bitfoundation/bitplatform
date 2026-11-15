namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// Drives BitEmptyState at /e2e/empty-state in a real browser: its parts stacked and centered by default, at the start
/// of the line (the right edge in right-to-left) when aligned to start; the title a heading only where it is asked to
/// be one; the default words; the parts kept inside a column narrower than their words; and the empty states checked
/// by axe.
/// </summary>
[TestClass]
public class BitEmptyStateE2ETests : E2ETestBase
{
    private const string Route = "/e2e/empty-state";

    private ILocator Part(string id, string part) => Page.Locator($"#{id} > .bit-ems-{part}");

    private static void Near(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < 1.5, $"{what}: expected about {expected}, got {actual}.");
    }

    [TestMethod]
    public async Task ThePartsOfAnEmptyState_StackDownTheMiddleOfIt()
    {
        await OpenAsync(Route);

        var root = (await Page.Locator("#ems-full").BoundingBoxAsync())!;
        var middle = root.X + root.Width / 2;

        double? bottom = null;

        foreach (var part in new[] { "ico", "ttl", "dsc", "con" })
        {
            var box = (await Part("ems-full", part).BoundingBoxAsync())!;

            Near(middle, box.X + box.Width / 2, $"the middle of the {part}");

            if (bottom is not null) Assert.IsTrue(box.Y >= bottom.Value - 0.5, $"the {part} is under the part before it.");
            bottom = box.Y + box.Height;
        }

        var textAlign = await Part("ems-full", "dsc").EvaluateAsync<string>("el => getComputedStyle(el).textAlign");
        Assert.AreEqual("center", textAlign);
    }

    [TestMethod]
    [DataRow("ems-start", false)]
    [DataRow("ems-rtl", true)]
    public async Task AnEmptyStateAlignedToStart_LinesItsPartsUpAtTheStartOfTheLine(string id, bool rtl)
    {
        await OpenAsync(Route);

        var root = (await Page.Locator($"#{id}").BoundingBoxAsync())!;
        var padding = await Page.Locator($"#{id}").EvaluateAsync<double>(
            rtl ? "el => parseFloat(getComputedStyle(el).paddingRight)" : "el => parseFloat(getComputedStyle(el).paddingLeft)");

        var edge = rtl ? root.X + root.Width - padding : root.X + padding;

        foreach (var part in new[] { "ico", "ttl", "dsc", "con" })
        {
            var box = (await Part(id, part).BoundingBoxAsync())!;

            Near(edge, rtl ? box.X + box.Width : box.X, $"the {(rtl ? "right" : "left")} edge of the {part}");
        }
    }

    [TestMethod]
    public async Task TheTitle_IsAHeadingOnlyWhereOneIsAskedFor()
    {
        await OpenAsync(Route);

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Your inbox is empty", Level = 2 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Nothing matches these filters" })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AnEmptyStateGivenNothing_SaysItHasNoItems()
    {
        await OpenAsync(Route);

        await Expect(Part("ems-default", "ttl")).ToHaveTextAsync("No items found");
        await Expect(Page.Locator("#ems-default > *")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task TheTitleAndTheLineUnderIt_AreDrawnInTheirColorsAndSizes()
    {
        await OpenAsync(Route);

        var primary = await Page.ResolveColorAsync("--bit-clr-fg-pri");
        var secondary = await Page.ResolveColorAsync("--bit-clr-fg-sec");

        Assert.AreEqual(primary, await Part("ems-full", "ttl").EvaluateAsync<string>("el => getComputedStyle(el).color"));
        Assert.AreEqual(secondary, await Part("ems-full", "dsc").EvaluateAsync<string>("el => getComputedStyle(el).color"));
        Assert.AreEqual(secondary, await Part("ems-full", "ico").EvaluateAsync<string>("el => getComputedStyle(el).color"));

        var title = await Part("ems-full", "ttl").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");
        var description = await Part("ems-full", "dsc").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");
        var icon = await Part("ems-full", "ico").EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).fontSize)");

        Assert.IsTrue(icon > title && title > description, $"the icon ({icon}px) is larger than the title ({title}px), which is larger than the line under it ({description}px).");
    }

    [TestMethod]
    public async Task AnEmptyStateInANarrowColumn_KeepsItsPartsInsideIt()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#ems-narrow");
        var box = (await root.BoundingBoxAsync())!;

        Near(140, box.Width, "the empty state is as wide as its column");

        var overflows = await root.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");
        Assert.IsFalse(overflows, "a word longer than the column wraps instead of overflowing it.");

        foreach (var part in new[] { "ttl", "dsc", "con" })
        {
            var partBox = (await Part("ems-narrow", part).BoundingBoxAsync())!;
            Assert.IsTrue(partBox.X >= box.X - 0.5 && partBox.X + partBox.Width <= box.X + box.Width + 0.5, $"the {part} stays inside the column.");
        }

        // The two actions that do not fit side by side wrap onto a second row.
        var buttons = Page.Locator("#ems-narrow > .bit-ems-con > *");
        var first = (await buttons.Nth(0).BoundingBoxAsync())!;
        var second = (await buttons.Nth(1).BoundingBoxAsync())!;
        Assert.IsTrue(second.Y >= first.Y + first.Height - 0.5 || second.X >= first.X + first.Width, "the actions wrap or sit side by side, never on top of each other.");
    }

    [TestMethod]
    public async Task TheEmptyStates_PassAxe()
    {
        await OpenAsync(Route);

        await Expect(Part("ems-full", "ttl")).ToBeVisibleAsync();

        await AssertNoAxeViolationsAsync("#ems-full");
        await AssertNoAxeViolationsAsync("#ems-start");
        await AssertNoAxeViolationsAsync("#ems-rtl");
        await AssertNoAxeViolationsAsync("#ems-default");
    }
}
