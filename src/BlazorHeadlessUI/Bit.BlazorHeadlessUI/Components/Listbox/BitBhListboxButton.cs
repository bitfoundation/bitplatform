using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The trigger of a <see cref="BitBhListbox{TValue}"/>. Renders as
/// <c>&lt;button role="combobox"&gt;</c> with <c>aria-haspopup="listbox"</c>,
/// <c>aria-expanded</c>, <c>aria-controls</c> and <c>aria-activedescendant</c>. Keeps
/// focus while the panel is open and handles all keyboard interaction.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhListboxButton<TValue> : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhListboxContext<TValue>? ListboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhListboxButtonRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhListboxButtonRenderContext<TValue>>? Template { get; set; }

    /// <summary>
    /// Text shown when nothing is selected and neither <see cref="ChildContent"/> nor
    /// <see cref="Template"/> is supplied (the button then shows the selected option text).
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => ListboxContext?.IsOpen ?? false;
    private bool IsDisabled => ListboxContext?.Disabled ?? false;

    protected override void OnInitialized()
    {
        if (ListboxContext is not null) ListboxContext.Listbox.Button = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ListboxContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // While open the button owns every key (incl. Escape); don't let an
        // enclosing popover / dialog react to them as well.
        builder.AddEventStopPropagationAttribute(32, "onkeydown", IsOpen);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is null && ChildContent is not null)
        {
            builder.AddContent(50, ChildContent);
        }
        else if (ListboxContext is not null)
        {
            var text = string.Join(", ", ListboxContext.State.SelectedTexts());
            if (Template is not null)
            {
                builder.AddContent(51, Template(new BitBhListboxButtonRenderContext<TValue>
                {
                    IsOpen = IsOpen,
                    Disabled = IsDisabled,
                    Value = ListboxContext.SingleValue,
                    Values = ListboxContext.MultiValues,
                    Text = text.Length == 0 ? null : text,
                }));
            }
            else
            {
                // No content supplied: show the selection, or the placeholder.
                builder.AddContent(52, text.Length == 0 ? Placeholder : text);
            }
        }
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

        if (isButton) attrs["type"] = "button";
        else attrs["tabindex"] = IsDisabled ? "-1" : "0";

        attrs["role"] = "combobox";
        attrs["aria-haspopup"] = "listbox";
        SetAriaBool(attrs, "aria-expanded", IsOpen);

        if (ListboxContext is not null)
        {
            if (IsOpen)
            {
                attrs["aria-controls"] = ListboxContext.OptionsId;
                if (ListboxContext.ActiveId is { } active)
                    attrs["aria-activedescendant"] = active;
            }
            if (ListboxContext.Field?.LabelledBy is { } label)
                attrs["aria-labelledby"] = label;
            if (ListboxContext.Field?.DescribedBy is { } describedBy)
                attrs["aria-describedby"] = describedBy;
            if (ListboxContext.Invalid)
                attrs["aria-invalid"] = "true";
            if (ListboxContext.Required)
                attrs["aria-required"] = "true";
            SetDataFlag(attrs, "placeholder", ListboxContext.Multiple ? ListboxContext.MultiValues.Count == 0 : !ListboxContext.HasValue);
            SetDataFlag(attrs, "invalid", ListboxContext.Invalid);
        }

        attrs[BitBhKeyboard.KeysAttribute] = IsOpen
            ? "ArrowDown ArrowUp Home End PageUp PageDown Enter Space"
            : "ArrowDown ArrowUp Home End Enter Space";

        if (IsDisabled)
        {
            if (isButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsDisabled ? Task.CompletedTask : ListboxContext?.Listbox.ToggleAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
        => ListboxContext?.Listbox.HandleKeyDownAsync(args) ?? Task.CompletedTask;

    public void Dispose()
    {
        if (ListboxContext?.Listbox.Button == this) ListboxContext.Listbox.Button = null;
    }
}
