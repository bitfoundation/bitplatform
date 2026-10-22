namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiCopyButton"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the copy buttons of an application, or of one part of it, agree on: whether they show
/// their label and how long they confirm a copy. The text to copy, the label and the callback are left out on
/// purpose: they are what makes one copy button the one it is.
/// </remarks>
public class BitAiCopyButtonParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiCopyButton"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiCopyButton value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiCopyButton)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the copy button.
    /// </summary>
    public BitAiCopyButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// How long the confirmation stays on the button after a copy.
    /// </summary>
    public TimeSpan? ConfirmationDuration { get; set; }

    /// <summary>
    /// Shows the label beside the icon.
    /// </summary>
    public bool? ShowLabel { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the copy button.
    /// </summary>
    public BitAiCopyButtonClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiCopyButton"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiCopyButton"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiCopyButton"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiCopyButton"/>.
    /// </remarks>
    /// <param name="bitAiCopyButton">
    /// The <see cref="BitAiCopyButton"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiCopyButton bitAiCopyButton)
    {
        if (bitAiCopyButton is null) return;

        UpdateBaseParameters(bitAiCopyButton);

        if (Classes is not null)
        {
            bitAiCopyButton.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (ConfirmationDuration.HasValue)
        {
            bitAiCopyButton.TakeFromCascade(nameof(ConfirmationDuration), ConfirmationDuration.Value, static b => b.ConfirmationDuration, static (b, v) => b.ConfirmationDuration = v);
        }

        if (ShowLabel.HasValue)
        {
            bitAiCopyButton.TakeFromCascade(nameof(ShowLabel), ShowLabel.Value, static b => b.ShowLabel, static (b, v) => b.ShowLabel = v);
        }

        if (Styles is not null)
        {
            bitAiCopyButton.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }
    }
}
