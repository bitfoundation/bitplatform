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
    /// This method does not overwrite existing values on <paramref name="bitSwipeTrap"/>.
    /// </remarks>
    /// <param name="bitSwipeTrap">
    /// The <see cref="BitSwipeTrap"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitSwipeTrap bitSwipeTrap)
    {
        if (bitSwipeTrap is null) return;

        UpdateBaseParameters(bitSwipeTrap);

        if (KeyboardTrigger.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(KeyboardTrigger)))
        {
            bitSwipeTrap.KeyboardTrigger = KeyboardTrigger.Value;
        }

        if (OrientationLock.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(OrientationLock)))
        {
            bitSwipeTrap.OrientationLock = OrientationLock.Value;

            bitSwipeTrap.ClassBuilder.Reset();
        }

        if (SkipSelector.HasValue() && bitSwipeTrap.HasNotBeenSet(nameof(SkipSelector)))
        {
            bitSwipeTrap.SkipSelector = SkipSelector;
        }

        if (Threshold.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(Threshold)))
        {
            bitSwipeTrap.Threshold = Threshold.Value;
        }

        if (Throttle.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(Throttle)))
        {
            bitSwipeTrap.Throttle = Throttle.Value;
        }

        if (TouchOnly.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(TouchOnly)))
        {
            bitSwipeTrap.TouchOnly = TouchOnly.Value;
        }

        if (Trigger.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(Trigger)))
        {
            bitSwipeTrap.Trigger = Trigger.Value;
        }

        if (TriggerVelocity.HasValue && bitSwipeTrap.HasNotBeenSet(nameof(TriggerVelocity)))
        {
            bitSwipeTrap.TriggerVelocity = TriggerVelocity.Value;
        }
    }
}
