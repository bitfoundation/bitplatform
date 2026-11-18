using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A section of options in a <see cref="BitBhListboxOptions{TValue}"/> or a <see cref="BitBhComboboxOptions{TValue}"/>
/// (<c>role="group"</c>), named by its <see cref="BitBhOptionGroupLabel"/> - React Aria's ListBoxSection. Keyboard
/// navigation and typeahead run across sections as if they were one list; screen readers announce the section a
/// highlighted option belongs to.
///
/// <code>
/// &lt;BitBhListboxOptions TValue="string"&gt;
///     &lt;BitBhOptionGroup&gt;
///         &lt;BitBhOptionGroupLabel&gt;Fruits&lt;/BitBhOptionGroupLabel&gt;
///         &lt;BitBhListboxOption TValue="string" Value="@("apple")"&gt;Apple&lt;/BitBhListboxOption&gt;
///     &lt;/BitBhOptionGroup&gt;
/// &lt;/BitBhListboxOptions&gt;
/// </code>
/// <para>The default elements are <c>div</c>s; with a <c>&lt;ul&gt;</c> options panel set <c>As="li"</c> on the group.</para>
/// </summary>
public class BitBhOptionGroup : BitBhComponentBase
{
    private bool _hasLabel;
    private BitBhOptionGroupContext? _context;

    /// <summary>The label and the options.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string LabelId => $"{ComponentId}-label";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhOptionGroupContext(LabelId, RegisterLabel);
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhOptionGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (_hasLabel && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = LabelId;
        return attrs;
    }

    private void RegisterLabel(bool present)
    {
        if (_hasLabel == present) return;
        _hasLabel = present;
        StateHasChanged();
    }
}
