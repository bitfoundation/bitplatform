using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A single option of a <see cref="BitBhCombobox{TValue}"/> (<c>&lt;li role="option"&gt;</c>).
/// Clicking selects it without taking focus out of the input; hovering highlights it.
/// Emits <c>aria-selected</c>, <c>data-active</c>, <c>data-selected</c> and <c>data-disabled</c>.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhComboboxOption<TValue> : BitBhOptionBase<TValue>
{
    [CascadingParameter]
    private BitBhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhComboboxOptionRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhComboboxOptionRenderContext<TValue>>? Template { get; set; }

    protected override bool RootDisabled => ComboboxContext?.Disabled ?? false;

    protected override Task SelectAsync() => ComboboxContext?.SelectAsync(Value) ?? Task.CompletedTask;

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is null && ChildContent is null) return;
        builder.AddContent(sequence, BitBhContent.Pick(Template, ChildContent, new BitBhComboboxOptionRenderContext<TValue>
        {
            Value = Value,
            IsSelected = IsSelected,
            IsActive = IsActive,
            Disabled = IsItemDisabled,
        }));
    }
}
