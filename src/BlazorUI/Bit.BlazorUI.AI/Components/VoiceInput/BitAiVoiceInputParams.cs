using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiVoiceInput"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the voice inputs of an application, or of one part of it, agree on: the language they
/// listen for and their look. The callbacks are left out on purpose: they are what makes one voice input the one it is.
/// </remarks>
public class BitAiVoiceInputParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiVoiceInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiVoiceInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiVoiceInput)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiVoiceInputParams))]
    public BitAiVoiceInputParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the voice input.
    /// </summary>
    public BitAiVoiceInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The recognition language, as a BCP 47 language tag such as <c>en-US</c>.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the voice input.
    /// </summary>
    public BitAiVoiceInputClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiVoiceInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiVoiceInput"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiVoiceInput"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiVoiceInput"/>.
    /// </remarks>
    /// <param name="bitAiVoiceInput">
    /// The <see cref="BitAiVoiceInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiVoiceInput bitAiVoiceInput)
    {
        if (bitAiVoiceInput is null) return;

        UpdateBaseParameters(bitAiVoiceInput);

        if (Classes is not null)
        {
            bitAiVoiceInput.TakeFromCascade(nameof(Classes), Classes, static v => v.Classes, static (v, value) => v.Classes = value);
        }

        if (Language.HasValue())
        {
            bitAiVoiceInput.TakeFromCascade(nameof(Language), Language, static v => v.Language, static (v, value) => v.Language = value);
        }

        if (Styles is not null)
        {
            bitAiVoiceInput.TakeFromCascade(nameof(Styles), Styles, static v => v.Styles, static (v, value) => v.Styles = value);
        }
    }
}
