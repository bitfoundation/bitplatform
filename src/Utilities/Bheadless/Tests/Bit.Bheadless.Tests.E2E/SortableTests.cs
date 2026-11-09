using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// Moving items between the lists of a sortable group, which only a browser carries out: the pointer drag that leaves
/// its list - the runtime hit-tests the other lists of the group, marks the ones that take the item and makes room in
/// the one under the pointer, an empty one included - and the keyboard move across the lists, whose every step is
/// announced in the live region and takes the focus along. Driven on the kanban example of the sortable page.
/// </summary>
[TestClass]
public sealed class SortableTests : DemoTest
{
    private const string Board = "Drag between lists (kanban)";

    /// <summary>The live sample of the example titled <paramref name="title"/>.</summary>
    private static ILocator Sample(IPage page, string title) =>
        page.Locator($"{Content} .bh-example", new PageLocatorOptions { Has = page.Locator("h2", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(title)}\\s*$") }) })
            .Locator(".bh-example-panel .demo-row").First;

    private static ILocator Column(ILocator board, string name) => board.Locator($"ul[aria-label=\"{name}\"]");

    private static Task<string[]> CardsAsync(ILocator column) =>
        column.EvaluateAsync<string[]>("ul => [...ul.children].map(li => li.querySelector('[data-bit-bh-sortable-handle]').getAttribute('aria-label').replace(/^Move /, ''))");

    private static ILocator Handle(ILocator board, string card) => board.Locator($"[data-bit-bh-sortable-handle][aria-label=\"Move {card}\"]");

    /// <summary>The focused element's list and name: "In progress|Move Write release notes".</summary>
    private static Task<string?> FocusedCardAsync(IPage page) => page.EvaluateAsync<string?>(
        "() => document.activeElement?.closest('ul')?.getAttribute('aria-label') + '|' + document.activeElement?.getAttribute('aria-label')");

    [TestMethod]
    public async Task The_keyboard_moves_a_card_between_columns_with_announcements()
    {
        var page = await OpenDemoAsync("/sortable");
        var board = Sample(page, Board);
        await board.ScrollIntoViewIfNeededAsync();
        var liveRegion = page.Locator("[data-bit-bh-live-region]");

        // Escape puts a card that went to another column back where it was.
        await Handle(board, "Review pull requests").FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(liveRegion).ToContainTextAsync("Picked up Review pull requests. Position 3 of 3.");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(liveRegion).ToContainTextAsync("Review pull requests moved to In progress, position 2 of 2.");
        await PollAsync(() => FocusedCardAsync(page), "In progress|Move Review pull requests", "the focus on the card in its new column");
        await page.Keyboard.PressAsync("Escape");
        await Expect(liveRegion).ToContainTextAsync("Moving cancelled. Review pull requests returned to To do, position 3.");
        await PollAsync(() => CardsAsync(Column(board, "To do")), cards => cards.SequenceEqual(["Write release notes", "Fix flaky login test", "Review pull requests"]), "the To do column restored");
        await PollAsync(() => FocusedCardAsync(page), "To do|Move Review pull requests", "the focus back on the card");

        // Across two columns, into the empty one, and dropped there.
        await Handle(board, "Write release notes").FocusAsync();
        await page.Keyboard.PressAsync(" ");
        await Expect(liveRegion).ToContainTextAsync("Picked up Write release notes. Position 1 of 3.");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(liveRegion).ToContainTextAsync("Write release notes moved to In progress, position 1 of 2.");
        await PollAsync(() => FocusedCardAsync(page), "In progress|Move Write release notes", "the focus on the card in In progress");
        await page.Keyboard.PressAsync("ArrowRight");
        await Expect(liveRegion).ToContainTextAsync("Write release notes moved to Done, position 1 of 1.");
        await PollAsync(() => FocusedCardAsync(page), "Done|Move Write release notes", "the focus on the card in Done");
        await Expect(Handle(board, "Write release notes")).ToHaveAttributeAsync("aria-pressed", "true");
        await ExpectAccessibleAsync(page);

        await page.Keyboard.PressAsync(" ");
        await Expect(liveRegion).ToContainTextAsync("Dropped Write release notes in Done at position 1 of 1.");
        await Expect(Handle(board, "Write release notes")).ToHaveAttributeAsync("aria-pressed", "false");
        Assert.AreSequenceEqual(new[] { "Write release notes" }, await CardsAsync(Column(board, "Done")));
        Assert.AreSequenceEqual(new[] { "Fix flaky login test", "Review pull requests" }, await CardsAsync(Column(board, "To do")));
        await Expect(board.Locator("p.sortable-output")).ToContainTextAsync("from todo[0] to done[0]");
        await ExpectAccessibleAsync(page);
    }

    [TestMethod]
    public async Task A_pointer_drags_a_card_into_another_column()
    {
        var page = await OpenDemoAsync("/sortable");
        var board = Sample(page, Board);
        await board.ScrollIntoViewIfNeededAsync();
        var todo = Column(board, "To do");
        var doing = Column(board, "In progress");
        var done = Column(board, "Done");

        // Onto the top half of "Update dependencies": the card lands before it.
        var target = (await doing.Locator("> li").First.BoundingBoxAsync())!;
        await DragAsync(page, Handle(board, "Write release notes"), target.X + target.Width / 2, target.Y + target.Height / 4, async () =>
        {
            // The columns that take the card are marked, and the one under the pointer.
            await Expect(doing).ToHaveAttributeAsync("data-drop-target", "");
            await Expect(done).ToHaveAttributeAsync("data-drop-target", "");
        }, async () => await Expect(doing).ToHaveAttributeAsync("data-drag-over", ""));

        await PollAsync(() => CardsAsync(doing), cards => cards.SequenceEqual(["Write release notes", "Update dependencies"]), "the card at the top of In progress");
        Assert.AreSequenceEqual(new[] { "Fix flaky login test", "Review pull requests" }, await CardsAsync(todo));
        await Expect(page.Locator("[data-bit-bh-live-region]")).ToContainTextAsync("Dropped Write release notes in In progress at position 1 of 2.");
        // Nothing is left marked, and nothing moved in the DOM but what Blazor rendered.
        await Expect(board.Locator("[data-drop-target], [data-drag-over], [data-dragging]")).ToHaveCountAsync(0);
        await PollAsync(() => board.EvaluateAsync<int>("b => [...b.querySelectorAll('li')].filter(li => li.style.transform).length"), 0, "no item left transformed");
    }

    [TestMethod]
    public async Task A_pointer_drops_a_card_into_an_empty_column_and_one_that_refuses_it_is_no_target()
    {
        var page = await OpenDemoAsync("/sortable");
        var board = Sample(page, Board);
        await board.ScrollIntoViewIfNeededAsync();
        var todo = Column(board, "To do");
        var doing = Column(board, "In progress");
        var done = Column(board, "Done");
        await Expect(done).ToHaveAttributeAsync("data-empty", "");

        // "Done" refuses blocked cards (CanAccept): it is no drop target, and a drop on it leaves the card home - dropped
        // level with the first card of its column, where a reorder along the column would have put it first.
        var first = (await Handle(board, "Write release notes").BoundingBoxAsync())!;
        var doneBox = (await done.BoundingBoxAsync())!;
        var y = Math.Clamp(first.Y + first.Height / 2, doneBox.Y + 2, doneBox.Y + doneBox.Height - 2);
        await DragAsync(page, Handle(board, "Fix flaky login test"), doneBox.X + doneBox.Width / 2, y,
            async () => await Expect(doing).ToHaveAttributeAsync("data-drop-target", ""),
            async () => await Expect(done).Not.ToHaveAttributeAsync("data-drop-target", ""));
        await Expect(page.Locator("[data-bit-bh-live-region]")).ToContainTextAsync("Reordering cancelled. Fix flaky login test returned to position 2.");
        Assert.AreSequenceEqual(new[] { "Write release notes", "Fix flaky login test", "Review pull requests" }, await CardsAsync(todo));
        await Expect(done).ToHaveAttributeAsync("data-empty", "");

        // Into the empty column.
        doneBox = (await done.BoundingBoxAsync())!;
        await DragAsync(page, Handle(board, "Update dependencies"), doneBox.X + doneBox.Width / 2, doneBox.Y + doneBox.Height / 2,
            async () => await Expect(done).ToHaveAttributeAsync("data-drop-target", ""),
            async () => await Expect(done).ToHaveAttributeAsync("data-drag-over", ""));
        await PollAsync(() => CardsAsync(done), cards => cards.SequenceEqual(["Update dependencies"]), "the card in Done");
        await Expect(page.Locator("[data-bit-bh-live-region]")).ToContainTextAsync("Dropped Update dependencies in Done at position 1 of 1.");
        await Expect(done).Not.ToHaveAttributeAsync("data-empty", "");
        await Expect(doing).ToHaveAttributeAsync("data-empty", "");
        await ExpectAccessibleAsync(page);
    }

    /// <summary>
    /// Drags <paramref name="handle"/> to (<paramref name="x"/>, <paramref name="y"/>): starts the drag, waits until
    /// <paramref name="started"/> holds (the runtime learns the drop targets from .NET a round trip later), moves on in
    /// steps, checks <paramref name="arrived"/> and drops.
    /// </summary>
    private static async Task DragAsync(IPage page, ILocator handle, float x, float y, Func<Task> started, Func<Task> arrived)
    {
        var box = (await handle.BoundingBoxAsync())!;
        var startX = box.X + box.Width / 2;
        var startY = box.Y + box.Height / 2;
        await page.Mouse.MoveAsync(startX, startY);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(startX + 12, startY + 2, new MouseMoveOptions { Steps = 3 });
        await started();
        await page.Mouse.MoveAsync(x, y, new MouseMoveOptions { Steps = 12 });
        await arrived();
        await page.Mouse.UpAsync();
    }
}
