namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid on a phone: a 390px touch viewport, where the grid stays inside the page and scrolls its own columns
/// sideways, a tap on a header sorts, a tap on a row selects it and makes its cell the focused one, and a tap on a
/// detail toggle and on a group toggle open them.
/// </summary>
[TestClass]
public class BitDataGridMobileE2ETests : BitDataGridE2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
    };

    [TestMethod]
    public async Task Phone_AWideGrid_StaysInThePageAndScrollsItsColumnsSideways()
    {
        await OpenAsync("/e2e/data-grid");

        var page = await Page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, document.documentElement.scrollWidth]");
        var root = await BoxAsync(Grid("grid-basic"));
        Assert.IsTrue(root.X + root.Width <= page[0] + 1, "The grid overflows the phone's viewport.");

        var viewport = Viewport("grid-basic");
        var widths = await viewport.EvaluateAsync<double[]>("el => [el.scrollWidth, el.clientWidth]");
        Assert.IsTrue(widths[0] > widths[1], "The grid's columns do not overflow its own viewport.");

        await viewport.EvaluateAsync("el => el.scrollLeft = 200");
        await NextFramesAsync();
        Assert.AreEqual(200, await viewport.EvaluateAsync<double>("el => el.scrollLeft"), 1);
    }

    [TestMethod]
    public async Task Phone_TapOnAHeader_SortsTheColumn()
    {
        await OpenAsync("/e2e/data-grid");

        await HeaderButton("grid-basic", "Name").TapAsync();

        await Expect(HeaderCell("grid-basic", 2)).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(Cell("grid-basic", 0, 2)).ToHaveTextAsync("Camera");
    }

    [TestMethod]
    public async Task Phone_TapOnARow_SelectsIt()
    {
        await OpenAsync("/e2e/data-grid/selection");

        await Cell("grid-single", 3, 2).TapAsync();

        await Expect(DataRow("grid-single", 3)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Text("single-selected")).ToHaveTextAsync("Lamp");
        await Expect(Cell("grid-single", 3, 2)).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task Phone_TapOnTheToggles_OpensADetailAndAGroup()
    {
        await OpenAsync("/e2e/data-grid/detail");

        await Cell("grid-detail", 0, 1).Locator("button").TapAsync();
        await Expect(Cell("grid-detail", 0, 1).Locator("button")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Text("toggle-log")).ToHaveTextAsync("Acme:True");

        await OpenAsync("/e2e/data-grid/grouping");
        await HeaderRow("grid-group").GetByRole(AriaRole.Button, new() { Name = "Group by Category", Exact = true }).TapAsync();
        var toggle = Page.Locator("#grid-group .bit-dtg-group-row .bit-dtg-group-cell > button").First;
        await toggle.TapAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
