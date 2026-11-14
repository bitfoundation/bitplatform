namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's failed reads on /e2e/data-grid/read-errors, handled through OnReadError: the error row taking the
/// place of the rows, its Retry button keeping the keyboard focus while it reads again and when that read fails too,
/// the focus moving to the first cell once the rows are back - also in a server-virtualized grid, where Virtualize's
/// own rows replace the ones read ahead of it - and a window that fails as the virtualized grid scrolls to it.
/// </summary>
[TestClass]
public class BitDataGridReadErrorE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/read-errors";

    private ILocator ErrorCell(string gridId) => MessageRow(gridId).Locator("> .bit-dtg-read-error");

    private ILocator RetryButton(string gridId) => ErrorCell(gridId).Locator("button.bit-dtg-retry");

    // Waits for the error row to tell the read failed, rather than that it is reading again.
    private async Task ExpectErrorRowAsync(string gridId)
    {
        await Expect(ErrorCell(gridId).Locator("> span")).ToHaveTextAsync("The records could not be loaded.");
        await Expect(RetryButton(gridId)).ToHaveTextAsync("Retry");
    }

    // Turns the reads of the paged grid to failing and pages to the second page, whose read fails.
    private async Task FailTheSecondPageAsync()
    {
        const string id = "grid-paged";
        await Expect(Cell(id, 0, 1)).ToHaveTextAsync("1");

        await Page.Locator("#btn-fail").ClickAsync();
        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();

        await ExpectErrorRowAsync(id);
        await Expect(Text("errors")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task AFailedPage_ShowsTheErrorRowInPlaceOfTheRows_AndKeepsThePager()
    {
        await OpenAsync(Url);
        await FailTheSecondPageAsync();

        const string id = "grid-paged";
        await Expect(DataRows(id)).ToHaveCountAsync(0);
        await Expect(Text("last-error")).ToHaveTextAsync("The read of rows 6-10 failed.");
        await Expect(LiveRegion(id)).ToContainTextAsync("The records could not be loaded.");
        await Expect(Table(id)).Not.ToHaveAttributeAsync("aria-busy", "true");

        // The row is only as wide as this grid's two columns, too narrow for the message and its button side by side: the
        // button wraps under the message, both in the middle of the row, rather than being clipped off its end.
        var cell = await BoxAsync(ErrorCell(id));
        var message = await BoxAsync(ErrorCell(id).Locator("> span"));
        var button = await BoxAsync(RetryButton(id));
        Assert.IsTrue(button.Y >= message.Y + message.Height, "The Retry button is not under the message.");
        Assert.IsTrue(button.X >= cell.X && button.X + button.Width <= cell.X + cell.Width, "The Retry button is not wholly inside the row.");
        Assert.IsTrue(Math.Abs(button.X + button.Width / 2 - (cell.X + cell.Width / 2)) < 2, "The Retry button is not in the middle of the row.");
        Assert.IsTrue(Math.Abs(message.X + message.Width / 2 - (cell.X + cell.Width / 2)) < 2, "The message is not in the middle of the row.");

        // The total read before stays, so the pager still pages away from the failed page.
        await Expect(Pagers(id).Locator(".bit-dtg-pager-info")).ToContainTextAsync("of 23");
        await Page.Locator("#btn-fail").ClickAsync();
        await Pagers(id).Locator("button[aria-label='Next page']").ClickAsync();
        await Expect(Cell(id, 0, 1)).ToHaveTextAsync("11");
        await Expect(MessageRow(id)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Retry_KeepsTheFocusOnItsButtonWhileItReads_AndWhenTheReadFailsAgain()
    {
        await OpenAsync(Url);
        await FailTheSecondPageAsync();

        const string id = "grid-paged";
        await Page.Locator("#btn-hold").ClickAsync();
        await RetryButton(id).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        // While the rows are read again the button stays, marked busy, and the focus with it.
        await Expect(Text("held")).ToHaveTextAsync("yes");
        await Expect(ErrorCell(id)).ToContainTextAsync("Loading…");
        await Expect(RetryButton(id)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(RetryButton(id)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Table(id)).ToHaveAttributeAsync("aria-busy", "true");
        await ExpectFocusedAsync(RetryButton(id));

        // A press while it reads asks for nothing more.
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("reads")).ToHaveTextAsync("3");

        await Page.Locator("#btn-release").ClickAsync();
        await Expect(Text("errors")).ToHaveTextAsync("2");
        await ExpectErrorRowAsync(id);
        await Expect(RetryButton(id)).Not.ToHaveAttributeAsync("aria-busy", "true");

        // The release button took the focus to press it; the Retry button, pressed again, keeps it through the failure.
        await RetryButton(id).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("errors")).ToHaveTextAsync("3");
        await ExpectFocusedAsync(RetryButton(id));
    }

    [TestMethod]
    public async Task Retry_MovesTheFocusToTheFirstCell_WhenTheRowsComeBack()
    {
        await OpenAsync(Url);
        await FailTheSecondPageAsync();

        const string id = "grid-paged";
        await Page.Locator("#btn-fail").ClickAsync();
        await RetryButton(id).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Cell(id, 0, 1)).ToHaveTextAsync("6");
        await Expect(MessageRow(id)).ToHaveCountAsync(0);
        await ExpectFocusedAsync(Cell(id, 0, 1));
    }

    [TestMethod]
    public async Task Virtualize_AWindowThatFailsAsTheGridScrolls_ShowsTheErrorRow_AndRetryBringsTheRowsBackWithTheFocus()
    {
        await OpenAsync(Url);

        const string id = "grid-virtual";
        await Expect(Cell(id, 0, 1)).ToHaveTextAsync("1");

        await Page.Locator("#btn-fail-virtual").ClickAsync();
        await Viewport(id).EvaluateAsync("vp => vp.scrollTop = vp.scrollHeight");

        await ExpectErrorRowAsync(id);
        await Expect(Text("virtual-errors")).ToHaveTextAsync("1");
        await Expect(DataRows(id)).ToHaveCountAsync(0);

        // Retry reads the first window again, the way the grid's first render does, and Virtualize's rows take over from
        // it: the focus the Retry button held goes to their first cell, and stays there through that handover.
        await RetryButton(id).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Cell(id, 0, 1)).ToHaveTextAsync("1");
        await Expect(MessageRow(id)).ToHaveCountAsync(0);
        await ExpectFocusedAsync(Cell(id, 0, 1));
        await Page.WaitForTimeoutAsync(500);
        await ExpectFocusedAsync(Cell(id, 0, 1));
        await Expect(Text("virtual-errors")).ToHaveTextAsync("1");
    }
}
