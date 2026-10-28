using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// An optional button that opens / closes the combobox popup — typically the "▾"
/// chevron at the end of the input. It is outside the tab order
/// (<c>tabindex="-1"</c>); clicking it keeps focus in the input.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhComboboxButton<TValue> : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhComboboxButtonRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhComboboxButtonRenderContext<TValue>>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => ComboboxContext?.IsOpen ?? false;
    private bool IsDisabled => ComboboxContext?.Disabled ?? false;

    protected override void OnInitialized()
    {
        if (ComboboxContext is not null) ComboboxContext.Combobox.Button = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            On<MouseEventArgs>("onclick", HandleClick));
        builder.AddAttribute(31, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
        builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhComboboxButtonRenderContext<TValue> { IsOpen = IsOpen, Disabled = IsDisabled }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        if (isButton)
            attrs["type"] = "button";
        else
            attrs["role"] = "button";
        attrs["tabindex"] = "-1";
        if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
            attrs["aria-label"] = "Show options";
        SetAriaBool(attrs, "aria-expanded", ComboboxContext?.Combobox.IsListboxShown ?? false);
        if (ComboboxContext is not null)
            attrs["aria-controls"] = ComboboxContext.OptionsId;
        // "disabled" exists on form controls only.
        if (IsDisabled)
        {
            if (isButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs _)
    {
        if (IsDisabled || ComboboxContext is null) return;
        var combobox = ComboboxContext.Combobox;
        // Focusing the input may open the panel by itself (OpenOnFocus): toggle from the state the click saw,
        // or the click would open it and close it right away.
        var wasOpen = combobox.IsOpen;
        await combobox.FocusInputAsync();
        if (wasOpen) await combobox.CloseAsync();
        else await combobox.OpenAsync(BitBhMenuFocus.None);
    }

    public void Dispose()
    {
        if (ComboboxContext?.Combobox.Button == this) ComboboxContext.Combobox.Button = null;
    }
}
