using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A combobox is a select you can type into. It is built on the browser's own
/// <c>&lt;input list&gt;</c>, so filtering, keyboard navigation and screen-reader support come from
/// the platform; what the library adds is the mapping between the label the user reads and the value
/// the model stores, which is the only thing a bare datalist cannot do.
/// </summary>
[TestClass]
public class ComboboxRenderingTests : ComponentTestBase
{
    private static BormDefinition Schema(bool allowCustom = false) => BormBuilder.Create()
        .Combobox("country", f => f
            .Label("Country")
            .Options(("fr", "France"), ("gb", "United Kingdom"), ("de", "Germany"))
            .AsCombobox(allowCustom))
        .Build();

    private Bunit.IRenderedComponent<BormView> RenderForm(BormDefinition form, BormState? state = null)
        => state is null
            ? Render<BormView>(p => p.Add(x => x.Definition, form))
            : Render<BormView>(p => p.Add(x => x.State, state));

    [TestMethod]
    public void It_renders_an_input_bound_to_a_datalist_of_the_options()
    {
        var cut = RenderForm(Schema());
        var input = cut.Find("input#borm_country");

        Assert.AreEqual("borm_country_list", input.GetAttribute("list"));
        Assert.AreEqual("list", input.GetAttribute("aria-autocomplete"));
        Assert.AreEqual(3, cut.FindAll("#borm_country_list option").Count);
    }

    [TestMethod]
    public void It_leaves_the_combobox_role_to_the_browser_rather_than_lying_about_the_popup()
    {
        // An <input list> is already a combobox in the accessibility tree. Declaring the role by hand
        // takes on aria-expanded's contract — and the popup's open/closed state is the browser's alone,
        // so the only value that could be written is a permanent "false" told to a screen-reader user
        // at the exact moment the list is open.
        var input = RenderForm(Schema()).Find("input#borm_country");

        Assert.IsNull(input.GetAttribute("role"));
        Assert.IsNull(input.GetAttribute("aria-expanded"));
    }

    [TestMethod]
    public void The_datalist_offers_labels_because_that_is_what_the_browser_puts_in_the_box()
    {
        var cut = RenderForm(Schema());

        var values = cut.FindAll("#borm_country_list option").Select(o => o.GetAttribute("value")).ToList();

        Assert.AreSequenceEqual(["France", "United Kingdom", "Germany"], values);
    }

    [TestMethod]
    public void Choosing_a_label_stores_the_options_value()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = RenderForm(Schema(), state);

        cut.Find("input#borm_country").Change("United Kingdom");

        Assert.AreEqual("gb", state.GetValue("country"));
    }

    [TestMethod]
    public void The_box_shows_the_label_of_whatever_value_the_model_holds()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        state.SetValue("country", "de");

        var cut = RenderForm(Schema(), state);

        Assert.AreEqual("Germany", cut.Find("input#borm_country").GetAttribute("value"));
    }

    [TestMethod]
    public void Matching_a_label_ignores_case_because_the_user_is_typing_it()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = RenderForm(Schema(), state);

        cut.Find("input#borm_country").Change("france");

        Assert.AreEqual("fr", state.GetValue("country"));
    }

    [TestMethod]
    public void An_entry_on_no_list_is_kept_rather_than_discarded()
    {
        // The same promise the rest of the library makes: what the user typed is never thrown away
        // without saying so. The rule below is what says so.
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        var cut = RenderForm(Schema(), state);

        cut.Find("input#borm_country").Change("Atlantis");

        Assert.AreEqual("Atlantis", state.GetValue("country"));
        Assert.AreEqual("Atlantis", cut.Find("input#borm_country").GetAttribute("value"));
    }

    [TestMethod]
    public void Clearing_the_box_clears_the_value()
    {
        var state = new BormState(Schema(), new BormDictionaryDataAccessor());
        state.SetValue("country", "fr");
        var cut = RenderForm(Schema(), state);

        cut.Find("input#borm_country").Change("");

        Assert.IsNull(state.GetValue("country"));
    }

    [TestMethod]
    public void A_disabled_option_is_not_offered_at_all()
    {
        // A datalist has no disabled state, so offering a choice the field will then reject is worse
        // than leaving it out.
        var form = BormBuilder.Create()
            .Combobox("x", f => f.Options(
                new BormSelectOption("a", "Available"),
                new BormSelectOption("b", "Sold out", Disabled: true)))
            .Build();

        Assert.ContainsSingle(RenderForm(form).FindAll("#borm_x_list option"));
    }
}

[TestClass]
public class ComboboxValidationTests
{
    private static BormDefinition Closed() => BormBuilder.Create()
        .Combobox("country", f => f.Options(("fr", "France"), ("gb", "United Kingdom")).AsCombobox())
        .Build();

    [TestMethod]
    public async Task An_answer_that_is_on_no_list_is_reported()
    {
        var state = new BormState(Closed(), new BormDictionaryDataAccessor());
        state.SetValue("country", "Atlantis");

        Assert.IsFalse(await state.ValidateAsync());
        Assert.IsNotEmpty(state.MessagesFor("country"));
    }

    [TestMethod]
    public async Task An_answer_on_the_list_passes()
    {
        var state = new BormState(Closed(), new BormDictionaryDataAccessor());
        state.SetValue("country", "gb");

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task An_empty_answer_is_Requireds_business_not_this_rules()
    {
        var state = new BormState(Closed(), new BormDictionaryDataAccessor());

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task AllowCustom_accepts_anything()
    {
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("ams", "Amsterdam")).AsCombobox(allowCustom: true))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("city", "Delft");

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task AllowCustom_removes_a_rule_an_earlier_call_added()
    {
        // The builder is a chain, and .AsCombobox() then .AsCombobox(true) has to mean the second one.
        var form = BormBuilder.Create()
            .Combobox("city", f => f.Options(("ams", "Amsterdam")).AsCombobox().AsCombobox(allowCustom: true))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("city", "Delft");

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public async Task Options_that_arrive_from_a_provider_are_not_judged_here()
    {
        // They live in the form's runtime state, not in the schema, so at rule time there is nothing to
        // compare against — and failing every answer would be worse than checking none.
        var form = BormBuilder.Create()
            .Combobox("city", f => f
                .OptionsFrom(_ => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>(
                    [new BormSelectOption("ams", "Amsterdam")]))
                .AsCombobox())
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("city", "anything at all");

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void A_combobox_counts_as_a_choice_field_so_options_do_not_turn_it_into_a_dropdown()
    {
        var form = BormBuilder.Create()
            .Combobox("x", f => f.AsCombobox().Options(("a", "A")))
            .Build();

        Assert.AreEqual(BormFieldType.Combobox, form.FindField("x")!.Type);
    }
}

[TestClass]
public class TagsInputTests : ComponentTestBase
{
    private static BormDefinition Schema(int? max = null) => BormBuilder.Create()
        .Tags("labels", f => f.Label("Labels").AsTags(max))
        .Build();

    private (Bunit.IRenderedComponent<BormView> Cut, BormState State) RenderForm(int? max = null)
    {
        var state = new BormState(Schema(max), new BormDictionaryDataAccessor());
        return (Render<BormView>(p => p.Add(x => x.State, state)), state);
    }

    private static List<string> TagsOf(BormState state)
        => state.GetValue("labels") is IEnumerable<object?> items
            ? items.Select(i => i?.ToString() ?? "").ToList()
            : [];

    [TestMethod]
    public void Enter_commits_a_tag()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("urgent");
        cut.Find(".bit-borm-tags__entry").KeyDown("Enter");

        Assert.AreSequenceEqual(["urgent"], TagsOf(state));
    }

    [TestMethod]
    public void The_entry_box_belongs_to_no_form_so_Enter_cannot_submit_one()
    {
        // Enter is how every tag input is used. Without detaching the box from its form owner, the
        // first tag a user typed would post the form instead of being added.
        var (cut, _) = RenderForm();

        Assert.AreEqual("borm_labels_none", cut.Find(".bit-borm-tags__entry").GetAttribute("form"));
    }

    [TestMethod]
    public void A_separator_commits_the_tag_before_it()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("urgent,");

        Assert.AreSequenceEqual(["urgent"], TagsOf(state));
    }

    [TestMethod]
    public void Pasting_a_separated_list_becomes_several_tags_not_one()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("red,green,blue");

        // The trailing fragment stays in the box until it is committed in its own right.
        Assert.AreSequenceEqual(["red", "green"], TagsOf(state));
    }

    [TestMethod]
    public void A_duplicate_is_refused_whatever_its_casing()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("Urgent");
        cut.Find(".bit-borm-tags__entry").KeyDown("Enter");
        cut.Find(".bit-borm-tags__entry").Input("urgent");
        cut.Find(".bit-borm-tags__entry").KeyDown("Enter");

        Assert.AreSequenceEqual(["Urgent"], TagsOf(state));
    }

    [TestMethod]
    public void Whitespace_only_entries_are_ignored()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("   ");
        cut.Find(".bit-borm-tags__entry").KeyDown("Enter");

        Assert.IsEmpty(TagsOf(state));
    }

    [TestMethod]
    public void Each_tag_is_a_chip_with_a_remove_button_that_names_it()
    {
        var (cut, state) = RenderForm();
        state.SetValue("labels", new List<string> { "urgent", "bug" });
        cut.Render();

        var buttons = cut.FindAll(".bit-borm-tag__remove");

        Assert.AreEqual(2, buttons.Count);
        Assert.AreEqual("Remove urgent", buttons[0].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void Removing_a_chip_removes_the_tag()
    {
        var (cut, state) = RenderForm();
        state.SetValue("labels", new List<string> { "urgent", "bug" });
        cut.Render();

        cut.FindAll(".bit-borm-tag__remove")[0].Click();

        Assert.AreSequenceEqual(["bug"], TagsOf(state));
    }

    [TestMethod]
    public void Backspace_on_an_empty_box_removes_the_last_tag()
    {
        var (cut, state) = RenderForm();
        state.SetValue("labels", new List<string> { "urgent", "bug" });
        cut.Render();

        cut.Find(".bit-borm-tags__entry").KeyDown("Backspace");

        Assert.AreSequenceEqual(["urgent"], TagsOf(state));
    }

    [TestMethod]
    public void Backspace_with_something_typed_edits_the_text_instead()
    {
        var (cut, state) = RenderForm();
        state.SetValue("labels", new List<string> { "urgent" });
        cut.Render();

        cut.Find(".bit-borm-tags__entry").Input("bu");
        cut.Find(".bit-borm-tags__entry").KeyDown("Backspace");

        Assert.AreSequenceEqual(["urgent"], TagsOf(state));
    }

    [TestMethod]
    public void Leaving_the_box_commits_what_was_half_typed_rather_than_losing_it()
    {
        var (cut, state) = RenderForm();

        cut.Find(".bit-borm-tags__entry").Input("urgent");
        cut.Find(".bit-borm-tags__entry").Blur();

        Assert.AreSequenceEqual(["urgent"], TagsOf(state));
    }

    [TestMethod]
    public void A_full_list_stops_accepting_and_stops_offering_the_box()
    {
        var (cut, state) = RenderForm(max: 2);
        state.SetValue("labels", new List<string> { "a", "b" });
        cut.Render();

        Assert.IsEmpty(cut.FindAll(".bit-borm-tags__entry"));
        Assert.AreSequenceEqual(["a", "b"], TagsOf(state));
    }

    [TestMethod]
    public void A_read_only_field_shows_its_tags_but_offers_no_way_to_change_them()
    {
        var (cut, state) = RenderForm();
        state.SetValue("labels", new List<string> { "urgent" });
        state.ReadOnly = true;
        cut.Render();

        Assert.ContainsSingle(cut.FindAll(".bit-borm-tag"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-tag__remove"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-tags__entry"));
    }

    [TestMethod]
    public void The_group_is_labelled_and_wired_for_assistive_tech()
    {
        var (cut, _) = RenderForm();
        var group = cut.Find(".bit-borm-tags");

        Assert.AreEqual("group", group.GetAttribute("role"));
        Assert.AreEqual("borm_labels_label", group.GetAttribute("aria-labelledby"));
    }
}

/// <summary>
/// The schema's own "and when this changes, do that" — what TanStack Form calls listeners and Formly
/// calls hooks. Neither a computed value (which owns its field outright) nor a cascading options
/// dependency (which reloads choices) can express it, because what the handler writes is still the
/// user's to edit afterwards.
/// </summary>
[TestClass]
public class ChangeHandlerTests
{
    [TestMethod]
    public void A_handler_runs_when_its_field_changes()
    {
        var form = BormBuilder.Create()
            .Text("country", f => f.OnChange(ctx => ctx.ClearSibling("city")))
            .Text("city")
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("city", "Paris");

        state.SetValue("country", "de");

        Assert.IsNull(state.GetValue("city"));
    }

    [TestMethod]
    public void A_handler_does_not_run_for_a_change_to_another_field()
    {
        var runs = 0;
        var form = BormBuilder.Create()
            .Text("a", f => f.OnChange(_ => runs++))
            .Text("b")
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("b", "x");

        Assert.AreEqual(0, runs);
    }

    [TestMethod]
    public void A_handler_is_silent_while_the_form_is_being_constructed()
    {
        // Applying a default is the form initialising, not the user changing something. Without this a
        // handler that clears a sibling would wipe the data the form was handed.
        var runs = 0;
        var form = BormBuilder.Create()
            .Text("a", f => f.Default("seed").OnChange(_ => runs++))
            .Build();

        _ = new BormState(form, new BormDictionaryDataAccessor());

        Assert.AreEqual(0, runs);
    }

    [TestMethod]
    public void What_a_handler_writes_does_not_mark_the_field_touched()
    {
        // The user has not been there, so the field must not open covered in errors.
        var form = BormBuilder.Create()
            .Text("plan", f => f.OnChange(ctx => ctx.SetSibling("seats", 5)))
            .Integer("seats", f => f.Required())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("plan", "team");

        Assert.AreEqual(5, state.GetValue("seats"));
        Assert.IsFalse(state.IsTouched("seats"));
    }

    [TestMethod]
    public void Inside_a_repeater_a_handler_means_that_row()
    {
        var form = BormBuilder.Create()
            .Array("rows", row => row
                .Text("country", f => f.OnChange(ctx => ctx.ClearSibling("city")))
                .Text("city"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var rows = form.FindField("rows")!;
        state.AddArrayItem(rows, "rows");
        state.AddArrayItem(rows, "rows");
        state.SetValue("rows[0].city", "Paris");
        state.SetValue("rows[1].city", "Berlin");

        state.SetValue("rows[0].country", "de");

        Assert.IsNull(state.GetValue("rows[0].city"));
        Assert.AreEqual("Berlin", state.GetValue("rows[1].city"));
    }

    [TestMethod]
    public void A_handler_can_read_the_new_value()
    {
        string? seen = null;
        var form = BormBuilder.Create()
            .Text("a", f => f.OnChange(ctx => seen = ctx.Value as string))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("a", "hello");

        Assert.AreEqual("hello", seen);
    }

    [TestMethod]
    public void Two_handlers_that_answer_each_other_settle_instead_of_recursing()
    {
        var form = BormBuilder.Create()
            .Integer("a", f => f.OnChange(ctx => ctx.SetSibling("b", Convert.ToInt32(ctx.Value ?? 0) + 1)))
            .Integer("b", f => f.OnChange(ctx => ctx.SetSibling("a", Convert.ToInt32(ctx.Value ?? 0) + 1)))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("a", 1);

        // No stack overflow, and both fields hold a number.
        Assert.IsNotNull(state.GetValue("a"));
        Assert.IsNotNull(state.GetValue("b"));
    }

    [TestMethod]
    public void A_handler_runs_for_a_computed_value_too()
    {
        // A derived value is a change like any other; the schema should not have one kind of change it
        // cannot react to.
        string? seen = null;
        var form = BormBuilder.Create()
            .Text("first")
            .Text("full", f => f
                .Computed(ctx => ctx.Sibling("first"), "first")
                .OnChange(ctx => seen = ctx.Value as string))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("first", "Ada");

        Assert.AreEqual("Ada", seen);
    }
}

[TestClass]
public class NewWidgetJsonRoundTripTests
{
    [TestMethod]
    public void A_combobox_survives_export_and_re_import()
    {
        var form = BormBuilder.Create()
            .Combobox("country", f => f.Options(("fr", "France")).AsCombobox())
            .Build();

        var reimported = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));

        Assert.AreEqual(BormFieldType.Combobox, reimported.FindField("country")!.Type);
    }

    [TestMethod]
    public void A_tags_field_survives_export_and_re_import()
    {
        var form = BormBuilder.Create().Tags("labels", f => f.AsTags(5)).Build();

        var reimported = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(form));
        var field = reimported.FindField("labels")!;

        Assert.AreEqual(BormFieldType.Tags, field.Type);
        Assert.AreEqual(5, field.MaxItems);
    }
}
