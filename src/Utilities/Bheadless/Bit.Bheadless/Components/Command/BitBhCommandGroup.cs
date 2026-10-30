using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A titled group of <see cref="BitBhCommandItem"/>s (<c>role="group"</c>, labelled by
/// its heading). Hidden automatically when none of its items match the search.
/// </summary>
public class BitBhCommandGroup : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    /// <summary>The group heading text.</summary>
    [Parameter]
    public string? Heading { get; set; }

    /// <summary>Custom heading markup (alternative to <see cref="Heading"/>).</summary>
    [Parameter]
    public RenderFragment? HeadingContent { get; set; }

    /// <summary>Always render, regardless of the search.</summary>
    [Parameter]
    public bool ForceMount { get; set; }

    /// <summary>The group's items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string HeadingId => $"{ComponentId}-heading";

    private bool IsVisible => CommandContext?.IsVisible(this) ?? true;

    protected override void OnInitialized() => CommandContext?.RegisterGroup(this);

    public void Dispose() => CommandContext?.UnregisterGroup(this);

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (!IsVisible) builder.AddAttribute(25, "hidden", true);
        builder.AddElementReferenceCapture(30, CaptureElement);

        if (Heading is not null || HeadingContent is not null)
        {
            builder.OpenElement(40, "div");
            builder.AddAttribute(41, "id", HeadingId);
            builder.AddAttribute(42, "aria-hidden", "true");
            builder.AddAttribute(43, "data-bit-bh-command-group-heading", "");
            if (HeadingContent is not null) builder.AddContent(44, HeadingContent);
            else builder.AddContent(45, Heading);
            builder.CloseElement();
        }

        builder.OpenElement(50, "div");
        builder.AddAttribute(51, "role", "group");
        if (Heading is not null || HeadingContent is not null)
            builder.AddAttribute(52, "aria-labelledby", HeadingId);
        builder.AddAttribute(53, "data-bit-bh-command-group-items", "");
        builder.OpenComponent<CascadingValue<BitBhCommandGroup>>(54);
        builder.AddComponentParameter(55, "Value", this);
        builder.AddComponentParameter(56, "IsFixed", true);
        builder.AddComponentParameter(57, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // The wrapper only holds the visual heading; the inner element is the group.
        attrs["role"] = "presentation";
        attrs["data-bit-bh-command-group"] = "";
        if (CommandContext is { Sort: true } ctx && !string.IsNullOrWhiteSpace(ctx.Search))
            attrs["style"] = $"order:{(-(int)(ctx.GroupScore(this) * 1000)).ToString(CultureInfo.InvariantCulture)}";
        return attrs;
    }
}
