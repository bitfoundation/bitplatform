using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiChat"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the chats of an application agree on: what the composer offers, the words of the empty
/// state and the composer, whether the system messages are shown, and their classes and styles. The session, the
/// conversation, the templates and the callbacks are left out on purpose: they are what makes one chat the one it is.
/// </remarks>
public class BitAiChatParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiChat"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiChat value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiChat)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiChatParams))]
    public BitAiChatParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Offers attachments in the composer.
    /// </summary>
    public bool? AllowAttachments { get; set; }

    /// <summary>
    /// Offers dictation in the composer.
    /// </summary>
    public bool? AllowDictation { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the chat.
    /// </summary>
    public BitAiChatClassStyles? Classes { get; set; }

    /// <summary>
    /// The supporting line of the empty state.
    /// </summary>
    public string? EmptyBody { get; set; }

    /// <summary>
    /// The heading of the empty state.
    /// </summary>
    public string? EmptyTitle { get; set; }

    /// <summary>
    /// Hides the system messages from the transcript.
    /// </summary>
    public bool? HideSystemMessages { get; set; }

    /// <summary>
    /// The largest file the composer will stage, in bytes.
    /// </summary>
    public long? MaxAttachmentSize { get; set; }

    /// <summary>
    /// The placeholder of the composer.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the chat.
    /// </summary>
    public BitAiChatClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiChat"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiChat"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiChat"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiChat"/>.
    /// </remarks>
    /// <param name="bitAiChat">
    /// The <see cref="BitAiChat"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiChat bitAiChat)
    {
        if (bitAiChat is null) return;

        UpdateBaseParameters(bitAiChat);

        if (AllowAttachments.HasValue)
        {
            bitAiChat.TakeFromCascade(nameof(AllowAttachments), AllowAttachments.Value, static c => c.AllowAttachments, static (c, v) => c.AllowAttachments = v);
        }

        if (AllowDictation.HasValue)
        {
            bitAiChat.TakeFromCascade(nameof(AllowDictation), AllowDictation.Value, static c => c.AllowDictation, static (c, v) => c.AllowDictation = v);
        }

        if (Classes is not null)
        {
            bitAiChat.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (EmptyBody.HasValue())
        {
            bitAiChat.TakeFromCascade(nameof(EmptyBody), EmptyBody, static c => c.EmptyBody, static (c, v) => c.EmptyBody = v);
        }

        if (EmptyTitle.HasValue())
        {
            bitAiChat.TakeFromCascade(nameof(EmptyTitle), EmptyTitle, static c => c.EmptyTitle, static (c, v) => c.EmptyTitle = v);
        }

        if (HideSystemMessages.HasValue)
        {
            bitAiChat.TakeFromCascade(nameof(HideSystemMessages), HideSystemMessages.Value, static c => c.HideSystemMessages, static (c, v) => c.HideSystemMessages = v);
        }

        if (MaxAttachmentSize.HasValue)
        {
            bitAiChat.TakeFromCascade(nameof(MaxAttachmentSize), MaxAttachmentSize.Value, static c => c.MaxAttachmentSize, static (c, v) => c.MaxAttachmentSize = v);
        }

        if (Placeholder.HasValue())
        {
            bitAiChat.TakeFromCascade(nameof(Placeholder), Placeholder, static c => c.Placeholder, static (c, v) => c.Placeholder = v);
        }

        if (Styles is not null)
        {
            bitAiChat.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }
    }
}
