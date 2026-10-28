using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Groups related menu items (<c>role="group"</c>). A <see cref="BitBhMenuLabel"/>
/// inside the group names it automatically through <c>aria-labelledby</c>.
/// </summary>
public class BitBhMenuGroup : BitBhComponentBase
{
    private bool _hasLabel;

    /// <summary>The group's items and optional label.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string LabelId => $"{ComponentId}-label";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhMenuGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", new BitBhMenuGroupContext(LabelId, RegisterLabel));
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (_hasLabel)
            attrs["aria-labelledby"] = LabelId;
        return attrs;
    }

    private void RegisterLabel(bool present)
    {
        if (_hasLabel == present) return;
        _hasLabel = present;
        StateHasChanged();
    }
}

/// <summary>Cascaded by <see cref="BitBhMenuGroup"/> so its label can name it.</summary>
public sealed class BitBhMenuGroupContext
{
    private readonly Action<bool> _registerLabel;

    internal BitBhMenuGroupContext(string labelId, Action<bool> registerLabel)
    {
        LabelId = labelId;
        _registerLabel = registerLabel;
    }

    /// <summary>The id the group's label renders with.</summary>
    public string LabelId { get; }

    internal void RegisterLabel(bool present) => _registerLabel(present);
}

/// <summary>
/// A non-interactive heading inside a menu. Inside a <see cref="BitBhMenuGroup"/> it
/// labels the group; elsewhere it is presentational text that keyboard
/// navigation skips.
/// </summary>
public class BitBhMenuLabel : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhMenuGroupContext? Group { get; set; }

    /// <summary>The label text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => Group?.RegisterLabel(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Group?.LabelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // A label outside a group is presentational inside role="menu".
        if (Group is null)
            attrs["role"] = "presentation";
        return attrs;
    }

    public void Dispose() => Group?.RegisterLabel(false);
}

/// <summary>A visual divider between menu items (<c>role="separator"</c>).</summary>
public class BitBhMenuSeparator : BitBhComponentBase
{
    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "separator";
        attrs["aria-orientation"] = "horizontal";
        return attrs;
    }
}
