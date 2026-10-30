using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless, accessible two-state Switch (<c>role="switch"</c>).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Checked</c> with <c>EditForm</c> validation, or uncontrolled.</item>
///   <item><b>Keyboard</b> — Space and Enter toggle.</item>
///   <item><b>Forms</b> — <see cref="BitBhField"/> wiring (label / description) and a hidden input for plain posts.</item>
///   <item><b>Styling</b> — <c>data-state="checked|unchecked"</c> on the switch and its <see cref="BitBhSwitchThumb"/>.</item>
/// </list>
///
/// <code>
/// &lt;BitBhSwitch @bind-Checked="enabled" aria-label="Notifications" class="switch"&gt;
///     &lt;BitBhSwitchThumb class="thumb" /&gt;
/// &lt;/BitBhSwitch&gt;
/// </code>
/// </summary>
public class BitBhSwitch : BitBhCheckableBase
{
    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhSwitchRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhSwitchRenderContext>? Template { get; set; }

    protected override string Role => "switch";

    protected override bool EnterToggles => true;

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        var context = new BitBhSwitchRenderContext { IsChecked = IsChecked, Disabled = IsDisabled };
        builder.OpenComponent<CascadingValue<BitBhSwitchRenderContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", context);
        builder.AddComponentParameter(sequence + 2, "ChildContent", BitBhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetAriaBool(attrs, "aria-checked", IsChecked);
        SetDataState(attrs, IsChecked, "checked", "unchecked");
        return attrs;
    }
}
