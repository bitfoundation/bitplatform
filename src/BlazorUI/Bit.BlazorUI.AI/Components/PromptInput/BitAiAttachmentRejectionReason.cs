namespace Bit.BlazorUI;

/// <summary>
/// Why the <see cref="BitAiPromptInput"/> turned a file away.
/// </summary>
public enum BitAiAttachmentRejectionReason
{
    /// <summary>The file is larger than <see cref="BitAiPromptInput.MaxAttachmentSize"/>.</summary>
    TooLarge = 0,

    /// <summary>The file does not match <see cref="BitAiPromptInput.Accept"/>.</summary>
    TypeNotAllowed = 1,

    /// <summary>The browser could not deliver the content, or the transfer was interrupted.</summary>
    ReadFailed = 2,

    /// <summary>Staging the file would exceed <see cref="BitAiPromptInput.MaxAttachments"/>.</summary>
    TooMany = 3,
}
