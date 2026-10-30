using Microsoft.AspNetCore.Components;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Base class for floating content parts — popover panels, menu panels,
/// listbox / combobox option lists, tooltip and hover-card content. Handles
/// anchor positioning, portalling, dismissal (Escape / outside click), presence
/// (exit animations) and the <c>hidden</c>-until-positioned behaviour, so
/// subclasses only describe <em>what</em> they need.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (stays
/// <c>closed</c> while an exit animation runs), <c>data-side</c>,
/// <c>data-align</c>, plus the anchor CSS variables documented on
/// <see cref="BitBhAnchorOptions"/>.</para>
/// </summary>
public abstract class BitBhFloatingContentBase : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFloatingController? _floating;
    private BitBhFloatingScope? _scope;

    [CascadingParameter]
    private BitBhFloatingScope? ParentScope { get; set; }

    /// <summary>The scope cascaded to this surface's content (nested layers, arrow).</summary>
    protected BitBhFloatingScope Scope => _scope ??= new BitBhFloatingScope(() => _floating?.Handle ?? 0);

    /// <summary>The handle of the enclosing floating surface, used to nest layers.</summary>
    protected int ParentLayerHandle => ParentScope?.Handle ?? 0;

    /// <summary>The id of an arrow registered inside the content.</summary>
    protected string? ArrowId => _scope?.ArrowId;

    /// <summary>
    /// Positions the content relative to its trigger with collision handling.
    /// When null the content is left in normal flow for you to position with CSS.
    /// Accepts a placement string: <c>Anchor="@("bottom start")"</c>.
    /// </summary>
    [Parameter]
    public BitBhAnchorOptions? Anchor { get; set; }

    /// <summary>
    /// Teleports the content to the end of <c>&lt;body&gt;</c> (or <see cref="PortalTarget"/>)
    /// while open, escaping <c>overflow</c> and stacking contexts. Cascading values,
    /// events and re-renders keep working.
    /// </summary>
    [Parameter]
    public bool Portal { get; set; }

    /// <summary>Optional id or CSS selector of the element the content is portalled into.</summary>
    [Parameter]
    public string? PortalTarget { get; set; }

    /// <summary>
    /// When true the content element is removed from the DOM while closed. By default
    /// it stays in the DOM with the <c>hidden</c> attribute (cheaper to reopen and keeps
    /// <c>aria-controls</c> references valid).
    /// </summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>The controller driving the JS surface.</summary>
    protected BitBhFloatingController Floating => _floating ??= new BitBhFloatingController(Interop, OnDismissAsync, StateHasChanged, DispatchExceptionAsync);

    /// <summary>Whether the owning component is open.</summary>
    protected abstract bool IsOpen { get; }

    /// <summary>The anchor options in effect (subclasses may supply defaults).</summary>
    protected virtual BitBhAnchorOptions? EffectiveAnchor => Anchor;

    /// <summary>Whether the content is portalled.</summary>
    protected virtual bool EffectivePortal => Portal;

    /// <summary>Whether the content is open or still animating out.</summary>
    protected bool IsPresent => Floating.IsPresent(IsOpen);

    /// <summary>Whether the content element should be rendered at all.</summary>
    protected bool ShouldRenderContent => !Unmount || IsPresent;

    /// <summary>Whether the content element should carry the <c>hidden</c> attribute.</summary>
    protected bool IsHidden => Floating.ShouldHide(IsOpen, EffectiveAnchor is not null || EffectivePortal);

    /// <summary>Describes how the JS runtime should open the surface.</summary>
    protected abstract BitBhFloatingRequest CreateFloatingRequest();

    /// <summary>Called when the layer asks to be dismissed (Escape, outside click, focus outside).</summary>
    protected abstract Task OnDismissAsync(BitBhDismissReason reason);

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // Lets the runtime keep the element visible while a CSS exit animation runs.
        attrs["data-bit-bh-presence"] = "";
        return attrs;
    }

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
        => Floating.SyncAsync(() => IsOpen, CreateFloatingRequest);

    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_floating is not null)
            await _floating.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
