namespace Bit.Bortable;

/// <summary>
/// App-wide defaults for every <see cref="Bortable{TItem}"/> list. A value set here replaces the parameter's own
/// default for every list that does not set that parameter itself; a list that does keeps its own. A value left
/// <c>null</c> leaves the parameter's default as it is. Set them where the services are registered:
/// <code>builder.Services.AddBitBortableServices(options =&gt; { options.Animation = 200; options.Messages = German; });</code>
/// </summary>
public sealed class BortableOptions
{
    /// <summary>The default of <see cref="Bortable{TItem}.Animation"/>.</summary>
    public int? Animation { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.SwapThreshold"/>.</summary>
    public double? SwapThreshold { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.Delay"/>.</summary>
    public int? Delay { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.TouchDelay"/>.</summary>
    public int? TouchDelay { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.DelayTolerance"/>.</summary>
    public int? DelayTolerance { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.ForceFallback"/>.</summary>
    public bool? ForceFallback { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.Scroll"/>.</summary>
    public bool? Scroll { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.ScrollSensitivity"/>.</summary>
    public int? ScrollSensitivity { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.ScrollSpeed"/>.</summary>
    public int? ScrollSpeed { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.Keyboard"/>.</summary>
    public bool? Keyboard { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.RevertOnSpill"/>.</summary>
    public bool? RevertOnSpill { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.Messages"/>: the texts of every list, in the app's language.</summary>
    public BortableMessages? Messages { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.GhostClass"/>.</summary>
    public string? GhostClass { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.ChosenClass"/>.</summary>
    public string? ChosenClass { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.DragClass"/>.</summary>
    public string? DragClass { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.SwapClass"/>.</summary>
    public string? SwapClass { get; set; }

    /// <summary>The default of <see cref="Bortable{TItem}.SelectedClass"/>.</summary>
    public string? SelectedClass { get; set; }

    /// <summary>
    /// Gives <paramref name="list"/> every default set here, in place of its parameters' own defaults. Called before
    /// the list's first parameters are assigned, so a parameter the page sets overwrites it and keeps its own value.
    /// </summary>
    internal void ApplyTo<TItem>(Bortable<TItem> list)
    {
        if (Animation is { } animation) list.Animation = animation;
        if (SwapThreshold is { } swapThreshold) list.SwapThreshold = swapThreshold;
        if (Delay is { } delay) list.Delay = delay;
        if (TouchDelay is { } touchDelay) list.TouchDelay = touchDelay;
        if (DelayTolerance is { } delayTolerance) list.DelayTolerance = delayTolerance;
        if (ForceFallback is { } forceFallback) list.ForceFallback = forceFallback;
        if (Scroll is { } scroll) list.Scroll = scroll;
        if (ScrollSensitivity is { } sensitivity) list.ScrollSensitivity = sensitivity;
        if (ScrollSpeed is { } speed) list.ScrollSpeed = speed;
        if (Keyboard is { } keyboard) list.Keyboard = keyboard;
        if (RevertOnSpill is { } revert) list.RevertOnSpill = revert;
        if (Messages is { } messages) list.Messages = messages;
        if (GhostClass is { } ghost) list.GhostClass = ghost;
        if (ChosenClass is { } chosen) list.ChosenClass = chosen;
        if (DragClass is { } drag) list.DragClass = drag;
        if (SwapClass is { } swap) list.SwapClass = swap;
        if (SelectedClass is { } selected) list.SelectedClass = selected;
    }
}

/// <summary>One <c>AddBitBortableServices(options =&gt; ...)</c> call's configuration, kept until a scope builds its options.</summary>
internal sealed class BortableConfiguration(Action<BortableOptions> configure)
{
    public void Apply(BortableOptions options) => configure(options);
}
