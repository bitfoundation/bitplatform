using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A run of fields sharing a Group name renders as a real fieldset. Grouping is what turns a long
/// column of controls into something a person can scan, and a fieldset is what makes the grouping
/// exist for a screen reader as well as for the eye.
/// </summary>
[TestClass]
public class FieldGroupTests : ComponentTestBase
{
    private static BormDefinition Grouped() => BormBuilder.Create()
        .Text("firstName", f => f.Group("Your name"))
        .Text("lastName", f => f.Group("Your name"))
        .Text("email", f => f.Group("Contact details"))
        .Text("note")
        .Build();

    [TestMethod]
    public void Fields_sharing_a_group_are_wrapped_in_one_named_fieldset()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Grouped()));
        var groups = cut.FindAll("fieldset.bit-borm-fieldgroup");

        Assert.AreEqual(2, groups.Count);
        Assert.AreEqual("Your name", groups[0].QuerySelector("legend")!.TextContent);
        Assert.AreEqual(2, groups[0].QuerySelectorAll(".bit-borm-field").Count);
        Assert.AreEqual("Contact details", groups[1].QuerySelector("legend")!.TextContent);
    }

    [TestMethod]
    public void An_ungrouped_field_is_left_where_it_was()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Grouped()));

        // "note" belongs to nothing, so it must not be swept into the group above it.
        Assert.IsEmpty(cut.FindAll("fieldset.bit-borm-fieldgroup input#borm_note"));
        Assert.IsNotNull(cut.Find("input#borm_note"));
    }

    [TestMethod]
    public void Two_separated_blocks_that_reuse_a_name_stay_two_blocks()
    {
        // Grouping by runs rather than globally is what keeps the declared order intact: collecting
        // them would silently move the second block up next to the first.
        var form = BormBuilder.Create()
            .Text("a", f => f.Group("Details"))
            .Text("b")
            .Text("c", f => f.Group("Details"))
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.AreEqual(2, cut.FindAll("fieldset.bit-borm-fieldgroup").Count);
    }

    [TestMethod]
    public void A_group_survives_a_JSON_round_trip()
    {
        var json = BormJsonSchemaExporter.Export(Grouped());
        var reimported = BormJsonSchemaImporter.Import(json);

        Assert.AreEqual("Your name", reimported.FindField("firstName")!.Group);
        Assert.AreEqual("Contact details", reimported.FindField("email")!.Group);
        Assert.IsNull(reimported.FindField("note")!.Group);
    }

    [TestMethod]
    public void DataAnnotations_group_names_reach_the_rendered_form()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, BormSchemaGenerator.Generate<GroupedProfile>()));

        Assert.AreEqual("Identity", cut.Find("fieldset.bit-borm-fieldgroup legend").TextContent);
    }
}

public class GroupedProfile
{
    [System.ComponentModel.DataAnnotations.Display(GroupName = "Identity")]
    public string FirstName { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Display(GroupName = "Identity")]
    public string LastName { get; set; } = "";

    public string Note { get; set; } = "";
}

/// <summary>
/// BormField places one field wherever the page wants it, so a schema-driven form is not an
/// all-or-nothing block: the layout can be the page's, while labels, validation, conditions and the
/// accessibility wiring stay the schema's.
/// </summary>
[TestClass]
public class BormFieldTests : ComponentTestBase
{
    private static BormState State() => new(
        BormBuilder.Create()
            .Text("email", f => f.Label("Email").Required())
            .Object("address", a => a.Text("city"))
            .Build(),
        new BormDictionaryDataAccessor());

    [TestMethod]
    public void It_renders_the_named_field_with_its_schema_intact()
    {
        var state = State();
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "email"));

        Assert.AreEqual("true", cut.Find("input#borm_email").GetAttribute("aria-required"));
        Assert.Contains("Email", cut.Find("label[for='borm_email']").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_nested_path_works_too()
    {
        var state = State();
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "address.city"));

        Assert.IsNotNull(cut.Find("input#borm_address_city"));
    }

    [TestMethod]
    public void An_unknown_field_renders_nothing_by_default()
    {
        // A schema chosen at run time may not have the field a fixed layout mentions; refusing to
        // render is better than refusing to load the page.
        var state = State();
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "nope"));

        Assert.AreEqual(string.Empty, cut.Markup);
    }

    [TestMethod]
    public void An_unknown_field_can_be_made_to_throw_instead()
    {
        var state = State();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
            Render<BormField>(p => p
                .Add(x => x.State, state)
                .Add(x => x.Name, "nope")
                .Add(x => x.ThrowIfMissing, true)));

        Assert.Contains("nope", ex.Message, StringComparison.Ordinal);
    }
}

[TestClass]
public class FormAttributeSplatTests : ComponentTestBase
{
    [TestMethod]
    public void The_forms_own_class_is_never_replaced_by_a_splatted_one()
    {
        // Every input renderer splats first so its own wiring wins; the form does the same, or a stray
        // `class` would erase bit-borm-form and take the whole stylesheet with it.
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, BormBuilder.Create().Text("a").Build())
            .AddUnmatched("class", "mine"));

        var form = cut.Find("form");

        Assert.Contains("bit-borm-form", form.GetAttribute("class")!, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Unmatched_attributes_still_reach_the_form()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, BormBuilder.Create().Text("a").Build())
            .AddUnmatched("data-testid", "signup"));

        Assert.AreEqual("signup", cut.Find("form").GetAttribute("data-testid"));
    }
}
