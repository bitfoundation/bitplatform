using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Renders its children into a different part of the DOM — by default a portal
/// root at the end of <c>&lt;body&gt;</c> (or the <see cref="BitBhPortalOutlet"/>, or
/// <see cref="Target"/>) — to escape <c>overflow: hidden</c> and stacking contexts.
///
/// <para>
/// Unlike re-rendering content elsewhere, the content stays in its place in the
/// <em>component</em> tree: cascading values (contexts of dialogs, menus, forms),
/// event handlers and re-renders keep working. Only the DOM node is moved.
/// During prerendering (no JS yet) the content renders in place.
/// </para>
///
/// <code>
/// &lt;BitBhPortal&gt;
///     &lt;div class="toast"&gt;Saved!&lt;/div&gt;
/// &lt;/BitBhPortal&gt;
/// </code>
/// </summary>
public class BitBhPortal : BitBhComponentBase, IAsyncDisposable
{
    private int _handle;
    private string? _movedTo;
    private bool _syncing;
    private bool _disposed;

    [CascadingParameter]
    private BitBhPortalContext? GroupContext { get; set; }

    /// <summary>Whether to teleport the content. When false it renders in place. Default: true.</summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>Id or CSS selector of the destination container. Defaults to the enclosing <see cref="BitBhPortalGroup"/>, the <see cref="BitBhPortalOutlet"/>, or a root appended to <c>&lt;body&gt;</c>.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>The teleported content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string? EffectiveTarget => Target ?? GroupContext?.TargetId;

    protected override Task OnAfterRenderAsync(bool firstRender) => SyncAsync();

    // A parent re-render while a move is in flight would otherwise see no handle and move the content a
    // second time, so that a later restore put it back into the portal root instead of its own place.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var target = EffectiveTarget ?? string.Empty;
                if (_handle > 0 && (Enabled is false || _movedTo != target))
                {
                    await RestoreAsync();
                    continue;
                }
                if (Enabled && _handle == 0)
                {
                    _handle = await Interop.PortalMoveAsync(ComponentId, EffectiveTarget);
                    _movedTo = target;
                    // Nothing moved (no browser): nothing to undo or retry until the next render.
                    if (_handle == 0) break;
                    continue;
                }
                break;
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await RestoreAsync();
    }

    private async Task RestoreAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        _movedTo = null;
        await Interop.PortalRestoreAsync(handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bit-bh-portal"] = "";
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "display:contents";
        return attrs;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        // Blazor removes the node wherever it lives; just drop the placeholder. An in-flight move is
        // undone by SyncAsync once it returns.
        if (_syncing is false) await RestoreAsync();
        GC.SuppressFinalize(this);
    }
}
