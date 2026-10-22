namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiThinkingIndicator"/> component.
/// </summary>
/// <remarks>
/// The label is left out on purpose: the text the indicators of an application share is the Thinking text of the
/// string table (<see cref="BitAiUiOptions.Strings"/>), and a Label set on one indicator is what makes it the one it is.
/// </remarks>
public class BitAiThinkingIndicatorParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiThinkingIndicator"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiThinkingIndicator value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiThinkingIndicator)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the thinking indicator.
    /// </summary>
    public BitAiThinkingIndicatorClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the thinking indicator.
    /// </summary>
    public BitAiThinkingIndicatorClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiThinkingIndicator"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiThinkingIndicator"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiThinkingIndicator"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiThinkingIndicator"/>.
    /// </remarks>
    /// <param name="bitAiThinkingIndicator">
    /// The <see cref="BitAiThinkingIndicator"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiThinkingIndicator bitAiThinkingIndicator)
    {
        if (bitAiThinkingIndicator is null) return;

        UpdateBaseParameters(bitAiThinkingIndicator);

        if (Classes is not null)
        {
            bitAiThinkingIndicator.TakeFromCascade(nameof(Classes), Classes, static t => t.Classes, static (t, v) => t.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiThinkingIndicator.TakeFromCascade(nameof(Styles), Styles, static t => t.Styles, static (t, v) => t.Styles = v);
        }
    }
}
