using Bunit;

namespace Bit.Borm.Tests;

/// <summary>
/// A form's steps shown as tabs, an accordion or sections: moved between freely, validated together,
/// and pointing the user at whichever part has something to fix.
/// </summary>
[TestClass]
public class StepLayoutTests : ComponentTestBase
{
    private static BormDefinition Form(BormStepLayout layout) => BormBuilder.Create()
        .Text("name", f => f.Required())
        .Text("email")
        .Text("city", f => f.Required())
        .Checkbox("billing")
        .Text("vat", f => f.Required())
        .StepLayout(layout)
        .Step("person", s => s.Title("Person").Fields("name", "email"))
        .Step("address", s => s.Title("Address").Fields("city", "billing"))
        .Step("tax", s => s.Title("Tax").Fields("vat").VisibleWhen("{billing}"))
        .Build();

    [TestMethod]
    public void Tabs_show_one_panel_and_any_tab_can_be_opened_without_validating_the_one_left()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Tabs)));

        var tabs = cut.FindAll("[role=tab]");
        Assert.HasCount(2, tabs);   // "Tax" is hidden until billing is ticked
        Assert.AreEqual("true", tabs[0].GetAttribute("aria-selected"));
        Assert.AreEqual("0", tabs[0].GetAttribute("tabindex"));
        Assert.AreEqual("-1", tabs[1].GetAttribute("tabindex"));
        Assert.IsNotNull(cut.Find("#borm_name"));
        Assert.IsEmpty(cut.FindAll("#borm_city"));

        tabs[1].Click();

        Assert.IsNotNull(cut.Find("#borm_city"));
        Assert.IsEmpty(cut.FindAll("#borm_name"));
        // Leaving an unanswered required field behind is allowed: tabs are not a wizard.
        Assert.IsEmpty(cut.Instance.Form.AllMessages);
        Assert.AreEqual(cut.Find("[role=tabpanel]").GetAttribute("aria-labelledby"), cut.FindAll("[role=tab]")[1].Id);
    }

    [TestMethod]
    public void The_arrow_keys_home_and_end_move_between_tabs()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Tabs)));
        var state = cut.Instance.Form;

        cut.FindAll("[role=tab]")[0].KeyDown("ArrowRight");
        Assert.AreEqual(1, state.CurrentStepIndex);
        cut.FindAll("[role=tab]")[1].KeyDown("ArrowRight");   // wraps to the first
        Assert.AreEqual(0, state.CurrentStepIndex);
        cut.FindAll("[role=tab]")[0].KeyDown("End");
        Assert.AreEqual(1, state.CurrentStepIndex);
        cut.FindAll("[role=tab]")[1].KeyDown("Home");
        Assert.AreEqual(0, state.CurrentStepIndex);
    }

    [TestMethod]
    public async Task Submit_validates_every_tab_and_turns_to_the_first_with_something_to_fix()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Tabs)));
        var state = cut.Instance.Form;
        await cut.InvokeAsync(() => state.SetValue("name", "Ada"));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        Assert.AreEqual(1, state.CurrentStepIndex);
        var address = cut.FindAll("[role=tab]")[1];
        Assert.AreEqual("1", address.QuerySelector(".bit-borm-tabs__badge")!.TextContent);
        StringAssert.Contains(address.TextContent, "1 problem to fix");
        // A hidden tab is never the reason a submit fails.
        Assert.IsEmpty(state.MessagesFor("vat"));
    }

    [TestMethod]
    public void Enter_in_a_tab_submits_the_form_rather_than_moving_to_the_next_tab()
    {
        var submitted = false;
        var form = BormBuilder.Create().Text("a").Text("b").StepLayout(BormStepLayout.Tabs)
            .Step("one", s => s.Fields("a")).Step("two", s => s.Fields("b")).Build();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.OnValidSubmit, _ => submitted = true));

        cut.Find("form").Submit();

        Assert.IsTrue(submitted);
        Assert.AreEqual(0, cut.Instance.Form.CurrentStepIndex);
        // Every tab is reachable from the start.
        Assert.IsTrue(cut.Instance.Form.IsStepReachable(1));
    }

    [TestMethod]
    public async Task An_accordion_opens_its_first_section_and_every_section_with_an_error_after_a_submit()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Accordion)));
        var sections = cut.FindAll("details.bit-borm-section");
        Assert.HasCount(2, sections);
        Assert.IsTrue(sections[0].HasAttribute("open"));
        Assert.IsFalse(sections[1].HasAttribute("open"));
        // Closed sections keep their fields in the page, so nothing they hold is lost.
        Assert.IsNotNull(cut.Find("#borm_city"));

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());

        sections = cut.FindAll("details.bit-borm-section");
        Assert.IsTrue(sections[0].HasAttribute("open"));
        Assert.IsTrue(sections[1].HasAttribute("open"));
        Assert.Contains("has-errors", sections[1].ClassList);
    }

    [TestMethod]
    public void An_accordion_section_opens_and_closes_from_its_summary_and_never_answers_its_own_toggle()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Accordion)));
        var section = (int i) => cut.FindAll("details.bit-borm-section")[i];

        // The browser raises toggle for the open attribute this component renders too; answering it flipped the
        // section back, which raised it again, forever.
        Assert.IsTrue(cut.FindAll("details.bit-borm-section").All(d => !d.HasAttribute("blazor:ontoggle")));
        Assert.IsTrue(section(1).QuerySelector("summary")!.HasAttribute("blazor:onclick"));

        section(1).QuerySelector("summary")!.Click();
        Assert.IsTrue(section(1).HasAttribute("open"));
        Assert.IsTrue(section(0).HasAttribute("open"));

        section(1).QuerySelector("summary")!.Click();
        Assert.IsFalse(section(1).HasAttribute("open"));
    }

    [TestMethod]
    public void Sections_show_every_visible_step_as_a_titled_region()
    {
        var cut = Render<BormView>(p => p.Add(x => x.Definition, Form(BormStepLayout.Sections)).Add(x => x.TitleLevel, 2));

        var sections = cut.FindAll("section.bit-borm-section");
        Assert.HasCount(2, sections);
        var title = sections[1].QuerySelector("[role=heading]")!;
        Assert.AreEqual("Address", title.TextContent);
        Assert.AreEqual("3", title.GetAttribute("aria-level"));
        Assert.AreEqual(title.Id, sections[1].GetAttribute("aria-labelledby"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-stepper"));
    }

    [TestMethod]
    public void The_layout_travels_through_json_schema_and_a_wizard_stays_the_default()
    {
        var tabs = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(Form(BormStepLayout.Accordion)));
        Assert.AreEqual(BormStepLayout.Accordion, tabs.StepLayout);
        Assert.IsFalse(tabs.IsWizard);
        Assert.IsTrue(tabs.HasSteps);

        var wizard = BormJsonSchemaImporter.Import(BormJsonSchemaExporter.Export(Form(BormStepLayout.Wizard)));
        Assert.IsTrue(wizard.IsWizard);
        Assert.DoesNotContain("x-stepLayout", BormJsonSchemaExporter.Export(wizard));

        var unknown = BormJsonSchemaImporter.Import("""{"type":"object","properties":{"a":{}},"x-stepLayout":"carousel","x-steps":[{"id":"s","fields":["a"]}]}""");
        Assert.AreEqual(BormStepLayout.Wizard, unknown.StepLayout);
    }
}
