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
        foreach (var list in new[] { "#vertical .item", "#horizontal .item", "#handled .item" })
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
