using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Components.Inputs.ColorPicker;

/// <summary>
/// A page holding <c>@bind-Color</c>, as a component rather than as a pair of bUnit callbacks: the binding
/// has to be received by a real ComponentBase for the re-render it triggers to happen where a real page
/// would trigger it - synchronously, from inside the callback the picker is awaiting.
/// </summary>
internal sealed class ColorPickerBindingHost : ComponentBase
{
    [Parameter] public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    /// A consumer that answers a change with a different color, which is the normalizing or clamping half
    /// of a binding rather than a plain echo of it.
    /// </summary>
    [Parameter] public Func<string, string>? Rewrite { get; set; }

    public BitColorPicker Picker { get; private set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<BitColorPicker>(0);
        builder.AddComponentParameter(1, nameof(BitColorPicker.Color), Color);
        builder.AddComponentParameter(2, nameof(BitColorPicker.ColorChanged),
                                      EventCallback.Factory.Create<string>(this, value => Color = Rewrite is null ? value : Rewrite(value)));
        builder.AddComponentReferenceCapture(3, picker => Picker = (BitColorPicker)picker);
        builder.CloseComponent();
    }
}
