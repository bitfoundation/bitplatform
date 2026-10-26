using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A low-level focus-management primitive. While <see cref="Enabled"/> it can:
/// <list type="bullet">
///   <item><b>Auto-focus</b> — move focus inside on activation (<c>[data-bit-bh-autofocus]</c>, <see cref="InitialFocus"/>, or the first tabbable element).</item>
///   <item><b>Trap</b> — keep focus inside: Tab / Shift+Tab wrap, and focus that escapes (mouse, AT) is pulled back.</item>
///   <item><b>Loop</b> — wrap Tab at the edges without pulling focus back.</item>
///   <item><b>Restore</b> — return focus to where it was (or <see cref="ReturnFocus"/>) on deactivation, unless the user already moved it elsewhere.</item>
/// </list>
/// Scopes stack: only the most recently activated trap is enforced, and focus
/// inside layers opened from within the scope (portalled popovers, listboxes) is allowed.
///
/// <para>Used by dialogs and modal popovers; useful for custom drawers, slide-overs and editors.</para>
///
/// <code>
/// &lt;BitBhFocusScope Trapped="true" Enabled="isEditing"&gt; … &lt;/BitBhFocusScope&gt;
/// </code>
/// </summary>
public class BitBhFocusScope : BitBhComponentBase, IAsyncDisposable
{
    private int _handle;
    private bool _syncing;
    private bool _disposed;

    /// <summary>Whether the scope is active. Default: true.</summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>Trap focus inside while active.</summary>
    [Parameter]
    public bool Trapped { get; set; }

    /// <summary>Wrap Tab / Shift+Tab at the edges while active.</summary>
    [Parameter]
    public bool Loop { get; set; }

    /// <summary>Move focus inside when the scope activates. Default: true.</summary>
    [Parameter]
    public bool AutoFocus { get; set; } = true;

    /// <summary>Restore focus when the scope deactivates. Default: true.</summary>
    [Parameter]
    public bool RestoreFocus { get; set; } = true;

    /// <summary>Element to focus on activation (defaults to <c>[data-bit-bh-autofocus]</c> or the first tabbable element).</summary>
    [Parameter]
    public ElementReference? InitialFocus { get; set; }

    /// <summary>Element to focus on deactivation (defaults to the element focused before activation).</summary>
    [Parameter]
    public ElementReference? ReturnFocus { get; set; }

    /// <summary>The scoped content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override Task OnAfterRenderAsync(bool firstRender) => SyncAsync();

    // Enabled can flip while a create is in flight. One loop applies the changes in order, so a scope
    // created after Enabled went false, or after the component was disposed, is destroyed again rather
    // than left on top of the stack, trapping focus.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                if (Enabled && _handle == 0)
                {
                    _handle = await Interop.CreateFocusScopeAsync(ComponentId, new BitBhFocusScopeOptions
                    {
                        Trap = Trapped,
                        Loop = Loop,
                        AutoFocus = AutoFocus ? "first" : "none",
                        // AutoFocus off focuses nothing, InitialFocus included.
                        AutoFocusElement = AutoFocus ? InitialFocus : null,
                        RestoreFocus = RestoreFocus,
                        ReturnFocusElement = ReturnFocus,
                    });
                    // No container to scope (no browser): nothing to undo or retry until the next render.
                    if (_handle == 0) break;
                    continue;
                }
                if (Enabled is false && _handle > 0)
                {
                    await DestroyAsync();
                    continue;
                }
                break;
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await DestroyAsync();
    }

    private async Task DestroyAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.DestroyFocusScopeAsync(handle, RestoreFocus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BitBhFocusTrapContext>>(0);
        builder.AddComponentParameter(1, "Value", new BitBhFocusTrapContext { Enabled = Enabled });
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
        // Programmatically focusable fallback target when nothing inside is.
        attrs["tabindex"] = "-1";
        SetDataState(attrs, Enabled, "active", "inactive");
        return attrs;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // An in-flight create is undone by SyncAsync once it returns.
        if (_syncing is false) await DestroyAsync();
        GC.SuppressFinalize(this);
    }
}
