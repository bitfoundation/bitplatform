using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// Undo and redo: a change is everything one answer set off, taken back together and made again together,
/// with rows that keep their identity and fields judged again as they are put back.
/// </summary>
[TestClass]
public class HistoryTests : ComponentTestBase
{
    private static (BormState State, Dictionary<string, object?> Data) Form(Action<BormBuilder>? configure = null, int limit = 50)
    {
        var builder = BormBuilder.Create().Text("name", f => f.Required()).Text("city");
        configure?.Invoke(builder);
        var data = new Dictionary<string, object?>();
        var state = new BormState(builder.Build(), new BormDictionaryDataAccessor(data)) { HistoryLimit = limit };
        return (state, data);
    }

    private static async Task Later(BormState state, Action change)
    {
        // Two writes to one field within a second are one change; the tests that mean two wait them apart.
        await Task.Delay(1100);
        change();
    }

    [TestMethod]
    public async Task No_history_is_kept_until_it_is_asked_for()
    {
        var (state, data) = Form(limit: 0);
        state.SetValue("name", "Ada");

        Assert.IsFalse(state.CanUndo);
        Assert.IsFalse(await state.UndoAsync());
        Assert.AreEqual("Ada", data["name"]);
    }

    [TestMethod]
    public async Task Changes_are_taken_back_newest_first_and_made_again_in_order()
    {
        var (state, data) = Form();
        state.SetValue("name", "Ada");
        state.SetValue("city", "London");
        Assert.AreEqual("city", state.NextUndoPath);

        Assert.IsTrue(await state.UndoAsync());
        Assert.IsNull(data["city"]);
        Assert.AreEqual("Ada", data["name"]);
        Assert.IsTrue(await state.UndoAsync());
        Assert.IsNull(data["name"]);
        Assert.IsFalse(state.CanUndo);

        Assert.IsTrue(await state.RedoAsync());
        Assert.AreEqual("Ada", data["name"]);
        Assert.AreEqual("city", state.NextRedoPath);

        // A new change after an undo starts a new future: what was taken back cannot be made again.
        state.SetValue("city", "Paris");
        Assert.IsFalse(state.CanRedo);
    }

    [TestMethod]
    public async Task One_field_written_in_quick_succession_or_tidied_on_the_way_out_is_one_change()
    {
        var (state, data) = Form(b => b.Text("code", f => f.Trim()));
        state.SetValue("code", "a");
        state.SetValue("code", "ab");
        state.SetValue("code", " abc ");
        state.NormalizeField(state.Definition.FindField("code")!, "code");
        Assert.AreEqual("abc", data["code"]);

        await state.UndoAsync();
        Assert.IsNull(data["code"]);
        Assert.IsFalse(state.CanUndo);

        // Typed and typed back to where it was: there is nothing to undo.
        state.SetValue("name", "x");
        state.SetValue("name", null);
        Assert.IsFalse(state.CanUndo);
    }

    [TestMethod]
    public async Task What_a_change_handler_did_is_undone_with_the_change_and_the_handler_is_not_run_again()
    {
        var runs = 0;
        var (state, data) = Form(b => b
            .Text("country", f => f.OnChange(ctx =>
            {
                runs++;
                ctx.SetSibling("city", ctx.Value is "fr" ? "Paris" : null);
            })));
        state.SetValue("city", "Berlin");
        await Later(state, () => state.SetValue("country", "fr"));
        Assert.AreEqual("Paris", data["city"]);
        Assert.AreEqual(1, runs);

        await state.UndoAsync();
        Assert.IsNull(data["country"]);
        Assert.AreEqual("Berlin", data["city"]);

        await state.RedoAsync();
        Assert.AreEqual("fr", data["country"]);
        Assert.AreEqual("Paris", data["city"]);
        Assert.AreEqual(1, runs);
    }

    [TestMethod]
    public async Task Undoing_the_answer_that_hid_a_field_shows_it_again_with_its_answer()
    {
        var (state, data) = Form(b => b
            .Checkbox("company")
            .Text("vat", f => f.VisibleWhen("company", BormConditionOperator.IsTrue).ClearOnHide()));
        state.SetValue("company", true);
        state.SetValue("vat", "NL123");
        await Later(state, () => state.SetValue("company", false));
        Assert.IsNull(data["vat"]);

        await state.UndoAsync();

        Assert.AreEqual(true, data["company"]);
        Assert.AreEqual("NL123", data["vat"]);
    }

    [TestMethod]
    public async Task Rows_added_moved_and_removed_come_back_as_the_same_rows_with_their_totals()
    {
        var form = BormBuilder.For<ExpressionInvoice>()
            .Array(x => x.Lines, line => line
                .Field(l => l.Product, f => f.Required())
                .Field(l => l.Quantity)
                .Field(l => l.UnitPrice)
                .Computed(l => l.LineTotal, "{Quantity} * {UnitPrice}"))
            .Computed(x => x.Total, "sum({Lines[].LineTotal})")
            .Build();
        var invoice = new ExpressionInvoice();
        var state = new BormState(form, new BormModelDataAccessor(invoice)) { HistoryLimit = 50 };
        var lines = form.Fields[0];

        state.AddArrayItem(lines, "Lines");
        state.SetValue("Lines[0].Product", "A");
        state.SetValue("Lines[0].Quantity", 2);
        state.SetValue("Lines[0].UnitPrice", 5m);
        state.AddArrayItem(lines, "Lines");
        state.SetValue("Lines[1].Product", "B");
        var (first, second) = (invoice.Lines[0], invoice.Lines[1]);

        state.MoveArrayItem("Lines", 0, 1);
        CollectionAssert.AreEqual(new[] { second, first }, invoice.Lines);
        await state.UndoAsync();
        CollectionAssert.AreEqual(new[] { first, second }, invoice.Lines);

        state.RemoveArrayItem("Lines", 0);
        Assert.AreEqual(0m, invoice.Total);
        await state.UndoAsync();
        Assert.AreSame(first, invoice.Lines[0]);
        Assert.AreEqual(10m, invoice.Total);

        state.ClearArrayItems("Lines");
        await state.UndoAsync();
        CollectionAssert.AreEqual(new[] { first, second }, invoice.Lines);

        await state.RedoAsync();
        Assert.IsEmpty(invoice.Lines);
    }

    [TestMethod]
    public async Task A_row_put_back_by_undo_brings_its_messages_and_touched_state_back()
    {
        // Removing a row drops its messages with it; taking the removal back left the row - its empty required
        // product and all - looking valid until something judged it again, while the rows around it kept theirs.
        var model = new RegistrationModel
        {
            Items = [new LineItem { Product = "", Quantity = 5 }, new LineItem { Product = "B", Quantity = 500 }]
        };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        await state.ValidateAsync();
        state.MarkTouched("Items[0].Product");
        var removed = model.Items[0];

        state.RemoveArrayItem("Items", 0);
        Assert.IsEmpty(state.MessagesFor("Items[0].Product"));
        Assert.HasCount(1, state.MessagesFor("Items[0].Quantity"), "the row that moved up keeps its own message");

        await state.UndoAsync();

        Assert.AreSame(removed, model.Items[0]);
        Assert.AreEqual("Items[0].Product", Assert.ContainsSingle(state.MessagesFor("Items[0].Product")).FieldPath);
        Assert.IsTrue(state.IsTouched("Items[0].Product"));
        Assert.HasCount(1, state.MessagesFor("Items[1].Quantity"));
        Assert.IsEmpty(state.MessagesFor("Items[1].Product"), "the message went back to its own row, not the one now beside it");

        await state.RedoAsync();
        Assert.IsEmpty(state.MessagesFor("Items[0].Product"));
        Assert.HasCount(1, state.MessagesFor("Items[0].Quantity"));
    }

    [TestMethod]
    public async Task A_row_put_back_by_undo_brings_its_refused_entry_and_dirty_flags_back()
    {
        // Undo used to give a removed row back its messages but not the entry behind a conversion message - the
        // box showed the model's value under a complaint about text it no longer held - nor its dirty flags.
        var model = new RegistrationModel { Items = [new LineItem { Product = "A", Quantity = 5 }, new LineItem { Product = "B", Quantity = 6 }] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        state.SetValue("Items[0].Product", "Changed");
        state.SetValue("Items[0].Quantity", "abc");
        Assert.AreEqual("abc", state.RefusedEntry("Items[0].Quantity"));
        Assert.IsTrue(state.IsDirty("Items[0].Product"));

        state.RemoveArrayItem("Items", 0);
        Assert.IsNull(state.RefusedEntry("Items[0].Quantity"));
        Assert.IsFalse(state.IsDirty("Items[0].Product"));

        await state.UndoAsync();

        Assert.AreEqual("abc", state.RefusedEntry("Items[0].Quantity"));
        Assert.IsNotEmpty(state.MessagesFor("Items[0].Quantity"));
        Assert.IsTrue(state.IsDirty("Items[0].Product"));
        Assert.IsNull(state.RefusedEntry("Items[1].Quantity"));
        Assert.IsFalse(state.IsDirty("Items[1].Product"));
    }

    [TestMethod]
    public async Task A_row_added_again_by_redo_brings_back_what_it_carried_when_undo_took_it_away()
    {
        var model = new RegistrationModel { Items = [new LineItem { Product = "A", Quantity = 5 }] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        state.AddArrayItem(state.Definition.FindField("Items")!, "Items");
        state.MarkTouched("Items[1].Product");
        await state.ValidateAsync();
        Assert.IsNotEmpty(state.MessagesFor("Items[1].Product"));

        await state.UndoAsync();
        Assert.HasCount(1, model.Items);
        Assert.IsEmpty(state.MessagesFor("Items[1].Product"));

        await state.RedoAsync();
        Assert.HasCount(2, model.Items);
        Assert.IsNotEmpty(state.MessagesFor("Items[1].Product"));
        Assert.IsTrue(state.IsTouched("Items[1].Product"));
    }

    [TestMethod]
    public async Task A_row_object_listed_twice_keeps_each_copys_own_state_through_undo()
    {
        // The state was kept by row object, so of one object at two indexes only the first index's state survived,
        // and restoring gave it to both copies.
        var shared = new LineItem { Product = "A", Quantity = 5 };
        var model = new RegistrationModel { Items = [shared, new LineItem { Product = "B", Quantity = 6 }, shared] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        state.MarkTouched("Items[2].Product");

        state.ClearArrayItems("Items");
        await state.UndoAsync();

        Assert.HasCount(3, model.Items);
        Assert.IsFalse(state.IsTouched("Items[0].Product"));
        Assert.IsTrue(state.IsTouched("Items[2].Product"));
    }

    [TestMethod]
    public async Task Removing_one_copy_of_a_row_object_listed_twice_gives_each_occurrence_its_own_state_back()
    {
        // Undo matched the copy left behind with the first occurrence by reference, so the removed copy's touched
        // flag came back on the other one.
        var shared = new LineItem { Product = "A", Quantity = 5 };
        var model = new RegistrationModel { Items = [shared, shared] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        state.MarkTouched("Items[0].Product");

        state.RemoveArrayItem("Items", 0);
        Assert.IsFalse(state.IsTouched("Items[0].Product"), "the copy that moved up was never touched");

        await state.UndoAsync();
        Assert.IsTrue(state.IsTouched("Items[0].Product"));
        Assert.IsFalse(state.IsTouched("Items[1].Product"));

        await state.RedoAsync();
        Assert.HasCount(1, model.Items);
        Assert.IsFalse(state.IsTouched("Items[0].Product"));

        await state.UndoAsync();
        Assert.IsTrue(state.IsTouched("Items[0].Product"));
        Assert.IsFalse(state.IsTouched("Items[1].Product"));
    }

    [TestMethod]
    public async Task Moving_one_copy_of_a_row_object_listed_twice_takes_its_own_state_back_on_undo()
    {
        var shared = new LineItem { Product = "A", Quantity = 5 };
        var model = new RegistrationModel { Items = [shared, new LineItem { Product = "B", Quantity = 6 }, shared] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        state.MarkTouched("Items[0].Product");

        state.MoveArrayItem("Items", 0, 2);
        Assert.IsTrue(state.IsTouched("Items[2].Product"));

        await state.UndoAsync();
        Assert.IsTrue(state.IsTouched("Items[0].Product"));
        Assert.IsFalse(state.IsTouched("Items[2].Product"));

        await state.RedoAsync();
        Assert.IsTrue(state.IsTouched("Items[2].Product"));
        Assert.IsFalse(state.IsTouched("Items[1].Product"));
    }

    [TestMethod]
    public async Task A_row_put_back_by_undo_does_not_bring_back_state_for_a_field_the_schema_has_since_lost()
    {
        // NotifyDefinitionChanged dropped what a removed field carried, but undo gave a removed row all it carried
        // when it left - an error for a field no longer on the page kept the form from being valid.
        var model = new RegistrationModel { Items = [new LineItem { Product = "", Quantity = 5 }, new LineItem { Product = "B", Quantity = 6 }] };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(), new BormModelDataAccessor(model)) { HistoryLimit = 50 };
        await state.ValidateAsync();
        state.MarkTouched("Items[0].Product");
        state.MarkTouched("Items[0].Quantity");
        Assert.IsNotEmpty(state.MessagesFor("Items[0].Product"));

        state.RemoveArrayItem("Items", 0);
        var template = state.Definition.FindField("Items")!.ItemTemplate!;
        template.Children.Remove(template.Children.Single(f => f.Name == "Product"));
        state.NotifyDefinitionChanged();

        await state.UndoAsync();

        Assert.HasCount(2, model.Items);
        Assert.IsEmpty(state.MessagesFor("Items[0].Product"));
        Assert.IsFalse(state.IsTouched("Items[0].Product"));
        Assert.IsTrue(state.IsTouched("Items[0].Quantity"), "what the schema still has comes back");
        Assert.IsFalse(state.AllMessages.Any(m => m.FieldPath.StartsWith("Items[", StringComparison.Ordinal)), "nothing in the rows can block a submit");
    }

    [TestMethod]
    public async Task A_field_put_back_is_judged_again_as_a_change_would_be()
    {
        var (state, _) = Form();
        await state.SubmitAsync(_ => Task.CompletedTask);
        Assert.HasCount(1, state.MessagesFor("name"));

        state.SetValue("name", "Ada");
        await state.ValidateFieldAsync("name");
        Assert.IsEmpty(state.MessagesFor("name"));

        await state.UndoAsync();
        Assert.HasCount(1, state.MessagesFor("name"));
    }

    [TestMethod]
    public async Task Only_the_last_changes_up_to_the_limit_are_kept_and_reset_or_a_draft_starts_again()
    {
        var (state, data) = Form(b => b.Text("a").Text("b"), limit: 2);
        state.SetValue("name", "1");
        state.SetValue("a", "2");
        state.SetValue("b", "3");

        await state.UndoAsync();
        await state.UndoAsync();
        Assert.IsFalse(await state.UndoAsync());
        Assert.AreEqual("1", data["name"]);

        state.SetValue("city", "x");
        state.Reset();
        Assert.IsFalse(state.CanUndo);

        var draft = BormDraft.Serialize(state);
        state.SetValue("city", "y");
        BormDraft.Restore(state, draft);
        Assert.IsFalse(state.CanUndo);
    }

    [TestMethod]
    public async Task Putting_one_field_back_with_ResetField_can_itself_be_undone()
    {
        var (state, data) = Form();
        state.SetValue("name", "Ada");
        state.ResetField("name");
        Assert.IsNull(data["name"]);

        await state.UndoAsync();
        Assert.AreEqual("Ada", data["name"]);
    }

    [TestMethod]
    public void The_view_offers_Undo_and_Redo_names_what_they_act_on_and_says_what_it_did()
    {
        var form = BormBuilder.Create().Text("name", f => f.Label("Full name")).Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data).Add(x => x.ShowUndo, true));
        var undo = () => cut.FindAll("button").Single(b => b.TextContent == "Undo");

        // aria-disabled, so the button keeps the focus when the last change is undone.
        Assert.AreEqual("true", undo().GetAttribute("aria-disabled"));
        Assert.IsFalse(undo().HasAttribute("disabled"));

        cut.Find("#borm_name").Change("Ada");
        Assert.IsNull(undo().GetAttribute("aria-disabled"));
        Assert.AreEqual("Undo the change to Full name", undo().GetAttribute("aria-label"));

        undo().Click();

        cut.WaitForAssertion(() => Assert.IsFalse(data.TryGetValue("name", out var v) && v is not null));
        StringAssert.Contains(cut.Find(".bit-borm-form__actions [aria-live]").TextContent, "Undone: Full name");
        Assert.IsNull(cut.FindAll("button").Single(b => b.TextContent == "Redo").GetAttribute("aria-disabled"));
    }

    [TestMethod]
    public async Task Undoing_the_answer_a_cascading_choice_depends_on_puts_back_the_choice_it_emptied()
    {
        var form = BormBuilder.Create()
            .Text("Country")
            .Select("City", f => f.OptionsFrom(_ => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>([new("NYC", "New York")]), "Country"))
            .Build();
        var data = new Dictionary<string, object?> { ["Country"] = "US", ["City"] = "NYC" };
        var state = new BormState(form, new BormDictionaryDataAccessor(data)) { HistoryLimit = 50 };

        state.SetValue("Country", "CA");
        Assert.IsNull(data["City"]);

        // Put back with the country it belongs to, not emptied again by that country coming back.
        Assert.IsTrue(await state.UndoAsync());
        Assert.AreEqual("US", data["Country"]);
        Assert.AreEqual("NYC", data["City"]);

        Assert.IsTrue(await state.RedoAsync());
        Assert.AreEqual("CA", data["Country"]);
        Assert.IsNull(data["City"]);

        Assert.IsTrue(await state.UndoAsync());
        Assert.AreEqual("NYC", data["City"]);
    }

    [TestMethod]
    public async Task A_disabled_form_takes_no_change_from_its_Undo_button()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var data = new Dictionary<string, object?>();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data).Add(x => x.ShowUndo, true));
        var undo = () => cut.FindAll("button").Single(b => b.TextContent == "Undo");
        cut.Find("#borm_name").Change("Ada");
        var state = cut.Instance.Form;

        // aria-disabled keeps the button focusable, so the click itself is refused.
        await cut.InvokeAsync(() => state.Disabled = true);
        Assert.AreEqual("true", undo().GetAttribute("aria-disabled"));
        undo().Click();
        Assert.AreEqual("Ada", data["name"]);

        await cut.InvokeAsync(() => state.Disabled = false);
        undo().Click();
        cut.WaitForAssertion(() => Assert.IsNull(data["name"]));
    }
}
