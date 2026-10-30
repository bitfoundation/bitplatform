using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Content shown inside <see cref="BitBhComboboxOptions{TValue}"/> when there are no
/// options (e.g. "No results"). A listbox may only contain options, so the panel is no
/// <c>listbox</c> while it shows this; the content is a <c>role="status"</c> live region,
/// announced as it appears.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhComboboxEmpty<TValue> : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The empty-state content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void OnInitialized()
    {
        if (ComboboxContext is not null) ComboboxContext.Combobox.Empty = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ComboboxContext is null || ComboboxContext.State.Count > 0) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "status";
        return attrs;
    }

    public void Dispose()
    {
        if (ComboboxContext?.Combobox.Empty == this) ComboboxContext.Combobox.Empty = null;
    }
}
