using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitLongPress"/> component.
/// </summary>
/// <remarks>
/// It carries how the long presses of an app are held - how long, how still, by what - and whether the keyboard has
/// them too. What a press does stays on each long press.
/// </remarks>
public partial class BitLongPressParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitLongPress"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitLongPress value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitLongPress)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLongPressParams))]
    public BitLongPressParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The time in milliseconds a press has to be held before it is a long press.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// Makes the long press a tab stop whose context-menu keys (Shift+F10 and the Menu key) raise OnLongPress.
    /// </summary>
    public bool? KeyboardTrigger { get; set; }

    /// <summary>
    /// How far in pixels the pointer may move while it is held before the press is called off.
    /// </summary>
    public decimal? MoveTolerance { get; set; }

    /// <summary>
    /// Leaves the mouse out: only a touch or a pen is held.
    /// </summary>
    public bool? TouchOnly { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitLongPress"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitLongPress"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitLongPress"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitLongPress"/>.
    /// </remarks>
    /// <param name="bitLongPress">
    /// The <see cref="BitLongPress"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitLongPress bitLongPress)
    {
        if (bitLongPress is null) return;

        UpdateBaseParameters(bitLongPress);

        if (Delay.HasValue)
        {
            bitLongPress.TakeFromCascade(nameof(Delay), Delay.Value, static l => l.Delay, static (l, v) => l.Delay = v);
        }

        if (KeyboardTrigger.HasValue)
        {
            bitLongPress.TakeFromCascade(nameof(KeyboardTrigger), KeyboardTrigger.Value, static l => l.KeyboardTrigger, static (l, v) => l.KeyboardTrigger = v);
        }

        if (MoveTolerance.HasValue)
        {
            bitLongPress.TakeFromCascade(nameof(MoveTolerance), MoveTolerance.Value, static l => l.MoveTolerance, static (l, v) => l.MoveTolerance = v);
        }

        if (TouchOnly.HasValue)
        {
            bitLongPress.TakeFromCascade(nameof(TouchOnly), TouchOnly.Value, static l => l.TouchOnly, static (l, v) => l.TouchOnly = v);
        }
    }
}
