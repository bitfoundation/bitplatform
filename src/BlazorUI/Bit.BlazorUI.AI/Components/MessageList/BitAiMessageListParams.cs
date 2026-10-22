namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiMessageList"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the transcripts of an application agree on: whether they show the system messages, and
/// their classes and styles. The conversation, the session, the templates and the callbacks are left out on purpose:
/// they are what makes one transcript the one it is.
/// </remarks>
public class BitAiMessageListParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiMessageList"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiMessageList value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiMessageList)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the message list.
    /// </summary>
    public BitAiMessageListClassStyles? Classes { get; set; }

    /// <summary>
    /// Hides the messages the user did not write and the model did not produce (the system and developer ones).
    /// </summary>
    public bool? HideSystemMessages { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message list.
    /// </summary>
    public BitAiMessageListClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiMessageList"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiMessageList"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiMessageList"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiMessageList"/>.
    /// </remarks>
    /// <param name="bitAiMessageList">
    /// The <see cref="BitAiMessageList"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiMessageList bitAiMessageList)
    {
        if (bitAiMessageList is null) return;

        UpdateBaseParameters(bitAiMessageList);

        if (Classes is not null)
        {
            bitAiMessageList.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (HideSystemMessages.HasValue)
        {
            bitAiMessageList.TakeFromCascade(nameof(HideSystemMessages), HideSystemMessages.Value, static m => m.HideSystemMessages, static (m, v) => m.HideSystemMessages = v);
        }

        if (Styles is not null)
        {
            bitAiMessageList.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }
    }
}
