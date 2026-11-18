using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>The clear button of a <see cref="BitBhSearchField"/>: named "Clear search", out of the tab order, disabled while empty.</summary>
public class BitBhSearchFieldClear : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSearchField? SearchField { get; set; }

    /// <summary>The content (an icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", On<MouseEventArgs>("onclick", () => SearchField?.ClearAsync() ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = SearchField is not { } owner || owner.IsDisabled || owner.CurrentValue.Length == 0;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled) attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        attrs["tabindex"] = "-1";
        attrs["aria-label"] = Strings.ClearSearch;
        if (SearchField is { } field)
        {
            attrs["aria-controls"] = field.InputId;
            field.AddState(attrs);
        }
        attrs["data-bit-bh-interactive"] = "";
        return attrs;
    }
}
