using System.ComponentModel.DataAnnotations;

namespace Bit.Borm.Tests;

/// <summary>
/// A date bound that renders on the control but is never enforced is worse than no bound at all: the
/// picker refuses the date, the user types it anyway, and the form accepts it.
/// </summary>
[TestClass]
public class DateRangeRuleTests
{
    private sealed class Trip
    {
        [Range(typeof(DateTime), "2024-01-01", "2024-12-31")]
        public DateTime Departure { get; set; }
    }

    private static BormState Form<T>(T model) where T : class
        => new(BormSchemaGenerator.Generate<T>(), new BormModelDataAccessor(model));

    [TestMethod]
    public async Task A_date_outside_the_annotated_window_is_reported()
    {
        var state = Form(new Trip { Departure = new DateTime(2025, 6, 1) });

        Assert.IsFalse(await state.ValidateAsync());
        Assert.ContainsSingle(state.MessagesFor("Departure"));
    }

    [TestMethod]
    public async Task A_date_inside_the_annotated_window_passes()
    {
        var state = Form(new Trip { Departure = new DateTime(2024, 6, 1) });

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task The_bounds_of_a_date_window_are_inclusive()
    {
        Assert.IsTrue(await Form(new Trip { Departure = new DateTime(2024, 1, 1) }).ValidateAsync());
        Assert.IsTrue(await Form(new Trip { Departure = new DateTime(2024, 12, 31) }).ValidateAsync());
    }

    [TestMethod]
    public async Task The_builder_takes_dates_directly_rather_than_ole_automation_numbers()
    {
        var form = BormBuilder.Create()
            .Field("when", BormFieldType.Date, f => f
                .Range(new DateTime(2030, 1, 1), new DateTime(2030, 12, 31)))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("when", new DateTime(2029, 12, 31));

        Assert.IsFalse(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_DateOnly_value_is_judged_against_a_DateOnly_window()
    {
        var form = BormBuilder.Create()
            .Field("when", BormFieldType.Date, f => f
                .Range(new DateOnly(2030, 1, 1), new DateOnly(2030, 12, 31)))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("when", new DateOnly(2030, 6, 1));
        Assert.IsTrue(await state.ValidateAsync());

        state.SetValue("when", new DateOnly(2031, 6, 1));
        Assert.IsFalse(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_time_of_day_window_is_enforced_on_the_same_scale_it_was_declared()
    {
        var form = BormBuilder.Create()
            .Field("start", BormFieldType.Time, f => f
                .Range(new TimeOnly(9, 0), new TimeOnly(17, 0)))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("start", new TimeOnly(10, 30));
        Assert.IsTrue(await state.ValidateAsync());

        state.SetValue("start", new TimeOnly(18, 0));
        Assert.IsFalse(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task The_message_names_the_dates_rather_than_the_numbers_they_are_stored_as()
    {
        var state = Form(new Trip { Departure = new DateTime(2025, 6, 1) });
        await state.ValidateAsync();

        var message = state.MessagesFor("Departure")[0].Message;

        // 45292 is what the bound is stored as; it is not something a user can act on.
        Assert.DoesNotContain("45292", message, StringComparison.Ordinal);
        Assert.Contains("2024", message, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task A_numeric_range_is_untouched_by_any_of_this()
    {
        var state = Form(new RegistrationModel { FirstName = "Ada", Email = "a@b.com", Age = 12 });
        await state.ValidateAsync();

        Assert.ContainsSingle(state.MessagesFor("Age"));
        Assert.Contains("18", state.MessagesFor("Age")[0].Message, StringComparison.Ordinal);
    }
}

/// <summary>
/// A rule that rejects <c>" a@b.com "</c> for its spaces is technically right and of no use to
/// anyone. Normalizers fix the value instead of the complaint.
/// </summary>
[TestClass]
public class NormalisationTests
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Text("email", f => f.Trim().Required())
        .Text("reference", f => f.Normalize(v => v is string s ? s.ToUpperInvariant() : v))
        .Build();

    private static BormState Form() => new(Schema(), new BormDictionaryDataAccessor());

    [TestMethod]
    public void Submitting_tidies_every_field_before_anything_is_judged()
    {
        var state = Form();
        state.SetValue("email", "  ada@example.com  ");
        state.SetValue("reference", "ab-12");

        state.NormalizeAll();

        Assert.AreEqual("ada@example.com", state.GetValue("email"));
        Assert.AreEqual("AB-12", state.GetValue("reference"));
    }

    [TestMethod]
    public async Task A_field_holding_only_whitespace_becomes_empty_and_so_fails_its_required_rule()
    {
        var state = Form();
        state.SetValue("email", "   ");

        Assert.IsFalse(await state.SubmitAsync());
        Assert.IsNull(state.GetValue("email"));
        Assert.ContainsSingle(state.MessagesFor("email"));
    }

    [TestMethod]
    public async Task A_value_that_only_needed_trimming_is_accepted_rather_than_reported()
    {
        var state = Form();
        state.SetValue("email", "  ada@example.com  ");

        Assert.IsTrue(await state.SubmitAsync());
        Assert.AreEqual("ada@example.com", state.GetValue("email"));
    }

    [TestMethod]
    public void Trim_can_be_asked_to_keep_an_empty_string_rather_than_null_it()
    {
        var form = BormBuilder.Create().Text("note", f => f.Trim(emptyBecomesNull: false)).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("note", "  ");
        state.NormalizeAll();

        Assert.AreEqual("", state.GetValue("note"));
    }

    [TestMethod]
    public void Tidying_a_value_does_not_claim_the_user_visited_the_field()
    {
        var state = Form();
        state.SetValueQuietly("email", "  ada@example.com  ");

        state.NormalizeAll();

        Assert.IsFalse(state.IsTouched("email"));
    }

    [TestMethod]
    public void A_normalizer_runs_on_every_row_of_a_repeater()
    {
        var form = BormBuilder.Create()
            .ArrayOf("codes", BormFieldType.Text, f => f
                .Items(max: 5), item => item.Trim())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var field = form.FindField("codes")!;
        state.AddArrayItem(field, "codes");
        state.AddArrayItem(field, "codes");
        state.SetValue("codes[0]", " a ");
        state.SetValue("codes[1]", " b ");

        state.NormalizeAll();

        Assert.AreEqual("a", state.GetValue("codes[0]"));
        Assert.AreEqual("b", state.GetValue("codes[1]"));
    }

    [TestMethod]
    public void A_normalizer_that_changes_nothing_leaves_the_field_clean()
    {
        var state = Form();
        state.AcceptChanges();

        state.NormalizeAll();

        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void The_normalizer_survives_a_clone_of_the_schema()
    {
        var copy = Schema().Clone();

        Assert.IsNotNull(copy.FindField("email")!.Normalize);
    }
}
