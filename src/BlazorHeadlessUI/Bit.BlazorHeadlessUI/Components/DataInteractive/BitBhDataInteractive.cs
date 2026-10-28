using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless wrapper component that forwards <c>data-hover</c>, <c>data-active</c>,
/// and <c>data-focus</c> attributes to its child element for consistent interactive
/// state styling.
///
/// <para><b>Inspired by Headless UI's DataInteractive / Button data attributes:</b></para>
/// <list type="bullet">
///   <item>
///     <b><c>data-hover</c></b> — like <c>:hover</c>, but ignored on touch devices
///     to avoid sticky hover states.
///   </item>
///   <item>
///     <b><c>data-focus</c></b> — present while focused; <b><c>data-focus-visible</c></b> — like
///     <c>:focus-visible</c> (keyboard focus only).
///   </item>
///   <item>
///     <b><c>data-active</c></b> — like <c>:active</c>, but is removed when dragging
///     off of the element.
///   </item>
///   <item>
///     <b><c>data-disabled</c></b> — present when the component is disabled.
///   </item>
/// </list>
///
/// <para>
/// This component wraps any element and manages interactive state tracking via
/// DOM events. It renders a single element (configurable via <see cref="BitBhComponentBase.As"/>)
/// and applies data attributes that can be targeted with CSS attribute selectors
/// like <c>[data-hover]</c>, <c>[data-active]</c>, <c>[data-focus]</c>.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BitBhDataInteractive As="a" href="/dashboard" class="nav-link"&gt;
///     Dashboard
/// &lt;/BitBhDataInteractive&gt;
/// </code>
///
/// <para><b>With render context:</b></para>
/// <code>
/// &lt;BitBhDataInteractive Context="state"&gt;
///     &lt;span class="@(state.Hover ? "text-blue-500" : "")"&gt;
///         Hover me
///     &lt;/span&gt;
/// &lt;/BitBhDataInteractive&gt;
/// </code>
///
/// <para><b>CSS styling:</b></para>
/// <code>
/// .nav-link[data-hover] { background: var(--hover-bg); }
/// .nav-link[data-active] { background: var(--active-bg); }
/// .nav-link[data-focus] { outline: 2px solid var(--focus-ring); }
/// </code>
/// </summary>
public class BitBhDataInteractive : BitBhComponentBase
{
    private bool _isHovered;
    private bool _isActive;
    private bool _isFocused;
    private bool _isTouchDevice;

    /// <summary>Whether the component is disabled. Prevents interactive state tracking.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhDataInteractiveContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhDataInteractiveContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    private BitBhDataInteractiveContext Context => new()
    {
        Hover = _isHovered,
        Focus = _isFocused,
        Active = _isActive,
        Disabled = Disabled,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        // Mouse events for hover tracking (ignored on touch)
        builder.AddAttribute(30, "onpointerenter",
            On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
        builder.AddAttribute(31, "onpointerleave",
            On<PointerEventArgs>("onpointerleave", HandlePointerLeave));

        // Active state tracking
        builder.AddAttribute(40, "onpointerdown",
            On<PointerEventArgs>("onpointerdown", HandlePointerDown));
        builder.AddAttribute(41, "onpointerup",
            On<PointerEventArgs>("onpointerup", HandlePointerUp));
        builder.AddAttribute(42, "onpointercancel",
            On<PointerEventArgs>("onpointercancel", HandlePointerCancel));

        // Focus tracking (focus-visible semantics)
        builder.AddAttribute(50, "onfocusin",
            On<FocusEventArgs>("onfocusin", HandleFocusIn));
        builder.AddAttribute(51, "onfocusout",
            On<FocusEventArgs>("onfocusout", HandleFocusOut));

        // Touch detection
        builder.AddAttribute(60, "ontouchstart",
            On<TouchEventArgs>("ontouchstart", HandleTouchStart));

        if (Ref is not null)
            builder.AddElementReferenceCapture(70, Ref);

        if (Template is not null || ChildContent is not null)
            builder.AddContent(80, BitBhContent.Pick(Template, ChildContent, Context));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        // data-hover / data-active / data-focus / data-focus-visible are maintained
        // in the browser by the JS runtime (instant, touch-aware, focus-visible
        // aware). The C# state below only feeds the render context.
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", Disabled);
        // Tell assistive tech too (the element stays focusable, like aria-disabled controls should) -
        // where the element is something that can be disabled: ARIA 1.2 allows aria-disabled on
        // widget roles only, not on a plain (generic) div or span.
        if (Disabled && HasWidgetSemantics) attrs["aria-disabled"] = "true";

        return attrs;
    }

    private bool HasWidgetSemantics
        => GetAdditionalAttribute("role") is not null
           || Tag.ToLowerInvariant() is "button" or "a" or "input" or "select" or "textarea" or "summary" or "option";

    // ── Hover tracking ───────────────────────────────────────────────────────

    private void HandlePointerEnter(PointerEventArgs args)
    {
        if (Disabled) return;

        // Ignore touch-originated pointer events to avoid sticky hover on mobile
        if (_isTouchDevice || string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return;

        _isHovered = true;
        StateHasChanged();
    }

    private void HandlePointerLeave(PointerEventArgs args)
    {
        if (!_isHovered && !_isActive) return;

        _isHovered = false;
        _isActive = false; // Clear active when pointer leaves (drag-off behaviour)
        StateHasChanged();
    }

    // ── Active/press tracking ────────────────────────────────────────────────

    private void HandlePointerDown(PointerEventArgs args)
    {
        if (Disabled) return;

        _isActive = true;
        StateHasChanged();
    }

    private void HandlePointerUp(PointerEventArgs args)
    {
        if (!_isActive) return;

        _isActive = false;
        StateHasChanged();
    }

    private void HandlePointerCancel(PointerEventArgs args)
    {
        if (!_isActive) return;

        _isActive = false;
        StateHasChanged();
    }

    // ── Focus tracking (focus-visible semantics) ─────────────────────────────

    private void HandleFocusIn(FocusEventArgs args)
    {
        if (Disabled) return;

        _isFocused = true;
        StateHasChanged();
    }

    private void HandleFocusOut(FocusEventArgs args)
    {
        if (!_isFocused) return;

        _isFocused = false;
        StateHasChanged();
    }

    // ── Touch detection ──────────────────────────────────────────────────────

    private void HandleTouchStart(TouchEventArgs args)
    {
        // Mark as touch device to suppress hover on subsequent pointer events
        _isTouchDevice = true;
    }
}
