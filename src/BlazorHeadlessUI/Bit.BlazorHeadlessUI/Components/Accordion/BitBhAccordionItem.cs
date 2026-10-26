using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// One section of a <see cref="BitBhAccordion"/>, identified by <see cref="Value"/>.
/// Contains a <see cref="BitBhAccordionHeader"/> (with a <see cref="BitBhAccordionTrigger"/>)
/// and a <see cref="BitBhAccordionContent"/>.
/// </summary>
public class BitBhAccordionItem : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAccordionContext? AccordionContext { get; set; }

    /// <summary>The item's unique value.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Header and content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => AccordionContext?.IsOpen(Value) ?? false;
    private bool IsDisabled => Disabled || (AccordionContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BitBhAccordionItemContext
        {
            Value = Value,
            IsOpen = IsOpen,
            Disabled = IsDisabled,
            TriggerId = $"{ComponentId}-trigger",
            ContentId = $"{ComponentId}-content",
            Toggle = () => AccordionContext?.Toggle(Value),
            Accordion = AccordionContext,
        };

        builder.OpenComponent<CascadingValue<BitBhAccordionItemContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataValue(attrs, "orientation", (AccordionContext?.Orientation ?? BitBhOrientation.Vertical).ToAttribute());
        return attrs;
    }
}
