using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// An optional wrapper that groups multiple <see cref="BitBhPopover"/> components so
/// that opening one automatically closes the others (mutual exclusion) — the
/// typical behaviour of a navigation bar with flyout menus.
///
/// <para>
/// When popovers are not inside a group they operate independently — any number
/// can be open at the same time.
/// </para>
/// </summary>
public class BitBhPopoverGroup : BitBhComponentBase
{
    private readonly List<BitBhPopover> _popovers = new();
    private BitBhPopoverGroupContext? _context;

    /// <summary>Child content. Should contain one or more <see cref="BitBhPopover"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _context ??= new BitBhPopoverGroupContext(
            register: p => { if (!_popovers.Contains(p)) _popovers.Add(p); },
            unregister: p => _popovers.Remove(p),
            closeOthersAsync: async except =>
            {
                foreach (var p in _popovers.Where(p => p != except).ToArray())
                    await p.CloseFromGroupAsync();
            });

        builder.OpenComponent<CascadingValue<BitBhPopoverGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", _context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(inner =>
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
}
