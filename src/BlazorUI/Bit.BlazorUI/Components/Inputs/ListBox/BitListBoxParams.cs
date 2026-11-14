using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitListBox{TItem, TValue}"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that say nothing about the item and value types or about what a single list box holds, so
/// one object fits every list box under it. The items, the values, the label and the callbacks stay on the list box.
/// </remarks>
public partial class BitListBoxParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitListBox{TItem, TValue}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitListBox value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// The name is the bare name of the component, so a single cascade reaches every list box whatever it is generic over.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitListBox";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitListBoxParams))]
    public BitListBoxParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the list box.
    /// </summary>
    public BitListBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The height (any CSS length) past which the list scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Shows the validation messages of the cascading EditContext under the list box.
    /// </summary>
    public bool? ShowValidationMessage { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the list box.
    /// </summary>
    public BitListBoxClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitListBox{TItem, TValue}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitListBox{TItem, TValue}"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitListBox"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitListBox"/>.
    /// </remarks>
    /// <param name="bitListBox">
    /// The <see cref="BitListBox{TItem, TValue}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem, TValue>(BitListBox<TItem, TValue> bitListBox) where TItem : class
    {
        if (bitListBox is null) return;

        UpdateBaseParameters(bitListBox);

        if (Classes is not null)
        {
            bitListBox.TakeFromCascade(nameof(Classes), Classes, static l => l.Classes, static (l, v) => l.Classes = v);
        }

        if (MaxHeight is not null)
        {
            bitListBox.TakeFromCascade(nameof(MaxHeight), MaxHeight, static l => l.MaxHeight, static (l, v) => l.MaxHeight = v);
        }

        if (ShowValidationMessage.HasValue)
        {
            bitListBox.TakeFromCascade(nameof(ShowValidationMessage), ShowValidationMessage.Value, static l => l.ShowValidationMessage, static (l, v) => l.ShowValidationMessage = v);
        }

        if (Styles is not null)
        {
            bitListBox.TakeFromCascade(nameof(Styles), Styles, static l => l.Styles, static (l, v) => l.Styles = v);
        }
    }
}
