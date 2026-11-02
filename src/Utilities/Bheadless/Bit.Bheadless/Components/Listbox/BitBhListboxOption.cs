using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A single option of a <see cref="BitBhListbox{TValue}"/> (<c>&lt;li role="option"&gt;</c>).
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Click selects it (and closes the panel in single-select mode) without moving focus off the button.</item>
///   <item>Hover highlights it (<c>data-active</c> / <c>data-highlighted</c>).</item>
///   <item>Emits <c>aria-selected</c> and <c>data-selected</c>, <c>data-disabled</c>.</item>
/// </list>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BitBhListboxOption<TValue> : BitBhOptionBase<TValue>
{
    [CascadingParameter]
    private BitBhListboxContext<TValue>? ListboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhListboxOptionRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhListboxOptionRenderContext<TValue>>? Template { get; set; }

    protected override bool RootDisabled => ListboxContext?.Disabled ?? false;

    protected override Task SelectAsync() => ListboxContext?.SelectAsync(Value) ?? Task.CompletedTask;

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is null && ChildContent is null) return;
        builder.AddContent(sequence, BitBhContent.Pick(Template, ChildContent, new BitBhListboxOptionRenderContext<TValue>
        {
            Value = Value,
            IsSelected = IsSelected,
            IsActive = IsActive,
            Disabled = IsItemDisabled,
        }));
    }
}
