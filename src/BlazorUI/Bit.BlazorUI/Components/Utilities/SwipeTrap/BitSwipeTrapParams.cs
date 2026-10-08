namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitSwipeTrap"/> component.
/// </summary>
/// <remarks>
/// What each swipe trap holds for itself - its content and its callbacks - is not a group default, so it is not
/// carried here.
/// </remarks>
public class BitSwipeTrapParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitSwipeTrap"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitSwipeTrap value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitSwipeTrap)}";



    public string Name => ParamName;



    /// <summary>
    /// Lets the arrow keys raise the OnTrigger event, in their own direction, while the swipe trap itself has the focus.
    /// </summary>
    public bool? KeyboardTrigger { get; set; }

    /// <summary>
    /// The orientation lock in which the swipe trap traps the swipe actions.
    /// </summary>
    public BitSwipeOrientation? OrientationLock { get; set; }

    /// <summary>
    /// A CSS selector of descendant elements on which starting a swipe is ignored.
    /// </summary>
    public string? SkipSelector { get; set; }

    /// <summary>
    /// The distance in pixels a gesture must cover before the swipe trap takes it over.
    /// </summary>
    public decimal? Threshold { get; set; }

    /// <summary>
    /// The throttle time in milliseconds between two calls of the OnMove event.
    /// </summary>
    public int? Throttle { get; set; }

    /// <summary>
    /// Ignores mouse swipes, trapping only touch (and pen) gestures.
    /// </summary>
    public bool? TouchOnly { get; set; }

    /// <summary>
    /// The swiping point to trigger the OnTrigger event: a fraction of the element's size (below 1) or pixels.
    /// </summary>
    public decimal? Trigger { get; set; }

    /// <summary>
    /// The release velocity in pixels per millisecond that triggers the OnTrigger event as a flick.
    /// </summary>
    public decimal? TriggerVelocity { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitSwipeTrap"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitSwipeTrap"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitSwipeTrap"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitSwipeTrap"/>. What it supplies is recorded on
    /// the component, which puts back the value it replaced once this object stops supplying one.
    /// </remarks>
    /// <param name="bitSwipeTrap">
    /// The <see cref="BitSwipeTrap"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSwipeTrap bitSwipeTrap)
    {
        if (bitSwipeTrap is null) return;

        UpdateBaseParameters(bitSwipeTrap);

        if (KeyboardTrigger.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(KeyboardTrigger), KeyboardTrigger.Value, static s => s.KeyboardTrigger, static (s, v) => s.KeyboardTrigger = v);
        }

        if (OrientationLock.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(OrientationLock), OrientationLock, static s => s.OrientationLock, static (s, v) => s.OrientationLock = v);
        }

        if (SkipSelector.HasValue())
        {
            bitSwipeTrap.TakeFromCascade(nameof(SkipSelector), SkipSelector, static s => s.SkipSelector, static (s, v) => s.SkipSelector = v);
        }

        if (Threshold.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(Threshold), Threshold, static s => s.Threshold, static (s, v) => s.Threshold = v);
        }

        if (Throttle.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(Throttle), Throttle, static s => s.Throttle, static (s, v) => s.Throttle = v);
        }

        if (TouchOnly.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(TouchOnly), TouchOnly.Value, static s => s.TouchOnly, static (s, v) => s.TouchOnly = v);
        }

        if (Trigger.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(Trigger), Trigger, static s => s.Trigger, static (s, v) => s.Trigger = v);
        }

        if (TriggerVelocity.HasValue)
        {
            bitSwipeTrap.TakeFromCascade(nameof(TriggerVelocity), TriggerVelocity, static s => s.TriggerVelocity, static (s, v) => s.TriggerVelocity = v);
        }
    }
}
