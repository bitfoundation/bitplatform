using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiDataTable at /e2e/aidatatable in a real browser: one row per object and one column per key, the union of
/// the keys in the order they first appear (and growing as rows are added), nested values as their JSON text, null as an
/// empty cell, anything but an array of objects, a wide table scrolling inside its own box, the theme tokens, RTL, the
/// public variables on an instance and an ancestor, and Styles / Classes of the parts.
/// </summary>
[TestClass]
public class BitAiDataTableE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aidatatable";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator HeaderCells(string id) => Root(id).Locator(":scope > table.bit-aidtb-tbl > thead > tr.bit-aidtb-hrw > th.bit-aidtb-hcl");

    private ILocator Rows(string id) => Root(id).Locator(":scope > table.bit-aidtb-tbl > tbody > tr.bit-aidtb-row");

    [TestMethod]
    public async Task Basic_EachObject_IsARowAndEachKeyAColumnInTheOrderItFirstAppears()
    {
        await OpenAsync(Route);

        await Expect(Root("dtb-basic")).ToHaveClassAsync(HasClass("bit-aidtb"));

        // The keys of both rows, humanized, the second row's own one last.
        await Expect(HeaderCells("dtb-basic")).ToHaveTextAsync(["First Name", "Age", "Remote", "Address", "Note", "Tags"]);
        await Expect(HeaderCells("dtb-basic").First).ToHaveAttributeAsync("scope", "col");
        await Expect(Rows("dtb-basic")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Cells_NestedValuesAreTheirJsonAndAMissingOrNullOneIsEmpty()
    {
        await OpenAsync(Route);

        await Expect(Rows("dtb-basic").Nth(0).Locator("td.bit-aidtb-cel")).ToHaveTextAsync(["Ada", "36", "true", "{\"city\":\"London\"}", "", ""]);
        await Expect(Rows("dtb-basic").Nth(1).Locator("td.bit-aidtb-cel")).ToHaveTextAsync(["Grace", "45", "false", "", "", "[\"navy\",\"cobol\"]"]);
    }

    [TestMethod]
    public async Task Shapes_NotAnArray_RendersAnEmptyTable()
    {
        await OpenAsync(Route);

        await Expect(Root("dtb-object").Locator("table.bit-aidtb-tbl")).ToBeAttachedAsync();
        await Expect(HeaderCells("dtb-object")).ToHaveCountAsync(0);
        await Expect(Rows("dtb-object")).ToHaveCountAsync(0);

        await Expect(HeaderCells("dtb-empty")).ToHaveCountAsync(0);
        await Expect(Rows("dtb-empty")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Shapes_AnItemThatIsNotAnObject_IsAnEmptyRow()
    {
        await OpenAsync(Route);

        await Expect(HeaderCells("dtb-scalar-item")).ToHaveTextAsync(["Name"]);
        await Expect(Rows("dtb-scalar-item")).ToHaveCountAsync(3);
        await Expect(Rows("dtb-scalar-item").Locator("td")).ToHaveTextAsync(["Ada", "", "Grace"]);
    }

    [TestMethod]
    public async Task Grow_RowsAddedLater_BringTheirColumnsWithThem()
    {
        await OpenAsync(Route);

        await Expect(HeaderCells("dtb-grow")).ToHaveTextAsync(["Name"]);

        await Page.Locator("#btn-dtb-add").ClickAsync();
        await Expect(Page.Locator("#dtb-grow-count")).ToHaveTextAsync("2");
        await Expect(HeaderCells("dtb-grow")).ToHaveTextAsync(["Name", "Role"]);
        await Expect(Rows("dtb-grow").Nth(0).Locator("td")).ToHaveTextAsync(["Ada", ""]);

        await Page.Locator("#btn-dtb-add").ClickAsync();
        await Expect(HeaderCells("dtb-grow")).ToHaveTextAsync(["Name", "Role", "Country"]);
        await Expect(Rows("dtb-grow")).ToHaveCountAsync(3);
        await Expect(Rows("dtb-grow").Nth(2).Locator("td")).ToHaveTextAsync(["Linus", "Kernel", "Finland"]);
    }

    [TestMethod]
    public async Task Scroll_AWideTable_ScrollsInsideItsBoxRatherThanWideningThePage()
    {
        await OpenAsync(Route);

        var root = Root("dtb-wide");
        var box = (await Page.Locator("#dtb-scroll-box").BoundingBoxAsync())!;
        var rootBox = (await root.BoundingBoxAsync())!;
        Assert.AreEqual(box.Width, rootBox.Width, 1, "The table keeps to the width of its box.");

        await Expect(root).ToHaveCSSAsync("overflow-x", "auto");
        var widths = await root.EvaluateAsync<double[]>("e => [e.scrollWidth, e.clientWidth]");
        Assert.IsTrue(widths[0] > widths[1], $"The table scrolls sideways inside it: {widths[0]} > {widths[1]}.");

        // The header cells keep to one line, so the columns are as wide as their names.
        await Expect(HeaderCells("dtb-wide").First).ToHaveCSSAsync("white-space", "nowrap");

        await root.EvaluateAsync("e => e.scrollLeft = e.scrollWidth");
        Assert.IsTrue(await root.EvaluateAsync<double>("e => e.scrollLeft") > 0);
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollX"), "The page itself did not scroll.");
    }

    [TestMethod]
    public async Task Theme_TheTable_ReadsTheBitThemeTokens()
    {
        await OpenAsync(Route);

        var root = Root("dtb-basic");
        await Expect(root).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", await root.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
        await Expect(root).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");

        var table = root.Locator(":scope > table");
        await Expect(table).ToHaveCSSAsync("border-collapse", "collapse");
        await Expect(table).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(table).ToHaveCSSAsync("font-size", await root.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));

        var header = HeaderCells("dtb-basic").First;
        await Expect(header).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(header).ToHaveCSSAsync("text-align", "start");

        // A rule under every row but the last, which the frame closes.
        var divider = await Page.ResolveColorAsync("var(--bit-clr-brd-sec)");
        await Expect(Rows("dtb-basic").Nth(0).Locator("td").First).ToHaveCSSAsync("border-bottom-color", divider);
        await Expect(Rows("dtb-basic").Nth(0).Locator("td").First).Not.ToHaveCSSAsync("border-bottom-style", "none");
        await Expect(Rows("dtb-basic").Nth(1).Locator("td").First).ToHaveCSSAsync("border-bottom-style", "none");
    }

    [TestMethod]
    [DataRow("dtb-rtl", true)]
    [DataRow("dtb-rtl-inherited", false)]
    public async Task Rtl_TheColumns_ReadRightToLeft(string id, bool explicitDir)
    {
        await OpenAsync(Route);

        if (explicitDir) await Expect(Root(id)).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root(id).Locator(":scope > table")).ToHaveCSSAsync("direction", "rtl");

        // The first column is the rightmost one, and each cell starts at its right edge.
        var first = await HeaderCells(id).Nth(0).RectAsync();
        var second = await HeaderCells(id).Nth(1).RectAsync();
        Assert.IsTrue(first.Left >= second.Right - 1, "First Name is to the right of Age.");

        var text = await HeaderCells(id).Nth(0).EvaluateAsync<double[]>(
            "e => { const r = document.createRange(); r.selectNodeContents(e); const b = r.getBoundingClientRect(); return [b.left, b.right]; }");
        var padding = AiE2EHelpers.ParsePx(await HeaderCells(id).Nth(0).EvaluateAsync<string>("e => getComputedStyle(e).paddingRight"));
        Assert.AreEqual(first.Right - padding, text[1], 1.5, "The header text starts at the right edge of its cell.");
    }

    [TestMethod]
    public async Task Variables_OnTheInstance_RestyleTheTable()
    {
        await OpenAsync(Route);

        var root = Root("dtb-vars");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(7, 8, 9)");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "9px");

        var table = root.Locator(":scope > table");
        await Expect(table).ToHaveCSSAsync("font-size", "17px");
        await Expect(table).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        var header = HeaderCells("dtb-vars").First;
        await Expect(header).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(header).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(header).ToHaveCSSAsync("font-weight", "300");
        await Expect(header).ToHaveCSSAsync("padding", "3px 5px");
        await Expect(header).ToHaveCSSAsync("border-bottom-color", "rgb(10, 11, 12)");

        var cell = Rows("dtb-vars").Nth(0).Locator("td").First;
        await Expect(cell).ToHaveCSSAsync("padding", "3px 5px");
        await Expect(cell).ToHaveCSSAsync("border-bottom-color", "rgb(10, 11, 12)");
        await Expect(cell).ToHaveCSSAsync("color", "rgb(1, 2, 3)");

        // A value set on an ancestor reaches the tables inside it.
        await Expect(HeaderCells("dtb-vars-ancestor").First).ToHaveCSSAsync("background-color", "rgb(20, 21, 22)");
    }

    [TestMethod]
    public async Task Styles_ReachTheirParts()
    {
        await OpenAsync(Route);

        await Expect(HeaderCells("dtb-styled").First).ToHaveCSSAsync("color", "rgb(30, 31, 32)");
        await Expect(Rows("dtb-styled").First.Locator("td").First).ToHaveCSSAsync("color", "rgb(33, 34, 35)");
        await Expect(Rows("dtb-styled").First).ToHaveClassAsync(HasClass("dtb-row-class"));
        await Expect(Root("dtb-styled").Locator("thead > tr")).ToHaveClassAsync(HasClass("dtb-header-class"));
    }
}
