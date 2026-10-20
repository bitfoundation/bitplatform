using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// Renders the components for real, so the parts a unit test on the schema cannot see — the emitted
/// HTML, the accessibility wiring, and whether a field re-renders at all — are covered too.
/// </summary>
public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        Services.AddBorm();
    }
}

[TestClass]
public class FieldRenderingTests : ComponentTestBase
{
    private Bunit.IRenderedComponent<BormView> RenderForm(BormDefinition form, object? model = null)
        => model is null
            ? Render<BormView>(p => p.Add(x => x.Definition, form))
            : Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Model, model));

    [TestMethod]
    public void A_text_field_is_labelled_and_wired_for_assistive_tech()
    {
        var form = BormBuilder.Create()
            .Text("email", f => f.Label("Email").Required().Help("We never share it.").Autocomplete("email"))
            .Build();

        var cut = RenderForm(form);
        var input = cut.Find("input#borm_email");
        var label = cut.Find("label[for='borm_email']");

        Assert.Contains("Email", label.TextContent, StringComparison.Ordinal);
        Assert.AreEqual("true", input.GetAttribute("aria-required"));
        Assert.AreEqual("email", input.GetAttribute("autocomplete"));
        // The help text is announced with the field rather than being orphaned next to it.
        Assert.AreEqual("borm_email_help", input.GetAttribute("aria-describedby"));
        Assert.IsNotNull(cut.Find("#borm_email_help"));
    }

    [TestMethod]
    public void The_form_opts_out_of_native_browser_validation()
    {
        // Borm owns validation; leaving the browser's on would let `pattern` block submit with a
        // bubble the app cannot style, translate or read.
        var cut = RenderForm(BormBuilder.Create().Text("a").Build());
        Assert.IsTrue(cut.Find("form").HasAttribute("novalidate"));
    }

    [TestMethod]
    public void A_read_only_field_is_readonly_not_disabled()
    {
        var cut = RenderForm(BormBuilder.Create().Text("a", f => f.ReadOnly()).Build());
        var input = cut.Find("input#borm_a");

        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.IsFalse(input.HasAttribute("disabled"));
    }

    [TestMethod]
    public void A_conditionally_disabled_field_is_disabled()
    {
        var form = BormBuilder.Create()
            .Checkbox("locked")
            .Text("a", f => f.DisabledWhen("locked", BormConditionOperator.IsTrue))
            .Build();

        var data = new Dictionary<string, object?> { ["locked"] = true };
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form).Add(x => x.Data, data));

        Assert.IsTrue(cut.Find("input#borm_a").HasAttribute("disabled"));
    }

    [TestMethod]
    public void Numeric_attributes_are_written_invariantly()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var form = BormBuilder.Create().Number("price", f => f.Range(0.5, 10.5).Step(0.5)).Build();

            var input = RenderForm(form).Find("input#borm_price");

            // "0,5" would be an invalid HTML attribute value.
            Assert.AreEqual("0.5", input.GetAttribute("min"));
            Assert.AreEqual("10.5", input.GetAttribute("max"));
            Assert.AreEqual("0.5", input.GetAttribute("step"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    public void A_radio_group_is_labelled_as_a_group()
    {
        var form = BormBuilder.Create()
            .Radio("kind", f => f.Label("Account type").Options(("a", "A"), ("b", "B")).Required())
            .Build();

        var cut = RenderForm(form);
        var group = cut.Find("[role='radiogroup']");

        // There is no single control for a <label for> to point at, so the group names itself.
        Assert.AreEqual("borm_kind_label", group.GetAttribute("aria-labelledby"));
        Assert.IsNotNull(cut.Find("#borm_kind_label"));
        Assert.AreEqual(2, cut.FindAll("input[type=radio]").Count);
    }

    [TestMethod]
    public void A_hidden_field_renders_nothing_visible()
    {
        var form = BormBuilder.Create()
            .Text("shown")
            .Text("secret", f => f.VisibleWhen("shown", BormConditionOperator.IsNotEmpty))
            .Build();

        var cut = RenderForm(form);

        Assert.IsNotNull(cut.Find("input#borm_shown"));
        Assert.IsEmpty(cut.FindAll("input#borm_secret"));
    }

    [TestMethod]
    public void A_file_field_renders_a_file_input()
    {
        var form = BormBuilder.Create()
            .File("cv", f => f.Accept(".pdf", multiple: true))
            .Build();

        var input = RenderForm(form).Find("input[type=file]");

        Assert.AreEqual(".pdf", input.GetAttribute("accept"));
        Assert.IsTrue(input.HasAttribute("multiple"));
    }

    [TestMethod]
    public void An_unregistered_custom_renderer_fails_with_a_message_naming_the_key()
    {
        var form = BormBuilder.Create()
            .Field("rating", BormFieldType.Custom, f => f.CustomRenderer("stars"))
            .Build();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => RenderForm(form));
        Assert.Contains("stars", ex.Message, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Multi_column_layout_emits_a_grid()
    {
        var form = BormBuilder.Create()
            .Columns(2)
            .Text("a")
            .Text("b", f => f.ColumnSpan(2))
            .Build();

        var cut = RenderForm(form);

        Assert.Contains("bit-borm-grid", cut.Find(".bit-borm-form__body").ClassName!, StringComparison.Ordinal);
        Assert.Contains("span 2", cut.Find(".bit-borm-field:last-child").GetAttribute("style")!, StringComparison.Ordinal);
    }
}

[TestClass]
public class ValidationRenderingTests : ComponentTestBase
{
    [TestMethod]
    public async Task Errors_appear_only_after_the_user_engages_with_the_form()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        // A blank form does not open covered in red.
        Assert.IsEmpty(cut.FindAll(".bit-borm-message"));

        await cut.Find("form").SubmitAsync();

        var message = cut.Find(".bit-borm-message--error");
        Assert.AreEqual("This field is required.", message.TextContent);
        Assert.AreEqual("true", cut.Find("input#borm_name").GetAttribute("aria-invalid"));
        // The error is announced with the field, not left as an unassociated paragraph.
        Assert.Contains("borm_name_error", cut.Find("input#borm_name").GetAttribute("aria-describedby")!, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task The_error_summary_lists_problems_in_schema_order_and_links_to_each_field()
    {
        var form = BormBuilder.Create()
            .Text("first", f => f.Label("First").Required())
            .Text("second", f => f.Label("Second").Required())
            .Build();

        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.ShowErrorSummary, true));

        Assert.IsEmpty(cut.FindAll(".bit-borm-summary"));

        await cut.Find("form").SubmitAsync();

        var links = cut.FindAll(".bit-borm-summary__list a");
        Assert.AreEqual(2, links.Count);
        // Built on this page's own address: a bare "#borm_first" resolves against <base href>, not the page.
        Assert.AreEqual("http://localhost/#borm_first", links[0].GetAttribute("href"));
        Assert.Contains("First:", links[0].TextContent, StringComparison.Ordinal);
        Assert.AreEqual("http://localhost/#borm_second", links[1].GetAttribute("href"));

        // The summary takes focus itself, so it must NOT also be a live region: a screen reader would
        // announce it once for the region changing and again for focus arriving.
        Assert.IsNull(cut.Find(".bit-borm-summary").GetAttribute("role"));
        Assert.AreEqual("-1", cut.Find(".bit-borm-summary").GetAttribute("tabindex"));
    }

    [TestMethod]
    public async Task The_error_summary_announces_itself_when_it_does_not_take_focus()
    {
        var form = BormBuilder.Create()
            .Text("first", f => f.Label("First").Required())
            .Build();

        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormErrorSummary>(p => p
            .Add(x => x.State, state)
            .Add(x => x.AutoFocus, false));

        await state.SubmitAsync();
        cut.Render();

        // Nothing else would tell the user anything, so here the live region is the announcement.
        Assert.AreEqual("alert", cut.Find(".bit-borm-summary").GetAttribute("role"));
    }

    [TestMethod]
    public async Task An_unlabelled_field_is_still_named_in_the_error_summary()
    {
        // A hand-built schema need not label every field; "This field is required." on its own names
        // nothing, which is the one job a summary entry has. (The builders always humanise a label
        // from the name, so an unlabelled field only arrives from a definition assembled by hand.)
        var form = new BormDefinition
        {
            Fields = { new BormFieldDefinition("orderRef", BormFieldType.Text)
                { Validators = { new BormRequiredRule() } } }
        };

        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.ShowErrorSummary, true));

        await cut.Find("form").SubmitAsync();

        Assert.Contains("orderRef:", cut.Find(".bit-borm-summary__list").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Form_level_messages_are_shown_even_without_a_summary()
    {
        var form = BormBuilder.For<BookingModel>()
            .Field(x => x.Start)
            .Field(x => x.End)
            .MustAll(m => m.End >= m.Start, "End must be on or after start.")
            .Build();

        var model = new BookingModel { Start = new DateOnly(2024, 5, 10), End = new DateOnly(2024, 5, 1) };
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Model, model));

        await cut.Find("form").SubmitAsync();

        Assert.Contains(e => e.TextContent == "End must be on or after start.", cut.FindAll(".bit-borm-message"));
    }

    [TestMethod]
    public async Task A_valid_submit_reaches_the_handler_exactly_once()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var calls = 0;

        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.OnValidSubmit, (BormState _) => calls++));

        await cut.Find("form").SubmitAsync();

        Assert.AreEqual(1, calls);
    }
}

[TestClass]
public class ArrayRenderingTests : ComponentTestBase
{
    private static BormDefinition Invoice() => BormBuilder.For<RegistrationModel>()
        .Array(x => x.Items, i => i.Field(l => l.Product).Field(l => l.Quantity),
            f => f.Items(min: 1, max: 2).Attr("itemNoun", "line"))
        .Build();

    [TestMethod]
    public void An_empty_array_explains_itself_and_offers_an_add_button()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Invoice())
            .Add(x => x.Model, new RegistrationModel()));

        Assert.AreEqual("No lines yet.", cut.Find(".bit-borm-array__empty").TextContent);
        Assert.Contains("Add line", cut.Find(".bit-borm-btn--add").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Adding_and_removing_items_updates_the_model_and_the_dom()
    {
        var model = new RegistrationModel();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Invoice())
            .Add(x => x.Model, model));

        cut.Find(".bit-borm-btn--add").Click();

        Assert.ContainsSingle(model.Items);
        Assert.IsNotNull(cut.Find("input#borm_Items_0_Product"));

        // At the minimum of one item, removal is refused rather than silently breaking the rule.
        Assert.IsTrue(cut.Find(".bit-borm-btn--danger").HasAttribute("disabled"));

        cut.Find(".bit-borm-btn--add").Click();
        Assert.AreEqual(2, model.Items.Count);

        // And at the maximum of two, no more can be added.
        Assert.IsTrue(cut.Find(".bit-borm-btn--add").HasAttribute("disabled"));

        cut.FindAll(".bit-borm-btn--danger")[0].Click();
        Assert.ContainsSingle(model.Items);
    }

    [TestMethod]
    public void Reorder_buttons_are_labelled_for_screen_readers()
    {
        var model = new RegistrationModel();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Invoice())
            .Add(x => x.Model, model));

        cut.Find(".bit-borm-btn--add").Click();
        cut.Find(".bit-borm-btn--add").Click();

        var buttons = cut.FindAll(".bit-borm-btn--icon");
        Assert.AreEqual("Move line 1 up", buttons[0].GetAttribute("aria-label"));
        Assert.AreEqual("Move line 1 down", buttons[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public void Moving_an_item_carries_its_values_with_it()
    {
        var model = new RegistrationModel();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Invoice())
            .Add(x => x.Model, model));

        cut.Find(".bit-borm-btn--add").Click();
        cut.Find(".bit-borm-btn--add").Click();
        cut.Find("input#borm_Items_0_Product").Change("first");
        cut.Find("input#borm_Items_1_Product").Change("second");

        // "Move down" on the first row.
        cut.FindAll(".bit-borm-btn--icon")[1].Click();

        Assert.AreEqual("second", model.Items[0].Product);
        Assert.AreEqual("first", model.Items[1].Product);
    }
}

[TestClass]
public class RenderEfficiencyTests : ComponentTestBase
{
    /// <summary>
    /// Counts how often it is rendered, so a test can prove a field was left alone. The counter is
    /// carried in a DI-registered object rather than a static so the tests stay independent.
    /// </summary>
    private sealed class RenderCounter
    {
        private readonly Dictionary<string, int> _byPath = new(StringComparer.Ordinal);

        public void Record(string path) => _byPath[path] = _byPath.GetValueOrDefault(path) + 1;

        public int this[string path] => _byPath.GetValueOrDefault(path);
    }

    private sealed class CountingInput : BormInputBase
    {
        [Microsoft.AspNetCore.Components.Inject] private RenderCounter Counter { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            Counter.Record(Context.Path);
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "id", Context.ElementId);
            builder.AddAttribute(2, "value", Context.StringValue);
            builder.AddAttribute(3, "onchange",
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.ChangeEventArgs>(
                    this, e => Context.SetFromStringAsync(e.Value?.ToString())));
            builder.CloseElement();
        }
    }

    [TestMethod]
    public void Editing_one_field_does_not_re_render_the_others()
    {
        // Without per-field render gating, every keystroke re-diffs every control on the form — which
        // is what makes large schema-driven forms feel slow.
        var counter = new RenderCounter();
        Services.AddSingleton(counter);
        // Registered before the base fixture's registry would win: AddBorm uses TryAdd.
        Services.AddSingleton<IBormFieldRendererRegistry>(_ =>
        {
            var registry = new BormFieldRendererRegistry();
            registry.Register(BormFieldType.Text, typeof(CountingInput));
            return registry;
        });

        var form = BormBuilder.Create()
            .Text("a").Text("b").Text("c").Text("d").Text("e")
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));

        Assert.AreEqual(1, counter["a"]);
        Assert.AreEqual(1, counter["e"]);

        cut.Find("input#borm_a").Change("hello");

        // The edited field redraws; every other field is left exactly as it was.
        Assert.IsTrue(counter["a"] > 1, "the edited field should re-render");
        foreach (var untouched in (string[])["b", "c", "d", "e"])
            Assert.AreEqual(1, counter[untouched]);
    }

    [TestMethod]
    public void A_field_whose_visibility_flips_does_re_render()
    {
        var form = BormBuilder.Create()
            .Text("trigger")
            .Text("dependent", f => f.VisibleWhen("trigger", BormConditionOperator.IsNotEmpty))
            .Build();

        var cut = Render<BormView>(p => p.Add(x => x.Definition, form));
        Assert.IsEmpty(cut.FindAll("input#borm_dependent"));

        cut.Find("input#borm_trigger").Change("something");

        Assert.IsNotNull(cut.Find("input#borm_dependent"));
    }
}

[TestClass]
public class WizardRenderingTests : ComponentTestBase
{
    private static BormDefinition Wizard() => BormBuilder.For<SignupModel>()
        .Field(x => x.Email, f => f.Required())
        .Field(x => x.IsBusiness)
        .Field(x => x.CompanyName)
        .Field(x => x.Country)
        .Step("contact", s => s.Title("Contact").Fields("Email"))
        .Step("company", s => s.Title("Company").Fields("CompanyName")
            .VisibleWhen(nameof(SignupModel.IsBusiness), BormConditionOperator.IsTrue))
        .Step("done", s => s.Title("Done").Fields("Country"))
        .Build();

    [TestMethod]
    public void Hidden_steps_leave_no_gap_in_the_numbering()
    {
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.Model, new SignupModel()));

        var numbers = cut.FindAll(".bit-borm-stepper__num").Select(n => n.TextContent).ToList();

        // The company step is hidden, so the remaining two are 1 and 2 — not 1 and 3.
        Assert.AreSequenceEqual(["1", "2"], numbers);
    }

    [TestMethod]
    public void A_step_will_not_advance_while_its_own_fields_are_invalid()
    {
        var model = new SignupModel();
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.Model, model));

        cut.Find(".bit-borm-btn--primary").Click();
        Assert.Contains("Contact", cut.Find(".bit-borm-stepper__step.is-active").TextContent, StringComparison.Ordinal);
        Assert.IsNotEmpty(cut.FindAll(".bit-borm-message--error"));

        cut.Find("input#borm_Email").Change("ada@example.com");
        cut.Find(".bit-borm-btn--primary").Click();

        Assert.Contains("Done", cut.Find(".bit-borm-stepper__step.is-active").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public void Completed_steps_are_clickable_and_upcoming_ones_are_not()
    {
        var model = new SignupModel { Email = "ada@example.com" };
        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, Wizard())
            .Add(x => x.Model, model));

        Assert.IsEmpty(cut.FindAll(".bit-borm-stepper__link"));

        cut.Find(".bit-borm-btn--primary").Click(); // advance to "Done"

        var links = cut.FindAll(".bit-borm-stepper__link");
        Assert.ContainsSingle(links);

        links[0].Click();
        Assert.Contains("Contact", cut.Find(".bit-borm-stepper__step.is-active").TextContent, StringComparison.Ordinal);
    }
}

[TestClass]
public class ReadOnlyModeTests : ComponentTestBase
{
    [TestMethod]
    public void Read_only_mode_locks_every_control_and_hides_the_buttons()
    {
        var form = BormBuilder.Create()
            .Text("name")
            .Select("kind", f => f.Options(("a", "A")))
            .Checkbox("agree")
            .Build();

        var cut = Render<BormView>(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.ReadOnly, true)
            .Add(x => x.ShowResetButton, true));

        Assert.IsTrue(cut.Find("input#borm_name").HasAttribute("readonly"));
        // A <select> and a checkbox have no readonly attribute, so they have to be disabled instead.
        Assert.IsTrue(cut.Find("select#borm_kind").HasAttribute("disabled"));
        Assert.IsTrue(cut.Find("input#borm_agree").HasAttribute("disabled"));
        Assert.IsEmpty(cut.FindAll(".bit-borm-form__actions"));
    }
}
