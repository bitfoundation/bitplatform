using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// A text input that remembers the element it rendered on every render, so a test can tell an element
/// that was updated in place from one that was thrown away and recreated — the difference between a
/// caret that stays put and one that is lost.
/// </summary>
public sealed class ElementSpyTextInput : BormTextInput
{
    public static readonly List<string> ElementIds = [];

    protected override void OnAfterRender(bool firstRender)
    {
        ElementIds.Add(Element.Id);
        base.OnAfterRender(firstRender);
    }
}

/// <summary>
/// Regressions found by reviewing the components as a user meets them: the error summary's links, the
/// cascading select that never reloaded, the tag list that crashed on its own data, the debounced value
/// that Enter left behind, and the rest. Each test is the failing scenario, written to pass.
/// </summary>
[TestClass]
public class ComponentRegressionTests : ComponentTestBase
{
    private IRenderedComponent<BormView> RenderForm(
        BormDefinition form,
        IDictionary<string, object?>? data = null,
        Action<ComponentParameterCollectionBuilder<BormView>>? more = null)
        => Render<BormView>(p =>
        {
            p.Add(x => x.Definition, form);
            if (data is not null) p.Add(x => x.Data, data);
            more?.Invoke(p);
        });

    private int FocusCalls()
        => JSInterop.Invocations.Count(i => i.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- error summary links

    [TestMethod]
    public async Task A_summary_link_names_this_page_and_does_not_navigate()
    {
        // A bare "#borm_first" resolves against <base href="/">, not the page — on any form not at the
        // root the router followed it to the home page and the form was gone.
        Services.GetRequiredService<NavigationManager>().NavigateTo("register?step=2#top");
        var form = BormBuilder.Create().Text("first", f => f.Required()).Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.ShowErrorSummary, true));

        await cut.Find("form").SubmitAsync();

        var link = cut.Find(".bit-borm-summary__list a");
        Assert.AreEqual("http://localhost/register?step=2#borm_first", link.GetAttribute("href"));
        Assert.IsTrue(link.HasAttribute("blazor:onclick:preventdefault"),
            "a plain click must be kept from following the link; the handler moves focus itself");
    }

    [TestMethod]
    public async Task Summary_links_land_on_an_element_for_every_kind_of_field()
    {
        var form = BormBuilder.Create()
            .Radio("color", f => f.Required().Options(("r", "Red"), ("g", "Green")))
            .MultiSelect("days", f => f.Items(min: 1).Options(("mon", "Monday"), ("tue", "Tuesday")))
            .ArrayOf("lines", BormFieldType.Text, f => f.Items(min: 1))
            .Object("address", c => c.Text("city"))
            .Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.ShowErrorSummary, true));

        await cut.Find("form").SubmitAsync();
        await cut.InvokeAsync(() => cut.Instance.Form.SetServerError("address", "Cannot deliver there"));

        var hrefs = cut.FindAll(".bit-borm-summary__list a").Select(a => a.GetAttribute("href")!).ToList();
        Assert.AreEqual(4, hrefs.Count);
        foreach (var href in hrefs)
        {
            var id = href[(href.IndexOf('#', StringComparison.Ordinal) + 1)..];
            Assert.ContainsSingle(cut.FindAll($"[id='{id}']"));
        }
    }

    // ---------------------------------------------------------------- cascading options

    private static BormDefinition CityAfterCountry(Func<string?, ValueTask<IReadOnlyList<BormSelectOption>>> cities)
        => BormBuilder.Create()
            .Select("country", f => f.Options(("fr", "France"), ("de", "Germany")))
            .Select("city", f => f.OptionsFrom(ctx => cities(ctx.Value("country") as string), "country"))
            .Build();

    [TestMethod]
    public void A_dependent_list_that_started_empty_loads_when_its_dependency_is_chosen()
    {
        var form = CityAfterCountry(country => ValueTask.FromResult<IReadOnlyList<BormSelectOption>>(
            country == "fr" ? [new("paris", "Paris")] : []));
        var cut = RenderForm(form);

        cut.Find("#borm_country").Change("fr");

        Assert.Contains("Paris", cut.Find("#borm_city").InnerHtml, StringComparison.Ordinal);
    }

    [TestMethod]
    public void A_dependent_list_from_an_async_provider_that_started_empty_loads_too()
    {
        var form = CityAfterCountry(async country =>
        {
            await Task.Delay(10);
            return country == "fr" ? [new("paris", "Paris")] : [];
        });
        var cut = RenderForm(form);
        cut.WaitForAssertion(() => Assert.IsFalse(cut.Find("#borm_city").HasAttribute("aria-busy")));

        cut.Find("#borm_country").Change("fr");

        cut.WaitForAssertion(() => Assert.Contains("Paris", cut.Find("#borm_city").InnerHtml, StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task A_failing_provider_is_not_retried_on_every_render_but_is_retried_when_invalidated()
    {
        var calls = 0;
        var form = BormBuilder.Create()
            .Text("note")
            .Select("city", f => f.OptionsFrom(_ =>
            {
                calls++;
                throw new InvalidOperationException("down");
            }))
            .Build();
        var cut = RenderForm(form);
        var afterFirstRender = calls;

        cut.Find("#borm_note").Change("x");
        cut.Find("#borm_note").Change("y");
        Assert.AreEqual(afterFirstRender, calls);

        await cut.InvokeAsync(() => cut.Instance.Form.InvalidateOptions("city"));
        Assert.AreEqual(afterFirstRender + 1, calls);
    }

    // ---------------------------------------------------------------- tags

    [TestMethod]
    public void A_tag_list_holding_a_duplicate_renders_and_keeps_rendering()
    {
        var form = BormBuilder.Create().Tags("skills").Text("note").Build();
        var data = new Dictionary<string, object?> { ["skills"] = new List<string> { "a", "a", "b" } };
        var cut = RenderForm(form, data);

        cut.Find("#borm_note").Change("x");

        Assert.AreEqual(3, cut.FindAll(".bit-borm-tag").Count);
    }

    [TestMethod]
    public void Removing_a_duplicated_tag_removes_only_the_chip_that_was_pressed()
    {
        var form = BormBuilder.Create().Tags("skills").Build();
        var data = new Dictionary<string, object?> { ["skills"] = new List<string> { "a", "b", "a" } };
        var cut = RenderForm(form, data);

        cut.FindAll(".bit-borm-tag__remove")[2].Click();

        Assert.AreSequenceEqual(["a", "b"], ((IEnumerable<object?>)data["skills"]!).Select(t => t?.ToString()));
    }

    // ---------------------------------------------------------------- debounced values

    [TestMethod]
    public async Task Enter_in_a_debounced_field_submits_what_was_typed()
    {
        var form = BormBuilder.Create().Text("q", f => f.Required().UpdateOnInput(debounceMilliseconds: 500)).Build();
        object? submitted = null;
        var cut = RenderForm(form, new Dictionary<string, object?>(), p => p
            .Add(x => x.OnValidSubmit, (BormState s) => { submitted = s.GetValue("q"); }));

        // Enter commits the box (change) and then submits the form, with no blur in between.
        _ = cut.Find("#borm_q").InputAsync(new ChangeEventArgs { Value = "hello" });
        await cut.Find("#borm_q").ChangeAsync(new ChangeEventArgs { Value = "hello" });
        await cut.Find("form").SubmitAsync();

        Assert.AreEqual("hello", submitted);
    }

    [TestMethod]
    public async Task A_debounced_value_survives_its_wizard_step_going_away()
    {
        var form = BormBuilder.Create()
            .Text("q", f => f.UpdateOnInput(debounceMilliseconds: 500))
            .Text("r")
            .Step("one", s => s.Fields("q"))
            .Step("two", s => s.Fields("r"))
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = RenderForm(form, data);

        // No change event this time: the step goes away with the keystrokes still waiting.
        _ = cut.Find("#borm_q").InputAsync(new ChangeEventArgs { Value = "hello" });
        await cut.InvokeAsync(async () => await cut.Instance.Form.NextStepAsync());

        cut.WaitForAssertion(() => Assert.IsNotNull(cut.Find("#borm_r")));
        Assert.AreEqual("hello", data["q"]);
        await Task.Delay(600);
        Assert.IsNull(data.GetValueOrDefault("r"));
    }

    [TestMethod]
    public async Task A_debounced_value_is_not_written_back_into_a_field_a_condition_has_hidden()
    {
        // The condition hid the field and ClearOnHide emptied it; writing the pending text on the way
        // out would undo that, and the stale value would reach the submit handler.
        var form = BormBuilder.Create()
            .Checkbox("show", f => f.Default(true))
            .Text("q", f => f.UpdateOnInput(debounceMilliseconds: 500).ClearOnHide()
                .VisibleWhen("show", BormConditionOperator.IsTrue))
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = RenderForm(form, data);

        _ = cut.Find("#borm_q").InputAsync(new ChangeEventArgs { Value = "hello" });
        await cut.InvokeAsync(() => cut.Instance.Form.SetValue("show", false));

        Assert.IsEmpty(cut.FindAll("#borm_q"));
        await Task.Delay(600);
        Assert.IsNull(data.GetValueOrDefault("q"));
    }

    [TestMethod]
    public async Task A_debounced_value_does_not_recreate_a_row_removed_under_it()
    {
        var form = BormBuilder.Create()
            .ArrayOf("codes", BormFieldType.Text, configureItem: f => f.UpdateOnInput(debounceMilliseconds: 500))
            .Build();
        var data = new Dictionary<string, object?> { ["codes"] = new List<object?> { "A", "B" } };
        var cut = RenderForm(form, data);

        _ = cut.Find("#borm_codes_1").InputAsync(new ChangeEventArgs { Value = "B-EDITED" });
        await cut.InvokeAsync(() => cut.Instance.Form.RemoveArrayItem("codes", 1));
        await Task.Delay(600);

        Assert.AreSequenceEqual(["A"], ((List<object?>)data["codes"]!).Cast<string>());
    }

    [TestMethod]
    public async Task A_debounced_value_follows_its_row_when_an_earlier_row_is_removed()
    {
        var form = BormBuilder.Create()
            .Array("items", item => item.Text("name", f => f.UpdateOnInput(debounceMilliseconds: 300)))
            .Build();
        var data = new Dictionary<string, object?>();
        var cut = RenderForm(form, data);
        var state = cut.Instance.Form;
        var items = form.FindField("items")!;
        await cut.InvokeAsync(() =>
        {
            state.AddArrayItem(items, "items");
            state.AddArrayItem(items, "items");
            state.AddArrayItem(items, "items");
            state.SetValue("items[0].name", "zero");
            state.SetValue("items[1].name", "one");
            state.SetValue("items[2].name", "two");
        });

        _ = cut.Find("#borm_items_1_name").InputAsync(new ChangeEventArgs { Value = "one-edited" });
        await cut.InvokeAsync(() => state.RemoveArrayItem("items", 0));
        await Task.Delay(500);

        Assert.AreEqual("one-edited", state.GetValue("items[0].name"));
        Assert.AreEqual("two", state.GetValue("items[1].name"));
    }

    // ---------------------------------------------------------------- a rebuilt state

    [TestMethod]
    public async Task Fields_can_be_focused_after_the_view_rebuilds_its_state()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var cut = RenderForm(form, new Dictionary<string, object?>());
        var first = cut.Instance.Form;

        cut.Render(p => p.Add(x => x.Definition, form).Add(x => x.Data, new Dictionary<string, object?>()));
        var second = cut.Instance.Form;

        Assert.AreNotSame(first, second);
        Assert.IsTrue(await cut.InvokeAsync(() => second.FocusAsync("name").AsTask()));
    }

    [TestMethod]
    public void The_summary_takes_focus_on_the_first_failed_submit_of_a_rebuilt_state()
    {
        var form = BormBuilder.Create().Text("name", f => f.Required()).Build();
        var cut = RenderForm(form, new Dictionary<string, object?>(), p => p.Add(x => x.ShowErrorSummary, true));
        cut.Find("form").Submit();
        Assert.IsTrue(FocusCalls() > 0);

        cut.Render(p => p
            .Add(x => x.Definition, form)
            .Add(x => x.Data, new Dictionary<string, object?>())
            .Add(x => x.ShowErrorSummary, true));
        var before = FocusCalls();
        cut.Find("form").Submit();

        Assert.IsTrue(FocusCalls() > before);
    }

    // ---------------------------------------------------------------- read-only repeaters

    [TestMethod]
    public void A_read_only_repeater_leaves_its_rows_readable_and_locks_its_buttons()
    {
        var form = BormBuilder.Create().Array("items", i => i.Text("name")).Build();
        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { BormDictionaryDataAccessor.NewObject(), BormDictionaryDataAccessor.NewObject() }
        };
        var cut = RenderForm(form, data, p => p.Add(x => x.ReadOnly, true));

        Assert.IsFalse(cut.Find("fieldset.bit-borm-array").HasAttribute("disabled"));
        var input = cut.Find("input#borm_items_0_name");
        Assert.IsTrue(input.HasAttribute("readonly"));
        Assert.IsFalse(input.HasAttribute("disabled"));
        foreach (var b in cut.FindAll(".bit-borm-array button"))
        {
            Assert.IsTrue(b.HasAttribute("disabled"));
        }
    }

    [TestMethod]
    public void A_repeater_locked_by_its_own_condition_makes_its_rows_read_only()
    {
        var form = BormBuilder.Create()
            .Checkbox("sent")
            .ArrayOf("codes", BormFieldType.Text, f => f.ReadOnlyWhen("sent", BormConditionOperator.IsTrue))
            .Build();
        var data = new Dictionary<string, object?> { ["sent"] = true, ["codes"] = new List<object?> { "A1" } };
        var cut = RenderForm(form, data);

        var row = cut.Find("input#borm_codes_0");
        Assert.IsTrue(row.HasAttribute("readonly"));
        Assert.IsFalse(row.HasAttribute("disabled"));
    }

    // ---------------------------------------------------------------- messages with no control

    [TestMethod]
    public async Task A_message_about_an_object_as_a_whole_is_shown_on_its_group()
    {
        var form = BormBuilder.Create().Object("address", c => c.Text("city")).Build();
        var cut = RenderForm(form);

        await cut.InvokeAsync(() => cut.Instance.Form.SetServerError("address", "Cannot deliver there"));

        var group = cut.Find("fieldset.bit-borm-group");
        Assert.Contains("Cannot deliver there", cut.Find("#borm_address_error").TextContent, StringComparison.Ordinal);
        Assert.Contains("borm_address_error", group.GetAttribute("aria-describedby")!, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task A_message_about_a_whole_repeater_row_is_shown_on_that_row()
    {
        var form = BormBuilder.Create().Array("items", i => i.Text("name")).Build();
        var data = new Dictionary<string, object?> { ["items"] = new List<object?> { BormDictionaryDataAccessor.NewObject() } };
        var cut = RenderForm(form, data);

        await cut.InvokeAsync(() => cut.Instance.Form.SetServerError("items[0]", "Duplicates line 2"));

        Assert.Contains("Duplicates line 2", cut.Find("#borm_items_0_error").TextContent, StringComparison.Ordinal);
        Assert.AreEqual("borm_items_0_error", cut.Find(".bit-borm-array__item").GetAttribute("aria-describedby"));
    }

    [TestMethod]
    public async Task A_message_about_a_hidden_field_is_shown()
    {
        var form = BormBuilder.Create().Field("id", BormFieldType.Hidden).Build();
        var cut = RenderForm(form);

        await cut.InvokeAsync(() => cut.Instance.Form.SetServerError("id", "This record was deleted"));

        Assert.Contains("This record was deleted", cut.Markup, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task A_failed_submit_with_nowhere_to_put_focus_is_announced()
    {
        // A server objecting to a hidden field's value: the only error has no control that can take
        // focus, so focus-the-first-error (on by default) has nowhere to go.
        var form = BormBuilder.Create().Field("token", BormFieldType.Hidden).Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.ConfigureState, s => s.ExternalValidator = (_, _, _) =>
            ValueTask.FromResult<IReadOnlyList<BormValidationMessage>>(
                [new BormValidationMessage("token", "This record changed while you were editing it")])));

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("problem", cut.Find("form > [role=status]").TextContent, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("changed while", cut.Find("#borm_token_error").TextContent, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task A_message_about_an_object_as_a_whole_sends_focus_into_it()
    {
        var form = BormBuilder.Create().Object("address", c => c.Text("city")).Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.ConfigureState, s => s.ExternalValidator = (_, _, _) =>
            ValueTask.FromResult<IReadOnlyList<BormValidationMessage>>(
                [new BormValidationMessage("address", "Cannot deliver there")])));
        var before = FocusCalls();

        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.IsTrue(FocusCalls() > before));
        Assert.Contains("Cannot deliver there", cut.Find("#borm_address_error").TextContent, StringComparison.Ordinal);
        Assert.AreEqual("", cut.Find("form > [role=status]").TextContent.Trim());
    }

    [TestMethod]
    public async Task A_message_about_the_list_as_a_whole_can_be_focused()
    {
        var form = BormBuilder.Create()
            .Array("lines", item => item.Text("product"), f => f.Items(min: 1))
            .Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.ShowErrorSummary, true));
        await cut.Find("form").SubmitAsync();
        var before = FocusCalls();

        await cut.Find(".bit-borm-summary__list a").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.IsTrue(FocusCalls() > before);
        Assert.AreEqual("-1", cut.Find("#borm_lines").GetAttribute("tabindex"));
    }

    // ---------------------------------------------------------------- a field placed on its own

    [TestMethod]
    public void A_field_placed_on_its_own_follows_the_state()
    {
        var form = BormBuilder.Create().Text("email", f => f.Label("Email").Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "email"));

        cut.Find("#borm_email").Change("x");
        cut.Find("#borm_email").Change("");
        cut.Find("#borm_email").Blur();

        Assert.AreEqual("borm_email_error", cut.Find("#borm_email").GetAttribute("aria-describedby"));
        Assert.IsNotEmpty(cut.FindAll("#borm_email_error"));
    }

    [TestMethod]
    public void A_field_placed_inside_a_view_renders_once_per_change_not_twice()
    {
        // Both the view and the field listen to the state; a component already queued to render is not
        // queued again, so the field's own subscription costs nothing inside a view.
        var form = BormBuilder.Create().Text("a").Text("b").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormView>(p => p
            .Add(x => x.State, state)
            .AddChildContent<BormField>(f => f.Add(x => x.State, state).Add(x => x.Name, "b")));
        var field = cut.FindComponent<BormField>();
        var before = field.RenderCount;

        cut.Find("#borm_a").Change("x");

        Assert.AreEqual(before + 1, field.RenderCount);
    }

    [TestMethod]
    public async Task A_field_placed_on_its_own_stops_listening_once_it_is_gone()
    {
        var form = BormBuilder.Create().Text("email").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var cut = Render<BormField>(p => p.Add(x => x.State, state).Add(x => x.Name, "email"));

        await DisposeComponentsAsync();

        state.SetValue("email", "after");   // would throw into a disposed component if still subscribed
        Assert.AreEqual("after", state.GetValue("email"));
    }

    // ---------------------------------------------------------------- clearable fields

    [TestMethod]
    public void A_clearable_live_field_keeps_its_input_element_as_the_first_character_arrives()
    {
        ElementSpyTextInput.ElementIds.Clear();
        Services.AddBorm(r => r.Register(BormFieldType.Text, typeof(ElementSpyTextInput)));
        var form = BormBuilder.Create().Text("city", f => f.Clearable().UpdateOnInput()).Build();
        var cut = RenderForm(form);
        var first = ElementSpyTextInput.ElementIds[^1];

        cut.Find("#borm_city").Input("A");

        Assert.IsNotEmpty(cut.FindAll(".bit-borm-affix--btn"));
        Assert.AreEqual(first, ElementSpyTextInput.ElementIds[^1]);
    }

    // ---------------------------------------------------------------- ids

    [TestMethod]
    public void Two_forms_on_one_page_keep_their_ids_apart_with_a_prefix_each()
    {
        var schema = BormBuilder.Create().Email("email", f => f.Help("We never share it.")).Build();
        var cut = Render(builder =>
        {
            builder.OpenComponent<BormView>(0);
            builder.AddAttribute(1, nameof(BormView.Definition), schema);
            builder.AddAttribute(2, nameof(BormView.IdPrefix), "login_");
            builder.CloseComponent();
            builder.OpenComponent<BormView>(3);
            builder.AddAttribute(4, nameof(BormView.Definition), schema);
            builder.AddAttribute(5, nameof(BormView.IdPrefix), "signup_");
            builder.CloseComponent();
        });

        Assert.ContainsSingle(cut.FindAll("#login_email"));
        Assert.ContainsSingle(cut.FindAll("#signup_email"));
        Assert.AreEqual("signup_email_help", cut.Find("#signup_email").GetAttribute("aria-describedby"));
        Assert.AreEqual("signup_email", cut.FindAll("label")[1].GetAttribute("for"));
    }

    [TestMethod]
    public void The_default_prefix_is_unchanged_and_an_unsafe_one_is_folded()
    {
        var form = BormBuilder.Create().Text("a").Build();
        Assert.IsNotNull(RenderForm(form).Find("#borm_a"));

        var state = new BormState(form, new BormDictionaryDataAccessor()) { IdPrefix = "my form#" };
        Assert.AreEqual("my_form_a", new BormFieldContext(state, form.FindField("a")!, "a").ElementId);
    }

    // ---------------------------------------------------------------- announcements

    [TestMethod]
    public void The_submit_status_region_is_in_the_page_before_there_is_anything_to_say()
    {
        var form = BormBuilder.Create().Text("a", f => f.Required()).Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.FocusFirstError, false));

        Assert.AreEqual(string.Empty, cut.Find("form > [role=status]").TextContent.Trim());

        cut.Find("form").Submit();

        Assert.Contains("problem", cut.Find("form > [role=status]").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [TestMethod]
    public void Moving_a_row_and_emptying_the_list_are_both_announced()
    {
        var form = BormBuilder.Create().ArrayOf("codes", BormFieldType.Text).Build();
        var data = new Dictionary<string, object?> { ["codes"] = new List<object?> { "A", "B" } };
        var cut = RenderForm(form, data);
        string Live() => cut.Find(".bit-borm-array > p.bit-borm-sr-only[aria-live='polite']").TextContent;

        var before = Live();
        cut.FindAll(".bit-borm-array__item")[0].QuerySelectorAll("button")[1].Click(); // move row 1 down
        Assert.AreNotEqual(before, Live());
        Assert.Contains("2", Live(), StringComparison.Ordinal);

        cut.FindAll(".bit-borm-btn--danger")[0].Click();
        cut.FindAll(".bit-borm-btn--danger")[0].Click();
        Assert.IsFalse(string.IsNullOrWhiteSpace(Live()));
    }

    [TestMethod]
    public async Task An_OnFieldChanged_handler_that_fails_asynchronously_is_reported_not_swallowed()
    {
        var form = BormBuilder.Create().Text("a").Build();
        var cut = RenderForm(form, null, p => p.Add(x => x.OnFieldChanged, async (string _) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("autosave failed");
        }));

        await cut.Find("#borm_a").ChangeAsync(new ChangeEventArgs { Value = "x" });

        var error = await Assert.ThrowsAsync<Exception>(async () =>
        {
            for (var i = 0; i < 20; i++)
            {
                await Task.Delay(10);
                cut.Render();
            }
        });
        Assert.Contains("autosave failed", error.ToString(), StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------- read-only submit

    [TestMethod]
    public async Task Enter_in_a_read_only_form_does_not_submit_it_but_SubmitAsync_still_does()
    {
        var form = BormBuilder.Create().Text("a").Build();
        var submits = 0;
        var cut = RenderForm(form, null, p => p
            .Add(x => x.ReadOnly, true)
            .Add(x => x.OnValidSubmit, (BormState _) => { submits++; }));

        await cut.Find("form").SubmitAsync();
        Assert.AreEqual(0, submits);

        await cut.InvokeAsync(() => cut.Instance.SubmitAsync());
        Assert.AreEqual(1, submits);
    }

    // ---------------------------------------------------------------- file inputs

    private sealed class FakeFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;
        public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
        public long Size => 10;
        public string ContentType => "text/plain";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(new byte[10]);
    }

    [TestMethod]
    public void Clearing_a_file_field_puts_a_fresh_file_input_in_place()
    {
        var form = BormBuilder.Create().File("cv").Build();
        var data = new Dictionary<string, object?> { ["cv"] = new FakeFile("cv.pdf") };
        var cut = RenderForm(form, data);
        var before = cut.FindComponent<InputFile>().Instance;

        cut.Find(".bit-borm-file .bit-borm-btn--danger").Click();

        Assert.IsNull(data["cv"]);
        Assert.AreNotSame(before, cut.FindComponent<InputFile>().Instance);
    }

    [TestMethod]
    public async Task Resetting_a_file_field_puts_a_fresh_file_input_in_place_too()
    {
        var form = BormBuilder.Create().File("cv").Build();
        var cut = RenderForm(form, new Dictionary<string, object?>());
        await cut.InvokeAsync(() => cut.Instance.Form.SetValue("cv", new FakeFile("cv.pdf")));
        var before = cut.FindComponent<InputFile>().Instance;

        await cut.InvokeAsync(() => cut.Instance.Form.Reset());

        Assert.AreNotSame(before, cut.FindComponent<InputFile>().Instance);
    }
}
