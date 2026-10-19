using System.ComponentModel.DataAnnotations;
using Bunit;

namespace Bit.Borm.Tests;

public class OptionalRow
{
    public string Kind { get; set; } = "";
    public string? Email { get; set; }
}

public class OptionalRowSheet
{
    /// <summary>A root field sharing a name with one inside every row. This is where the bug lived.</summary>
    public string? Email { get; set; }

    /// <summary>A root-only field, so the fallback the scope was written for still has a case to prove.</summary>
    public string? SheetName { get; set; }

    public List<OptionalRow> Rows { get; set; } = [];
}

/// <summary>
/// A scoped read has to choose between the row's field and the root's. Choosing on "did the scoped
/// read come back null" is wrong, because a row's own field is allowed to be empty — and when it is,
/// the root's field of the same name would answer for it.
/// </summary>
[TestClass]
public class ScopedReadExistenceTests
{
    private static BormState Form(OptionalRowSheet model)
    {
        var schema = BormBuilder.For<OptionalRowSheet>()
            .Field(x => x.Email)
            .Array(x => x.Rows, row => row
                .Field(r => r.Kind)
                .Field(r => r.Email, f => f.VisibleWhen("Email", BormConditionOperator.IsNotEmpty)))
            .Build();

        return new BormState(schema, new BormModelDataAccessor(model));
    }

    [TestMethod]
    public void An_empty_row_field_is_not_answered_for_by_the_root()
    {
        var model = new OptionalRowSheet
        {
            Email = "root@example.com",
            Rows = [new OptionalRow { Email = null }]
        };
        var state = Form(model);
        var rowEmail = state.Definition.FindField("Rows")!.ItemTemplate!.Children.Single(c => c.Name == "Email");

        // The row's own Email is empty, so the condition is false — regardless of what the root holds.
        Assert.IsFalse(state.IsVisible(rowEmail, "Rows[0].Email"));
    }

    [TestMethod]
    public void A_filled_row_field_still_wins_over_the_root()
    {
        var model = new OptionalRowSheet
        {
            Email = null,
            Rows = [new OptionalRow { Email = "row@example.com" }]
        };
        var state = Form(model);
        var rowEmail = state.Definition.FindField("Rows")!.ItemTemplate!.Children.Single(c => c.Name == "Email");

        Assert.IsTrue(state.IsVisible(rowEmail, "Rows[0].Email"));
    }

    [TestMethod]
    public void A_name_the_row_does_not_have_still_falls_back_to_the_root()
    {
        // This is what the fallback was written for, and it has to keep working: an absolute reference
        // from inside a repeater to a field that only exists at the top level.
        var model = new OptionalRowSheet { SheetName = "Q3 leads", Rows = [new OptionalRow()] };
        var scoped = BormScopedDataReader.ForOwnerOf(
            new BormModelDataAccessor(model), "Rows[0].Kind");

        // A row has no SheetName of its own, so the reference can only mean the sheet's.
        Assert.AreEqual("Q3 leads", scoped.GetValue("SheetName"));
        Assert.IsNull(scoped.GetValue("Nonexistent"));
    }

    [TestMethod]
    public void TryGetValue_tells_absent_apart_from_empty()
    {
        var accessor = new BormModelDataAccessor(new OptionalRowSheet { Rows = [new OptionalRow()] });

        Assert.IsTrue(accessor.TryGetValue("Rows[0].Email", out var present));
        Assert.IsNull(present);

        Assert.IsFalse(accessor.TryGetValue("Rows[0].Nonexistent", out _));
        Assert.IsFalse(accessor.TryGetValue("Rows[5].Email", out _));
    }

    [TestMethod]
    public void The_dictionary_store_reports_a_missing_key_as_missing()
    {
        var accessor = new BormDictionaryDataAccessor(new Dictionary<string, object?> { ["a"] = null });

        Assert.IsTrue(accessor.TryGetValue("a", out var present));
        Assert.IsNull(present);
        Assert.IsFalse(accessor.TryGetValue("b", out _));
    }
}

public class CollectionAnnotationsModel
{
    [MinLength(1), MaxLength(3)]
    public List<string> Tags { get; set; } = [];

    [MinLength(2)]
    public string Name { get; set; } = "";
}

/// <summary>
/// [MinLength] and [MaxLength] mean item counts on a collection, exactly as [Length] does. Mapping
/// them to the string rule made "at least one tag" pass on an empty list — silently, which is the
/// worst way for a rule to fail.
/// </summary>
[TestClass]
public class CollectionLengthAnnotationTests
{
    private static BormState Form(CollectionAnnotationsModel model)
        => new(BormSchemaGenerator.Generate<CollectionAnnotationsModel>(),
            new BormModelDataAccessor(model));

    [TestMethod]
    public async Task An_empty_list_fails_its_minimum()
    {
        var state = Form(new CollectionAnnotationsModel { Name = "Ada" });

        await state.ValidateAsync();

        Assert.IsNotEmpty(state.MessagesFor("Tags"));
    }

    [TestMethod]
    public async Task A_list_within_bounds_passes()
    {
        var state = Form(new CollectionAnnotationsModel { Name = "Ada", Tags = ["one", "two"] });

        await state.ValidateAsync();

        Assert.IsEmpty(state.MessagesFor("Tags"));
    }

    [TestMethod]
    public async Task Too_many_items_fails_the_maximum()
    {
        var state = Form(new CollectionAnnotationsModel { Name = "Ada", Tags = ["a", "b", "c", "d"] });

        await state.ValidateAsync();

        Assert.IsNotEmpty(state.MessagesFor("Tags"));
    }

    [TestMethod]
    public void The_bounds_reach_the_schema_as_item_counts()
    {
        var form = BormSchemaGenerator.Generate<CollectionAnnotationsModel>();
        var tags = form.FindField("Tags")!;

        Assert.AreEqual(1, tags.MinItems);
        Assert.AreEqual(3, tags.MaxItems);
        Assert.IsNull(tags.MinLength);
    }

    [TestMethod]
    public async Task A_string_is_still_measured_in_characters()
    {
        var state = Form(new CollectionAnnotationsModel { Name = "A", Tags = ["one"] });

        await state.ValidateAsync();

        Assert.IsNotEmpty(state.MessagesFor("Name"));
    }
}

[TestClass]
public class ContradictoryBoundsTests
{
    [TestMethod]
    public void A_range_that_crosses_over_is_reported()
    {
        var form = BormBuilder.Create().Number("n", f => f.Range(10, 5)).Build();

        Assert.Contains(d => d.Message.Contains("Min (10) is greater than Max (5)", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void So_are_crossed_lengths_and_item_counts()
    {
        var form = BormBuilder.Create()
            .Text("s", f => f.MinLength(20).MaxLength(5))
            .ArrayOf("xs", BormFieldType.Text, f => f.Items(min: 4, max: 2))
            .Build();

        Assert.AreEqual(2, form.Validate().Count(d => d.Message.Contains("no value can satisfy both", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void An_editable_computed_field_is_reported()
    {
        // Anything typed into it is overwritten the next time a dependency changes.
        var form = BormBuilder.Create()
            .Number("total", f => f.Computed(_ => 0))
            .Build();

        Assert.Contains(d => d.Message.Contains("computed field is editable", StringComparison.Ordinal), form.Validate());
    }

    [TestMethod]
    public void A_sound_schema_reports_nothing()
    {
        var form = BormBuilder.Create()
            .Number("n", f => f.Range(5, 10))
            .Text("s", f => f.MinLength(2).MaxLength(50))
            .Build();

        Assert.IsEmpty(form.Validate());
    }
}

[TestClass]
public class DisabledFormTests : ComponentTestBase
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Text("a")
        .Select("b", f => f.Options(("x", "X")))
        .Build();

    [TestMethod]
    public void A_disabled_form_disables_its_controls_and_its_buttons()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.Disabled, true));

        Assert.IsTrue(cut.Find("input#borm_a").HasAttribute("disabled"));
        Assert.IsTrue(cut.Find("select#borm_b").HasAttribute("disabled"));
        Assert.IsTrue(cut.Find("button[type=submit]").HasAttribute("disabled"));
    }

    [TestMethod]
    public void Disabled_is_not_the_same_as_busy()
    {
        // aria-busy means "working on it", which a locked form is not.
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.Disabled, true));

        Assert.IsFalse(cut.Find("form").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public void Turning_it_back_off_re_enables_everything()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.Disabled, true));

        cut.Render(p => p.Add(x => x.Definition, cut.Instance.Form.Definition).Add(x => x.Disabled, false));

        Assert.IsFalse(cut.Find("input#borm_a").HasAttribute("disabled"));
    }
}

[TestClass]
public class CharacterCountIsNonDestructiveTests : ComponentTestBase
{
    [TestMethod]
    public void A_counted_field_drops_its_maxlength_attribute()
    {
        // maxlength silently truncates a pasted answer. A counter plus the length rule lets the user
        // keep their text and edit it down, which is the whole reason to show a count.
        var form = BormBuilder.Create()
            .Text("title", f => f.MaxLength(60).CharacterCount())
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.IsFalse(cut.Find("input#borm_title").HasAttribute("maxlength"));
    }

    [TestMethod]
    public void A_field_without_a_counter_keeps_it()
    {
        var form = BormBuilder.Create().Text("title", f => f.MaxLength(60)).Build();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.AreEqual("60", cut.Find("input#borm_title").GetAttribute("maxlength"));
    }

    [TestMethod]
    public async Task Going_over_the_limit_is_still_an_error()
    {
        var form = BormBuilder.Create()
            .Text("title", f => f.MaxLength(5).CharacterCount())
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        state.SetValue("title", "far too long");
        await state.ValidateAsync();

        Assert.IsNotEmpty(state.MessagesFor("title"));
    }
}

[TestClass]
public class RangeAnnouncementTests : ComponentTestBase
{
    [TestMethod]
    public void A_slider_with_a_unit_announces_the_unit()
    {
        var form = BormBuilder.Create()
            .Field("weight", BormFieldType.Range, f => f.Range(0, 200).Suffix(" kg"))
            .Build();
        var data = new Dictionary<string, object?> { ["weight"] = 70 };

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        Assert.AreEqual("70 kg", cut.Find("input#borm_weight").GetAttribute("aria-valuetext"));
    }

    [TestMethod]
    public void A_bare_slider_leaves_the_announcement_to_the_browser()
    {
        var form = BormBuilder.Create()
            .Field("n", BormFieldType.Range, f => f.Range(0, 10))
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.IsFalse(cut.Find("input#borm_n").HasAttribute("aria-valuetext"));
    }
}
