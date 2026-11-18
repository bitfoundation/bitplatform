using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Shared behaviour of the pagination parts that go to a page: a button, or a link when the pagination has a
/// <see cref="BitBhPagination.PageHref"/>. A part that can't go anywhere (Previous on the first page) is disabled -
/// a link without an <c>href</c> and <c>aria-disabled</c>.
/// </summary>
public abstract class BitBhPaginationTargetBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhPaginationContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The owning pagination.</summary>
    protected BitBhPagination? Pagination => Context?.Owner;

    private bool IsLink => Pagination?.PageHref is not null && As is null;

    protected override string DefaultTag => IsLink ? "a" : "button";

    /// <summary>The page this part goes to, or null when it can't go anywhere.</summary>
    protected abstract int? TargetPage { get; }

    /// <summary>The accessible name given to the part (an <c>aria-label</c> you set replaces it).</summary>
    protected abstract string? DefaultLabel { get; }

    /// <summary>Whether this part stands for the current page.</summary>
    protected virtual bool IsCurrent => false;

    private bool IsDisabled => Pagination is null || Pagination.Disabled || TargetPage is null;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (!IsLink)
            builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => !IsDisabled && TargetPage is { } page && Pagination is { } p ? p.GoToAsync(page) : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var disabled = IsDisabled;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (disabled && !IsCurrent) attrs["disabled"] = true;
        }
        else if (Tag.Equals("a", StringComparison.OrdinalIgnoreCase))
        {
            if (!disabled && TargetPage is { } page && Pagination?.PageHref is { } href) attrs["href"] = href(page);
            else
            {
                attrs["role"] = "link";
                attrs["aria-disabled"] = "true";
            }
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = disabled ? "-1" : "0";
            attrs[BitBhKeyboard.KeysAttribute] = "Enter Space";
            if (disabled) attrs["aria-disabled"] = "true";
        }
        if (DefaultLabel is { } label) attrs["aria-label"] = label;
        if (IsCurrent) attrs["aria-current"] = "page";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", disabled && !IsCurrent);
        SetDataFlag(attrs, "selected", IsCurrent);
        return attrs;
    }
}
