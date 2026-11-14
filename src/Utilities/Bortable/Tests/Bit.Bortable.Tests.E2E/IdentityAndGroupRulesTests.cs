using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The demos of <c>ItemKey</c> (rows loaded again keep their selection), <c>PutGroups</c> (a trash list that takes
/// the items of two groups), <c>MinItems</c> (a pipeline that keeps two steps) and the swap partner the events name,
/// with real mouse drags and clicks.
/// </summary>
[TestClass]
public sealed class IdentityAndGroupRulesTests : BortableTest
{
    private const string Selected = "bit-bortable-selected";

    [TestMethod]
    public async Task Rows_loaded_again_keep_their_selection_when_keyed_by_id()
    {
        var page = await OpenAsync("/simple-list");
        await page.Locator("#keys-list").ScrollIntoViewIfNeededAsync();
        await Item(page, "keys-list", "Write the spec").ClickAsync();
        await Item(page, "keys-list", "Build the API").ClickAsync();
        await Expect(page.Locator($"#keys-list > .{Selected}")).ToHaveTextAsync(["Write the spec", "Build the API"]);

        await page.Locator("#keys-reload").ClickAsync();

        await Expect(page.Locator("#keys-loads")).ToHaveTextAsync("2 times");
        await Expect(page.Locator($"#keys-list > .{Selected}")).ToHaveTextAsync(["Write the spec", "Build the API"]);
    }

    [TestMethod]
    public async Task Without_the_key_rows_loaded_again_are_new_items()
    {
        var page = await OpenAsync("/simple-list");
        await page.Locator("#keys-list").ScrollIntoViewIfNeededAsync();
        await page.Locator("#keys-by-id").UncheckAsync();
        await Item(page, "keys-list", "Review the design").ClickAsync();
        await Expect(page.Locator($"#keys-list > .{Selected}")).ToHaveCountAsync(1);

        await page.Locator("#keys-reload").ClickAsync();

        await Expect(page.Locator("#keys-loads")).ToHaveTextAsync("2 times");
        await Expect(page.Locator($"#keys-list > .{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_row_loaded_again_is_dragged_as_the_row_the_list_holds_now()
    {
        var page = await OpenAsync("/simple-list");
        await page.Locator("#keys-list").ScrollIntoViewIfNeededAsync();
        await page.Locator("#keys-reload").ClickAsync();
        await Expect(page.Locator("#keys-loads")).ToHaveTextAsync("2 times");

        await DragAsync(page,
            (await BoxAsync(Item(page, "keys-list", "Write the spec"))).Centre,
            (await BoxAsync(Item(page, "keys-list", "Build the API"))).At(0.5, 0.8));

        await AssertOrderAsync(page, "keys-list", "keys-order", "Review the design", "Build the API", "Write the spec", "Ship it");
    }

    [TestMethod]
    public async Task The_trash_takes_tasks_and_bugs_which_never_mix()
    {
        var page = await OpenAsync("/shared");
        await page.Locator("#trash-bin").ScrollIntoViewIfNeededAsync();

        // A task released over Bugs: Bugs is another group and does not take it.
        await DragAsync(page,
            (await BoxAsync(Item(page, "trash-tasks", "Write the docs"))).Centre,
            (await BoxAsync(Item(page, "trash-bugs", "Crash on save"))).Centre);
        await Expect(Placeholder(page, "trash-bugs")).ToHaveCountAsync(0);
        await AssertLabelsAsync(page, "trash-bugs", ["Crash on save", "Typo in the footer"]);

        await DragAsync(page,
            (await BoxAsync(Item(page, "trash-tasks", "Write the docs"))).Centre,
            (await BoxAsync(page.Locator("#trash-bin"))).Centre);
        await AssertOrderAsync(page, "trash-bin", "trash-order", "Write the docs");
        await Expect(page.Locator("#trash-last")).ToHaveTextAsync("Write the docs, from the tasks group");

        await DragAsync(page,
            (await BoxAsync(Item(page, "trash-bugs", "Crash on save"))).Centre,
            (await BoxAsync(Item(page, "trash-bin", "Write the docs"))).At(0.5, 0.3));
        await AssertOrderAsync(page, "trash-bin", "trash-order", "Crash on save", "Write the docs");
        await Expect(page.Locator("#trash-last")).ToHaveTextAsync("Crash on save, from the bugs group");
        await AssertLabelsAsync(page, "trash-tasks", ["Plan the release", "Tidy the backlog"]);
        await AssertLabelsAsync(page, "trash-bugs", ["Typo in the footer"]);
    }

    [TestMethod]
    public async Task The_pipeline_keeps_its_two_steps()
    {
        var page = await OpenAsync("/rules");
        await page.Locator("#steps-pipeline").ScrollIntoViewIfNeededAsync();

        // Three steps: one may go.
        await DragAsync(page,
            (await BoxAsync(Item(page, "steps-pipeline", "Deploy"))).Centre,
            (await ListBoxAsync(page, "steps-shelf")).At(0.5, 0.95));
        await Expect(page.Locator("#steps-order")).ToHaveTextAsync("Build → Test");
        await AssertLabelsAsync(page, "steps-shelf", ["Lint", "Benchmark", "Deploy"]);

        // Two steps: the shelf does not take a third away.
        await PressAsync(page, (await BoxAsync(Item(page, "steps-pipeline", "Test"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "steps-shelf", "Lint"))).Centre);
        await Expect(Placeholder(page, "steps-shelf")).ToHaveCountAsync(0);
        await ReleaseAsync(page);
        await Expect(page.Locator("#steps-order")).ToHaveTextAsync("Build → Test");
        await AssertLabelsAsync(page, "steps-shelf", ["Lint", "Benchmark", "Deploy"]);

        // A step added from the shelf lets one leave again.
        await DragAsync(page,
            (await BoxAsync(Item(page, "steps-shelf", "Lint"))).Centre,
            (await BoxAsync(Item(page, "steps-pipeline", "Build"))).At(0.5, 0.3));
        await Expect(page.Locator("#steps-order")).ToHaveTextAsync("Lint → Build → Test");
        await DragAsync(page,
            (await BoxAsync(Item(page, "steps-pipeline", "Test"))).Centre,
            (await ListBoxAsync(page, "steps-shelf")).At(0.5, 0.95));
        await Expect(page.Locator("#steps-order")).ToHaveTextAsync("Lint → Build");
    }

    [TestMethod]
    public async Task A_swap_names_the_item_it_traded_with()
    {
        var page = await OpenAsync("/swap");

        await DragAsync(page,
            (await BoxAsync(Item(page, "swap-list", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "swap-list", "Item 5"))).Centre);

        await AssertOrderAsync(page, "swap-list", "swap-order", "Item 5", "Item 2", "Item 3", "Item 4", "Item 1", "Item 6");
        await Expect(page.Locator("#swap-last")).ToHaveTextAsync("Item 1 ⇄ Item 5");
    }
}
