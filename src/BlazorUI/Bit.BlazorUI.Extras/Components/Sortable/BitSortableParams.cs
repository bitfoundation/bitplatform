using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSortable{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the sortable lists of an application, or of one part of it, agree on: how a drag feels
/// (the animation, the delays, the swap threshold, the edge scrolling), the texts they announce in the app's language,
/// and their look. The items, the groups, the rules and the callbacks are left out on purpose: they are what makes one
/// list the one it is.
/// </remarks>
public partial class BitSortableParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSortable{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSortable value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSortable<object>)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitSortableParams))]
    public BitSortableParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The duration in milliseconds of the slide animation of the items moving to their new positions.
    /// </summary>
    public int? Animation { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the sortable list.
    /// </summary>
    public BitSortableClassStyles? Classes { get; set; }

    /// <summary>
    /// How long, in milliseconds, a mouse or pen press on an item must be held before it can be dragged.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// How far, in pixels, the pointer may move while a delay runs before the press is taken for a scroll or a click.
    /// </summary>
    public int? DelayTolerance { get; set; }

    /// <summary>
    /// What an empty list shows, in place of <see cref="BitStrings.NoItemsFound"/>.
    /// </summary>
    public string? EmptyText { get; set; }

    /// <summary>
    /// Whether the mouse drags like a finger or a pen: with pointer events and a preview that follows the pointer.
    /// </summary>
    public bool? ForceFallback { get; set; }

    /// <summary>
    /// Whether the items can be picked up, moved and dropped from the keyboard.
    /// </summary>
    public bool? Keyboard { get; set; }

    /// <summary>
    /// Whether a drag released outside every list that takes it puts everything back where it was.
    /// </summary>
    public bool? RevertOnSpill { get; set; }

    /// <summary>
    /// Whether a pointer drag near an edge scrolls what is there.
    /// </summary>
    public bool? Scroll { get; set; }

    /// <summary>
    /// How close, in pixels, the pointer must come to an edge to scroll it.
    /// </summary>
    public int? ScrollSensitivity { get; set; }

    /// <summary>
    /// The most pixels an edge scrolls per frame.
    /// </summary>
    public int? ScrollSpeed { get; set; }

    /// <summary>
    /// The texts the lists give assistive technology, in the app's language.
    /// </summary>
    public BitSortableStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the sortable list.
    /// </summary>
    public BitSortableClassStyles? Styles { get; set; }

    /// <summary>
    /// The fraction (0-1) of an item that acts as its swap zone.
    /// </summary>
    public double? SwapThreshold { get; set; }

    /// <summary>
    /// How long, in milliseconds, a finger must rest on an item before it drags it.
    /// </summary>
    public int? TouchDelay { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSortable{TItem}"/> instance with any values that have been
    /// set on this object, if those properties have not already been set on the <see cref="BitSortable{TItem}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSortable"/> will be
    /// updated. This method does not overwrite existing values on <paramref name="bitSortable"/>.
    /// </remarks>
    /// <param name="bitSortable">
    /// The <see cref="BitSortable{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitSortable<TItem> bitSortable)
    {
        if (bitSortable is null) return;

        UpdateBaseParameters(bitSortable);

        if (Animation.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(Animation), Animation.Value, static s => s.Animation, static (s, v) => s.Animation = v);
        }

        if (Classes is not null)
        {
            bitSortable.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Delay.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(Delay), Delay.Value, static s => s.Delay, static (s, v) => s.Delay = v);
        }

        if (DelayTolerance.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(DelayTolerance), DelayTolerance.Value, static s => s.DelayTolerance, static (s, v) => s.DelayTolerance = v);
        }

        if (EmptyText is not null)
        {
            bitSortable.TakeFromCascade(nameof(EmptyText), EmptyText, static s => s.EmptyText, static (s, v) => s.EmptyText = v);
        }

        if (ForceFallback.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(ForceFallback), ForceFallback.Value, static s => s.ForceFallback, static (s, v) => s.ForceFallback = v);
        }

        if (Keyboard.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(Keyboard), Keyboard.Value, static s => s.Keyboard, static (s, v) => s.Keyboard = v);
        }

        if (RevertOnSpill.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(RevertOnSpill), RevertOnSpill.Value, static s => s.RevertOnSpill, static (s, v) => s.RevertOnSpill = v);
        }

        if (Scroll.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(Scroll), Scroll.Value, static s => s.Scroll, static (s, v) => s.Scroll = v);
        }

        if (ScrollSensitivity.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(ScrollSensitivity), ScrollSensitivity.Value, static s => s.ScrollSensitivity, static (s, v) => s.ScrollSensitivity = v);
        }

        if (ScrollSpeed.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(ScrollSpeed), ScrollSpeed.Value, static s => s.ScrollSpeed, static (s, v) => s.ScrollSpeed = v);
        }

        if (Strings is not null)
        {
            bitSortable.TakeFromCascade(nameof(Strings), Strings, static s => s.Strings, static (s, v) => s.Strings = v);
        }

        if (Styles is not null)
        {
            bitSortable.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (SwapThreshold.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(SwapThreshold), SwapThreshold.Value, static s => s.SwapThreshold, static (s, v) => s.SwapThreshold = v);
        }

        if (TouchDelay.HasValue)
        {
            bitSortable.TakeFromCascade(nameof(TouchDelay), TouchDelay.Value, static s => s.TouchDelay, static (s, v) => s.TouchDelay = v);
        }
    }
}
