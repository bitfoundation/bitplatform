namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiSourceCard"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the source cards of an application, or of one part of it, agree on: their look. The
/// source, its number and the callback are left out on purpose: they are what makes one card the one it is.
/// </remarks>
public class BitAiSourceCardParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiSourceCard"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiSourceCard value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiSourceCard)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the source card.
    /// </summary>
    public BitAiSourceCardClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the source card.
    /// </summary>
    public BitAiSourceCardClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiSourceCard"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiSourceCard"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiSourceCard"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiSourceCard"/>.
    /// </remarks>
    /// <param name="bitAiSourceCard">
    /// The <see cref="BitAiSourceCard"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiSourceCard bitAiSourceCard)
    {
        if (bitAiSourceCard is null) return;

        UpdateBaseParameters(bitAiSourceCard);

        if (Classes is not null)
        {
            bitAiSourceCard.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiSourceCard.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
