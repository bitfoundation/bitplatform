using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Bunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.DataTable;

[TestClass]
public class BitAiDataTableTests : BitAiTestBase
{
    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }



    [TestMethod]
    public void BitAiDataTableShouldTakeItsColumnsFromTheUnionOfTheKeysInTheOrderTheyAppear()
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("""[{"name":"Ada"},{"name":"Alan","field":"logic"}]""")));

        CollectionAssert.AreEqual(new[] { "Name", "Field" }, table.FindAll("th").Select(th => th.TextContent).ToArray());
        Assert.AreEqual("col", table.Find("th").GetAttribute("scope"));

        var rows = table.FindAll("tbody tr");
        Assert.AreEqual(2, rows.Count);
        CollectionAssert.AreEqual(new[] { "Ada", "" }, rows[0].QuerySelectorAll("td").Select(td => td.TextContent).ToArray());
        CollectionAssert.AreEqual(new[] { "Alan", "logic" }, rows[1].QuerySelectorAll("td").Select(td => td.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiDataTableShouldShowANestedValueAsItsJsonAndANullAsAnEmptyCell()
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("""[{"tags":["a","b"],"n":2.50,"x":null,"ok":true}]""")));

        CollectionAssert.AreEqual(new[] { """["a","b"]""", "2.50", "", "true" }, table.FindAll("td").Select(td => td.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiDataTableShouldRenderAnEmptyTableForAValueThatIsNotAnArray()
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("""{"a":1}""")));

        Assert.AreEqual(0, table.FindAll("th").Count);
        Assert.AreEqual(0, table.FindAll("tbody tr").Count);
    }

    [TestMethod]
    public void BitAiDataTableShouldBeANamedGroupTheKeyboardCanScroll()
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("""[{"a":1}]""")));

        // WebKit does not make a scroller focusable on its own, so the box the table scrolls in sideways is put in the
        // tab order itself while the table overflows it (the page script BitAiScrollStops.ts reads the mark), and named
        // for what is reached there - as a group, never a region: the tables of a chat would otherwise be a run of
        // landmarks of one name.
        var root = table.Find(".bit-aidtb");
        Assert.AreEqual("group", root.GetAttribute("role"));
        Assert.IsTrue(root.HasAttribute("data-bit-ai-scroll"));
        Assert.IsNull(root.GetAttribute("tabindex"));
        Assert.AreEqual("Table", root.GetAttribute("aria-label"));

        table.Render(p => p.Add(c => c.AriaLabel, "Flights"));
        Assert.AreEqual("Flights", table.Find(".bit-aidtb").GetAttribute("aria-label"));

        var splatted = RenderComponent<BitAiDataTable>(p => p
            .Add(c => c.Rows, Json("""[{"a":1}]"""))
            .Add(c => c.HtmlAttributes, new Dictionary<string, object> { ["aria-label"] = "Results", ["tabindex"] = "-1" }));
        Assert.AreEqual("Results", splatted.Find(".bit-aidtb").GetAttribute("aria-label"));
        Assert.AreEqual("-1", splatted.Find(".bit-aidtb").GetAttribute("tabindex"));
    }

    [TestMethod]
    public void BitAiDataTableShouldFollowItsRowsAsTheyChange()
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("""[{"a":1}]""")));

        table.Render(p => p.Add(c => c.Rows, Json("""[{"a":1},{"a":2,"b":3}]""")));

        Assert.AreEqual(2, table.FindAll("th").Count);
        Assert.AreEqual(2, table.FindAll("tbody tr").Count);
    }

    // The table is built again only for another element, and another element of the same document is one.
    [TestMethod]
    public void BitAiDataTableShouldFollowAnotherArrayOfTheSameDocument()
    {
        var document = Json("""{"first":[{"a":1}],"second":[{"b":2},{"c":3}]}""");
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, document.GetProperty("first")));

        table.Render(p => p.Add(c => c.Rows, document.GetProperty("first")));
        CollectionAssert.AreEqual(new[] { "A" }, table.FindAll("th").Select(h => h.TextContent).ToArray());

        table.Render(p => p.Add(c => c.Rows, document.GetProperty("second")));
        CollectionAssert.AreEqual(new[] { "B", "C" }, table.FindAll("th").Select(h => h.TextContent).ToArray());
        Assert.AreEqual(2, table.FindAll("tbody tr").Count);
    }

    // Telling the rows handed in from the ones built from reads nothing of the latter, whose document the host may have
    // disposed of by the time it hands in another.
    [TestMethod]
    public void BitAiDataTableShouldFollowAnotherDocumentOnceTheOldOneIsDisposed()
    {
        var old = JsonDocument.Parse("""[{"a":1}]""");
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, old.RootElement));
        old.Dispose();

        using var next = JsonDocument.Parse("""[{"b":2}]""");
        table.Render(p => p.Add(c => c.Rows, next.RootElement));

        CollectionAssert.AreEqual(new[] { "B" }, table.FindAll("th").Select(h => h.TextContent).ToArray());
    }

    [TestMethod]
    public void BitAiDataTableShouldRenderTheRootAndItsParts()
    {
        var table = RenderComponent<BitAiDataTable>(p => p
            .Add(c => c.Rows, Json("""[{"a":1}]"""))
            .Add(c => c.Id, "t1")
            .Add(c => c.Class, "custom")
            .Add(c => c.Style, "margin:1px")
            .Add(c => c.Dir, BitDir.Rtl)
            .Add(c => c.Classes, new() { Root = "c-root", Table = "c-table", HeaderRow = "c-hrow", HeaderCell = "c-head", Row = "c-row", Cell = "c-cell" })
            .Add(c => c.Styles, new() { Cell = "color:red" }));

        var root = table.Find(".bit-aidtb");

        Assert.AreEqual("t1", root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom"));
        Assert.IsTrue(root.ClassList.Contains("c-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-rtl"));
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");
        Assert.IsTrue(table.Find(".bit-aidtb > table.bit-aidtb-tbl").ClassList.Contains("c-table"));
        Assert.IsTrue(table.Find("thead tr.bit-aidtb-hrw").ClassList.Contains("c-hrow"));
        Assert.IsTrue(table.Find("th.bit-aidtb-hcl").ClassList.Contains("c-head"));
        Assert.IsTrue(table.Find("tbody tr.bit-aidtb-row").ClassList.Contains("c-row"));
        Assert.IsTrue(table.Find("td.bit-aidtb-cel").ClassList.Contains("c-cell"));
        Assert.AreEqual("color:red", table.Find("td.bit-aidtb-cel").GetAttribute("style"));
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitAiDataTableShouldRespectVisibility(BitVisibility visibility, string? expected)
    {
        var table = RenderComponent<BitAiDataTable>(p => p.Add(c => c.Rows, Json("[]")).Add(c => c.Visibility, visibility));

        var style = table.Find(".bit-aidtb").GetAttribute("style");

        if (expected is null) Assert.IsTrue(string.IsNullOrEmpty(style));
        else StringAssert.Contains(style, expected);
    }

    [TestMethod]
    public void BitAiDataTableShouldTakeTheCascadedParameters()
    {
        var component = RenderComponent<BitParams>(p =>
        {
            p.Add(x => x.Parameters, new List<IBitComponentParams>
            {
                new BitAiDataTableParams { Class = "cascaded", Classes = new() { Cell = "cascaded-cell" } }
            });
            p.AddChildContent<BitAiDataTable>(c => c.Add(x => x.Rows, Json("""[{"a":1}]""")));
        });

        Assert.IsTrue(component.Find(".bit-aidtb").ClassList.Contains("cascaded"));
        Assert.IsTrue(component.Find(".bit-aidtb-cel").ClassList.Contains("cascaded-cell"));
    }

    [TestMethod]
    public void BitAiDataTableParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitAiDataTable", BitAiDataTableParams.ParamName);
        Assert.AreEqual(BitAiDataTableParams.ParamName, new BitAiDataTableParams().Name);
    }
}
