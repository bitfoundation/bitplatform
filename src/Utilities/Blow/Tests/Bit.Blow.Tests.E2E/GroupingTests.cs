using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// The editing commands in a real browser, through the grouping demo: grouping the selection without moving it on
/// screen, undoing it by its label, and a batched edit landing as one undo step and one change.
/// </summary>
[TestClass]
public sealed class GroupingTests : BlowTest
{
    private static ILocator Button(IPage page, string name) =>
        page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator ButtonStartingWith(IPage page, string text) =>
        page.Locator("button", new() { HasTextRegex = new System.Text.RegularExpressions.Regex($"^\\s*{text}") });

    [TestMethod]
    public async Task Grouping_the_selection_wraps_it_in_a_group_without_moving_it_and_undo_names_the_step()
    {
        var page = await OpenAsync("/examples/grouping");
        await WaitForEdgesAnchoredAsync(page);
        var parse = await NodeBoxAsync(page, "parse");
        var check = await NodeBoxAsync(page, "check");

        await Button(page, "Group").ClickAsync();
        await WaitForQuietAsync(page);

        var group = page.Locator(".bit-blow-node.bit-blow-node-group");
        await Expect(group).ToHaveCountAsync(1);
        var box = await BoxAsync(page, ".bit-blow-node.bit-blow-node-group");
        Assert.IsTrue(box.X < parse.X && box.Y < parse.Y && box.Right > check.Right, "the group does not enclose the selection");
        AssertNear(parse.Centre, (await NodeBoxAsync(page, "parse")).Centre, 1, "parse moved when grouped");
        AssertNear(check.Centre, (await NodeBoxAsync(page, "check")).Centre, 1, "check moved when grouped");
        await Expect(page.Locator(".log li").First).ToContainTextAsync("Edited Api group");

        var undo = ButtonStartingWith(page, "↶ Undo");
        await Expect(undo).ToContainTextAsync("Undo group");
        await undo.ClickAsync();
        await WaitForQuietAsync(page);

        await Expect(group).ToHaveCountAsync(0);
        AssertNear(parse.Centre, (await NodeBoxAsync(page, "parse")).Centre, 1, "parse moved on undo");
        await Expect(page.Locator(".log li").First).ToContainTextAsync("Replaced History undo");
    }

    [TestMethod]
    public async Task A_batched_edit_is_one_undo_step()
    {
        var page = await OpenAsync("/examples/grouping");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page);

        await Button(page, "Add a chain (one step)").ClickAsync();
        await WaitForQuietAsync(page);
        Assert.HasCount(before.Length + 3, await IdsAsync(page));
        await Expect(page.Locator(".log li")).ToHaveCountAsync(1);
        await Expect(page.Locator(".log li").First).ToContainTextAsync("add chain");

        await ButtonStartingWith(page, "↶ Undo").ClickAsync();
        await WaitForQuietAsync(page);
        CollectionAssert.AreEquivalent(before, await IdsAsync(page), "one undo did not take the whole edit back");
    }

    [TestMethod]
    public async Task A_remote_edit_is_reported_as_remote_and_cannot_be_undone()
    {
        var page = await OpenAsync("/examples/grouping");
        await WaitForEdgesAnchoredAsync(page);
        var store = await NodeBoxAsync(page, "store");

        await Button(page, "Simulate a remote edit").ClickAsync();
        await WaitForQuietAsync(page);

        Assert.IsGreaterThan(store.X + 10, (await NodeBoxAsync(page, "store")).X, "the remote edit did not move the node");
        await Expect(page.Locator(".log li").First).ToContainTextAsync("Edited Remote remote move");
        await Expect(ButtonStartingWith(page, "↶ Undo")).ToBeDisabledAsync();
    }
}
