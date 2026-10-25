using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Borm.Tests;

/// <summary>
/// Which DOM event writes the value back. The default (<c>change</c>) fires on blur for a text box, so
/// a form that wants a live preview or an as-you-type total has to opt into <c>input</c>.
/// </summary>
[TestClass]
public class UpdateTriggerTests : BunitContext
{
    public UpdateTriggerTests() => Services.AddBorm();

    private (Bunit.IRenderedComponent<BormView> View, BormState State) RenderForm(
        BormDefinition form)
    {
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var view = Render<BormView>(p => p.Add(x => x.State, state));
        return (view, state);
    }

    [TestMethod]
    public void By_default_a_field_writes_on_change_and_wires_no_input_handler_at_all()
    {
        var form = BormBuilder.Create().Text("name").Build();
        var (view, state) = RenderForm(form);

        // Not merely ignored — never wired. On a Blazor Server circuit an `oninput` attribute costs a
        // round-trip per keystroke, so a field with no use for the event must not carry one.
        Assert.ThrowsExactly<Bunit.MissingEventHandlerException>(() => view.Find("input").Input("Ada"));

        view.Find("input").Change("Ada");
        Assert.AreEqual("Ada", state.GetValue("name"));
    }

    /// <summary>
    /// On a Blazor Server circuit every keystroke is a round trip. A box written as the user types whose value
    /// the control renders itself (<c>value="@x"</c> plus an <c>oninput</c> handler) has each render's value put
    /// back into it - after the keys typed since, which it erases: typed fast over a slow link, letters vanish. A
    /// binding marks the value as the browser's: its <c>input</c> handler names the attribute it updates, and the
    /// renderer takes each event's value as what the box holds, writing only a value that differs from it. bUnit
    /// sends no browser value with its events, so the flag the renderer acts on is what is checked.
    /// </summary>
    [TestMethod]
    public void Every_control_written_as_the_user_types_leaves_the_text_to_the_browser()
    {
        var form = BormBuilder.Create()
            .Text("title", f => f.UpdateOnInput())
            .TextArea("summary", f => f.UpdateOnInput())
            .Number("seats", f => f.UpdateOnInput())
            .Date("starts", f => f.UpdateOnInput())
            .Number("volume", f => f.AsRange())
            .Tags("skills")
            .Text("name")
            .Build();
        var (view, _) = RenderForm(form);

        foreach (var (owner, updates) in InputHandlers(view))
        {
            Assert.AreEqual("value", updates, $"{owner}: its input handler does not leave the value to the browser");
        }

        Assert.AreEqual(6, InputHandlers(view).Count, "one per control written as the user types; the change-driven field has none");
    }

    /// <summary>
    /// The other half of that binding: a rendered value that differs from what the browser sent is written into the
    /// box, so it has to be the text the user typed for as long as the model holds what that text says. A
    /// debounced field has not written it yet, and the model's 1 is the "1." being typed: rendering the model's text
    /// instead emptied a debounced box on every keystroke. A change the user did not type - the field tidied on
    /// blur, a value set from code - is shown.
    /// </summary>
    [TestMethod]
    public async Task A_bound_box_shows_what_was_typed_until_something_else_changes_the_value()
    {
        var form = BormBuilder.Create()
            .Text("username", f => f.UpdateOnInput(debounceMilliseconds: 60_000).Trim())
            .Number("price", f => f.UpdateOnInput())
            .Build();
        var (view, state) = RenderForm(form);

        // Still waiting out its debounce, so the model has nothing yet.
        _ = view.Find("#borm_username").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = " ada " });
        view.WaitForAssertion(() => Assert.AreEqual(" ada ", view.Find("#borm_username").GetAttribute("value")));
        Assert.IsNull(state.GetValue("username"));

        // Leaving the box writes it and tidies it: that is the model's doing, and the box shows it.
        await view.Find("#borm_username").BlurAsync(new Microsoft.AspNetCore.Components.Web.FocusEventArgs());
        Assert.AreEqual("ada", state.GetValue("username"));
        Assert.AreEqual("ada", view.Find("#borm_username").GetAttribute("value"));

        await view.Find("#borm_price").InputAsync(new Microsoft.AspNetCore.Components.ChangeEventArgs { Value = "1." });
        Assert.AreEqual(1d, state.GetValue("price"));
        Assert.AreEqual("1.", view.Find("#borm_price").GetAttribute("value"));

        await view.InvokeAsync(() => state.SetValue("price", 2.5));
        view.WaitForAssertion(() => Assert.AreEqual("2.5", view.Find("#borm_price").GetAttribute("value")));
    }

    /// <summary>Every <c>oninput</c> handler the form's controls bound, with the attribute it updates.</summary>
    private List<(string Owner, string? Updates)> InputHandlers(Bunit.IRenderedComponent<BormView> view)
    {
#pragma warning disable BL0006 // the render tree is the only place the flag the renderer acts on is visible
        var current = typeof(Microsoft.AspNetCore.Components.RenderTree.Renderer).GetMethod("GetCurrentRenderTreeFrames",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, [typeof(int)])!;
        var found = new List<(string, string?)>();
        foreach (var control in view.FindComponents<BormInputBase>())
        {
            var frames = (Microsoft.AspNetCore.Components.RenderTree.ArrayRange<Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrame>)current.Invoke(Renderer, [control.ComponentId])!;
            foreach (var frame in frames.Array.Take(frames.Count))
            {
                if (frame.FrameType == Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Attribute && frame.AttributeName == "oninput")
                {
                    found.Add((control.Instance.GetType().Name, frame.AttributeEventUpdatesAttributeName));
                }
            }
        }
#pragma warning restore BL0006
        return found;
    }

    [TestMethod]
    public void A_change_driven_field_with_a_counter_listens_to_input_without_writing()
    {
        var form = BormBuilder.Create()
            .Text("name", f => f.MaxLength(10).CharacterCount())
            .Build();
        var (view, state) = RenderForm(form);

        view.Find("input").Input("Ada");

        // The counter tracks what is on screen; the model still waits for the change event.
        Assert.Contains("3 / 10", view.Markup, StringComparison.Ordinal);
        Assert.IsNull(state.GetValue("name"));
    }

    [TestMethod]
    public void An_input_driven_field_writes_on_every_keystroke()
    {
        var form = BormBuilder.Create().Text("name", f => f.UpdateOnInput()).Build();
        var (view, state) = RenderForm(form);

        view.Find("input").Input("A");
        Assert.AreEqual("A", state.GetValue("name"));

        view.Find("input").Input("Ad");
        Assert.AreEqual("Ad", state.GetValue("name"));
    }

    [TestMethod]
    public void An_input_driven_field_ignores_the_change_event_so_it_never_writes_twice()
    {
        var writes = 0;
        var form = BormBuilder.Create().Text("name", f => f.UpdateOnInput()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());
        var view = Render<BormView>(p => p
            .Add(x => x.State, state)
            .Add(x => x.OnFieldChanged, _ => writes++));

        view.Find("input").Input("Ada");
        view.Find("input").Change("Ada");

        Assert.AreEqual(1, writes);
    }

    [TestMethod]
    public async Task A_debounced_field_writes_once_the_typing_stops()
    {
        var form = BormBuilder.Create().Text("q", f => f.UpdateOnInput(debounceMilliseconds: 60)).Build();
        var (view, state) = RenderForm(form);

        view.Find("input").Input("a");
        view.Find("input").Input("ab");
        view.Find("input").Input("abc");

        // Nothing has landed yet: every keystroke superseded the one before it.
        Assert.IsNull(state.GetValue("q"));

        await Task.Delay(300);
        Assert.AreEqual("abc", state.GetValue("q"));
    }

    [TestMethod]
    public void A_slider_always_writes_live_whatever_the_trigger_says()
    {
        // Dragging *is* the interaction; waiting for `change` would leave the readout a step behind.
        var form = BormBuilder.Create()
            .Field("volume", BormFieldType.Range, f => f.Range(0, 10))
            .Build();
        var (view, state) = RenderForm(form);

        view.Find("input[type=range]").Input("7");

        Assert.AreEqual(7d, state.GetValue("volume"));
    }
}

[TestClass]
public class FocusTests : BunitContext
{
    public FocusTests() => Services.AddBorm();

    [TestMethod]
    public async Task A_focus_target_is_registered_for_every_rendered_field()
    {
        var form = BormBuilder.Create().Text("name").Text("email").Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Render<BormView>(p => p.Add(x => x.State, state));

        // bUnit has no real DOM focus, so the contract under test is the registration, not the caret:
        // a path the form is rendering resolves, one it is not does not.
        Assert.IsTrue(await state.FocusAsync("name"));
        Assert.IsFalse(await state.FocusAsync("nope"));
    }

    [TestMethod]
    public async Task A_hidden_field_registers_no_focus_target()
    {
        var form = BormBuilder.Create()
            .Checkbox("advanced")
            .Text("detail", f => f.VisibleWhen("advanced", BormConditionOperator.IsTrue))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Render<BormView>(p => p.Add(x => x.State, state));

        Assert.IsFalse(await state.FocusAsync("detail"));
    }

    [TestMethod]
    public async Task A_control_with_no_focusable_element_reports_that_it_could_not_take_focus()
    {
        // Reporting success while doing nothing would make "focus the first error" stop at the first
        // field it cannot reach, instead of moving on to one it can. A field on a wizard step that is
        // not showing is not rendering anything, so there is nothing to focus.
        var form = BormBuilder.Create()
            .Text("name")
            .Text("notes")
            .Step("one", s => s.Title("One").Fields("name"))
            .Step("two", s => s.Title("Two").Fields("notes"))
            .Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        Render<BormView>(p => p.Add(x => x.State, state));

        Assert.IsTrue(await state.FocusAsync("name"));
        Assert.IsFalse(await state.FocusAsync("notes"));
    }

    [TestMethod]
    public async Task A_file_field_offers_its_group_as_the_focus_target()
    {
        // <InputFile> is a component, not an element, so there is no ElementReference for the control
        // itself — which used to make a required file the one field "go to the first error" and the
        // error summary's link silently walked past, on a form whose only problem was that file.
        var form = BormBuilder.Create().File("cv", f => f.Required()).Build();
        var state = new BormState(form, new BormDictionaryDataAccessor());

        var cut = Render<BormView>(p => p.Add(x => x.State, state));

        Assert.AreEqual("-1", cut.Find(".bit-borm-file").GetAttribute("tabindex"));
        Assert.IsTrue(await state.FocusAsync("cv"));
    }

    [TestMethod]
    public void A_radio_group_offers_the_group_itself_as_the_focus_target()
    {
        // No single radio represents the field, and landing on the group makes a screen reader announce
        // its name and role before the user arrows through the options.
        var form = BormBuilder.Create()
            .Radio("size", f => f.Options(("s", "Small"), ("l", "Large")))
            .Build();

        var html = Render<BormView>(p => p.Add(x => x.Definition, form)).Markup;

        Assert.Contains("role=\"radiogroup\"", html, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"-1\"", html, StringComparison.Ordinal);
    }

    [TestMethod]
    public void The_autofocus_attribute_is_no_longer_emitted()
    {
        // Browsers honour it only on the initial page load, which an interactive Blazor render is not;
        // leaving it in the markup would suggest a behaviour that never actually happens.
        var form = BormBuilder.Create().Text("name", f => f.Autofocus()).Build();

        var html = Render<BormView>(p => p.Add(x => x.Definition, form)).Markup;

        Assert.DoesNotContain("autofocus", html, StringComparison.OrdinalIgnoreCase);
    }
}
