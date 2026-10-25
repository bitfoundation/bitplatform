using Bunit;

namespace Bit.Borm.Tests;

public sealed class Subscription
{
    public DateOnly? CardExpiry { get; set; }
    public DateTime DeliveryWeek { get; set; }
    public string? BillingMonth { get; set; }
}

/// <summary>
/// The month and week pickers: what each stores, what each shows, and what happens to text that is
/// neither — which is what a browser without a picker of its own sends.
/// </summary>
[TestClass]
public class MonthAndWeekTests : ComponentTestBase
{
    private static BormDefinition Typed() => BormBuilder.For<Subscription>()
        .Field(x => x.CardExpiry, f => f.AsMonth())
        .Field(x => x.DeliveryWeek, f => f.AsWeek())
        .Field(x => x.BillingMonth, f => f.AsMonth())
        .Build();

    [TestMethod]
    public void A_date_typed_month_holds_its_first_day_and_shows_as_the_month()
    {
        var model = new Subscription();
        var state = new BormState(Typed(), new BormModelDataAccessor(model));
        var context = new BormFieldContext(state, state.Definition.FindField("CardExpiry")!, "CardExpiry");

        context.SetFromStringAsync("2027-09").Wait();

        Assert.AreEqual(new DateOnly(2027, 9, 1), model.CardExpiry);
        Assert.AreEqual("2027-09", context.StringValue);
    }

    [TestMethod]
    public void A_date_typed_week_holds_its_monday_and_shows_as_the_iso_week()
    {
        var model = new Subscription();
        var state = new BormState(Typed(), new BormModelDataAccessor(model));
        var context = new BormFieldContext(state, state.Definition.FindField("DeliveryWeek")!, "DeliveryWeek");

        // Week 1 of 2026 begins on Monday 29 December 2025: ISO weeks belong to the year of their Thursday.
        context.SetFromStringAsync("2026-W01").Wait();

        Assert.AreEqual(new DateTime(2025, 12, 29), model.DeliveryWeek);
        Assert.AreEqual("2026-W01", context.StringValue);
    }

    [TestMethod]
    public void Text_that_is_no_month_is_reported_rather_than_stored_whatever_holds_it()
    {
        var model = new Subscription();
        var state = new BormState(Typed(), new BormModelDataAccessor(model));

        state.SetValue("CardExpiry", BormValueConverter.FromInputString("Sept 2027", typeof(DateOnly?), BormFieldType.Month));
        state.SetValue("BillingMonth", "2027-13");

        Assert.IsNull(model.CardExpiry);
        Assert.IsNull(model.BillingMonth);
        Assert.AreEqual(BormValidationSource.Conversion, state.MessagesFor("CardExpiry").Single().Source);
        Assert.AreEqual(BormValidationSource.Conversion, state.MessagesFor("BillingMonth").Single().Source);

        state.SetValue("BillingMonth", "2027-12");
        Assert.AreEqual("2027-12", model.BillingMonth);
        Assert.IsEmpty(state.MessagesFor("BillingMonth"));
    }

    [TestMethod]
    public void A_schema_only_week_is_the_pickers_text_and_its_range_is_enforced()
    {
        var form = BormBuilder.Create()
            .Field("week", BormFieldType.Week, f => f.Range(new DateOnly(2026, 1, 5), new DateOnly(2026, 3, 30)))
            .Build();
        var data = new Dictionary<string, object?>();
        var state = new BormState(form, new BormDictionaryDataAccessor(data));
        var context = new BormFieldContext(state, form.Fields[0], "week");

        context.SetFromStringAsync("2026-w05").Wait();
        Assert.AreEqual("2026-W05", data["week"]);
        Assert.IsTrue(state.ValidateAsync().AsTask().Result);

        context.SetFromStringAsync("2026-W40").Wait();
        Assert.IsFalse(state.ValidateAsync().AsTask().Result);
    }

    [TestMethod]
    public void The_pickers_render_as_the_browsers_own_inputs_with_their_bounds()
    {
        var form = BormBuilder.Create()
            .Field("expiry", BormFieldType.Month, f => f.Range(new DateOnly(2026, 1, 1), new DateOnly(2030, 12, 1)))
            .Field("slot", BormFieldType.Week)
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        var expiry = cut.Find("#borm_expiry");
        Assert.AreEqual("month", expiry.GetAttribute("type"));
        Assert.AreEqual("2026-01", expiry.GetAttribute("min"));
        Assert.AreEqual("2030-12", expiry.GetAttribute("max"));
        Assert.AreEqual("week", cut.Find("#borm_slot").GetAttribute("type"));
    }

    [TestMethod]
    public void Both_travel_through_json_schema_as_widgets()
    {
        var back = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(Typed()));

        Assert.AreEqual(BormFieldType.Month, back.FindField("CardExpiry")!.Type);
        Assert.AreEqual(BormFieldType.Week, back.FindField("DeliveryWeek")!.Type);
    }

    [TestMethod]
    public void An_expression_reads_a_month_or_week_as_its_first_day()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["m"] = "2026-02", ["w"] = "2026-W10" });

        Assert.AreEqual(true, BormExpression.Parse("{m} < {w}").Evaluate(data));
        Assert.AreEqual(2, BormExpression.Parse("month({m})").Evaluate(data));
    }
}
