using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A schema changed in place under a live state, and the costs that have to stay proportionate as a
/// form grows. The cost tests count work rather than time it, so they say the same thing on every
/// machine: a formula is evaluated a bounded number of times however many rows there are.
/// </summary>
[TestClass]
public class DefinitionChangeTests
{
    [TestMethod]
    public void A_field_swapped_for_another_of_the_same_name_is_the_one_every_part_of_the_form_finds()
    {
        var calls = new List<string>();
        var form = BormBuilder.Create()
            .Text("name", f => f.OnChange(_ => calls.Add("old")))
            .Object("address", a => a.Text("city"))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("name", "a");
        Assert.IsNotNull(state.FindField("address.city"));

        // Replaced in the list, as the form's own list of fields draws it - without NotifyDefinitionChanged.
        form.Fields[0] = BormBuilder.Create().Text("name", f => f.OnChange(_ => calls.Add("new"))).Build().Fields[0];
        state.SetValue("name", "b");
        CollectionAssert.AreEqual(new[] { "old", "new" }, calls);
        Assert.AreSame(form.Fields[0], state.FindField("name"));

        var city = new BormFieldDefinition("city", BormFieldType.Select);
        form.Fields[1].Children[0] = city;
        Assert.AreSame(city, state.FindField("address.city"));

        // And one renamed in place answers to its new name only.
        city.Name = "town";
        Assert.IsNull(state.FindField("address.city"));
        Assert.AreSame(city, state.FindField("address.town"));
    }

    [TestMethod]
    public void A_computed_field_added_to_a_live_form_is_computed_once_the_state_is_told()
    {
        var form = BormBuilder.Create().Number("a").Number("b").Build();
        var data = new Dictionary<string, object?> { ["a"] = 2.0, ["b"] = 3.0 };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        form.Fields.Add(new BormFieldDefinition("sum", BormFieldType.Number));
        new BormFieldBuilder(form.Fields[^1]).Computed("{a} + {b}");
        state.NotifyDefinitionChanged();

        Assert.AreEqual(5m, data["sum"]);
        // A field the schema just gained is not a change the user made.
        Assert.IsFalse(state.IsFormDirty);

        state.SetValue("a", 10.0);
        Assert.AreEqual(13m, data["sum"]);
    }

    [TestMethod]
    public void A_condition_added_to_a_live_form_clears_what_it_hides()
    {
        var form = BormBuilder.Create().Checkbox("ship").Text("address").Build();
        var data = new Dictionary<string, object?> { ["ship"] = true, ["address"] = "1 Main St" };
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        new BormFieldBuilder(form.FindField("address")!).VisibleWhen("{ship}").ClearOnHide();
        state.NotifyDefinitionChanged();

        state.SetValue("ship", false);
        Assert.IsNull(data["address"]);
    }

    [TestMethod]
    public void A_removed_field_takes_its_messages_and_flags_with_it_but_not_the_users_other_answers()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Required())
            .Text("legacy", f => f.Required())
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("name", "Ada");
        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
        Assert.HasCount(1, state.MessagesFor("legacy"));

        form.Fields.Remove(form.FindField("legacy")!);
        state.NotifyDefinitionChanged();

        // Nothing left on the page can be blocking the submit.
        Assert.IsEmpty(state.MessagesFor("legacy"));
        Assert.IsTrue(state.IsValid);
        Assert.AreEqual("Ada", state.GetValue("name"));
        Assert.IsTrue(state.IsDirty("name"));
        Assert.IsTrue(state.ValidateAsync().AsTask().Result);
    }

    [TestMethod]
    public void A_field_added_with_a_default_gets_it_and_is_baselined_at_it()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        form.Fields.Add(new BormFieldDefinition("country", BormFieldType.Text) { DefaultValue = "NL" });
        state.NotifyDefinitionChanged();

        Assert.AreEqual("NL", data["country"]);
        Assert.IsFalse(state.IsDirty("country"));
        state.SetValue("country", "BE");
        state.Reset();
        Assert.AreEqual("NL", data["country"]);
    }
}

[TestClass]
public class ScaleTests
{
    public sealed class Line
    {
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class Order
    {
        public List<Line> Lines { get; set; } = [];
        public decimal Total { get; set; }
    }

    [TestMethod]
    public void Seeding_a_large_invoice_evaluates_each_formula_a_bounded_number_of_times()
    {
        var lineEvaluations = 0;
        var totalEvaluations = 0;
        var form = BormBuilder.For<Order>()
            .Array(x => x.Lines, l => l
                .Field(x => x.Quantity)
                .Field(x => x.Price)
                .Computed(x => x.Total, x => { lineEvaluations++; return x.Quantity * x.Price; },
                    dependsOn: [nameof(Line.Quantity), nameof(Line.Price)]))
            .Computed(x => x.Total, x => { totalEvaluations++; return x.Lines.Sum(l => l.Total); },
                dependsOn: [nameof(Order.Lines)])
            .Build();

        const int rows = 300;
        var order = new Order { Lines = Enumerable.Range(0, rows).Select(i => new Line { Quantity = 1 + i % 3, Price = 2 }).ToList() };
        _ = new BormState(form, new BormModelDataAccessor(order));

        Assert.AreEqual(order.Lines.Sum(l => l.Quantity * 2m), order.Total);
        // Two passes: one to compute, one to see that nothing moved. Seeding used to walk the whole
        // invoice once per line that changed, evaluating the total once per line too.
        Assert.IsLessThanOrEqualTo(rows * 2, lineEvaluations);
        Assert.IsLessThanOrEqualTo(3, totalEvaluations);
    }

    [TestMethod]
    public void Editing_one_line_re_evaluates_that_line_and_the_total_and_nothing_else()
    {
        var lineEvaluations = 0;
        var form = BormBuilder.For<Order>()
            .Array(x => x.Lines, l => l
                .Field(x => x.Quantity)
                .Field(x => x.Price)
                .Computed(x => x.Total, x => { lineEvaluations++; return x.Quantity * x.Price; },
                    dependsOn: [nameof(Line.Quantity), nameof(Line.Price)]))
            .Computed(x => x.Total, x => x.Lines.Sum(l => l.Total), dependsOn: [nameof(Order.Lines)])
            .Build();
        var order = new Order { Lines = Enumerable.Range(0, 100).Select(_ => new Line { Quantity = 1, Price = 2 }).ToList() };
        var state = new BormState(form, new BormModelDataAccessor(order));

        lineEvaluations = 0;
        state.SetValue("Lines[40].Quantity", 5);

        Assert.AreEqual(1, lineEvaluations);
        Assert.AreEqual(10m, order.Lines[40].Total);
        Assert.AreEqual(208m, order.Total);
    }

    [TestMethod]
    public void A_cycle_of_formulas_still_settles_when_the_form_is_seeded()
    {
        var form = BormBuilder.Create()
            .Number("a", f => f.Computed(ctx => (ctx.Sibling("b") as decimal? ?? 0) + 1, "b"))
            .Number("b", f => f.Computed(ctx => (ctx.Sibling("a") as decimal? ?? 0) + 1, "a"))
            .Build();

        // Bounded, not infinite: the point is that it returns.
        var state = new BormState(form, new BormDictionaryDataAccessor());
        Assert.IsNotNull(state.GetValue("a"));
    }
}

[TestClass]
public class LiveSchemaRenderingTests : ComponentTestBase
{
    [TestMethod]
    public void A_field_placed_before_it_exists_appears_once_the_schema_gains_it()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services);
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "nickname"));
        Assert.AreEqual(string.Empty, cut.Markup.Trim());

        form.Fields.Add(new BormFieldDefinition("nickname", BormFieldType.Text) { Label = "Nickname" });
        cut.Render();

        Assert.IsNotNull(cut.Find("#borm_nickname"));
    }
}

[TestClass]
public class MalformedPathTests : ComponentTestBase
{
    [TestMethod]
    public void A_server_error_on_a_malformed_path_is_listed_rather_than_taking_the_summary_down()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(), Services);
        // The summary lists messages once a submit has been tried, which a server error follows.
        state.RegisterSubmitAttempt();
        // Under a real field's name, so the schema lookup has to read the malformed index to answer.
        state.SetServerErrors([new BormValidationMessage("name[x]", "The server did not like this.")]);

        var cut = Render<BormView>(p => p.Add(x => x.State, state).Add(x => x.ShowErrorSummary, true));

        StringAssert.Contains(cut.Markup, "The server did not like this.");
    }
}
