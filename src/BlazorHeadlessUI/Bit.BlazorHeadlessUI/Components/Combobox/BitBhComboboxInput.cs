using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The text input of a <see cref="BitBhCombobox{TValue}"/>: <c>&lt;input role="combobox"&gt;</c>
/// with <c>aria-autocomplete="list"</c>, <c>aria-expanded</c>, <c>aria-controls</c> and
/// <c>aria-activedescendant</c>. Shows the live query while typing and the
/// selection's display text otherwise.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhComboboxInput<TValue> : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>Optional placeholder text.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    protected override string DefaultTag => "input";

    private bool IsOpen => ComboboxContext?.IsOpen ?? false;

    protected override void OnInitialized()
    {
        if (ComboboxContext is not null) ComboboxContext.Combobox.Input = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var text = ComboboxContext?.Combobox.InputText ?? string.Empty;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "value", text);
        builder.AddAttribute(31, "oninput",
            On<ChangeEventArgs>("oninput", HandleInput));
        // Typing updates "value" itself: don't let a stale render overwrite the caret.
        builder.SetUpdatesAttributeName("value");
        builder.AddAttribute(32, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // Also while an Escape would clear the closed input: an enclosing dialog or popover would
        // otherwise close on the same press.
        builder.AddEventStopPropagationAttribute(33, "onkeydown", IsOpen || (ComboboxContext?.Combobox.ClearsOnEscape ?? false));
        builder.AddAttribute(34, "onfocus",
            On<FocusEventArgs>("onfocus", HandleFocus));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["type"] = "text";
        attrs["role"] = "combobox";
        attrs["aria-autocomplete"] = "list";
        attrs["autocomplete"] = "off";
        attrs["autocorrect"] = "off";
        attrs["spellcheck"] = "false";
        // Expanded only while a listbox with options shows: a panel without options is hidden or shows the
        // empty state, which is no listbox to move into.
        var expanded = ComboboxContext?.Combobox.IsListboxShown ?? false;
        SetAriaBool(attrs, "aria-expanded", expanded);

        if (!string.IsNullOrEmpty(Placeholder))
            attrs["placeholder"] = Placeholder;

        if (ComboboxContext is not null)
        {
            attrs["aria-controls"] = ComboboxContext.OptionsId;
            if (expanded && ComboboxContext.ActiveId is { } active)
                attrs["aria-activedescendant"] = active;
            if (ComboboxContext.Field?.DescribedBy is { } describedBy)
                attrs["aria-describedby"] = describedBy;
            if (ComboboxContext.Invalid)
                attrs["aria-invalid"] = "true";
            if (ComboboxContext.Required)
                attrs["aria-required"] = "true";
            if (ComboboxContext.Disabled)
                attrs["disabled"] = true;

            var keys = IsOpen
                ? ComboboxContext.ActiveId is not null ? "ArrowDown ArrowUp PageDown PageUp Enter" : "ArrowDown ArrowUp PageDown PageUp"
                : "ArrowDown ArrowUp";
            attrs[BitBhKeyboard.KeysAttribute] = keys;

            SetDataFlag(attrs, "disabled", ComboboxContext.Disabled);
            SetDataFlag(attrs, "invalid", ComboboxContext.Invalid);
        }

        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private Task HandleInput(ChangeEventArgs args)
        => ComboboxContext?.Combobox.HandleInputAsync(args.Value?.ToString() ?? string.Empty) ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
        => ComboboxContext?.Combobox.HandleKeyDownAsync(args) ?? Task.CompletedTask;

    private Task HandleFocus(FocusEventArgs _)
        => ComboboxContext?.Combobox.HandleFocusAsync() ?? Task.CompletedTask;

    public void Dispose()
    {
        if (ComboboxContext?.Combobox.Input == this) ComboboxContext.Combobox.Input = null;
    }
}
