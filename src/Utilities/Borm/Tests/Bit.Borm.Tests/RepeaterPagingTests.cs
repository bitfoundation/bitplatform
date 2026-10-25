using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A long list shown a page at a time: every row still reachable - by the pager, by a row operation that
/// turns to where the row went, and by anything that focuses a field in it.
/// </summary>
[TestClass]
public class RepeaterPagingTests : ComponentTestBase
{
    private static (BormDefinition Form, Dictionary<string, object?> Data) Lines(int rows, int pageSize = 25)
    {
        var form = BormBuilder.Create()
            .Array("lines", l => l.Text("product", f => f.Label("Product").Required()),
                f => f.Label("Lines").Attr("itemNoun", "line").PageSize(pageSize))
            .Build();
        var list = new List<object?>();
        for (var i = 0; i < rows; i++) list.Add(new Dictionary<string, object?> { ["product"] = $"P{i + 1}" });
        return (form, new Dictionary<string, object?> { ["lines"] = list });
    }

    private static string[] Shown(IRenderedComponent<BormView> cut)
        => [.. cut.FindAll(".bit-borm-array__item input").Select(i => i.GetAttribute("value") ?? "")];

    [TestMethod]
    public void A_list_longer_than_a_page_shows_one_page_and_says_which_rows_it_is()
    {
        var (form, data) = Lines(60);
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        Assert.HasCount(25, Shown(cut));
        Assert.AreEqual("P1", Shown(cut)[0]);
        Assert.AreEqual("1–25 of 60", cut.Find(".bit-borm-array__range").TextContent);
        Assert.AreEqual("Pages of Lines", cut.Find(".bit-borm-array__pager").GetAttribute("aria-label"));
        // The row numbers and the accessible names stay those of the whole list.
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Next page").Click();
        Assert.AreEqual("P26", Shown(cut)[0]);
        Assert.AreEqual("line 26", cut.FindAll(".bit-borm-array__item")[0].GetAttribute("aria-label"));

        cut.Find(".bit-borm-array__page select").Change("2");
        Assert.HasCount(10, Shown(cut));
        Assert.AreEqual("51–60 of 60", cut.Find(".bit-borm-array__range").TextContent);
        Assert.IsTrue(cut.FindAll("button").Single(b => b.TextContent.Trim() == "Next page").HasAttribute("disabled"));
    }

    [TestMethod]
    public void A_short_list_has_no_pager_at_all()
    {
        var (form, data) = Lines(10);
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        Assert.HasCount(10, Shown(cut));
        Assert.IsEmpty(cut.FindAll(".bit-borm-array__pager"));
    }

    [TestMethod]
    public void A_row_added_is_shown_on_the_page_it_landed_on()
    {
        var (form, data) = Lines(25);
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        cut.Find(".bit-borm-btn--add").Click();

        Assert.HasCount(1, Shown(cut));
        Assert.AreEqual("26–26 of 26", cut.Find(".bit-borm-array__range").TextContent);
    }

    [TestMethod]
    public async Task Rows_on_other_pages_that_need_attention_are_counted_and_focusing_one_turns_to_it()
    {
        var (form, data) = Lines(60);
        ((Dictionary<string, object?>)((List<object?>)data["lines"]!)[40]!)["product"] = "";
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data).Add(x => x.FocusFirstError, false));
        var state = cut.Instance.Form;

        await cut.InvokeAsync(() => state.SubmitAsync(_ => Task.CompletedTask));

        StringAssert.Contains(cut.Find(".bit-borm-array__elsewhere").TextContent, "1 line on another page needs attention.");

        // What the error summary's link and focus-the-first-error do: the row's page comes first.
        await cut.InvokeAsync(async () => await state.FocusAsync("lines[40].product"));
        // Row 40 counted from nought is the 41st, on the second page.
        cut.WaitForAssertion(() => Assert.AreEqual("26–50 of 60", cut.Find(".bit-borm-array__range").TextContent));
        Assert.IsNotEmpty(cut.FindAll("#borm_lines_40_product"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-array__elsewhere"));
    }

    [TestMethod]
    public void The_pager_turns_to_a_problem_on_another_page_from_its_own_button()
    {
        var (form, data) = Lines(60);
        ((Dictionary<string, object?>)((List<object?>)data["lines"]!)[55]!)["product"] = "";
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data).Add(x => x.FocusFirstError, false));
        cut.Find("form").Submit();

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Show the first").Click();

        Assert.AreEqual("51–60 of 60", cut.Find(".bit-borm-array__range").TextContent);
    }

    [TestMethod]
    public void The_page_size_travels_through_json_schema()
    {
        var (form, _) = Lines(0, pageSize: 50);
        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        Assert.AreEqual(50, back.Fields[0].PageSize);
        Assert.AreEqual(50, form.Clone().Fields[0].PageSize);
    }
}
