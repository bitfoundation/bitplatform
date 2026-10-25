using Microsoft.AspNetCore.Components;

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

    /// <summary>Gives <paramref name="list"/> every default set here whose parameter it was not given.</summary>
    internal void ApplyTo<TItem>(Bortable<TItem> list, in ParameterView parameters)
    {
        var given = parameters;
        bool Unset(string name) => !given.TryGetValue<object>(name, out _);

        if (Animation is { } animation && Unset(nameof(Bortable<TItem>.Animation))) list.Animation = animation;
        if (SwapThreshold is { } swapThreshold && Unset(nameof(Bortable<TItem>.SwapThreshold))) list.SwapThreshold = swapThreshold;
        if (Delay is { } delay && Unset(nameof(Bortable<TItem>.Delay))) list.Delay = delay;
        if (TouchDelay is { } touchDelay && Unset(nameof(Bortable<TItem>.TouchDelay))) list.TouchDelay = touchDelay;
        if (DelayTolerance is { } delayTolerance && Unset(nameof(Bortable<TItem>.DelayTolerance))) list.DelayTolerance = delayTolerance;
        if (ForceFallback is { } forceFallback && Unset(nameof(Bortable<TItem>.ForceFallback))) list.ForceFallback = forceFallback;
        if (Scroll is { } scroll && Unset(nameof(Bortable<TItem>.Scroll))) list.Scroll = scroll;
        if (ScrollSensitivity is { } sensitivity && Unset(nameof(Bortable<TItem>.ScrollSensitivity))) list.ScrollSensitivity = sensitivity;
        if (ScrollSpeed is { } speed && Unset(nameof(Bortable<TItem>.ScrollSpeed))) list.ScrollSpeed = speed;
        if (Keyboard is { } keyboard && Unset(nameof(Bortable<TItem>.Keyboard))) list.Keyboard = keyboard;
        if (RevertOnSpill is { } revert && Unset(nameof(Bortable<TItem>.RevertOnSpill))) list.RevertOnSpill = revert;
        if (Messages is { } messages && Unset(nameof(Bortable<TItem>.Messages))) list.Messages = messages;
        if (GhostClass is { } ghost && Unset(nameof(Bortable<TItem>.GhostClass))) list.GhostClass = ghost;
        if (ChosenClass is { } chosen && Unset(nameof(Bortable<TItem>.ChosenClass))) list.ChosenClass = chosen;
        if (DragClass is { } drag && Unset(nameof(Bortable<TItem>.DragClass))) list.DragClass = drag;
        if (SwapClass is { } swap && Unset(nameof(Bortable<TItem>.SwapClass))) list.SwapClass = swap;
        if (SelectedClass is { } selected && Unset(nameof(Bortable<TItem>.SelectedClass))) list.SelectedClass = selected;
    }
}

/// <summary>One <c>AddBitBortableServices(options =&gt; ...)</c> call's configuration, kept until a scope builds its options.</summary>
internal sealed class BortableConfiguration(Action<BortableOptions> configure)
{
    public void Apply(BortableOptions options) => configure(options);
}
