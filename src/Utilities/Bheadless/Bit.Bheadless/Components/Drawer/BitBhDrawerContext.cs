using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>The edge of the screen a <see cref="BitBhDrawer"/> is attached to (and slides in from).</summary>
public enum BitBhDrawerSide
{
    /// <summary>The bottom edge (a bottom sheet).</summary>
    Bottom,

    /// <summary>The top edge.</summary>
    Top,

    /// <summary>The left edge.</summary>
    Left,

    /// <summary>The right edge.</summary>
    Right,

    /// <summary>The edge text starts at: left, or right in a right-to-left direction.</summary>
    Start,

    /// <summary>The edge text ends at: right, or left in a right-to-left direction.</summary>
    End,
}

/// <summary>
/// Cascading context provided by <see cref="BitBhDrawer"/> to its parts (<see cref="BitBhDrawerTrigger"/>,
/// <see cref="BitBhDrawerBackdrop"/>, <see cref="BitBhDrawerPanel"/>, <see cref="BitBhDrawerTitle"/>,
/// <see cref="BitBhDrawerDescription"/>, <see cref="BitBhDrawerClose"/>, <see cref="BitBhDrawerHandle"/>) and to
/// drawers nested in it. Carries the open state, the resolved side, ARIA wiring ids and the open / close callbacks.
/// </summary>
public sealed class BitBhDrawerContext : IBitBhCloseContext
{
    internal BitBhDrawerContext(BitBhDrawer owner) => Owner = owner;

    internal BitBhDrawer Owner { get; }

    /// <summary>Whether the drawer is open.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Whether the backdrop and panel are rendered (open, or animating out).</summary>
    public bool IsPresent { get; init; }

    /// <summary>Whether the backdrop and panel are hidden (closed, or waiting to be portalled).</summary>
    public bool IsHidden { get; init; }

    /// <summary>The base id the part ids derive from.</summary>
    public required string BaseId { get; init; }

    /// <summary>The screen edge the drawer is attached to: <c>top</c>, <c>right</c>, <c>bottom</c> or <c>left</c> (Start / End resolved).</summary>
    public required string Side { get; init; }

    /// <summary>Whether the drawer is modal.</summary>
    public bool Modal { get; init; }

    /// <summary>Whether a <see cref="BitBhDrawerTitle"/> is rendered.</summary>
    public bool HasTitle { get; init; }

    /// <summary>Whether a <see cref="BitBhDrawerDescription"/> is rendered.</summary>
    public bool HasDescription { get; init; }

    /// <summary>Whether the drawer was opened from another drawer (it renders inside that drawer's content).</summary>
    public bool IsNested { get; init; }

    /// <summary>How many drawers nested in this one are open in front of it.</summary>
    public int NestedDrawers { get; init; }

    /// <summary>The drawer's snap points, from the smallest to the largest; empty without any.</summary>
    public IReadOnlyList<BitBhDrawerSnapPoint> SnapPoints { get; init; } = [];

    /// <summary>The index in <see cref="SnapPoints"/> of the snap point the drawer rests at; -1 without snap points.</summary>
    public int ActiveSnapPointIndex { get; init; } = -1;

    /// <summary>The snap point the drawer rests at; null without snap points.</summary>
    public BitBhDrawerSnapPoint? ActiveSnapPoint => ActiveSnapPointIndex >= 0 && ActiveSnapPointIndex < SnapPoints.Count ? SnapPoints[ActiveSnapPointIndex] : (BitBhDrawerSnapPoint?)null;

    /// <summary>The deterministic id of the trigger.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>The deterministic id of the panel.</summary>
    public string PanelId => $"{BaseId}-panel";

    /// <summary>The deterministic id of the backdrop.</summary>
    public string BackdropId => $"{BaseId}-backdrop";

    /// <summary>The deterministic id used as <c>aria-labelledby</c> on the panel.</summary>
    public string TitleId => $"{BaseId}-title";

    /// <summary>The deterministic id used as <c>aria-describedby</c> on the panel.</summary>
    public string DescriptionId => $"{BaseId}-description";

    /// <summary>Opens the drawer.</summary>
    public Task OpenAsync() => Owner.OpenAsync();

    /// <summary>Closes the drawer.</summary>
    public Task CloseAsync() => Owner.CloseAsync();

    /// <summary>Requests dismissal (respects the <c>DismissOn…</c> parameters and <c>OnDismissRequest</c>).</summary>
    public Task DismissAsync(BitBhDismissReason reason) => Owner.DismissAsync(reason);

    /// <summary>Moves the drawer to one of its <see cref="SnapPoints"/>.</summary>
    public Task SetActiveSnapPointAsync(BitBhDrawerSnapPoint? snapPoint) => Owner.SetActiveSnapPointAsync(snapPoint);

    internal void Register(BitBhDrawerPart part, bool present) => Owner.Register(part, present);

    internal void RegisterPanel(Func<ElementReference>? initialFocus, bool dismissOnEscape) => Owner.RegisterPanel(initialFocus, dismissOnEscape);
}

/// <summary>Render-fragment context exposed by drawer parts for state-driven rendering.</summary>
public sealed record BitBhDrawerRenderContext
{
    /// <summary>Whether the drawer is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the drawer.</summary>
    public required Action Close { get; init; }
}

internal enum BitBhDrawerPart { Trigger, Backdrop, Title, Description }
