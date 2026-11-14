using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitStepper{TItem}"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that say nothing about the item type or about the progress of a single stepper, so one
/// object fits every stepper under it. The steps (Items), the current step (CurrentIndex) and the callbacks stay on the
/// stepper itself.
/// </remarks>
public partial class BitStepperParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitStepper{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitStepper value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// The name is the bare name of the component, so a single cascade reaches every stepper whatever it is generic over.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitStepper<object>)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitStepperParams))]
    public BitStepperParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the stepper.
    /// </summary>
    public BitStepperClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a completed step, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? CompletedIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a completed step, from the built-in Fluent UI icons.
    /// </summary>
    public string? CompletedIconName { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a step in error, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? ErrorIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a step in error, from the built-in Fluent UI icons.
    /// </summary>
    public string? ErrorIconName { get; set; }

    /// <summary>
    /// Keeps the user from skipping ahead: only the steps before the current one (and the current one) can be clicked.
    /// </summary>
    public bool? Linear { get; set; }

    /// <summary>
    /// Only shows where the process is: no step can be clicked.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the stepper.
    /// </summary>
    public BitStepperClassStyles? Styles { get; set; }

    /// <summary>
    /// Lays the steps out from top to bottom instead of from start to end.
    /// </summary>
    public bool? Vertical { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitStepper{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitStepper{TItem}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitStepper"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitStepper"/>.
    /// </remarks>
    /// <param name="bitStepper">
    /// The <see cref="BitStepper{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem>(BitStepper<TItem> bitStepper) where TItem : class
    {
        if (bitStepper is null) return;

        UpdateBaseParameters(bitStepper);

        if (Classes is not null)
        {
            bitStepper.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (CompletedIcon is not null)
        {
            bitStepper.TakeFromCascade(nameof(CompletedIcon), CompletedIcon, static s => s.CompletedIcon, static (s, v) => s.CompletedIcon = v);
        }

        if (CompletedIconName is not null)
        {
            bitStepper.TakeFromCascade(nameof(CompletedIconName), CompletedIconName, static s => s.CompletedIconName, static (s, v) => s.CompletedIconName = v);
        }

        if (ErrorIcon is not null)
        {
            bitStepper.TakeFromCascade(nameof(ErrorIcon), ErrorIcon, static s => s.ErrorIcon, static (s, v) => s.ErrorIcon = v);
        }

        if (ErrorIconName is not null)
        {
            bitStepper.TakeFromCascade(nameof(ErrorIconName), ErrorIconName, static s => s.ErrorIconName, static (s, v) => s.ErrorIconName = v);
        }

        if (Linear.HasValue)
        {
            bitStepper.TakeFromCascade(nameof(Linear), Linear.Value, static s => s.Linear, static (s, v) => s.Linear = v);
        }

        if (ReadOnly.HasValue)
        {
            bitStepper.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static s => s.ReadOnly, static (s, v) => s.ReadOnly = v);
        }

        if (Styles is not null)
        {
            bitStepper.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (Vertical.HasValue)
        {
            bitStepper.TakeFromCascade(nameof(Vertical), Vertical.Value, static s => s.Vertical, static (s, v) => s.Vertical = v);
        }
    }
}
