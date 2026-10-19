namespace Bit.Borm.Tests;

/// <summary>
/// A repeater operation changes the value of the field the list binds to as surely as typing does, so
/// anything watching FieldChanged — an autosave, a preview, a page-level dirty prompt — has to hear
/// about it. Every one of them used to be silent.
/// </summary>
[TestClass]
public class ArrayChangeNotificationTests
{
    private static (BormState State, List<string> Changed, BormFieldDefinition Items) Form(int rows = 0)
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(new RegistrationModel()));
        var items = form.FindField("Items")!;

        for (var i = 0; i < rows; i++) state.AddArrayItem(items, "Items");

        var changed = new List<string>();
        state.FieldChanged += changed.Add;
        return (state, changed, items);
    }

    [TestMethod]
    public void Adding_a_row_is_announced()
    {
        var (state, changed, items) = Form();

        state.AddArrayItem(items, "Items");

        Assert.AreSequenceEqual(["Items"], changed);
    }

    [TestMethod]
    public void Removing_a_row_is_announced()
    {
        var (state, changed, _) = Form(rows: 1);

        state.RemoveArrayItem("Items", 0);

        Assert.AreSequenceEqual(["Items"], changed);
    }

    [TestMethod]
    public void Moving_swapping_and_clearing_are_announced()
    {
        var (state, changed, _) = Form(rows: 3);

        state.MoveArrayItem("Items", 0, 2);
        state.SwapArrayItems("Items", 0, 1);
        state.ClearArrayItems("Items");

        Assert.AreSequenceEqual(["Items", "Items", "Items"], changed);
    }

    [TestMethod]
    public void Duplicating_a_row_is_announced_once_and_only_when_the_copy_is_there()
    {
        // The insert underneath it stays quiet: a listener that reacted to that first event would save
        // a blank line, because the values had not been copied in yet.
        var (state, changed, items) = Form(rows: 1);
        state.SetValue("Items[0].Product", "Widget");
        changed.Clear();

        state.DuplicateArrayItem(items, "Items", 0);

        Assert.AreSequenceEqual(["Items"], changed);
        Assert.AreEqual("Widget", state.GetValue("Items[1].Product"));
    }

    [TestMethod]
    public void An_operation_that_does_nothing_announces_nothing()
    {
        var (state, changed, _) = Form(rows: 2);

        state.RemoveArrayItem("Items", 99);
        state.MoveArrayItem("Items", 0, 0);
        state.SwapArrayItems("Items", 0, 5);
        state.ClearArrayItems("NotAField");

        Assert.IsEmpty(changed);
    }
}

/// <summary>
/// Snapshot and Reset(values) are the two halves of saving a draft: stash what the user has so far,
/// hand it back when they return.
/// </summary>
[TestClass]
public class SnapshotTests
{
    [TestMethod]
    public void It_maps_every_bound_path_to_its_value()
    {
        var model = new RegistrationModel { FirstName = "Ada", Email = "ada@example.com", Age = 36 };
        model.Address.City = "Delft";
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(model));

        var snapshot = state.Snapshot();

        Assert.AreEqual("Ada", snapshot["FirstName"]);
        Assert.AreEqual(36, snapshot["Age"]);
        // Nested paths are reached, not just the top level.
        Assert.AreEqual("Delft", snapshot["Address.City"]);
    }

    [TestMethod]
    public void A_repeater_contributes_one_entry_per_row_per_field()
    {
        var form = BormSchemaGenerator.Generate<RegistrationModel>();
        var state = new BormState(form, new BormModelDataAccessor(new RegistrationModel()));
        var items = form.FindField("Items")!;
        state.AddArrayItem(items, "Items");
        state.AddArrayItem(items, "Items");
        state.SetValue("Items[0].Product", "Widget");
        state.SetValue("Items[1].Product", "Gadget");

        var snapshot = state.Snapshot();

        Assert.AreEqual("Widget", snapshot["Items[0].Product"]);
        Assert.AreEqual("Gadget", snapshot["Items[1].Product"]);
    }

    [TestMethod]
    public void Object_containers_and_presentational_fields_hold_no_value_and_are_skipped()
    {
        var form = BormBuilder.Create()
            .Static("heading", "A heading")
            .Object("address", a => a.Text("city"))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        var snapshot = state.Snapshot();

        Assert.IsFalse(snapshot.ContainsKey("heading"));
        Assert.IsFalse(snapshot.ContainsKey("address"));
        Assert.IsTrue(snapshot.ContainsKey("address.city"));
    }

    [TestMethod]
    public void A_snapshot_round_trips_through_Reset()
    {
        var model = new RegistrationModel { FirstName = "Ada", Age = 36 };
        var state = new BormState(BormSchemaGenerator.Generate<RegistrationModel>(),
            new BormModelDataAccessor(model));

        var draft = state.Snapshot();
        state.SetValue("FirstName", "Grace");
        state.SetValue("Age", 45);

        state.Reset(draft);

        Assert.AreEqual("Ada", model.FirstName);
        Assert.AreEqual(36, model.Age);
        Assert.IsFalse(state.IsFormDirty);
    }
}

[TestClass]
public class DefinitionCloneTests
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Title("Shared")
        .Columns(2)
        .Select("country", f => f.Options(("fr", "France")))
        .Object("address", a => a.Text("city", c => c.Required()))
        .Array("lines", l => l.Text("product"))
        .Step("one", s => s.Title("One").Fields("country"))
        .Build();

    [TestMethod]
    public void The_copy_carries_the_same_shape()
    {
        var copy = Schema().Clone();

        Assert.AreEqual("Shared", copy.Title);
        Assert.AreEqual(2, copy.Columns);
        Assert.AreEqual(3, copy.Fields.Count);
        Assert.ContainsSingle(copy.Steps);
        Assert.IsNotNull(copy.FindByPath("address.city"));
        Assert.IsNotNull(copy.FindField("lines")!.ItemTemplate);
    }

    [TestMethod]
    public void Adding_an_option_to_the_copy_leaves_the_original_alone()
    {
        // The whole point: a shared schema tailored for one form must not edit the one everyone else
        // is looking at.
        var original = Schema();
        var copy = original.Clone();

        copy.FindField("country")!.Options.Add(new BormSelectOption("gb", "United Kingdom"));

        Assert.ContainsSingle(original.FindField("country")!.Options);
        Assert.AreEqual(2, copy.FindField("country")!.Options.Count);
    }

    [TestMethod]
    public void Editing_a_nested_field_or_an_item_template_leaves_the_original_alone()
    {
        var original = Schema();
        var copy = original.Clone();

        copy.FindByPath("address.city")!.Label = "Town";
        copy.FindField("lines")!.ItemTemplate!.Label = "Line";

        Assert.AreNotEqual("Town", original.FindByPath("address.city")!.Label);
        Assert.AreNotEqual("Line", original.FindField("lines")!.ItemTemplate!.Label);
    }

    [TestMethod]
    public void Editing_a_step_or_a_rule_list_leaves_the_original_alone()
    {
        var original = Schema();
        var copy = original.Clone();

        copy.Steps[0].Fields.Add("address");
        copy.FindByPath("address.city")!.Validators.Clear();

        Assert.ContainsSingle(original.Steps[0].Fields);
        Assert.IsNotEmpty(original.FindByPath("address.city")!.Validators);
    }

    [TestMethod]
    public void Behaviour_backed_by_a_delegate_is_carried_over_rather_than_lost()
    {
        var form = BormBuilder.Create()
            .Text("first")
            .Text("full", f => f.Computed(ctx => ctx.Sibling("first"), "first"))
            .Text("country", f => f.OnChange(ctx => ctx.ClearSibling("full")))
            .Build();

        var copy = form.Clone();
        var state = new BormState(copy, new BormDictionaryDataAccessor());
        state.SetValue("first", "Ada");

        Assert.AreEqual("Ada", state.GetValue("full"));
        Assert.IsNotNull(copy.FindField("country")!.OnChanged);
    }

    [TestMethod]
    public void A_cloned_schema_still_passes_its_own_diagnostics()
        => Assert.AreEqual(Schema().Validate().Count, Schema().Clone().Validate().Count);
}

/// <summary>
/// A dictionary-backed form was the one place in the library where <c>Email</c> and <c>email</c> were
/// different fields — a distinction nothing else can honour, and one the schema diagnostics already
/// report as two siblings binding to the same path.
/// </summary>
[TestClass]
public class DictionaryKeyCasingTests
{
    [TestMethod]
    public void A_value_seeded_with_different_casing_is_still_found()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["email"] = "ada@example.com" });

        Assert.AreEqual("ada@example.com", data.GetValue("Email"));
    }

    [TestMethod]
    public void Writing_with_different_casing_updates_the_same_entry()
    {
        var data = new BormDictionaryDataAccessor();

        data.SetValue("Email", "a@example.com");
        data.SetValue("email", "b@example.com");

        Assert.AreEqual("b@example.com", data.GetValue("EMAIL"));
    }

    [TestMethod]
    public void Nested_objects_resolve_the_same_way_as_the_root()
    {
        var data = new BormDictionaryDataAccessor();

        data.SetValue("Address.City", "Delft");

        Assert.AreEqual("Delft", data.GetValue("address.city"));
    }

    [TestMethod]
    public void A_repeater_row_created_by_the_form_resolves_the_same_way()
    {
        var form = BormBuilder.Create().Array("rows", r => r.Text("City")).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.AddArrayItem(form.FindField("rows")!, "rows");
        state.SetValue("rows[0].City", "Delft");

        Assert.AreEqual("Delft", state.GetValue("rows[0].city"));
    }

    [TestMethod]
    public void A_seed_dictionary_holding_two_keys_that_differ_only_in_case_does_not_throw()
    {
        // The caller's dictionary is the store itself, so both keys stay where the caller put them: a
        // path spelled exactly like one of them reads that one, and a write through either spelling
        // lands on its own entry rather than adding a third.
        var seed = new Dictionary<string, object?>
        {
            ["email"] = "first",
            ["Email"] = "second"
        };
        var data = new BormDictionaryDataAccessor(seed);

        Assert.AreEqual("first", data.GetValue("email"));
        Assert.AreEqual("second", data.GetValue("Email"));

        data.SetValue("Email", "changed");

        Assert.AreEqual(2, seed.Count);
        Assert.AreEqual("changed", seed["Email"]);
    }

    [TestMethod]
    public void TryGetValue_still_tells_absent_apart_from_null()
    {
        var data = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["a"] = null });

        Assert.IsTrue(data.TryGetValue("A", out var present));
        Assert.IsNull(present);
        Assert.IsFalse(data.TryGetValue("b", out _));
    }
}
