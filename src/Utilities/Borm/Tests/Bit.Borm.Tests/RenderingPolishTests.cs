using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// The rendering details that were declared, documented and quietly ignored: schema attributes that
/// never reached a group control, a label suppression that only worked on half the field types, and a
/// review-mode form that still offered to submit itself.
/// </summary>
[TestClass]
public class SchemaAttributeSplattingTests : ComponentTestBase
{
    private Bunit.IRenderedComponent<BormView> RenderForm(BormDefinition form)
        => Render<BormView>(p => p.Add(x => x.Definition, form));

    [TestMethod]
    public void A_radio_group_carries_the_schema_extra_attributes()
    {
        var form = BormBuilder.Create()
            .Field("kind", BormFieldType.Radio, f => f
                .Options(("a", "A"), ("b", "B"))
                .InputAttr("data-testid", "kind"))
            .Build();

        Assert.AreEqual("kind", RenderForm(form).Find(".bit-borm-radio-group").GetAttribute("data-testid"));
    }

    [TestMethod]
    public void A_multi_select_group_carries_the_schema_extra_attributes()
    {
        var form = BormBuilder.Create()
            .Field("days", BormFieldType.MultiSelect, f => f
                .Options(("mon", "Monday"))
                .InputAttr("data-testid", "days"))
            .Build();

        Assert.AreEqual("days", RenderForm(form).Find(".bit-borm-multiselect").GetAttribute("data-testid"));
    }

    [TestMethod]
    public void A_file_field_carries_the_schema_extra_attributes()
    {
        var form = BormBuilder.Create()
            .Field("cv", BormFieldType.File, f => f.InputAttr("data-testid", "cv"))
            .Build();

        Assert.AreEqual("cv", RenderForm(form).Find("input[type=file]").GetAttribute("data-testid"));
    }

    [TestMethod]
    public void The_renderers_own_wiring_still_wins_over_the_schemas()
    {
        // The splat comes first everywhere, so a schema cannot unhook the group's accessible name.
        var form = BormBuilder.Create()
            .Field("kind", BormFieldType.Radio, f => f
                .Label("Kind")
                .Options(("a", "A"))
                .InputAttr("role", "presentation"))
            .Build();

        Assert.AreEqual("radiogroup", RenderForm(form).Find(".bit-borm-radio-group").GetAttribute("role"));
    }
}

[TestClass]
public class CheckboxLabelSuppressionTests : ComponentTestBase
{
    private Bunit.IRenderedComponent<BormView> RenderForm(BormDefinition form)
        => Render<BormView>(p => p.Add(x => x.Definition, form));

    [TestMethod]
    public void HideLabel_removes_the_visible_text_and_keeps_the_accessible_name()
    {
        var form = BormBuilder.Create()
            .Checkbox("terms", f => f.Label("I accept the terms").HideLabel())
            .Build();

        var cut = RenderForm(form);
        var input = cut.Find("input#borm_terms");

        Assert.AreEqual("I accept the terms", input.GetAttribute("aria-label"));
        Assert.DoesNotContain("I accept the terms", cut.Find("label[for='borm_terms']").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_checkbox_with_its_label_shown_is_named_by_the_text_not_by_aria_label()
    {
        var form = BormBuilder.Create()
            .Checkbox("terms", f => f.Label("I accept the terms"))
            .Build();

        var cut = RenderForm(form);

        Assert.IsFalse(cut.Find("input#borm_terms").HasAttribute("aria-label"));
        Assert.Contains("I accept the terms", cut.Find("label[for='borm_terms']").TextContent, StringComparison.Ordinal);
    }
}

[TestClass]
public class ReadOnlyWizardTests : ComponentTestBase
{
    private static BormDefinition Wizard() => BormBuilder.Create()
        .Text("a", f => f.Label("A"))
        .Text("b", f => f.Label("B"))
        .Step("one", s => s.Title("One").Fields("a"))
        .Step("two", s => s.Title("Two").Fields("b"))
        .Build();

    [TestMethod]
    public void A_read_only_wizard_can_still_be_paged_through()
    {
        // Reading a form is not editing it, and a review screen that strands the user on step 1 of 2
        // is worse than one that shows a button it will not honour.
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.ReadOnly, true));

        var labels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();

        Assert.Contains("Next", labels);
        Assert.Contains("Back", labels);
    }

    [TestMethod]
    public void A_read_only_wizard_does_not_offer_to_submit_or_reset()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.ReadOnly, true)
            .Add(x => x.ShowResetButton, true));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Next").Click();

        var labels = cut.FindAll("button").Select(b => b.TextContent.Trim()).ToList();

        Assert.DoesNotContain("Submit", labels);
        Assert.DoesNotContain("Reset", labels);
        Assert.IsEmpty(cut.FindAll("button[type=submit]"));
    }

    [TestMethod]
    public void An_editable_wizard_still_submits_on_its_last_step()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Wizard()));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Next").Click();

        Assert.IsNotEmpty(cut.FindAll("button[type=submit]"));
    }

    [TestMethod]
    public void ShowSubmitButton_false_is_honoured_on_a_wizard_too()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.ShowSubmitButton, false));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Next").Click();

        Assert.IsEmpty(cut.FindAll("button[type=submit]"));
    }
}

[TestClass]
public class FormTitleLevelTests : ComponentTestBase
{
    private static BormDefinition Titled()
    {
        var form = BormBuilder.Create().Text("a").Build();
        form.Title = "Contact us";
        return form;
    }

    [TestMethod]
    public void The_title_is_a_level_two_heading_by_default()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Titled()));
        var title = cut.Find(".bit-borm-form__title");

        Assert.AreEqual("heading", title.GetAttribute("role"));
        Assert.AreEqual("2", title.GetAttribute("aria-level"));
    }

    [TestMethod]
    public void A_form_nested_deeper_in_the_page_can_say_so()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Titled())
            .Add(x => x.TitleLevel, 4));

        Assert.AreEqual("4", cut.Find(".bit-borm-form__title").GetAttribute("aria-level"));
    }

    [TestMethod]
    public void A_level_outside_the_range_is_clamped_rather_than_emitted()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Titled())
            .Add(x => x.TitleLevel, 99));

        Assert.AreEqual("6", cut.Find(".bit-borm-form__title").GetAttribute("aria-level"));
    }
}

[TestClass]
public class SubmitOutcomeAnnouncementTests : ComponentTestBase
{
    private static BormDefinition Schema() => BormBuilder.Create()
        .Text("a", f => f.Required())
        .Build();

    // The status region is always in the page — a live region inserted together with its text is not
    // reliably read out — so "says nothing" means it is empty, not that it is absent.
    private static string Said(IRenderedComponent<BormView> cut)
        => string.Concat(cut.FindAll("[role=status]").Select(e => e.TextContent)).Trim();

    [TestMethod]
    public void With_no_summary_and_no_error_focus_the_outcome_is_announced()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.FocusFirstError, false));

        cut.Find("form").Submit();

        var status = cut.Find("[role=status]");
        Assert.Contains("problem", status.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void The_summary_already_announces_itself_so_nothing_is_said_twice()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.ShowErrorSummary, true)
            .Add(x => x.FocusFirstError, false));

        cut.Find("form").Submit();

        Assert.AreEqual(string.Empty, Said(cut));
    }

    [TestMethod]
    public void Focusing_the_first_error_already_reads_it_out_so_nothing_is_said_twice()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Schema()));

        cut.Find("form").Submit();

        Assert.AreEqual(string.Empty, Said(cut));
    }

    [TestMethod]
    public void A_form_nobody_has_submitted_says_nothing()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Schema())
            .Add(x => x.FocusFirstError, false));

        Assert.AreEqual(string.Empty, Said(cut));
    }
}
