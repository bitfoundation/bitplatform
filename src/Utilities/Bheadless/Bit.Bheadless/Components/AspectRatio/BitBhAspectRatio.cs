using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Displays content within a desired ratio, regardless of the content's
/// intrinsic size. A faithful port of Radix UI's <c>AspectRatio</c> primitive.
///
/// <para><b>How it works:</b></para>
/// <para>
/// The component renders an outer wrapper <c>&lt;div&gt;</c> that uses the classic
/// "padding-bottom" trick (<c>padding-bottom: 100 / ratio %</c>) to reserve
/// vertical space proportional to its width. The polymorphic inner element is
/// absolutely positioned to fill that space, so any content (images, video,
/// maps, iframes) is contained at the exact ratio you specify.
/// </para>
///
/// <para><b>Key features (inspired by Radix UI):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Ratio control</b> — set <see cref="Ratio"/> to any positive value such as
///     <c>16d / 9d</c>, <c>4d / 3d</c>, or <c>1</c> (the default, a perfect square).
///   </item>
///   <item>
///     <b>Polymorphic inner element</b> — renders the content host as a
///     <c>&lt;div&gt;</c> by default; set <see cref="BitBhComponentBase.As"/> to
///     render as any element. The outer wrapper is always a <c>&lt;div&gt;</c>.
///   </item>
///   <item>
///     <b>Attribute merging</b> — your <c>class</c>, <c>style</c>, and any other
///     attributes are applied to the inner element. The component's required
///     positioning styles are appended last so the ratio always holds.
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhAspectRatio Ratio="16d / 9d"&gt;
///     &lt;img src="/landscape.jpg" alt="" style="width:100%;height:100%;object-fit:cover" /&gt;
/// &lt;/BitBhAspectRatio&gt;
/// </code>
/// </summary>
public class BitBhAspectRatio : BitBhComponentBase
{
    /// <summary>
    /// The desired width-to-height ratio, expressed as <c>width / height</c>.
    /// For example <c>16d / 9d</c> for widescreen or <c>1</c> for a square.
    /// Defaults to <c>1</c>. Non-positive values fall back to <c>1</c>.
    /// </summary>
    [Parameter]
    public double Ratio { get; set; } = 1d;

    /// <summary>Content rendered inside the ratio-constrained inner element.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The polymorphic inner element defaults to a <c>&lt;div&gt;</c>.</summary>
    protected override string DefaultTag => "div";

    // The positioning styles that make the ratio work. Applied to the inner
    // element and appended after any user-supplied style so they always win.
    private const string InnerPositionStyle = "position:absolute;top:0;right:0;bottom:0;left:0";

    private double EffectiveRatio => Ratio > 0d ? Ratio : 1d;

    private string PaddingBottom =>
        (100d / EffectiveRatio).ToString(CultureInfo.InvariantCulture) + "%";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // ── Outer wrapper: reserves space at the requested ratio ──────────────
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-radix-aspect-ratio-wrapper", "");
        builder.AddAttribute(2, "style",
            $"position:relative;width:100%;padding-bottom:{PaddingBottom}");

        // ── Inner element: fills the reserved space, hosts the content ────────
        builder.OpenElement(10, Tag);
        builder.AddAttribute(11, "id", ComponentId);
        builder.AddMultipleAttributes(12, BuildInnerAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(13, Ref);

        builder.AddContent(14, ChildContent);
        builder.CloseElement();

        builder.CloseElement();
    }

    /// <summary>
    /// Merges user-supplied attributes onto the inner element, then appends the
    /// required absolute-positioning style so it takes precedence over any
    /// user-supplied <c>style</c> (mirroring Radix's style spread order).
    /// </summary>
    private Dictionary<string, object> BuildInnerAttributes()
    {
        var attrs = GetFinalAttributes();

        if (attrs.TryGetValue("style", out var existing)
            && existing?.ToString() is { Length: > 0 } userStyle
            && !string.IsNullOrWhiteSpace(userStyle))
        {
            attrs["style"] = $"{userStyle.TrimEnd().TrimEnd(';')};{InnerPositionStyle}";
        }
        else
        {
            attrs["style"] = InnerPositionStyle;
        }

        return attrs;
    }
}
