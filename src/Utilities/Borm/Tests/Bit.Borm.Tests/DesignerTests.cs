using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// The form designer: fields added from a palette, edited through a properties form, moved, copied and deleted -
/// each change built into a definition, previewed and handed out - and an existing schema read in without losing
/// what the designer cannot show.
/// </summary>
[TestClass]
public class DesignerTests : ComponentTestBase
{
    private (IRenderedComponent<BormDesigner> Cut, Func<BormDefinition?> Last) Designer(BormDefinition? start = null)
    {
        BormDefinition? last = null;
        var cut = Render<BormDesigner>(p =>
        {
            p.Add(x => x.ValueChanged, d => last = d);
            if (start is not null) p.Add(x => x.Value, start);
        });
        return (cut, () => last);
    }

    private static void Add(IRenderedComponent<BormDesigner> cut, string kind)
        => cut.FindAll(".bit-borm-designer__kinds button").Single(b => b.TextContent == kind).Click();

    [TestMethod]
    public void A_field_added_from_the_palette_is_listed_chosen_previewed_and_handed_out()
    {
        var (cut, last) = Designer();
        Assert.IsNotNull(cut.Find(".bit-borm-designer__fields .bit-borm-designer__empty"));

        Add(cut, "Text");

        Assert.AreEqual("text1", last()!.Fields.Single().Name);
        Assert.AreEqual("true", cut.Find(".bit-borm-designer__select").GetAttribute("aria-pressed"));
        Assert.AreEqual("Edit Text", cut.Find(".bit-borm-designer__select").GetAttribute("aria-label"));
        Assert.AreEqual("text1", cut.Find("#designer_p_Name").GetAttribute("value"));
        Assert.IsNotNull(cut.Find("#designer_v_text1"));
        StringAssert.Contains(cut.Find(".bit-borm-designer__fields [aria-live]").TextContent, "Added Text.");
        StringAssert.Contains(cut.Find(".bit-borm-designer__json-text").GetAttribute("value"), "\"text1\"");
    }

    [TestMethod]
    public void Its_properties_become_the_fields_label_rules_and_conditions()
    {
        var (cut, last) = Designer();
        Add(cut, "Text");

        cut.Find("#designer_p_Label").Change("Full name");
        cut.Find("#designer_p_Required").Change(true);
        cut.Find("#designer_p_MaxLength").Change("40");
        cut.Find("#designer_p_VisibleWhen").Change("{text1} != 'hide'");

        var field = last()!.Fields.Single();
        Assert.AreEqual("Full name", field.Label);
        Assert.IsTrue(field.Required);
        Assert.AreEqual(40, field.MaxLength);
        Assert.IsInstanceOfType<BormExpressionCondition>(field.VisibleWhen);
        StringAssert.Contains(cut.Find("label[for=designer_v_text1]").TextContent, "Full name");
    }

    [TestMethod]
    public void A_choice_takes_one_option_per_line()
    {
        var (cut, last) = Designer();
        Add(cut, "Dropdown");

        cut.Find("#designer_p_Options").Change("nl = Netherlands\nde = Germany\nfr");

        CollectionAssert.AreEqual(new[] { "Netherlands", "Germany", "fr" }, last()!.Fields.Single().Options.Select(o => o.Label).ToArray());
        Assert.HasCount(4, cut.FindAll("#designer_v_select1 option"));
    }

    [TestMethod]
    public void An_expression_that_does_not_read_is_said_to_and_the_preview_keeps_the_last_form_that_did()
    {
        var (cut, last) = Designer();
        Add(cut, "Number");
        var before = last();

        cut.Find("#designer_p_Computed").Change("{number1} *");

        StringAssert.Contains(cut.Find("#designer_p_Computed_error").TextContent, "This does not read:");
        StringAssert.Contains(cut.Find(".bit-borm-designer__preview [role=alert]").TextContent, "This does not read:");
        Assert.AreSame(before, last());
    }

    [TestMethod]
    public void A_name_another_field_has_is_refused_and_nothing_is_built_from_it()
    {
        var (cut, last) = Designer();
        Add(cut, "Text");
        Add(cut, "Text");
        var before = last();

        cut.Find("#designer_p_Name").Change("TEXT1");

        Assert.AreEqual("Another field already has this name.", cut.Find("#designer_p_Name_error").TextContent.Trim().Replace("Error:", "").Trim());
        Assert.AreSame(before, last());
    }

    [TestMethod]
    public void Fields_are_moved_copied_and_deleted_by_buttons_that_say_what_they_did()
    {
        var (cut, last) = Designer();
        Add(cut, "Text");
        Add(cut, "Email");
        Add(cut, "Phone");

        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Move Phone up").Click();
        CollectionAssert.AreEqual(new[] { "text1", "tel1", "email1" }, last()!.Fields.Select(f => f.Name).ToArray());
        StringAssert.Contains(cut.Find(".bit-borm-designer__fields [aria-live]").TextContent, "Phone is now number 2 of 3.");

        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Duplicate Email").Click();
        Assert.AreEqual("email2", last()!.Fields[3].Name);

        cut.FindAll("button").Single(b => b.GetAttribute("aria-label") == "Delete Text").Click();
        CollectionAssert.AreEqual(new[] { "tel1", "email1", "email2" }, last()!.Fields.Select(f => f.Name).ToArray());
        StringAssert.Contains(cut.Find(".bit-borm-designer__fields [aria-live]").TextContent, "Deleted Text.");
        // The field that took the deleted one's place is the one chosen now.
        Assert.AreEqual("tel1", cut.Find("#designer_p_Name").GetAttribute("value"));
    }

    [TestMethod]
    public void An_existing_schema_is_read_in_and_what_the_designer_cannot_show_is_kept()
    {
        var start = BormBuilder.Create()
            .Text("country", f => f.Label("Country").OptionsFrom(_ => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>([new("nl", "NL")])))
            .Text("vat", f => f.Label("VAT").VisibleWhen(r => r.GetValue("country") is "nl", "country"))
            .Step("one", s => s.Title("One").Fields("country", "vat"))
            .Build();
        var (cut, last) = Designer(start);

        Assert.HasCount(2, cut.FindAll(".bit-borm-designer__item"));
        Assert.IsNotNull(cut.Find(".bit-borm-designer__note"));

        Add(cut, "Date");

        var built = last()!;
        Assert.IsNotNull(built.FindField("country")!.OptionsProvider);
        Assert.IsNotNull(built.FindField("vat")!.VisibleWhen);
        CollectionAssert.AreEqual(new[] { "country", "vat", "date1" }, built.Steps.Single().Fields.ToArray());
    }

    [TestMethod]
    public void A_json_schema_loaded_into_it_replaces_the_form_and_one_that_does_not_read_is_reported()
    {
        var (cut, last) = Designer();
        var json = cut.Find(".bit-borm-designer__json-text");

        json.Change("""{ "type": "object", "title": "Order", "properties": { "qty": { "type": "integer", "minimum": 1 } } }""");
        cut.FindAll("button").Single(b => b.TextContent == "Load this JSON").Click();
        Assert.AreEqual("Order", last()!.Title);
        Assert.AreEqual("qty", last()!.Fields.Single().Name);
        Assert.AreEqual(1, last()!.Fields.Single().Min);

        cut.Find(".bit-borm-designer__json-text").Change("{ not json");
        cut.FindAll("button").Single(b => b.TextContent == "Load this JSON").Click();
        StringAssert.Contains(cut.Find(".bit-borm-designer__json [role=alert]").TextContent, "That JSON could not be read:");
    }
}
