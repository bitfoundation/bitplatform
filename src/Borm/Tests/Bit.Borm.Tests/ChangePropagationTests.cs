using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// Every way a value can change has to look the same to the rest of the engine. A value the user typed
/// already ran the sweeps — clear the branches this answer closed, refresh what reads it, drop the
/// options that cascade off it, tell the listeners. A value the *form* changed on the user's behalf
/// was skipping them, which is how a computed field came to be invisible to conditions in an earlier
/// round; clearing a hidden field and undoing a single answer had the same hole.
/// </summary>
[TestClass]
public class ChangePropagationTests
{
    private static double Num(object? value) => value is null ? 0 : Convert.ToDouble(value);

    /// <summary>An options provider that reports whichever country is current, so a stale cache shows.</summary>
    private static BormOptionsProvider CountryEcho => ctx =>
        new ValueTask<IReadOnlyList<BormSelectOption>>(
            (IReadOnlyList<BormSelectOption>)[
                new BormSelectOption(ctx.Value("Country") as string ?? "none", "City")]);

    [TestMethod]
    public void Clearing_a_hidden_field_is_announced_like_any_other_change()
    {
        var form = BormBuilder.Create()
            .Checkbox("Shipping")
            .Text("City", f => f.VisibleWhen("Shipping", BormConditionOperator.IsTrue).ClearOnHide())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var changed = new List<string>();

        state.SetValue("Shipping", true);
        state.SetValue("City", "Lisbon");
        state.FieldChanged += changed.Add;

        state.SetValue("Shipping", false);

        // An autosave, a live preview or a page-level dirty prompt watching FieldChanged must hear
        // that City is now empty — otherwise it saves a city the model no longer holds.
        Assert.Contains("City", changed);
        Assert.IsNull(state.GetValue("City"));
    }

    [TestMethod]
    public void Clearing_a_hidden_field_refreshes_a_total_that_reads_it()
    {
        var form = BormBuilder.Create()
            .Checkbox("Extras")
            .Number("Extra", f => f.VisibleWhen("Extras", BormConditionOperator.IsTrue).ClearOnHide())
            .Number("Base")
            .Number("Total", f => f.ReadOnly()
                .Computed(ctx => Num(ctx.Sibling("Base")) + Num(ctx.Sibling("Extra")), "Base", "Extra"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("Base", 10d);
        state.SetValue("Extras", true);
        state.SetValue("Extra", 5d);
        Assert.AreEqual(15d, Num(state.GetValue("Total")));

        state.SetValue("Extras", false);

        // The extra is gone from the model; a total still claiming 15 is simply wrong.
        Assert.AreEqual(10d, Num(state.GetValue("Total")));
    }

    [TestMethod]
    public async Task Clearing_a_hidden_field_drops_the_options_that_cascade_off_it()
    {
        var form = BormBuilder.Create()
            .Checkbox("Custom")
            .Text("Country", f => f.VisibleWhen("Custom", BormConditionOperator.IsTrue).ClearOnHide())
            .Select("City", f => f.OptionsFrom(CountryEcho, "Country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var city = form.FindByPath("City")!;

        state.SetValue("Custom", true);
        state.SetValue("Country", "France");
        await state.EnsureOptionsAsync(city, "City");
        Assert.AreEqual("France", state.OptionsFor(city, "City")[0].Value);

        state.SetValue("Custom", false);

        // The country the list was built for no longer exists, so the cached list must not survive it.
        await state.EnsureOptionsAsync(city, "City");
        Assert.AreEqual("none", state.OptionsFor(city, "City")[0].Value);
    }

    [TestMethod]
    public void ResetField_runs_the_same_sweeps_a_typed_value_does()
    {
        var form = BormBuilder.Create()
            .Number("Quantity")
            .Number("Total", f => f.ReadOnly().Computed(ctx => Num(ctx.Sibling("Quantity")) * 2, "Quantity"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor(
            new Dictionary<string, object?> { ["Quantity"] = 3d }));
        var changed = new List<string>();
        state.FieldChanged += changed.Add;

        state.SetValue("Quantity", 10d);
        Assert.AreEqual(20d, Num(state.GetValue("Total")));

        changed.Clear();
        state.ResetField("Quantity");

        Assert.AreEqual(3d, Num(state.GetValue("Quantity")));
        Assert.AreEqual(6d, Num(state.GetValue("Total")));
        Assert.Contains("Quantity", changed);
    }

    [TestMethod]
    public async Task ResetField_drops_the_options_that_cascade_off_it()
    {
        var form = BormBuilder.Create()
            .Text("Country")
            .Select("City", f => f.OptionsFrom(CountryEcho, "Country"))
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor(
            new Dictionary<string, object?> { ["Country"] = "France" }));
        var city = form.FindByPath("City")!;

        state.SetValue("Country", "Spain");
        await state.EnsureOptionsAsync(city, "City");
        Assert.AreEqual("Spain", state.OptionsFor(city, "City")[0].Value);

        state.ResetField("Country");

        await state.EnsureOptionsAsync(city, "City");
        Assert.AreEqual("France", state.OptionsFor(city, "City")[0].Value);
    }

    [TestMethod]
    public void A_tag_added_and_taken_back_leaves_the_form_clean()
    {
        // Same shape as the multi-select: every edit writes a new list, so a baseline held by
        // reference reported the field dirty from the first tag and never clean again.
        var form = BormBuilder.Create().Tags("Skills").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor(
            new Dictionary<string, object?> { ["Skills"] = new List<string> { "csharp" } }));

        state.SetValue("Skills", new List<string> { "csharp", "blazor" });
        Assert.IsTrue(state.IsDirty("Skills"));

        state.SetValue("Skills", new List<string> { "csharp" });
        Assert.IsFalse(state.IsDirty("Skills"));
        Assert.IsFalse(state.IsFormDirty);
    }

    [TestMethod]
    public void Resetting_restores_a_collection_the_user_emptied()
    {
        var form = BormBuilder.Create().Tags("Skills").Build();
        var model = new Dictionary<string, object?> { ["Skills"] = new List<string> { "csharp" } };
        var state = new BormState(form, new BormDictionaryDataAccessor(model));

        // Emptied to nothing at all, not to an empty list — which is what ClearOnHide and a Clear
        // button both do, and what used to leave Reset with no live list to refill.
        state.SetValue("Skills", null);
        state.Reset();

        Assert.AreSequenceEqual(["csharp"], ((System.Collections.IEnumerable)state.GetValue("Skills")!)
            .Cast<object?>().Select(v => v?.ToString()));
    }

    [TestMethod]
    public void Clearing_a_hidden_field_settles_rather_than_recursing()
    {
        // Hiding one field clears it, which hides the next, which clears it. The sweep has to reach
        // the end of the chain and stop, not run out of stack.
        var form = BormBuilder.Create()
            .Checkbox("A")
            .Text("B", f => f.VisibleWhen("A", BormConditionOperator.IsTrue).ClearOnHide())
            .Text("C", f => f.VisibleWhen("B", BormConditionOperator.IsNotEmpty).ClearOnHide())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        state.SetValue("A", true);
        state.SetValue("B", "x");
        state.SetValue("C", "y");

        state.SetValue("A", false);

        Assert.IsNull(state.GetValue("B"));
        Assert.IsNull(state.GetValue("C"));
    }
}

/// <summary>
/// A multi-select's value is a set the user assembles by clicking, but the order it is stored in is
/// the schema's, not the click sequence's. Otherwise choosing A then B and choosing B then A produce
/// different data for the same answer — and a form put back exactly as it was reports unsaved changes.
/// </summary>
[TestClass]
public class MultiSelectOrderTests : BunitContext
{
    public MultiSelectOrderTests() => Services.AddBorm();

    private static BormDefinition Form() => BormBuilder.Create()
        .MultiSelect("Days", f => f.Options(("mon", "Monday"), ("tue", "Tuesday"), ("wed", "Wednesday")))
        .Build();

    private static List<string> Stored(BormState state)
        => ((System.Collections.IEnumerable)state.GetValue("Days")!).Cast<object?>()
            .Select(v => v?.ToString() ?? "").ToList();

    [TestMethod]
    public void The_stored_order_follows_the_options_not_the_clicks()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form()));
        var boxes = cut.FindAll(".bit-borm-multiselect input[type=checkbox]");

        // Chosen back to front.
        boxes[2].Change(true);
        cut.FindAll(".bit-borm-multiselect input[type=checkbox]")[0].Change(true);

        Assert.AreSequenceEqual(["mon", "wed"], Stored(cut.Instance.Form));
    }

    [TestMethod]
    public void A_flags_enum_multiselect_still_compares_as_one_value()
    {
        // It renders as a set of boxes but stores one combined value; snapshotting it as a collection
        // would leave it dirty for ever.
        var form = BormSchemaGenerator.Generate<TypedModel>();
        var model = new TypedModel { Availability = Days.Monday };
        var state = new BormState(form, new BormModelDataAccessor(model));

        state.SetValue("Availability", Days.Monday | Days.Tuesday);
        Assert.IsTrue(state.IsDirty("Availability"));

        state.SetValue("Availability", Days.Monday);
        Assert.IsFalse(state.IsDirty("Availability"));
    }

    [TestMethod]
    public void An_unchanged_selection_leaves_the_form_clean()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Form())
            .Add(x => x.Data, new Dictionary<string, object?> { ["Days"] = new List<string> { "mon", "wed" } }));
        var state = cut.Instance.Form;

        // Untick Monday, tick it again. The same two days are chosen, so nothing has changed.
        cut.FindAll(".bit-borm-multiselect input[type=checkbox]")[0].Change(false);
        Assert.IsTrue(state.IsDirty("Days"));

        cut.FindAll(".bit-borm-multiselect input[type=checkbox]")[0].Change(true);

        Assert.AreSequenceEqual(["mon", "wed"], Stored(state));
        Assert.IsFalse(state.IsDirty("Days"));
        Assert.IsFalse(state.IsFormDirty);
    }
}
