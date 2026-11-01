using FluentValidation;

namespace Bit.Borm.Tests;

public class TotalsModel
{
    public int Quantity { get; set; } = 2;
    public decimal UnitPrice { get; set; } = 5;
    public decimal Total { get; set; }
}

/// <summary>
/// Dirty tracking answers "has the user changed anything?", which is what an undo button, an unsaved-
/// changes prompt and a disabled save button all read. Anything the form does to itself must not count.
/// </summary>
[TestClass]
public class DirtyTrackingTests
{
    [TestMethod]
    public void A_form_with_computed_fields_starts_clean()
    {
        var form = BormBuilder.For<TotalsModel>()
            .Field(x => x.Quantity)
            .Field(x => x.UnitPrice)
            .Computed(x => x.Total, m => m.Quantity * m.UnitPrice,
                dependsOn: [nameof(TotalsModel.Quantity), nameof(TotalsModel.UnitPrice)])
            .Build();

        var model = new TotalsModel();
        var state = new BormState(form, new BormModelDataAccessor(model));

        // The total was seeded by the form, not typed by anyone.
        Assert.AreEqual(10m, model.Total);
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Editing_a_field_makes_the_form_dirty()
    {
        var form = BormBuilder.For<TotalsModel>().Field(x => x.Quantity).Build();
        var state = new BormState(form, new BormModelDataAccessor(new TotalsModel()));

        state.SetValue("Quantity", 9);

        Assert.IsTrue(state.IsFormDirty);
    }

    [TestMethod]
    public void Clearing_a_hidden_field_counts_as_a_change()
    {
        // The user did not type in the box, but the data they are about to submit is not what it was.
        var form = BormBuilder.For<SignupModel>()
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f
                .VisibleWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue)
                .ClearOnHide())
            .Build();

        var model = new SignupModel { IsBusiness = true, CompanyName = "Acme" };
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValueQuietly("IsBusiness", false);

        Assert.IsNull(model.CompanyName);
        Assert.IsTrue(state.IsDirty("CompanyName"));
    }
}

[TestClass]
public class ValidityReportingTests
{
    [TestMethod]
    public async Task IsValid_is_only_a_verdict_once_validation_has_run()
    {
        var form = BormBuilder.For<SignupModel>().Field(x => x.Email, f => f.Required()).Build();
        var state = new BormState(form, new BormModelDataAccessor(new SignupModel()));

        // No messages yet, so IsValid reports true even though Email is empty and required. That is
        // what HasValidated is for.
        Assert.IsTrue(state.IsValid);
        Assert.IsFalse(state.HasValidated);

        await state.ValidateAsync();

        Assert.IsTrue(state.HasValidated);
        Assert.IsFalse(state.IsValid);
    }

    [TestMethod]
    public async Task Reset_puts_the_verdict_back_to_unknown()
    {
        var form = BormBuilder.For<SignupModel>().Field(x => x.Email, f => f.Required()).Build();
        var state = new BormState(form, new BormModelDataAccessor(new SignupModel()));

        await state.ValidateAsync();
        state.Reset();

        Assert.IsFalse(state.HasValidated);
        Assert.IsTrue(state.IsValid);
    }
}

public class SeedMinItemsModel
{
    public List<LineItem> Lines { get; set; } = [];
}

[TestClass]
public class SeedMinItemsTests
{
    private static BormDefinition Schema(bool seed) => BormBuilder.For<SeedMinItemsModel>()
        .Array(x => x.Lines, line => line.Field(l => l.Product),
            f =>
            {
                f.Items(min: 1);
                if (seed) f.SeedMinItems();
            })
        .Build();

    [TestMethod]
    public void An_opted_in_repeater_opens_with_its_minimum_rows()
    {
        var model = new SeedMinItemsModel();
        var state = new BormState(Schema(seed: true), new BormModelDataAccessor(model));

        Assert.ContainsSingle(model.Lines);
        // Seeding is the form setting itself up, not the user adding a row.
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Without_opting_in_an_empty_list_stays_empty()
    {
        // On an edit form, "this record has no lines" can be the truth.
        var model = new SeedMinItemsModel();
        _ = new BormState(Schema(seed: false), new BormModelDataAccessor(model));

        Assert.IsEmpty(model.Lines);
    }

    [TestMethod]
    public void Rows_that_already_exist_are_not_topped_up_past_the_minimum()
    {
        var model = new SeedMinItemsModel { Lines = { new LineItem { Product = "Widget" } } };
        _ = new BormState(Schema(seed: true), new BormModelDataAccessor(model));

        Assert.ContainsSingle(model.Lines);
        Assert.AreEqual("Widget", model.Lines[0].Product);
    }

    [TestMethod]
    public void An_imported_minimum_of_millions_seeds_no_more_rows_than_a_draft_may_restore()
    {
        const string json = """
            {"type":"object","properties":{
              "rows":{"type":"array","minItems":100000000,"x-attributes":{"seedMinItems":true},"items":{"type":"string"}},
              "more":{"type":"array","minItems":100000000,"x-attributes":{"seedMinItems":true},"items":{"type":"string"}}}}
            """;
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        // The lists share the limit rather than each taking it.
        Assert.AreEqual(BormDraft.MaxRows, state.ArrayCount("rows") + state.ArrayCount("more"));
        Assert.IsFalse(state.IsFormDirty);
        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(5), $"took {clock.Elapsed}");
    }

    [TestMethod]
    public void Seeding_again_after_a_definition_change_tops_the_lists_up_rather_than_adding_as_many_rows_again()
    {
        const string json = """{"type":"object","properties":{"rows":{"type":"array","minItems":100000000,"x-attributes":{"seedMinItems":true},"items":{"type":"string"}}}}""";
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.NotifyDefinitionChanged();
        state.NotifyDefinitionChanged();

        Assert.AreEqual(BormDraft.MaxRows, state.ArrayCount("rows"));
    }

    [TestMethod]
    public void Rows_a_sweep_clears_as_soon_as_they_are_seeded_still_count_against_the_seeding_budget()
    {
        // The first list hides, and so clears, itself once it has rows. Counted by what survived, it cost
        // nothing and the next list was seeded as many rows again — five thousand such lists, fifty million.
        const string json = """
            {"type":"object","properties":{
              "a":{"type":"array","minItems":100000000,"x-attributes":{"seedMinItems":true},"x-clearOnHide":true,"x-visibleWhen":{"field":"a","op":"IsEmpty"},"items":{"type":"string"}},
              "b":{"type":"array","minItems":100000000,"x-attributes":{"seedMinItems":true},"items":{"type":"string"}}}}
            """;
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);

        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.AreEqual(0, state.ArrayCount("a"));
        Assert.AreEqual(0, state.ArrayCount("b"));
    }

    [TestMethod]
    public void A_list_that_takes_no_seeded_row_announces_no_change()
    {
        const string json = """{"type":"object","properties":{"Tags":{"type":"array","minItems":1,"x-attributes":{"seedMinItems":true},"items":{"type":"string"}}}}""";
        Assert.IsTrue(BormJsonSchemaImporter.TryImport(json, out var form, out var error), error);
        var model = new SeedFixedModel();
        var state = new BormState(form, new BormModelDataAccessor(model));
        var changed = new List<string>();
        state.FieldChanged += changed.Add;

        state.NotifyDefinitionChanged();

        Assert.IsEmpty(model.Tags);
        Assert.DoesNotContain("Tags", changed);
    }
}

/// <summary>A list the form cannot add to: the array is fixed in size and the property has no setter.</summary>
public class SeedFixedModel
{
    public string[] Tags { get; } = [];
}

public sealed class SignupFluentValidator : AbstractValidator<SignupModel>
{
    public SignupFluentValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        // Deliberately unconditional: the validator knows nothing about the form's visibility rules.
        RuleFor(x => x.CompanyName).NotEmpty().WithMessage("Company name is required.");
    }
}

[TestClass]
public class ExternalValidatorVisibilityTests
{
    private static BormState Form(SignupModel model)
    {
        var schema = BormBuilder.For<SignupModel>()
            .Field(x => x.Email)
            .Field(x => x.IsBusiness)
            .Field(x => x.CompanyName, f => f
                .VisibleWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue))
            .Build();

        return new BormState(schema, new BormModelDataAccessor(model))
            .UseFluentValidation(new SignupFluentValidator());
    }

    [TestMethod]
    public async Task An_external_rule_on_a_hidden_field_does_not_block_the_form()
    {
        // FluentValidation sees the whole model and knows nothing about the form's conditions. Left
        // alone, its rule would refuse the submit and point at a control that is not on the page.
        var state = Form(new SignupModel { Email = "a@b.c" });

        await state.ValidateAsync();

        Assert.IsEmpty(state.MessagesFor("CompanyName"));
        Assert.IsTrue(state.IsValid);
    }

    [TestMethod]
    public async Task The_same_rule_applies_once_the_field_is_shown()
    {
        var state = Form(new SignupModel { Email = "a@b.c", IsBusiness = true });

        await state.ValidateAsync();

        Assert.ContainsSingle(state.MessagesFor("CompanyName"));
    }
}
