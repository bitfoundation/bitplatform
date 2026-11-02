using System.Globalization;

namespace Bit.Bmotion.Tests.E2E;

/// <summary>BmotionReorderGroup: drag-to-reorder along either axis, by the whole item or by a grip.</summary>
[TestClass]
public sealed class ReorderTests : BmotionPageTest
{
    protected override string Route => "reorder";

    /// <summary>Every item listens for its drag once the bridge armed it; a press before that is lost.</summary>
    [TestInitialize]
    public async Task WaitUntilDraggableAsync()
    {
        foreach (var list in new[] { "#vertical .item", "#horizontal .item", "#handled .item", "#grid .cell", "#scrolling .item" })
            for (var i = 0; i < await Page.Locator(list).CountAsync(); i++)
                await WaitForDragReadyAsync($"{list} >> nth={i}");
    }

    /// <summary>The items' text in the order the document lays them out now.</summary>
    private Task<string[]> DomOrderAsync(string container)
        => Page.Locator($"#{container} .item").EvaluateAllAsync<string[]>("els => els.map(e => e.textContent.trim())");

    [TestMethod]
    public async Task DraggingAnItemPastItsNeighbours_CommitsTheNewOrder()
    {
        // Items are 40px tall with 4px gaps: A dragged 100px down lands after C.
        await DragByAsync("#vertical .item[data-item='A']", 0, 100, steps: 20);

        await ExpectEventAsync("vertical:reordered");
        await ExpectReadoutAsync("vertical-order", "B,C,A,D");
        await Assertions.Expect(Page.Locator("#vertical .item").Nth(2)).ToHaveTextAsync("A");
        CollectionAssert.AreEqual(new[] { "B", "C", "A", "D" }, await DomOrderAsync("vertical"));

        // Nothing is left displaced: every item sits in normal flow again.
        foreach (var id in new[] { "A", "B", "C", "D" }.Select(i => $"#vertical .item[data-item='{i}']"))
            await WaitForStateAsync(id, "s => Math.abs(s.y) < 0.5");
    }

    [TestMethod]
    public async Task ANewOrderFromOutside_SlidesTheItemsIntoPlace()
    {
        // The bound list mutated in place, D moved to the front: every other item steps down one place (40px items,
        // 4px apart) - and travels there, as for a drag, rather than jumping.
        await Page.Locator("#vertical-rotate").ScrollIntoViewIfNeededAsync();
        const string a = "#vertical .item[data-item='A']";
        var before = await ProbeAsync(a);

        await ClickAsync("vertical-rotate");
        var samples = await SampleAsync(a, 400);

        await ExpectReadoutAsync("vertical-order", "D,A,B,C");
        CollectionAssert.AreEqual(new[] { "D", "A", "B", "C" }, await DomOrderAsync("vertical"));
        var after = await WaitForRestAsync(a);
        Assert.AreEqual(44, after.Top - before.Top, 1, "A didn't end up one place down.");
        Assert.AreEqual(0, after.Y, 0.5, "The layout animation's transform was left on the item.");
        Assert.IsTrue(samples.Any(s => s.Top > before.Top + 8 && s.Top < after.Top - 8),
            $"No intermediate position: {string.Join(", ", samples.Select(s => s.Top.ToString("0", CultureInfo.InvariantCulture)))}");
    }

    [TestMethod]
    public async Task TheKeyboard_PicksAnItemUp_MovesIt_AndDropsIt_KeepingFocusOnIt()
    {
        var a = Page.Locator("#vertical .item[data-item='A']");
        await a.FocusAsync();
        Assert.AreEqual("0", await a.GetAttributeAsync("tabindex"), "a plain row is made focusable");

        await Page.Keyboard.PressAsync("Space");
        await Page.WaitForFunctionAsync("() => document.querySelector('[data-bm-announcer]')?.textContent.startsWith('Picked up')");
        // Held: the others make way at each press, nothing is committed yet.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForStateAsync("#vertical .item[data-item='B']", "s => s.y < -30");
        Assert.AreEqual("A,B,C,D", await ReadoutAsync("vertical-order"));

        await Page.Keyboard.PressAsync("Space");
        await ExpectEventAsync("vertical:reordered");
        await ExpectReadoutAsync("vertical-order", "B,C,A,D");
        CollectionAssert.AreEqual(new[] { "B", "C", "A", "D" }, await DomOrderAsync("vertical"));
        await Page.WaitForFunctionAsync("() => document.activeElement?.dataset.item === 'A'");
        await Page.WaitForFunctionAsync("() => document.querySelector('[data-bm-announcer]')?.textContent === 'Dropped at position 3 of 4.'");

        // Escape puts it back: pick it up again, move it, cancel.
        await Page.Keyboard.PressAsync("Space");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForFunctionAsync("() => document.querySelector('[data-bm-announcer]')?.textContent.startsWith('Cancelled')");
        foreach (var id in new[] { "A", "B", "C", "D" }.Select(i => $"#vertical .item[data-item='{i}']"))
            await WaitForStateAsync(id, "s => Math.abs(s.y) < 0.5");
        Assert.AreEqual("B,C,A,D", await ReadoutAsync("vertical-order"));
    }

    [TestMethod]
    public async Task AGrid_TakesTheCellNearestTheDraggedItem_AndTheKeyboardMovesARowAtATime()
    {
        // 3 columns of 60x40 cells, 4px apart: "1" dragged one cell right and one down lands where "5" was.
        await DragByAsync("#grid .cell[data-item='1']", 64, 44, steps: 16);
        await ExpectEventAsync("grid:reordered");
        await ExpectReadoutAsync("grid-order", "2,3,4,5,1,6");
        CollectionAssert.AreEqual(new[] { "2", "3", "4", "5", "1", "6" },
            await Page.Locator("#grid .cell").EvaluateAllAsync<string[]>("els => els.map(e => e.textContent.trim())"));
        foreach (var i in new[] { "1", "2", "3", "4", "5", "6" })
            await WaitForStateAsync($"#grid .cell[data-item='{i}']", "s => Math.abs(s.x) < 0.5 && Math.abs(s.y) < 0.5");

        // "6", the last cell, moved up a row from the keyboard: from index 5 to index 2, the end of the first row.
        await Page.Locator("#grid .cell[data-item='6']").FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("Space");
        await ExpectEventAsync("grid:reordered", 2);
        await ExpectReadoutAsync("grid-order", "2,3,6,4,5,1");
    }

    [TestMethod]
    public async Task HeldAtTheEdgeOfItsScroller_TheListScrolls_AndTheItemTravelsWithIt()
    {
        // Ten rows in a 150px scroller: R0 is carried down to the bottom edge and held there.
        var scroller = await BoxAsync("#scrolling");
        var (x, y) = await CentreAsync("#scrolling .item[data-item='R0']");
        // How far the list scrolls: the row carried past the end makes room for itself (a transform counts towards
        // the overflow), so the end is measured now.
        var end = await Page.Locator("#scrolling").EvaluateAsync<double>("s => s.scrollHeight - s.clientHeight");
        var edge = scroller.Y + scroller.Height - 6;
        await Page.Mouse.MoveAsync((float)x, (float)y);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= 10; i++)
            await Page.Mouse.MoveAsync((float)x, (float)(y + ((edge - y) * i / 10)));

        // From now on the pointer stands still. At each scroll event - the start of the frame after a scroll step,
        // before anything moves again - how far the row's centre is from it: what that step's frame showed. (A frame
        // first, so the last move is drawn: WebKit hands moves over at once, not once a frame.)
        await Page.EvaluateAsync("() => new Promise(r => requestAnimationFrame(() => r()))");
        await Page.EvaluateAsync("""
            () => {
                const s = document.getElementById('scrolling'), row = s.querySelector(".item[data-item='R0']");
                window.__rowCentres = [];
                s.addEventListener('scroll', () => {
                    const r = row.getBoundingClientRect();
                    window.__rowCentres.push(r.top + (r.height / 2));
                });
            }
            """);

        // It scrolls to the end with the pointer held still - and no further - and R0 stays under the pointer all the way.
        await Page.WaitForFunctionAsync($"() => document.getElementById('scrolling').scrollTop >= {end.ToString(CultureInfo.InvariantCulture)} - 1");
        await Page.WaitForTimeoutAsync(200);
        // As a frame shows it: read in an animation frame, after that frame's scroll events. (WebKit's own autoscroll
        // under a held mouse moves the list between frames; the drag holds it at the end before anything paints.)
        var shown = await Page.EvaluateAsync<double>("() => new Promise(r => requestAnimationFrame(() => r(document.getElementById('scrolling').scrollTop)))");
        Assert.AreEqual(end, shown, 1, "it stops at the end of the list");
        var trail = (await Page.EvaluateAsync<double[]>("() => window.__rowCentres")).Select(c => c - edge).ToArray();
        Assert.IsTrue(trail.Length >= 5, $"it scrolled in steps, one a frame ({trail.Length})");
        Assert.IsTrue(trail.All(d => Math.Abs(d) < 2), $"the held row trailed the pointer: {string.Join(", ", trail.Select(d => d.ToString("0.#")))}");

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("scrolling:reordered");
        await ExpectReadoutAsync("scrolling-order", "R1,R2,R3,R4,R5,R6,R7,R8,R9,R0");
    }

    [TestMethod]
    public async Task SiblingsMakeRoomWhileTheItemIsStillHeld()
    {
        await DragByAsync("#vertical .item[data-item='A']", 0, 60, steps: 15, release: false);

        // B moves up into A's slot before anything is committed.
        await WaitForStateAsync("#vertical .item[data-item='B']", "s => s.y < -30");
        await ExpectEventAsync("vertical:reordered", 0);

        await Page.Mouse.UpAsync();
        await ExpectEventAsync("vertical:reordered");
        await ExpectReadoutAsync("vertical-order", "B,A,C,D");
    }

    [TestMethod]
    public async Task ADragThatEndsWhereItStarted_ChangesNothing()
    {
        await DragByAsync("#vertical .item[data-item='B']", 0, 10, steps: 5);
        await Page.WaitForTimeoutAsync(400);

        await ExpectReadoutAsync("vertical-order", "A,B,C,D");
        CollectionAssert.AreEqual(new[] { "A", "B", "C", "D" }, await DomOrderAsync("vertical"));
    }

    [TestMethod]
    public async Task Horizontal_ReordersAlongX()
    {
        // 60px wide with 4px gaps: 1 dragged 140px right lands last.
        await DragByAsync("#horizontal .item[data-item='1']", 140, 0, steps: 20);

        await ExpectEventAsync("horizontal:reordered");
        await ExpectReadoutAsync("horizontal-order", "2,3,1");
    }

    [TestMethod]
    public async Task HandleSelector_OnlyTheGripDrags()
    {
        // Pressing the row's text side does nothing.
        var row = await BoxAsync("#handled .item[data-item='X']");
        await Page.Mouse.MoveAsync(row.X + 150, row.Y + 20);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(row.X + 150, row.Y + 120, new() { Steps = 10 });
        await Page.Mouse.UpAsync();
        await Page.WaitForTimeoutAsync(300);
        await ExpectReadoutAsync("handled-order", "X,Y,Z");

        await DragByAsync("#handled .item[data-item='X'] .grip", 0, 100, steps: 20);
        await ExpectEventAsync("handled:reordered");
        await ExpectReadoutAsync("handled-order", "Y,Z,X");
    }
}
