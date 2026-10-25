using Bunit;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Borm.Tests;

/// <summary>
/// The engine has to agree with itself about what changed and what is still true: a default is the
/// schema's, not the form's to edit; a change handler sees the form settled; replacing a container is a
/// change to everything inside it; and a verdict still true is not erased by an unrelated edit.
/// </summary>
[TestClass]
public class StateConsistencyRegressionTests : ComponentTestBase
{
    private static BormOptionsProvider CountryEcho => ctx =>
        new ValueTask<IReadOnlyList<BormSelectOption>>(
            (IReadOnlyList<BormSelectOption>)[
                new BormSelectOption(ctx.Value("Country") as string ?? "none", "City")]);

    // ---------------------------------------------------------------- defaults

    [TestMethod]
    public void A_collection_default_is_copied_rather_than_edited_in_place()
    {
        var form = BormBuilder.Create()
            .Array("Lines", i => i.Text("Name"), f => f.Default(new List<object?>()))
            .Build();

        var first = new BormState(form, new BormDictionaryDataAccessor());
        first.AddArrayItem(form.FindByPath("Lines")!, "Lines");
        first.SetValue("Lines[0].Name", "Widget");

        // The next form built from the same schema opens empty, and the schema still says "empty".
        var second = new BormState(form, new BormDictionaryDataAccessor());
        Assert.AreEqual(0, second.ArrayCount("Lines"));
        Assert.AreEqual(0, ((List<object?>)form.FindByPath("Lines")!.DefaultValue!).Count);
    }

    [TestMethod]
    public void Rows_do_not_share_a_nested_list_default()
    {
        var form = BormBuilder.Create()
            .Array("Rows", r => r.Array("Tags", t => t.Text("Value"), f => f.Default(new List<object?>())))
            .Build();
        var rows = form.FindByPath("Rows")!;

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.AddArrayItem(rows, "Rows");
        state.AddArrayItem(rows, "Rows");
        state.AddArrayItem(rows.ItemTemplate!.Children.Single(), "Rows[0].Tags");

        Assert.AreEqual(1, state.ArrayCount("Rows[0].Tags"));
        Assert.AreEqual(0, state.ArrayCount("Rows[1].Tags"));
    }

    [TestMethod]
    public void A_json_schema_array_default_is_not_shared_between_forms()
    {
        var form = BormJsonSchemaImporter.Import("""
            {
              "type": "object",
              "properties": {
                "lines": { "type": "array", "default": [], "items": { "type": "object", "properties": { "name": { "type": "string" } } } }
              }
            }
            """);

        var first = new BormState(form, new BormDictionaryDataAccessor());
        first.AddArrayItem(form.FindByPath("lines")!, "lines");

        Assert.AreEqual(0, new BormState(form, new BormDictionaryDataAccessor()).ArrayCount("lines"));
    }

    // ---------------------------------------------------------------- change handlers

    [TestMethod]
    public void A_change_handler_may_set_the_field_its_own_change_cascades_into()
    {
        var form = BormBuilder.Create()
            .Text("Country", f => f.OnChange(ctx => ctx.SetSibling("City", ctx.Value as string == "France" ? "Paris" : null)))
            .Select("City", f => f.OptionsFrom(CountryEcho, "Country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Country", "France");

        Assert.AreEqual("Paris", state.GetValue("City"));
    }

    [TestMethod]
    public void A_change_handler_reads_totals_already_recomputed()
    {
        object? seen = null;
        var form = BormBuilder.Create()
            .Number("Quantity", f => f.OnChange(ctx => seen = ctx.Sibling("Total")))
            .Number("Total", f => f.ReadOnly().Computed(ctx => Convert.ToDouble(ctx.Sibling("Quantity") ?? 0d) * 2, "Quantity"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Quantity", 3d);

        Assert.AreEqual(6d, seen);
    }

    // ---------------------------------------------------------------- containers

    [TestMethod]
    public async Task Resetting_a_container_drops_options_loaded_for_a_dependency_inside_it()
    {
        var form = BormBuilder.Create()
            .Object("Address", a => a
                .Text("Country")
                .Select("City", f => f.OptionsFrom(CountryEcho, "Country")))
            .Build();
        var city = form.FindByPath("Address.City")!;

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Address.Country", "PT");
        await state.EnsureOptionsAsync(city, "Address.City");
        Assert.AreEqual("PT", state.OptionsFor(city, "Address.City").Single().Value);

        state.ResetField("Address");
        await state.EnsureOptionsAsync(city, "Address.City");

        Assert.AreEqual("none", state.OptionsFor(city, "Address.City").Single().Value);
    }

    [TestMethod]
    public void Replacing_a_container_keeps_the_answer_that_came_with_it()
    {
        var form = BormBuilder.Create()
            .Object("Address", a => a
                .Text("Country")
                .Select("City", f => f.OptionsFrom(CountryEcho, "Country")))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var saved = new Dictionary<string, object?> { ["Country"] = "ES", ["City"] = "Madrid" };
        state.SetValue("Address", saved);

        // A prefilled address brings the city it was saved with; it was chosen for this country.
        Assert.AreEqual("Madrid", state.GetValue("Address.City"));
    }

    [TestMethod]
    public void Replacing_a_container_clears_an_answer_outside_it_that_cascades_off_it()
    {
        var form = BormBuilder.Create()
            .Object("Address", a => a.Text("Country"))
            .Select("City", f => f.OptionsFrom(CountryEcho, "Address.Country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Address.Country", "PT");
        state.SetValue("City", "Lisbon");

        state.SetValue("Address", new Dictionary<string, object?> { ["Country"] = "ES" });

        Assert.IsNull(state.GetValue("City"));
    }

    [TestMethod]
    public void Hiding_a_group_recomputes_a_total_that_reads_inside_it()
    {
        var form = BormBuilder.Create()
            .Checkbox("Ship")
            .Object("Shipping", s => s.Text("Country"),
                f => f.VisibleWhen("Ship", BormConditionOperator.IsTrue).ClearOnHide())
            .Number("Cost", f => f.ReadOnly()
                .Computed(ctx => ctx.Value("Shipping.Country") is string ? 5d : 0d, "Shipping.Country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Ship", true);
        state.SetValue("Shipping.Country", "PT");
        Assert.AreEqual(5d, state.GetValue("Cost"));

        state.SetValue("Ship", false);

        Assert.IsNull(state.GetValue("Shipping"));
        Assert.AreEqual(0d, state.GetValue("Cost"));
    }

    // ---------------------------------------------------------------- computed values

    [TestMethod]
    public void A_typed_computed_field_reached_through_a_nested_member_receives_the_model()
    {
        var form = new BormBuilder<RcOrder>()
            .Field(x => x.Price)
            .Field(x => x.Quantity)
            .Computed(x => x.Summary.Total, o => o.Price * o.Quantity)
            .Build();

        var model = new RcOrder();
        var state = new BormState(form, new BormModelDataAccessor(model));
        state.SetValue("Price", 2.5m);
        state.SetValue("Quantity", 4);

        Assert.AreEqual(10m, model.Summary.Total);
    }

    [TestMethod]
    public void A_formula_returning_another_numeric_type_is_not_a_change()
    {
        var form = new BormBuilder<RcTotals>()
            .Field(x => x.Quantity)
            .Field(x => x.Total, f => f.ReadOnly().Computed(_ => 5, "Quantity"))
            .Field(x => x.Pick, f => f.OptionsFrom(CountryEcho, "Total"))
            .Build();

        var model = new RcTotals { Total = 5m, Pick = "A" };
        var state = new BormState(form, new BormModelDataAccessor(model));
        var changed = new List<string>();
        state.FieldChanged += changed.Add;

        state.SetValue("Quantity", 2);

        // 5 and 5m are one number: the prefilled answer survives construction and the edit.
        Assert.AreEqual("A", model.Pick);
        Assert.DoesNotContain("Total", changed);
    }

    // ---------------------------------------------------------------- validation

    [TestMethod]
    public async Task A_rule_timing_out_fails_the_validation_instead_of_passing_silently_as_superseded()
    {
        var form = BormBuilder.Create()
            .Text("Name", f => f.MustAsync(_ => throw new TaskCanceledException("timed out"), "Taken"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await state.ValidateAsync());
        Assert.IsFalse(state.IsValidating);
    }

    [TestMethod]
    public void Text_an_untyped_integer_field_cannot_read_is_refused_and_reported()
    {
        var form = BormBuilder.Create().Integer("qty").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("qty", BormValueConverter.FromInputString("1.5", null, BormFieldType.Integer));

        Assert.IsNull(state.GetValue("qty"));
        Assert.IsTrue(state.HasErrors);
        Assert.HasCount(1, state.MessagesFor("qty"));

        state.SetValue("qty", BormValueConverter.FromInputString("3", null, BormFieldType.Integer));

        Assert.AreEqual(3L, state.GetValue("qty"));
        Assert.IsFalse(state.HasErrors);
    }

    [TestMethod]
    public void Two_pieces_of_text_compare_as_text_even_when_both_look_numeric()
    {
        Assert.IsFalse(BormConditionEvaluator.Compare("01234", BormConditionOperator.Equals, "1234"));
        Assert.IsFalse(BormConditionEvaluator.Compare("007", BormConditionOperator.In, new[] { "7" }));
        Assert.IsFalse(BormConditionEvaluator.Compare("7.0", BormConditionOperator.Equals, "7"));
        Assert.IsTrue(BormConditionEvaluator.Compare("ABC", BormConditionOperator.Equals, "abc"));

        // A real number against text is still compared as a number.
        Assert.IsTrue(BormConditionEvaluator.Compare(7, BormConditionOperator.Equals, "7.0"));
        Assert.IsTrue(BormConditionEvaluator.Compare(7L, BormConditionOperator.In, new[] { "7" }));
    }

    [TestMethod]
    public void A_group_holding_a_predicate_depends_on_everything()
    {
        var group = BormConditionGroup.Any(
            new BormPredicateCondition(d => d.GetValue("Age") is long age && age < 18),
            new BormFieldCondition("Guardian", BormConditionOperator.IsNotEmpty));
        Assert.IsFalse(group.Dependencies.Any());

        var form = BormBuilder.Create()
            .Integer("Age")
            .Text("Guardian")
            .Text("Note", f => f.VisibleWhen(group).ClearOnHide())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Age", 10L);
        state.SetValue("Note", "Needs a signature");

        state.SetValue("Age", 30L);

        Assert.IsNull(state.GetValue("Note"));
    }

    [TestMethod]
    public void A_group_holding_a_predicate_still_has_its_field_paths_checked()
    {
        var form = BormBuilder.Create()
            .Text("Note", f => f.VisibleWhen(BormConditionGroup.Any(
                new BormPredicateCondition(_ => true),
                new BormFieldCondition("Missing", BormConditionOperator.IsNotEmpty))))
            .Build();

        Assert.IsTrue(form.Validate().Any(d => d.Message.Contains("Missing", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Adding_a_row_keeps_a_uniqueness_error_that_is_still_true()
    {
        var form = BormBuilder.Create()
            .ArrayOf("Tags", BormFieldType.Text, f => f.UniqueItems("Tags must be unique"))
            .Build();
        var tags = form.FindByPath("Tags")!;

        var state = new BormState(form, new BormDictionaryDataAccessor(
            new Dictionary<string, object?> { ["Tags"] = new List<object?> { "a", "a", "b" } }));
        await state.ValidateAsync();
        Assert.HasCount(1, state.MessagesFor("Tags"));

        state.AddArrayItem(tags, "Tags");
        Assert.HasCount(1, state.MessagesFor("Tags"));

        // Removing a row that was not one of the duplicates changes nothing either…
        state.RemoveArrayItem("Tags", 2);
        Assert.HasCount(1, state.MessagesFor("Tags"));

        // …and removing a duplicate clears it.
        state.RemoveArrayItem("Tags", 1);
        Assert.IsEmpty(state.MessagesFor("Tags"));
    }

    [TestMethod]
    public async Task Revalidating_a_container_keeps_the_rejected_entries_of_its_children()
    {
        var form = new BormBuilder<RcPerson>()
            .Object(x => x.Address, a => a.Field(x => x.Zip))
            .Build();

        var state = new BormState(form, new BormModelDataAccessor(new RcPerson()));
        state.SetValue("Address.Zip", "abc");
        Assert.HasCount(1, state.MessagesFor("Address.Zip"));

        await state.ValidateFieldAsync("Address");

        Assert.HasCount(1, state.MessagesFor("Address.Zip"));
        Assert.IsTrue(state.HasErrors);
    }

    // ---------------------------------------------------------------- generation

    [TestMethod]
    public void A_list_of_objects_past_max_depth_is_left_off_rather_than_generated_without_a_template()
    {
        var form = BormSchemaGenerator.Generate<RcDeep>(new BormSchemaGeneratorOptions { MaxDepth = 1 });

        Assert.IsTrue(form.AllFields().Where(f => f.Type == BormFieldType.Array).All(f => f.ItemTemplate is not null));
        var inner = form.FindByPath("Inner")!;
        Assert.IsNull(inner.Children.FirstOrDefault(c => c.Name == "Items"));
        Assert.IsNotNull(inner.Children.FirstOrDefault(c => c.Name == "Tags")?.ItemTemplate);
    }

    // ---------------------------------------------------------------- submit

    [TestMethod]
    public async Task A_second_submit_while_one_is_in_flight_does_not_act_on_stale_messages()
    {
        var form = BormBuilder.Create()
            .Text("a")
            .Text("b")
            .Step("one", s => s.Fields("a"))
            .Step("two", s => s.Fields("b"))
            .Build();

        var saving = new TaskCompletionSource();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.OnValidSubmit, async (BormState s) =>
            {
                // Something reported against step one while the save is still running.
                s.SetServerError("a", "Taken");
                await saving.Task;
            }));

        var state = cut.Instance.Form;
        await cut.InvokeAsync(async () => await state.NextStepAsync());
        Assert.AreEqual(1, state.CurrentStepIndex);

        Task<bool> first = null!;
        await cut.InvokeAsync(() => { first = cut.Instance.SubmitAsync(); });
        var second = await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        Assert.IsFalse(second);
        Assert.AreEqual(1, state.CurrentStepIndex);

        saving.SetResult();
        // The handler recorded an error against step one while it ran, which is a submit that did not go
        // through: it reports false and says the server rejected it, rather than claiming success.
        Assert.IsFalse(await first);
        Assert.AreEqual(BormSubmitOutcome.Rejected, state.SubmitOutcome);
    }

    // ---------------------------------------------------------------- files

    [TestMethod]
    public async Task A_size_message_survives_a_later_accept_and_is_not_used_for_the_wrong_type()
    {
        var sizeThenType = BormBuilder.Create()
            .File("Doc", f => f.MaxFileSize(5_000_000, "Max 5 MB").Accept(".pdf"))
            .Build();
        Assert.AreEqual("Max 5 MB", await FirstMessage(sizeThenType, new RcFile("big.pdf", 10_000_000)));

        var typeThenSize = BormBuilder.Create()
            .File("Doc", f => f.Accept(".pdf").MaxFileSize(5_000_000, "Max 5 MB"))
            .Build();
        var wrongType = await FirstMessage(typeThenSize, new RcFile("small.docx", 10_000));
        Assert.IsNotNull(wrongType);
        Assert.AreNotEqual("Max 5 MB", wrongType);
        Assert.AreEqual("Max 5 MB", await FirstMessage(typeThenSize, new RcFile("big.pdf", 10_000_000)));

        static async Task<string?> FirstMessage(BormDefinition form, IBrowserFile file)
        {
            var state = new BormState(form, new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["Doc"] = file }));
            await state.ValidateAsync();
            return state.MessagesFor("Doc").FirstOrDefault()?.Message;
        }
    }
}

public sealed class RcOrder
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public RcSummary Summary { get; set; } = new();
}

public sealed class RcSummary
{
    public decimal Total { get; set; }
}

public sealed class RcTotals
{
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public string? Pick { get; set; }
}

public sealed class RcPerson
{
    public RcAddress Address { get; set; } = new();
}

public sealed class RcAddress
{
    public int Zip { get; set; }
}

public sealed class RcDeep
{
    public RcInner Inner { get; set; } = new();
}

public sealed class RcInner
{
    public List<RcItem> Items { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}

public sealed class RcItem
{
    public string? Name { get; set; }
}

public sealed class RcFile(string name, long size) : IBrowserFile
{
    public string Name => name;
    public DateTimeOffset LastModified => default;
    public long Size => size;
    public string ContentType => "application/octet-stream";
    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => new MemoryStream();
}
