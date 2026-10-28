using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiChatMessage"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the messages of an application agree on: their classes and styles. The message, its
/// conversation and session, whether it is the latest one and whether it animates are left out on purpose: they are
/// what makes one message the one it is.
/// </remarks>
public class BitAiChatMessageParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiChatMessage"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiChatMessage value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiChatMessage)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiChatMessageParams))]
    public BitAiChatMessageParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for different parts of the chat message.
    /// </summary>
    public BitAiChatMessageClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the chat message.
    /// </summary>
    public BitAiChatMessageClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiChatMessage"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiChatMessage"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiChatMessage"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiChatMessage"/>.
    /// </remarks>
    /// <param name="bitAiChatMessage">
    /// The <see cref="BitAiChatMessage"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiChatMessage bitAiChatMessage)
    {
        if (bitAiChatMessage is null) return;

        UpdateBaseParameters(bitAiChatMessage);

        if (Classes is not null)
        {
            bitAiChatMessage.TakeFromCascade(nameof(Classes), Classes, static m => m.Classes, static (m, v) => m.Classes = v);
        }

        if (Styles is not null)
        {
            bitAiChatMessage.TakeFromCascade(nameof(Styles), Styles, static m => m.Styles, static (m, v) => m.Styles = v);
        }
    }
}
