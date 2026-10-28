using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A container grouping controls such as buttons, toggles and links
/// (<c>role="toolbar"</c>, WAI-ARIA Toolbar pattern). The toolbar is a single tab
/// stop; arrow keys move between <see cref="BitBhToolbarButton"/>s,
/// <see cref="BitBhToolbarLink"/>s and <see cref="BitBhToggleGroup"/> items. Give it an
/// accessible name with <c>aria-label</c>.
///
/// <code>
/// &lt;BitBhToolbar aria-label="Formatting"&gt;
///     &lt;BitBhToggleGroup Type="BitBhToggleGroupType.Multiple" aria-label="Text style"&gt;
///         &lt;BitBhToggleGroupItem Value="bold" aria-label="Bold"&gt;B&lt;/BitBhToggleGroupItem&gt;
///     &lt;/BitBhToggleGroup&gt;
///     &lt;BitBhToolbarSeparator /&gt;
///     &lt;BitBhToolbarButton OnClick="Share"&gt;Share&lt;/BitBhToolbarButton&gt;
/// &lt;/BitBhToolbar&gt;
/// </code>
/// </summary>
public class BitBhToolbar : BitBhRovingFocusGroup
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Context.IsToolbar = true;
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var orientation = Orientation == BitBhRovingOrientation.Vertical ? "vertical" : "horizontal";
        attrs["role"] = "toolbar";
        attrs["aria-orientation"] = orientation;
        SetDataValue(attrs, "orientation", orientation);
        return attrs;
    }
}
