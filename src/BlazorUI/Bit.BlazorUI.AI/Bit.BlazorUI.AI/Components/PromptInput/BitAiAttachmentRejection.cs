namespace Bit.BlazorUI;

/// <summary>
/// A file the <see cref="BitAiPromptInput"/> did not stage, and why.
/// </summary>
/// <param name="FileName">The file name.</param>
/// <param name="ContentType">The MIME type the browser reported, which may be empty.</param>
/// <param name="Size">The size in bytes.</param>
/// <param name="Reason">Why the file was turned away.</param>
/// <remarks>
/// The composer always tells the user when it drops a file. This is the same information for the
/// application, which may want to log it or offer an alternative such as uploading to its own storage.
/// </remarks>
public sealed record BitAiAttachmentRejection(string FileName, string ContentType, long Size, BitAiAttachmentRejectionReason Reason);
