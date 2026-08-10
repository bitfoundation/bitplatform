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
    /// <summary>
    /// The color the page starts on. The one it is on from then is <see cref="Color"/>, which the binding
    /// writes to - a field of the page, as it would be in real markup, rather than the parameter itself.
    /// </summary>
    [Parameter] public string InitialColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// A consumer that answers a change with a different color, which is the normalizing or clamping half
    /// of a binding rather than a plain echo of it.
    /// </summary>
    [Parameter] public Func<string, string>? Rewrite { get; set; }

    public BitColorPicker Picker { get; private set; } = default!;

    /// <summary>
    /// What the page is holding: the initial color until the binding writes to it.
    /// </summary>
    public string Color { get; private set; } = "#FFFFFF";

    protected override void OnInitialized()
    {
        Color = InitialColor;

        base.OnInitialized();
    }

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
