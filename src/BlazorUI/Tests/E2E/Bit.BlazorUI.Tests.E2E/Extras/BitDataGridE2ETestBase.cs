namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What every BitDataGrid E2E class reads a grid by: its rows, its cells by column, its header buttons, and the few
/// waits a grid needs (a cell taking the DOM focus, a column reading back in order). The grids of a page are reached
/// by the Id each carries, and only their own parts are matched - never those of a grid nested in a detail row.
/// </summary>
public abstract class BitDataGridE2ETestBase : E2ETestBase
{
    /// <summary>The root of the grid with the given id.</summary>
    protected ILocator Grid(string gridId) => Page.Locator($"#{gridId}");

    /// <summary>The element carrying the grid role (role grid or treegrid).</summary>
    protected ILocator Table(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table");

    /// <summary>The scrolling viewport of the grid.</summary>
    protected ILocator Viewport(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport");

    /// <summary>The header row of column titles.</summary>
    protected ILocator HeaderRow(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-header > .bit-dtg-header-row");

    /// <summary>The header cells of the column titles, special columns included.</summary>
    protected ILocator HeaderCells(string gridId) => HeaderRow(gridId).Locator("> [role=columnheader]");

    /// <summary>The header cell of the column whose aria-colindex is given.</summary>
    protected ILocator HeaderCell(string gridId, int ariaColIndex) => HeaderRow(gridId).Locator($"> [aria-colindex='{ariaColIndex}']");

    /// <summary>The sort button of a column header, found by the title it carries.</summary>
    protected ILocator HeaderButton(string gridId, string title) => HeaderRow(gridId).Locator($"button.bit-dtg-htext[title='{title}']");

    /// <summary>The filter row.</summary>
    protected ILocator FilterRow(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-header > .bit-dtg-filter-row");

    /// <summary>The footer row.</summary>
    protected ILocator FooterRow(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-footer > .bit-dtg-footer-row");

    /// <summary>The rows of the body: data rows, group rows, message rows and detail rows alike.</summary>
    protected ILocator BodyRows(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-body > [role=row]");

    /// <summary>The data rows of the body, without the group, message, placeholder and detail rows.</summary>
    protected ILocator DataRows(string gridId)
        => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-body > .bit-dtg-row:not(.bit-dtg-group-row):not(.bit-dtg-message-row):not(.bit-dtg-placeholder-row):not(.bit-dtg-infinite-end)");

    /// <summary>The data row at the given 0-based position of the rendered rows.</summary>
    protected ILocator DataRow(string gridId, int index) => DataRows(gridId).Nth(index);

    /// <summary>The cells of every data row in the column whose aria-colindex is given.</summary>
    protected ILocator Cells(string gridId, int ariaColIndex) => DataRows(gridId).Locator($"> [aria-colindex='{ariaColIndex}']");

    /// <summary>The cell of a data row (0-based) in the column whose aria-colindex is given.</summary>
    protected ILocator Cell(string gridId, int rowIndex, int ariaColIndex) => DataRow(gridId, rowIndex).Locator($"> [aria-colindex='{ariaColIndex}']");

    /// <summary>The message row (empty or loading) of the body.</summary>
    protected ILocator MessageRow(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-viewport > .bit-dtg-table > .bit-dtg-body > .bit-dtg-message-row");

    /// <summary>The pagers of the grid, before and after the viewport.</summary>
    protected ILocator Pagers(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-pager");

    /// <summary>The toolbar of the grid.</summary>
    protected ILocator Toolbar(string gridId) => Page.Locator($"#{gridId} > .bit-dtg-toolbar");

    /// <summary>The visually hidden live region the grid announces its changes in.</summary>
    protected ILocator LiveRegion(string gridId) => Page.Locator($"#{gridId} > [role=status]");

    /// <summary>Waits for the cells of a column to read exactly the given texts, in order.</summary>
    protected Task ExpectColumnAsync(string gridId, int ariaColIndex, params string[] expected)
        => Expect(Cells(gridId, ariaColIndex)).ToHaveTextAsync(expected);

    /// <summary>Waits for the element the selector finds to be the focused element of the page.</summary>
    protected Task ExpectFocusedAsync(ILocator locator) => Expect(locator).ToBeFocusedAsync();

    /// <summary>The text of an element of the page, the way a test reads a value the page writes back.</summary>
    protected ILocator Text(string id) => Page.Locator($"#{id}");

    /// <summary>The bounding box of the first element the locator finds, failing when it has none.</summary>
    protected async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.First.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    /// <summary>Waits until a JavaScript predicate over the page holds - a DOM property no attribute reflects (a
    /// checkbox's indeterminate state), or what a stub of the page has recorded.</summary>
    protected Task WaitForJsAsync(string predicate, object? arg = null)
        => Page.WaitForFunctionAsync(predicate, arg, new() { Timeout = DefaultTimeout, PollingInterval = 50 });

    /// <summary>Polls a condition read from the page - a measured width, a rendered count - frame by frame until it
    /// holds, failing with the message once the default timeout has passed.</summary>
    protected async Task UntilAsync(Func<Task<bool>> condition, string message)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (await condition() is false)
        {
            if (watch.ElapsedMilliseconds > DefaultTimeout) Assert.Fail(message);
            await NextFramesAsync();
        }
    }

    /// <summary>Waits for the column titles of the header row to read exactly the given titles, in order. Read off the
    /// title each header's label carries, since the label also holds the sort arrow, the priority and the group toggle.</summary>
    protected async Task ExpectHeaderTitlesAsync(string gridId, params string[] titles)
    {
        var labels = HeaderRow(gridId).Locator("> [role=columnheader] .bit-dtg-htext");
        string[] actual = [];
        await UntilAsync(async () =>
        {
            actual = await labels.EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('title') ?? '')");
            return actual.SequenceEqual(titles);
        }, $"The column titles are not {string.Join(", ", titles)}.");
    }

    /// <summary>Fills a date or datetime-local input. Playwright's WebKit has no such inputs (they fall back to text
    /// boxes), and Blazor's own change handling throws on one, so there the test ends inconclusive at this point.</summary>
    protected async Task FillDateAsync(ILocator input, string value)
    {
        if (IsWebKit) Assert.Inconclusive("Playwright's WebKit has no date inputs: they fall back to text boxes, whose change event Blazor's own date handling throws on.");
        await input.FillAsync(value);
    }

    /// <summary>A computed style of the first element the locator finds.</summary>
    protected Task<string> StyleOfAsync(ILocator locator, string property)
        => locator.First.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");
}
