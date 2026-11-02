using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiStructuredDataView at /e2e/aistructureddataview in a real browser: an object as a list of labelled values
/// (humanized labels, a dash for a missing value, a tree for a nested one), an array of objects as a table, anything else
/// as a JSON tree, the structured script of the scripted transport filling the table row by row while it streams, a part
/// nothing has reached yet showing the thinking indicator, a schema the host registered drawn by its renderer (a
/// BitAiForm), the theme tokens, RTL and the public variables.
/// </summary>
[TestClass]
public class BitAiStructuredDataViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aistructureddataview";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator List(string id) => Root(id).Locator(":scope > dl.bit-aisdv-lst");

    [TestMethod]
    public async Task Object_IsListedAsLabelledValues()
    {
        await OpenAsync(Route);

        await Expect(Root("sdv-object")).ToHaveClassAsync(HasClass("bit-aisdv"));

        var list = List("sdv-object");
        await Expect(list.Locator(":scope > .bit-aisdv-ent > dt.bit-aisdv-trm")).ToHaveTextAsync(
            ["Flight Number", "From", "Departure time", "Seats", "Refundable", "Meal", "Gate", "Stops"]);

        var values = list.Locator(":scope > .bit-aisdv-ent > dd.bit-aisdv-val");
        await Expect(values.Nth(0)).ToHaveTextAsync("BA 117");
        await Expect(values.Nth(2)).ToHaveTextAsync("09:40");
        await Expect(values.Nth(3)).ToHaveTextAsync("2");

        // A boolean is the JSON it was written as, never .NET's "False".
        await Expect(values.Nth(4)).ToHaveTextAsync("false");
    }

    [TestMethod]
    public async Task Object_NestedValuesAreTreesAndAMissingOneIsADash()
    {
        await OpenAsync(Route);

        var values = List("sdv-object").Locator(":scope > .bit-aisdv-ent > dd.bit-aisdv-val");

        var dash = values.Nth(5).Locator(".bit-aisdv-emp");
        await Expect(dash).ToHaveTextAsync("-");
        await Expect(dash).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-ter)"));

        // A nested object is a tree opened one level deep: its own entries show, and it toggles.
        var gate = values.Nth(6).Locator(":scope > .bit-aijsv");
        await Expect(gate).ToBeVisibleAsync();
        var toggle = gate.Locator(".bit-aijsv-tgl").First;
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(gate.Locator(".bit-aijsv-key")).ToHaveTextAsync(["\"terminal\"", "\"door\""]);
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(toggle).ToContainTextAsync("{ 2 }");

        await Expect(values.Nth(7).Locator(":scope > .bit-aijsv .bit-aijsv-str")).ToHaveTextAsync("\"KEF\"");
    }

    [TestMethod]
    public async Task Shapes_AnArrayOfObjects_IsATable()
    {
        await OpenAsync(Route);

        var table = Root("sdv-rows").Locator(":scope > .bit-aidtb");
        await Expect(table.Locator("th")).ToHaveTextAsync(["Order", "Status", "Total"]);
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(2);
        await Expect(table.Locator("tbody tr").Nth(0).Locator("td")).ToHaveTextAsync(["ord_4821", "shipped", "129.5"]);
        await Expect(List("sdv-rows")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow("sdv-scalars")]
    [DataRow("sdv-mixed")]
    [DataRow("sdv-string")]
    public async Task Shapes_AnythingElse_IsAJsonTree(string id)
    {
        await OpenAsync(Route);

        await Expect(Root(id).Locator(":scope > .bit-aijsv")).ToBeVisibleAsync();
        await Expect(Root(id).Locator(".bit-aidtb")).ToHaveCountAsync(0);
        await Expect(List(id)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Streaming_TheTable_FillsInRowByRow()
    {
        await OpenAsync(Route);

        // Every row count the table goes through, as the deltas land.
        await Page.EvaluateAsync(
            """
            () => {
                const counts = window.__sdvRows = [];
                const read = () => {
                    const n = document.querySelectorAll('#sdv-stream .bit-aidtb tbody tr').length;
                    if (counts[counts.length - 1] !== n) counts.push(n);
                };
                new MutationObserver(read).observe(document.body, { childList: true, subtree: true, characterData: true });
            }
            """);

        await Page.Locator("#btn-sdv-stream").ClickAsync();

        // The rows arrive while the reply is still streaming.
        await Expect(Page.Locator("#sdv-stream-state")).ToHaveTextAsync("generating");
        await Expect(Root("sdv-stream").Locator(".bit-aidtb tbody tr").First).ToBeVisibleAsync();

        await Expect(Page.Locator("#sdv-stream-answers")).ToHaveTextAsync("1", new() { Timeout = 20000 });

        var table = Root("sdv-stream").Locator(":scope > .bit-aidtb");
        await Expect(table.Locator("th")).ToHaveTextAsync(["Option", "Effort", "Risk"]);
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(4);
        await Expect(table.Locator("tbody tr").Nth(3).Locator("td")).ToHaveTextAsync(["Rewrite", "High", "High"]);
        await Expect(Page.Locator("#sdv-stream-text")).ToHaveTextAsync("Four options, ranked by effort.");

        var counts = (await Page.EvaluateAsync<JsonElement>("() => window.__sdvRows")).EnumerateArray().Select(e => e.GetInt32()).Where(n => n > 0).ToArray();
        var seen = string.Join(",", counts);
        Assert.AreEqual(4, counts.Last(), seen);
        Assert.IsTrue(counts.Length >= 3, $"The table grew a row at a time, not all at once: {seen}.");
        CollectionAssert.AreEqual(counts.OrderBy(n => n).ToArray(), counts, $"Rows were only ever added: {seen}.");
    }

    [TestMethod]
    public async Task Streaming_APartNothingHasReached_ShowsTheThinkingIndicatorUntilItsFirstRow()
    {
        await OpenAsync(Route);

        await Expect(Root("sdv-partial").Locator(":scope > .bit-aithi")).ToBeVisibleAsync();

        var append = Page.Locator("#btn-sdv-append");
        await append.ClickAsync();
        await Expect(Page.Locator("#sdv-partial-step")).ToHaveTextAsync("1");

        // A half-written object is a row already, with the fields that have arrived.
        var table = Root("sdv-partial").Locator(":scope > .bit-aidtb");
        await Expect(Root("sdv-partial").Locator(".bit-aithi")).ToHaveCountAsync(0);
        await Expect(table.Locator("th")).ToHaveTextAsync(["Name"]);
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(1);

        await append.ClickAsync();
        await Expect(table.Locator("th")).ToHaveTextAsync(["Name", "Role"]);
        await Expect(table.Locator("tbody tr").Nth(0).Locator("td")).ToHaveTextAsync(["Ada", "Engineer"]);

        await append.ClickAsync();
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(2);
        await Expect(table.Locator("tbody tr").Nth(1).Locator("td")).ToHaveTextAsync(["Grace", ""]);

        await append.ClickAsync();
        await Expect(Page.Locator("#sdv-partial-step")).ToHaveTextAsync("4");
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Schema_ARendererRegisteredForTheSchema_DrawsThePart()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-sdv-form").ClickAsync();
        await Expect(Page.Locator("#sdv-form-answers")).ToHaveTextAsync("1");

        // The host renders the "form" schema as a BitAiForm, in place of the default table, list or tree.
        var form = Root("sdv-form").Locator(".bit-aifrm");
        await Expect(form).ToBeVisibleAsync();
        await Expect(form).ToContainTextAsync("Restaurant");
        await Expect(form).ToContainTextAsync("Guests");
        await Expect(Root("sdv-form").Locator(":scope > .bit-aisdv-lst")).ToHaveCountAsync(0);
        await Expect(Root("sdv-form").Locator(":scope > .bit-aijsv")).ToHaveCountAsync(0);
        await Expect(Root("sdv-form").Locator(".bit-airbd")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Theme_TheList_ReadsTheBitThemeTokens()
    {
        await OpenAsync(Route);

        var list = List("sdv-object");
        await Expect(list).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(list).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(list).ToHaveCSSAsync("border-top-left-radius", await list.ResolveLengthInAsync("var(--bit-shp-radius-surface)"));
        await Expect(list).ToHaveCSSAsync("display", "flex");
        await Expect(list).ToHaveCSSAsync("margin-top", "0px");

        var label = list.Locator("dt").First;
        await Expect(label).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(label).ToHaveCSSAsync("font-size", await label.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));

        // Each label sits above its value.
        var labelBox = await label.RectAsync();
        var valueBox = await list.Locator("dd").First.RectAsync();
        Assert.IsTrue(valueBox.Top >= labelBox.Bottom - 1, "The value is under its label.");
        await Expect(list.Locator("dd").First).ToHaveCSSAsync("margin-left", "0px");
    }

    [TestMethod]
    public async Task Rtl_TheList_ReadsRightToLeft()
    {
        await OpenAsync(Route);

        await Expect(Root("sdv-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var list = List("sdv-rtl");
        await Expect(list).ToHaveCSSAsync("direction", "rtl");

        // The labels and the values start on the right edge of the list.
        var listBox = await list.RectAsync();
        var label = await list.Locator("dt").First.EvaluateAsync<double[]>(
            "e => { const r = document.createRange(); r.selectNodeContents(e); const b = r.getBoundingClientRect(); return [b.left, b.right]; }");
        var padding = AiE2EHelpers.ParsePx(await list.EvaluateAsync<string>("e => getComputedStyle(e).paddingRight"));
        Assert.AreEqual(listBox.Right - padding - 1, label[1], 2, "The label hugs the right edge.");
    }

    [TestMethod]
    public async Task Variables_OnTheInstance_RestyleTheList()
    {
        await OpenAsync(Route);

        var list = List("sdv-vars");
        await Expect(list).ToHaveCSSAsync("row-gap", "13px");
        await Expect(list).ToHaveCSSAsync("padding-top", "11px");
        await Expect(list).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(list).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(list).ToHaveCSSAsync("border-top-left-radius", "7px");

        var label = list.Locator("dt").First;
        await Expect(label).ToHaveCSSAsync("color", "rgb(8, 9, 10)");
        await Expect(label).ToHaveCSSAsync("font-size", "19px");
        await Expect(label).ToHaveCSSAsync("font-weight", "700");
        await Expect(list.Locator("dd").First).ToHaveCSSAsync("color", "rgb(11, 12, 13)");
        await Expect(list.Locator(".bit-aisdv-emp")).ToHaveCSSAsync("color", "rgb(14, 15, 16)");
    }
}
