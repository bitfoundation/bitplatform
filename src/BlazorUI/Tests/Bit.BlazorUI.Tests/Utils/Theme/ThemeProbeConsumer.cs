using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI.Tests.Utils.Theme;

/// <summary>
/// Tiny consumer used by <c>BitThemeProvider</c> tests to inspect the cascaded value. Stamps the
/// resolved <see cref="BitTheme"/>'s primary main color into the DOM so a test can assert on it
/// without inventing test-only public API surface.
/// </summary>
internal sealed class ThemeProbeConsumer : ComponentBase
{
    [CascadingParameter] public BitTheme? Theme { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-primary", Theme?.Color?.Primary?.Main ?? "(none)");
        builder.CloseElement();
    }
}
