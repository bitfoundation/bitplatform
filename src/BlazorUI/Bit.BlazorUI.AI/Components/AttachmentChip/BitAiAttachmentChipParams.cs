namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiAttachmentChip"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the attachment chips of an application, or of one part of it, agree on: their look. The
/// attachment and the remove callback are left out on purpose: they are what makes one chip the one it is.
/// </remarks>
public class BitAiAttachmentChipParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiAttachmentChip"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiAttachmentChip value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiAttachmentChip)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the attachment chip.
    /// </summary>
    public BitAiAttachmentChipClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the attachment chip.
    /// </summary>
    public BitAiAttachmentChipClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiAttachmentChip"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiAttachmentChip"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiAttachmentChip"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiAttachmentChip"/>.
    /// </remarks>
    /// <param name="bitAiAttachmentChip">
    /// The <see cref="BitAiAttachmentChip"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiAttachmentChip bitAiAttachmentChip)
    {
        if (bitAiAttachmentChip is null) return;

        UpdateBaseParameters(bitAiAttachmentChip);

        if (Classes is not null)
        {
            bitAiAttachmentChip.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiAttachmentChip.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
