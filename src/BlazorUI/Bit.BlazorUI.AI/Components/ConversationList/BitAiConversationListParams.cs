namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiConversationList"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the conversation lists of an application agree on: whether they offer a new conversation,
/// and their classes and styles. The conversations, the one that is open and the callbacks are left out on purpose:
/// they are what makes one list the one it is.
/// </remarks>
public class BitAiConversationListParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiConversationList"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiConversationList value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiConversationList)}";



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the conversation list.
    /// </summary>
    public BitAiConversationListClassStyles? Classes { get; set; }

    /// <summary>
    /// Shows the new-conversation button at the top of the list.
    /// </summary>
    public bool? ShowNewButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the conversation list.
    /// </summary>
    public BitAiConversationListClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiConversationList"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiConversationList"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiConversationList"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiConversationList"/>.
    /// </remarks>
    /// <param name="bitAiConversationList">
    /// The <see cref="BitAiConversationList"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiConversationList bitAiConversationList)
    {
        if (bitAiConversationList is null) return;

        UpdateBaseParameters(bitAiConversationList);

        if (Classes is not null)
        {
            bitAiConversationList.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (ShowNewButton.HasValue)
        {
            bitAiConversationList.TakeFromCascade(nameof(ShowNewButton), ShowNewButton.Value, static c => c.ShowNewButton, static (c, v) => c.ShowNewButton = v);
        }

        if (Styles is not null)
        {
            bitAiConversationList.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
