namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiMessagePartView"/> component.
/// </summary>
/// <remarks>
/// What belongs here is the look every part view of a region shares. The part, its message, the conversation, the
/// session and the citation callback are left out on purpose: they are what makes one part view the one it is.
/// </remarks>
public class BitAiMessagePartViewParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiMessagePartView"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiMessagePartView value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiMessagePartView)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the message part view.
    /// </summary>
    public BitAiMessagePartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message part view.
    /// </summary>
    public BitAiMessagePartViewClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiMessagePartView"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiMessagePartView"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiMessagePartView"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiMessagePartView"/>.
    /// </remarks>
    /// <param name="bitAiMessagePartView">
    /// The <see cref="BitAiMessagePartView"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiMessagePartView bitAiMessagePartView)
    {
        if (bitAiMessagePartView is null) return;

        UpdateBaseParameters(bitAiMessagePartView);

        if (Classes is not null)
        {
            bitAiMessagePartView.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiMessagePartView.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }
    }
}
