using System.ComponentModel.DataAnnotations;
using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A length limit on a field that holds several strings applies to each of them. Reading the value as
/// a string and finding a list made the rule quietly do nothing — the same shape of bug as
/// <c>[MinLength]</c> on a collection being mapped to the string rule.
/// </summary>
[TestClass]
public class TagLengthRuleTests
{
    private static BormState Form(int min = 0, int max = 10)
    {
        var builder = BormBuilder.Create().Tags("labels", f =>
        {
            f.AsTags();
            if (min > 0) f.MinLength(min);
            f.MaxLength(max);
        });
        return new BormState(builder.Build(), new BormDictionaryDataAccessor());
    }

    [TestMethod]
    public async Task A_tag_over_the_limit_is_reported()
    {
        var state = Form(max: 5);
        state.SetValue("labels", new List<string> { "ok", "far too long" });

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("labels"));
    }

    [TestMethod]
    public async Task Tags_within_the_limit_pass()
    {
        var state = Form(max: 5);
        state.SetValue("labels", new List<string> { "ok", "fine" });

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_tag_under_the_minimum_is_reported()
    {
        var state = Form(min: 3, max: 20);
        state.SetValue("labels", new List<string> { "ab" });

        Assert.IsFalse(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task An_empty_tag_list_is_Requireds_business_not_this_rules()
    {
        var state = Form(min: 3, max: 20);

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_repeater_of_strings_is_untouched_by_the_change()
    {
        // Only a tag list holds several values the limit applies to individually. An array's length is
        // the collection-size rule's business, and reinterpreting it here would change what an existing
        // schema means.
        var form = BormBuilder.Create()
            .ArrayOf("items", BormFieldType.Text, f => f.MaxLength(3))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("items", new List<object?> { "far too long" });

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_plain_string_field_still_measures_the_string()
    {
        var form = BormBuilder.Create().Text("a", f => f.MaxLength(3)).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("a", "abcd");

        Assert.IsFalse(await state.ValidateAsync());
    }
}

[TestClass]
public class TagsFieldLabellingTests : ComponentTestBase
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Tags("labels", f => f.Label("Labels").AsTags())
        .Build();

    [TestMethod]
    public void The_label_is_a_span_the_group_points_at_not_a_for_attribute()
    {
        // The box a tag is typed into disappears once the list is full or the form is read-only, so a
        // `for` aimed at it would be left pointing at nothing.
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Schema()));

        Assert.IsEmpty(cut.FindAll("label[for='borm_labels']"));
        Assert.AreEqual("borm_labels_label", cut.Find(".bit-borm-tags").GetAttribute("aria-labelledby"));
        Assert.Contains("Labels", cut.Find("#borm_labels_label").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_group_carries_the_field_id_so_the_error_summary_can_link_to_it()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        Assert.AreEqual("borm_labels", cut.Find(".bit-borm-tags").GetAttribute("id"));
        Assert.AreEqual("borm_labels_entry", cut.Find(".bit-borm-tags__entry").GetAttribute("id"));
    }
}

[TestClass]
public class ComboboxDiagnosticTests
{
    [TestMethod]
    public void Two_options_with_the_same_label_are_reported()
    {
        // The entry is matched back to an option by its label, because the label is what the browser
        // puts in the box. Two of them makes the user's choice a coin toss.
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("par-fr", "Paris"), ("par-us", "Paris")).AsCombobox())
            .Build();

        var problems = form.Validate();

        Assert.Contains(p => p.Message.Contains("labelled 'Paris'", StringComparison.Ordinal), problems);
    }

    [TestMethod]
    public void Distinct_labels_are_not_reported()
    {
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("par", "Paris, France"), ("par-us", "Paris, Texas")).AsCombobox())
            .Build();

        Assert.DoesNotContain(p => p.Message.Contains("labelled", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void A_select_with_repeated_labels_is_not_reported_because_it_matches_on_value()
    {
        var form = BormBuilder.Create()
            .Select("city", f => f.Options(("par-fr", "Paris"), ("par-us", "Paris")))
            .Build();

        Assert.DoesNotContain(p => p.Message.Contains("labelled", StringComparison.Ordinal), form.Validate());
    }
}

public class BookingRequest : IValidatableObject
{
    [Required] public string Reference { get; set; } = "";
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public int Guests { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (End < Start)
            yield return new ValidationResult("The end date must be on or after the start date.", [nameof(End)]);

        if (Guests > 4 && Reference.Length == 0)
            yield return new ValidationResult("Large bookings need a reference.", [nameof(Guests), nameof(Reference)]);

        if (Guests < 0)
            yield return new ValidationResult("This booking does not make sense.");
    }
}

/// <summary>
/// The .NET-native counterpart of the FluentValidation bridge. The generator already turns the
/// attributes it understands into field rules; what it cannot see is a custom ValidationAttribute's
/// logic, or IValidatableObject — the standard way a model says "these two properties have to agree".
/// </summary>
[TestClass]
public class DataAnnotationsIntegrationTests
{
    private static BormState Form(BookingRequest model)
        => new BormState(BormSchemaGenerator.Generate<BookingRequest>(),
            new BormModelDataAccessor(model)).UseDataAnnotations();

    [TestMethod]
    public async Task IValidatableObject_failures_land_on_the_field_they_name()
    {
        var state = Form(new BookingRequest
        {
            Reference = "R1",
            Start = new DateOnly(2026, 5, 10),
            End = new DateOnly(2026, 5, 1)
        });

        Assert.IsFalse(await state.ValidateAsync());
        Assert.Contains(m => m.Message.Contains("end date", StringComparison.Ordinal), state.MessagesFor("End"));
    }

    [TestMethod]
    public async Task A_failure_naming_several_members_appears_under_each_of_them()
    {
        var state = Form(new BookingRequest { Guests = 8, Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 1, 2) });

        await state.ValidateAsync();

        Assert.Contains(m => m.Message.Contains("reference", StringComparison.Ordinal), state.MessagesFor("Guests"));
        Assert.Contains(m => m.Message.Contains("reference", StringComparison.Ordinal), state.MessagesFor("Reference"));
    }

    [TestMethod]
    public async Task A_failure_naming_no_member_is_a_form_level_message()
    {
        var state = Form(new BookingRequest
        {
            Reference = "R1",
            Guests = -1,
            Start = new DateOnly(2026, 1, 1),
            End = new DateOnly(2026, 1, 2)
        });

        await state.ValidateAsync();

        Assert.Contains(m => m.Message.Contains("does not make sense", StringComparison.Ordinal), state.MessagesFor(string.Empty));
    }

    [TestMethod]
    public async Task A_valid_model_passes()
    {
        var state = Form(new BookingRequest
        {
            Reference = "R1",
            Guests = 2,
            Start = new DateOnly(2026, 1, 1),
            End = new DateOnly(2026, 1, 2)
        });

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task A_failure_on_a_field_the_schema_hides_is_dropped()
    {
        // Same rule as FluentValidation: a validator sees the whole model and knows nothing about the
        // form's conditions, so without this a hidden branch would refuse the submit and point at a
        // control that is not on the page.
        var form = BormBuilder.For<BookingRequest>()
            .Field(x => x.Reference)
            .Field(x => x.Start)
            .Field(x => x.End, f => f.VisibleWhen(_ => false))
            .Field(x => x.Guests)
            .Build();

        var state = new BormState(form, new BormModelDataAccessor(new BookingRequest
        {
            Reference = "R1",
            Start = new DateOnly(2026, 5, 10),
            End = new DateOnly(2026, 5, 1)
        })).UseDataAnnotations();

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task Cross_property_messages_appear_even_while_a_required_field_is_still_empty()
    {
        // Validator.TryValidateObject skips IValidatableObject entirely once any property attribute has
        // failed, so the user would fix the last required field, press submit again, and only then be
        // told about something new. Everything found is reported at once instead.
        var state = Form(new BookingRequest
        {
            Reference = "",
            Start = new DateOnly(2026, 5, 10),
            End = new DateOnly(2026, 5, 1)
        });

        await state.ValidateAsync();

        Assert.IsNotEmpty(state.MessagesFor("Reference"));
        Assert.Contains(m => m.Message.Contains("end date", StringComparison.Ordinal), state.MessagesFor("End"));
    }

    [TestMethod]
    public async Task The_property_attributes_are_left_to_the_generated_field_rules()
    {
        // They are already field rules, so running Validator over them again would put two
        // differently-worded copies of "required" under the same box.
        var state = Form(new BookingRequest { Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 1, 2) });

        await state.ValidateAsync();

        Assert.ContainsSingle(state.MessagesFor("Reference"));
    }

    [TestMethod]
    public async Task Opting_in_enforces_the_property_attributes_for_a_schema_that_never_saw_them()
    {
        // A JSON-imported schema rendered over a typed model has no generated rules at all, so the
        // model's own constraints have to come from somewhere.
        var form = BormBuilder.Create().Text("Reference").Build();
        var state = new BormState(form, new BormModelDataAccessor(new BookingRequest()))
            .UseDataAnnotations(includePropertyAttributes: true);

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("Reference"));
    }

    [TestMethod]
    public async Task A_model_with_no_IValidatableObject_is_simply_quiet()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.FirstName)
            .Field(x => x.Email)
            .Build();
        var state = new BormState(form, new BormModelDataAccessor(new RegistrationModel
        {
            FirstName = "Ada",
            Email = "ada@example.com"
        })).UseDataAnnotations();

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void A_null_state_is_rejected()
        => Assert.ThrowsExactly<ArgumentNullException>(() => ((BormState)null!).UseDataAnnotations());
}

/// <summary>
/// <c>[Phone]</c> chose the telephone keyboard and nothing more: "not a phone!!" was saved as a phone number.
/// </summary>
[TestClass]
public class PhoneAnnotationTests
{
    private sealed class Contact
    {
        [Phone] public string? Phone { get; set; }
    }

    [TestMethod]
    [DataRow("not a phone!!", false)]
    [DataRow("+31 (6) 1234-5678", true)]
    [DataRow("555.123.4567 ext. 89", true)]
    [DataRow("", true)] // nothing given is Required()'s business, as it is for every other rule
    public async Task A_Phone_annotation_is_judged_as_the_attribute_judges_it(string value, bool valid)
    {
        var model = new Contact { Phone = value };
        var state = new BormState(BormSchemaGenerator.Generate<Contact>(), new BormModelDataAccessor(model));

        Assert.AreEqual(valid, await state.ValidateAsync());
    }
}
