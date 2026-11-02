using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAiPromptInput"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the composers of an application, or of one part of it, agree on: the files they take, the
/// limits they keep, the commands and mentions they offer, the placeholder and their look. The text, the session, the
/// draft key, the model and the templates and callbacks are left out on purpose: they are what makes one composer the one
/// it is.
/// </remarks>
public class BitAiPromptInputParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAiPromptInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAiPromptInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAiPromptInput)}";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiPromptInputParams))]
    public BitAiPromptInputParams() { }



    public string Name => ParamName;



    /// <summary>
    /// The file types to accept, in the syntax of the HTML <c>accept</c> attribute.
    /// </summary>
    public string? Accept { get; set; }

    /// <summary>
    /// Offers the attach button and accepts dropped or pasted files.
    /// </summary>
    public bool? AllowAttachments { get; set; }

    /// <summary>
    /// Offers the dictation button, when the browser supports it.
    /// </summary>
    public bool? AllowDictation { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the prompt input.
    /// </summary>
    public BitAiPromptInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The commands offered when the user types a forward slash at the start of the text.
    /// </summary>
    public IReadOnlyList<BitAiSlashCommand>? Commands { get; set; }

    /// <summary>
    /// The dictation language, as a BCP 47 language tag such as <c>en-US</c>.
    /// </summary>
    public string? DictationLanguage { get; set; }

    /// <summary>
    /// The largest file the composer stages, in bytes.
    /// </summary>
    public long? MaxAttachmentSize { get; set; }

    /// <summary>
    /// The most files that can be staged at once.
    /// </summary>
    public int? MaxAttachments { get; set; }

    /// <summary>
    /// The largest file whose bytes are held in memory, in bytes.
    /// </summary>
    public long? MaxInlineSize { get; set; }

    /// <summary>
    /// The most characters the text may hold.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The items offered when the user types an at sign.
    /// </summary>
    public IReadOnlyList<BitAiMentionItem>? Mentions { get; set; }

    /// <summary>
    /// The placeholder of the text.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the prompt input.
    /// </summary>
    public BitAiPromptInputClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAiPromptInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAiPromptInput"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAiPromptInput"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAiPromptInput"/>.
    /// </remarks>
    /// <param name="bitAiPromptInput">
    /// The <see cref="BitAiPromptInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAiPromptInput bitAiPromptInput)
    {
        if (bitAiPromptInput is null) return;

        UpdateBaseParameters(bitAiPromptInput);

        if (Accept.HasValue())
        {
            bitAiPromptInput.TakeFromCascade(nameof(Accept), Accept, static p => p.Accept, static (p, v) => p.Accept = v);
        }

        if (AllowAttachments.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(AllowAttachments), AllowAttachments.Value, static p => p.AllowAttachments, static (p, v) => p.AllowAttachments = v);
        }

        if (AllowDictation.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(AllowDictation), AllowDictation.Value, static p => p.AllowDictation, static (p, v) => p.AllowDictation = v);
        }

        if (Classes is not null)
        {
            bitAiPromptInput.TakeFromCascade(nameof(Classes), Classes, static p => p.Classes, static (p, v) => p.Classes = v);
        }

        if (Commands is not null)
        {
            bitAiPromptInput.TakeFromCascade(nameof(Commands), Commands, static p => p.Commands, static (p, v) => p.Commands = v);
        }

        if (DictationLanguage.HasValue())
        {
            bitAiPromptInput.TakeFromCascade(nameof(DictationLanguage), DictationLanguage, static p => p.DictationLanguage, static (p, v) => p.DictationLanguage = v);
        }

        if (MaxAttachmentSize.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(MaxAttachmentSize), MaxAttachmentSize.Value, static p => p.MaxAttachmentSize, static (p, v) => p.MaxAttachmentSize = v);
        }

        if (MaxAttachments.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(MaxAttachments), MaxAttachments.Value, static p => p.MaxAttachments, static (p, v) => p.MaxAttachments = v);
        }

        if (MaxInlineSize.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(MaxInlineSize), MaxInlineSize.Value, static p => p.MaxInlineSize, static (p, v) => p.MaxInlineSize = v);
        }

        if (MaxLength.HasValue)
        {
            bitAiPromptInput.TakeFromCascade(nameof(MaxLength), MaxLength, static p => p.MaxLength, static (p, v) => p.MaxLength = v);
        }

        if (Mentions is not null)
        {
            bitAiPromptInput.TakeFromCascade(nameof(Mentions), Mentions, static p => p.Mentions, static (p, v) => p.Mentions = v);
        }

        if (Placeholder.HasValue())
        {
            bitAiPromptInput.TakeFromCascade(nameof(Placeholder), Placeholder, static p => p.Placeholder, static (p, v) => p.Placeholder = v);
        }

        if (Styles is not null)
        {
            bitAiPromptInput.TakeFromCascade(nameof(Styles), Styles, static p => p.Styles, static (p, v) => p.Styles = v);
        }
    }
}
