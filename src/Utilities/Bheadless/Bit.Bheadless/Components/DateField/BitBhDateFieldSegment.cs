using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// One segment of a date or time field. An editable segment is a focusable <c>&lt;span role="spinbutton"&gt;</c> named
/// by its part ("month", "day", "year", "hour", "minute", "AM/PM" — <see cref="BitBhStrings.DateSegmentName"/>), with
/// <c>aria-valuenow</c> / <c>aria-valuemin</c> / <c>aria-valuemax</c> and <c>aria-valuetext</c> ("11 – November",
/// "Empty"); the field's group carries the field's label. A separator is an <c>aria-hidden</c> span.
///
/// <para><b>Styling hooks:</b> <c>data-type</c> (<c>year</c>, <c>month</c>, <c>day</c>, <c>hour</c>, <c>minute</c>,
/// <c>second</c>, <c>dayperiod</c>, <c>literal</c>), <c>data-placeholder</c>, <c>data-focused</c>, <c>data-disabled</c>,
/// <c>data-readonly</c>, <c>data-invalid</c>; style <c>:focus-visible</c> for the focused segment.</para>
/// </summary>
public class BitBhDateFieldSegment : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDateFieldContext? Context { get; set; }

    /// <summary>The segment to render: the context of <see cref="BitBhDateFieldSegments"/>' content (<c>Segment="context"</c>).</summary>
    [Parameter, EditorRequired]
    public BitBhDateSegment Segment { get; set; } = default!;

    /// <summary>Replaces the segment's text: <c>&lt;Template Context="segment"&gt;…&lt;/Template&gt;</c>.</summary>
    [Parameter]
    public RenderFragment<BitBhDateSegment>? Template { get; set; }

    protected override string DefaultTag => "span";

    private IBitBhDateFieldOwner? Owner => Context?.Owner;

    private bool IsEditable => Owner is not null && Segment is { IsEditable: true };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        if (IsEditable) builder.AddAttribute(10, "id", Owner!.SegmentId(Segment.Type));
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsEditable && Owner is { } owner)
        {
            var type = Segment.Type;
            builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => owner.HandleKeyDownAsync(type, e)));
            builder.AddAttribute(31, "onfocus", On<FocusEventArgs>("onfocus", () => owner.HandleFocus(type)));
            builder.AddAttribute(32, "onblur", On<FocusEventArgs>("onblur", () => owner.HandleBlurAsync(type)));
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Segment is not null)
        {
            if (Template is not null) builder.AddContent(50, Template(Segment));
            else builder.AddContent(51, Segment.Text);
        }
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Segment is null) return attrs;
        SetDataValue(attrs, "type", Segment.Type.ToString().ToLowerInvariant());
        if (Owner is not { } owner || !Segment.IsEditable)
        {
            attrs["aria-hidden"] = "true";
            return attrs;
        }

        var type = Segment.Type;
        attrs["role"] = "spinbutton";
        attrs["aria-label"] = owner.SegmentName(type);
        attrs["aria-valuemin"] = Segment.MinValue.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuemax"] = Segment.MaxValue.ToString(CultureInfo.InvariantCulture);
        if (Segment.Value is { } value) attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuetext"] = owner.ValueText(type);
        if (owner.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        if (owner.IsDisabled) attrs["aria-disabled"] = "true";
        else attrs["tabindex"] = "0";
        if (owner.IsReadOnly) attrs["aria-readonly"] = "true";
        if (owner.IsRequired) attrs["aria-required"] = "true";
        if (owner.IsInvalid) attrs["aria-invalid"] = "true";
        if (!owner.IsDisabled)
        {
            attrs[BitBhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight PageUp PageDown Home End Backspace Delete";
            attrs["data-bit-bh-interactive"] = "";
        }

        SetDataFlag(attrs, "placeholder", Segment.IsPlaceholder);
        SetDataFlag(attrs, "focused", owner.FocusedSegment == type);
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        SetDataFlag(attrs, "readonly", owner.IsReadOnly);
        SetDataFlag(attrs, "invalid", owner.IsInvalid);
        return attrs;
    }
}
