namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiReasoningPartView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the reasoning panels of an app, or of one part of it, agree on - the look of their parts.
/// The part is left out on purpose: it is what makes one panel the one it is. Whether a finished panel collapses is
/// <see cref="BitAiUiOptions.CollapseReasoning"/>.
/// </remarks>
public class BitAiReasoningPartViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiReasoningPartView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiReasoningPartView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiReasoningPartView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the reasoning panel.
    /// </summary>
    public BitAiReasoningPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the reasoning panel.
    /// </summary>
    public BitAiReasoningPartViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiReasoningPartView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiReasoningPartView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiReasoningPartView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiReasoningPartView"/>.
    /// </remarks>
    /// <param name="bitAiReasoningPartView">
    /// The <see cref="BitAiReasoningPartView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiReasoningPartView bitAiReasoningPartView)
    {
        if (bitAiReasoningPartView is null) return;

        UpdateBaseParameters(bitAiReasoningPartView);

        if (Classes is not null)
        {
            bitAiReasoningPartView.TakeFromCascade(nameof(Classes), Classes, static r => r.Classes, static (r, v) => r.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiReasoningPartView.TakeFromCascade(nameof(Styles), Styles, static r => r.Styles, static (r, v) => r.Styles = v);
        }
    }
}
