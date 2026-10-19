using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>Renders every UI string in Klingon, so anything hard-coded in English stands out.</summary>
public sealed class StubMessageProvider : IBormMessageProvider
{
    public string Get(string key, params object?[] args) => $"[{key}]";
}

[TestClass]
public class LocalisationTests : BunitContext
{
    public LocalisationTests() => Services.AddBorm();

    private void UseStubMessages() => Services.AddBormMessages(new StubMessageProvider());

    [TestMethod]
    public void Every_button_and_placeholder_goes_through_the_message_provider()
    {
        UseStubMessages();
        var form = BormBuilder.Create()
            .Select("topic", f => f.Options(("a", "A")))
            .ArrayOf("tags", BormFieldType.Text)
            .Build();

        var html = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.ShowResetButton, true)).Markup;

        Assert.Contains($"[{BormMessageKeys.Submit}]", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.Reset}]", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.SelectPlaceholder}]", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.ArrayAdd}]", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.ArrayEmpty}]", html, StringComparison.Ordinal);

        // Nothing English survived.
        Assert.DoesNotContain("-- Select --", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Submit<", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void An_explicit_button_label_still_wins_over_the_provider()
    {
        UseStubMessages();
        var form = BormBuilder.Create().Text("name").Build();

        var html = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.SubmitText, "Send it")).Markup;

        Assert.Contains("Send it", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_wizard_announces_its_position_and_names_its_stepper()
    {
        UseStubMessages();
        var form = BormBuilder.Create()
            .Text("a").Text("b")
            .Step("one", s => s.Fields("a"))
            .Step("two", s => s.Fields("b"))
            .Build();

        var html = Render<BormView>(p => p.Add(x => x.Definition, form)).Markup;

        Assert.Contains($"aria-label=\"[{BormMessageKeys.Progress}]\"", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.StepOf}]", html, StringComparison.Ordinal);
        // The step body is focusable, so advancing can take the user to the new content.
        Assert.Contains("class=\"bit-borm-step-body \" tabindex=\"-1\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_wizard_buttons_are_localised()
    {
        UseStubMessages();
        var form = BormBuilder.Create()
            .Text("a").Text("b")
            .Step("one", s => s.Fields("a"))
            .Step("two", s => s.Fields("b"))
            .Build();

        var html = Render<BormView>(p => p.Add(x => x.Definition, form)).Markup;

        Assert.Contains($"[{BormMessageKeys.Back}]", html, StringComparison.Ordinal);
        Assert.Contains($"[{BormMessageKeys.Next}]", html, StringComparison.Ordinal);
    }
}

[TestClass]
public class FieldPresentationTests : BunitContext
{
    public FieldPresentationTests() => Services.AddBorm();

    private string RenderMarkup(BormDefinition form)
        => Render<BormView>(p => p.Add(x => x.Definition, form)).Markup;

    [TestMethod]
    public void A_prefix_and_suffix_are_rendered_around_the_input()
    {
        var form = BormBuilder.Create()
            .Number("price", f => f.Label("Price").Prefix("$").Suffix("per month"))
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("bit-borm-affix--prefix", html, StringComparison.Ordinal);
        Assert.Contains("bit-borm-affix--suffix", html, StringComparison.Ordinal);
        // Decoration, not content: a screen reader reads the label, not the "$".
        Assert.Contains("aria-hidden=\"true\">$<", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_hidden_label_becomes_the_accessible_name()
    {
        var form = BormBuilder.Create()
            .Text("q", f => f.Label("Search").HideLabel())
            .Build();

        var html = RenderMarkup(form);

        Assert.DoesNotContain("class=\"bit-borm-label\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Search\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_radio_group_with_a_hidden_label_is_still_named()
    {
        var form = BormBuilder.Create()
            .Radio("size", f => f.Label("Size").HideLabel().Options(("s", "Small"), ("l", "Large")))
            .Build();

        var html = RenderMarkup(form);

        // Pointing aria-labelledby at an element that was never rendered would leave it anonymous.
        Assert.DoesNotContain("aria-labelledby", html, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Size\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_character_counter_reports_the_current_length()
    {
        var form = BormBuilder.Create()
            .TextArea("bio", f => f.MaxLength(200).CharacterCount())
            .Build();
        var data = new Dictionary<string, object?> { ["bio"] = "hello" };

        var html = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Data, data)).Markup;

        Assert.Contains("bit-borm-counter", html, StringComparison.Ordinal);
        Assert.Contains("5 / 200", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Schema_attributes_are_splatted_onto_the_control()
    {
        var form = BormBuilder.Create()
            .Text("code", f => f.InputAttr("data-testid", "code-box").InputAttr("spellcheck", "false"))
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("data-testid=\"code-box\"", html, StringComparison.Ordinal);
        Assert.Contains("spellcheck=\"false\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Splatted_attributes_cannot_overwrite_the_accessibility_wiring()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.Label("Name").Help("Hint").InputAttr("aria-describedby", "hijacked"))
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("aria-describedby=\"borm_name_help\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hijacked", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Suggestions_render_a_datalist_without_restricting_the_field()
    {
        var form = BormBuilder.Create()
            .Text("city", f => f.Suggest("Berlin", "Cairo"))
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("<datalist id=\"borm_city_list\"", html, StringComparison.Ordinal);
        Assert.Contains("list=\"borm_city_list\"", html, StringComparison.Ordinal);
        Assert.Contains("Berlin", html, StringComparison.Ordinal);
        // Still a text box, not a select.
        Assert.Contains("type=\"text\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Static_content_renders_a_heading_and_no_input()
    {
        var form = BormBuilder.Create()
            .Static("intro", "Your details", "We only use these to contact you.")
            .Text("name")
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("role=\"heading\"", html, StringComparison.Ordinal);
        Assert.Contains("Your details", html, StringComparison.Ordinal);
        Assert.Contains("We only use these to contact you.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("borm_intro\" class", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Static_content_is_never_validated_and_never_blocks_a_submit()
    {
        var form = BormBuilder.Create()
            .Static("intro", "Heading")
            .Text("name")
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Assert.IsTrue(await state.ValidateAsync());
    }

    [TestMethod]
    public void A_revealable_password_toggles_between_masked_and_plain()
    {
        var form = BormBuilder.Create()
            .Password("secret", f => f.Revealable())
            .Build();
        var view = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.Contains("type=\"password\"", view.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"false\"", view.Markup, StringComparison.Ordinal);

        view.Find("button[aria-pressed]").Click();

        Assert.Contains("type=\"text\"", view.Markup, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"true\"", view.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_password_is_masked_unless_the_schema_opts_into_revealing_it()
    {
        // Whether a password may be shown at all depends on where the form runs, so it is never assumed.
        var form = BormBuilder.Create().Password("secret").Build();

        var html = RenderMarkup(form);

        Assert.Contains("type=\"password\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-pressed", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_clear_button_appears_only_when_there_is_something_to_clear()
    {
        var form = BormBuilder.Create().Text("q", f => f.Clearable()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var view = Render<BormView>(p => p.Add(x => x.State, state));

        Assert.IsEmpty(view.FindAll("button.bit-borm-affix--btn"));

        view.Find("input").Change("hello");
        Assert.ContainsSingle(view.FindAll("button.bit-borm-affix--btn"));

        view.Find("button.bit-borm-affix--btn").Click();
        Assert.IsNull(state.GetValue("q"));
    }

    [TestMethod]
    public void A_switch_is_a_checkbox_that_announces_itself_as_one()
    {
        var form = BormBuilder.Create()
            .Checkbox("notify", f => f.Label("Email me").AsSwitch())
            .Build();

        var html = RenderMarkup(form);

        Assert.Contains("type=\"checkbox\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"switch\"", html, StringComparison.Ordinal);
        Assert.Contains("bit-borm-switch", html, StringComparison.Ordinal);
    }
}

[TestClass]
public class SchemaDiagnosticsTests
{
    [TestMethod]
    public void A_healthy_schema_reports_nothing()
    {
        var form = BormBuilder.For<RegistrationModel>()
            .Field(x => x.FirstName)
            .Field(x => x.Email)
            .Build();

        Assert.IsEmpty(form.Validate());
    }

    [TestMethod]
    public void Duplicate_sibling_names_are_reported()
    {
        // The builder rejects duplicates, so this is the schema-assembled-by-hand case.
        var form = new BormDefinition();
        form.Fields.Add(new BormFieldDefinition("name", BormFieldType.Text));
        form.Fields.Add(new BormFieldDefinition("Name", BormFieldType.Text));

        var problem = Assert.ContainsSingle(form.Validate());
        Assert.AreEqual(BormSchemaDiagnosticSeverity.Error, problem.Severity);
        Assert.Contains("overwrite each other", problem.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void An_array_without_an_item_template_is_reported()
    {
        var form = new BormDefinition();
        form.Fields.Add(new BormFieldDefinition("lines", BormFieldType.Array));

        var problem = Assert.ContainsSingle(form.Validate());
        Assert.Contains("ItemTemplate", problem.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_step_naming_an_unknown_field_is_reported()
    {
        var form = BormBuilder.Create()
            .Text("name")
            .Step("one", s => s.Fields("name", "nope"))
            .Build();

        var problem = Assert.ContainsSingle(d => d.Path == "nope", form.Validate());
        Assert.Contains("not in the schema", problem.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_condition_pointing_at_no_field_is_a_warning()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.VisibleWhen("ghost", BormConditionOperator.IsTrue))
            .Build();

        var problem = Assert.ContainsSingle(form.Validate());
        Assert.AreEqual(BormSchemaDiagnosticSeverity.Warning, problem.Severity);
        Assert.Contains("'ghost'", problem.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_condition_naming_a_sibling_inside_a_repeater_is_accepted()
    {
        var form = BormBuilder.For<ContactBook>()
            .Field(x => x.Kind)
            .Array(x => x.Rows, row => row
                .Field(r => r.Kind)
                .Field(r => r.Email, f => f.VisibleWhen("Kind", BormConditionOperator.Equals, "email")))
            .Build();

        Assert.IsEmpty(form.Validate());
    }

    [TestMethod]
    public void A_field_belonging_to_no_wizard_step_is_a_warning()
    {
        var form = BormBuilder.Create()
            .Text("shown").Text("orphan")
            .Step("one", s => s.Fields("shown"))
            .Build();

        var problem = Assert.ContainsSingle(form.Validate());
        Assert.AreEqual("orphan", problem.Path);
        Assert.AreEqual(BormSchemaDiagnosticSeverity.Warning, problem.Severity);
    }
}
