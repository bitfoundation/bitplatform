namespace Bit.Borm.Tests;

public class OrderModel
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName { get; set; } = "";
    public List<OrderLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
    public decimal TotalWithTax { get; set; }
}

public class OrderLine
{
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

[TestClass]
public class ComputedValueTests
{
    private static BormDefinition Schema() => BormBuilder.For<OrderModel>()
        .Field(x => x.FirstName)
        .Field(x => x.LastName)
        .Computed(x => x.FullName, m => $"{m.FirstName} {m.LastName}".Trim(),
            dependsOn: [nameof(OrderModel.FirstName), nameof(OrderModel.LastName)])
        .Array(x => x.Lines, line => line
            .Field(l => l.Quantity)
            .Field(l => l.UnitPrice)
            .Computed(l => l.LineTotal, l => l.Quantity * l.UnitPrice,
                dependsOn: [nameof(OrderLine.Quantity), nameof(OrderLine.UnitPrice)]))
        .Computed(x => x.Total, m => m.Lines.Sum(l => l.Quantity * l.UnitPrice),
            dependsOn: [nameof(OrderModel.Lines)])
        // Chained: depends on another computed field.
        .Computed(x => x.TotalWithTax, m => Math.Round(m.Total * 1.2m, 2),
            dependsOn: [nameof(OrderModel.Total)])
        .Build();

    [TestMethod]
    public void A_computed_field_refreshes_when_a_dependency_changes()
    {
        var model = new OrderModel();
        var state = new BormState(Schema(), new BormModelDataAccessor(model));

        state.SetValue("FirstName", "Ada");
        state.SetValue("LastName", "Lovelace");

        Assert.AreEqual("Ada Lovelace", model.FullName);
    }

    [TestMethod]
    public void An_unrelated_change_does_not_recompute()
    {
        var model = new OrderModel { FirstName = "Ada" };
        var form = BormBuilder.For<OrderModel>()
            .Field(x => x.FirstName)
            .Field(x => x.LastName)
            .Computed(x => x.FullName, m => m.FirstName, dependsOn: [nameof(OrderModel.FirstName)])
            .Build();

        var state = new BormState(form, new BormModelDataAccessor(model));
        Assert.AreEqual("Ada", model.FullName);

        model.FullName = "manually overridden";
        state.SetValue("LastName", "Lovelace");

        Assert.AreEqual("manually overridden", model.FullName);
    }

    [TestMethod]
    public void Computed_values_are_seeded_when_the_form_is_created()
    {
        var model = new OrderModel { FirstName = "Grace", LastName = "Hopper" };

        _ = new BormState(Schema(), new BormModelDataAccessor(model));

        Assert.AreEqual("Grace Hopper", model.FullName);
    }

    [TestMethod]
    public void A_formula_on_an_array_item_reads_that_item_not_the_root()
    {
        // The interesting case: the same expression has to work on row 0 and row 1 without knowing
        // which row it is on.
        var model = new OrderModel();
        var form = Schema();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var lines = form.FindField("Lines")!;

        state.AddArrayItem(lines, "Lines");
        state.AddArrayItem(lines, "Lines");

        state.SetValue("Lines[0].Quantity", 3);
        state.SetValue("Lines[0].UnitPrice", 10m);
        state.SetValue("Lines[1].Quantity", 2);
        state.SetValue("Lines[1].UnitPrice", 5.5m);

        Assert.AreEqual(30m, model.Lines[0].LineTotal);
        Assert.AreEqual(11m, model.Lines[1].LineTotal);
    }

    [TestMethod]
    public void A_change_inside_an_array_item_refreshes_a_total_over_the_whole_array()
    {
        var model = new OrderModel();
        var form = Schema();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var lines = form.FindField("Lines")!;

        state.AddArrayItem(lines, "Lines");
        state.SetValue("Lines[0].Quantity", 4);
        state.SetValue("Lines[0].UnitPrice", 25m);

        Assert.AreEqual(100m, model.Total);
        // ...and a computed field that depends on another computed field follows along.
        Assert.AreEqual(120m, model.TotalWithTax);
    }

    [TestMethod]
    public void Removing_an_item_refreshes_the_total()
    {
        var model = new OrderModel();
        var form = Schema();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var lines = form.FindField("Lines")!;

        state.AddArrayItem(lines, "Lines");
        state.SetValue("Lines[0].UnitPrice", 40m);
        Assert.AreEqual(40m, model.Total);

        state.RemoveArrayItem("Lines", 0);

        Assert.AreEqual(0m, model.Total);
    }

    [TestMethod]
    public void A_computed_field_is_read_only_by_default()
        => Assert.IsTrue(Schema().FindField("Total")!.ReadOnly);

    [TestMethod]
    public void Circular_formulas_settle_instead_of_recursing_forever()
    {
        // Two fields that each derive from the other never converge; the engine has to stop rather
        // than blow the stack.
        var form = BormBuilder.Create()
            .Text("trigger")
            .Integer("a", f => f.Computed(ctx => Convert.ToInt32(ctx.Value("b") ?? 0) + 1))
            .Integer("b", f => f.Computed(ctx => Convert.ToInt32(ctx.Value("a") ?? 0) + 1))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("trigger", "go");

        // No exception, and both fields hold a number.
        Assert.IsNotNull(state.GetValue("a"));
        Assert.IsNotNull(state.GetValue("b"));
    }

    [TestMethod]
    public void The_untyped_builder_can_read_siblings_relative_to_the_owning_object()
    {
        var form = BormBuilder.Create()
            .Number("width")
            .Number("height")
            .Number("area", f => f.Computed(
                ctx => Convert.ToDouble(ctx.Sibling("width") ?? 0d) * Convert.ToDouble(ctx.Sibling("height") ?? 0d),
                "width", "height"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("width", 3d);
        state.SetValue("height", 4d);

        Assert.AreEqual(12d, state.GetValue("area"));
    }

    [TestMethod]
    public void A_formula_reading_its_own_value_is_applied_once_when_the_form_opens_or_is_rebased()
    {
        var form = BormBuilder.Create()
            .Number("Counter", f => f.Computed("{Counter} + 1"))
            .Number("Price")
            .Number("Total", f => f.Computed("{Price} * 2"))
            .Build();
        var data = new Dictionary<string, object?> { ["Counter"] = 0d, ["Price"] = 5d };

        var state = new BormState(form, new BormDictionaryDataAccessor(data));

        // Once, as a single edit runs it once: another formula moving in the same pass is not a reason to run it again.
        Assert.AreEqual(1d, Convert.ToDouble(data["Counter"]));
        Assert.AreEqual(10d, Convert.ToDouble(data["Total"]));

        state.Reset([new("Counter", 10d)]);
        Assert.AreEqual(11d, Convert.ToDouble(data["Counter"]));
    }

    [TestMethod]
    public void A_formula_reading_its_own_value_runs_again_for_another_value_it_reads_that_moved_after_it()
    {
        // Running sum: reads itself and Step, and Step is computed after it in the same pass.
        var form = BormBuilder.Create()
            .Number("Sum", f => f.Computed("{Sum} + {Step}"))
            .Number("Base")
            .Number("Step", f => f.Computed("{Base} * 2"))
            .Build();
        var data = new Dictionary<string, object?> { ["Sum"] = 0d, ["Base"] = 1d, ["Step"] = 0d };

        _ = new BormState(form, new BormDictionaryDataAccessor(data));

        // First pass: Sum = 0 + 0, Step = 2; the second runs Sum again for Step, once: 0 + 2.
        Assert.AreEqual(2d, Convert.ToDouble(data["Step"]));
        Assert.AreEqual(2d, Convert.ToDouble(data["Sum"]));
    }
}
