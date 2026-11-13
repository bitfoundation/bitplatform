using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitPopConfirm"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the pop confirms of an app agree on: the words and the color of their buttons, the side
/// they open on and their look. The question, the trigger and whether one is open are the state of a single pop confirm.
/// </remarks>
public class BitPopConfirmParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitPopConfirm"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitPopConfirm value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitPopConfirm";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitPopConfirmParams))]
    public BitPopConfirmParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The text of the Cancel button.
    /// </summary>
    public string? CancelText { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the pop confirm.
    /// </summary>
    public BitPopConfirmClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the Confirm button.
    /// </summary>
    public BitColor? ConfirmColor { get; set; }

    /// <summary>
    /// The text of the Confirm button.
    /// </summary>
    public string? ConfirmText { get; set; }

    /// <summary>
    /// The side of the trigger the popup is placed on.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the pop confirm.
    /// </summary>
    public BitPopConfirmClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitPopConfirm"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitPopConfirm"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitPopConfirm"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitPopConfirm"/>.
    /// </remarks>
    /// <param name="bitPopConfirm">
    /// The <see cref="BitPopConfirm"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitPopConfirm bitPopConfirm)
    {
        if (bitPopConfirm is null) return;

        UpdateBaseParameters(bitPopConfirm);

        if (CancelText is not null)
        {
            bitPopConfirm.TakeFromCascade(nameof(CancelText), CancelText, static c => c.CancelText, static (c, v) => c.CancelText = v);
        }

        if (Classes is not null)
        {
            bitPopConfirm.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (ConfirmColor.HasValue)
        {
            bitPopConfirm.TakeFromCascade(nameof(ConfirmColor), ConfirmColor, static c => c.ConfirmColor, static (c, v) => c.ConfirmColor = v);
        }

        if (ConfirmText is not null)
        {
            bitPopConfirm.TakeFromCascade(nameof(ConfirmText), ConfirmText, static c => c.ConfirmText, static (c, v) => c.ConfirmText = v);
        }

        if (Placement.HasValue)
        {
            bitPopConfirm.TakeFromCascade(nameof(Placement), Placement, static c => c.Placement, static (c, v) => c.Placement = v);
        }

        if (Styles is not null)
        {
            bitPopConfirm.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
