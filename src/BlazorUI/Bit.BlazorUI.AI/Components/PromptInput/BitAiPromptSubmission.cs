namespace Bit.BlazorUI;

/// <summary>
/// What the <see cref="BitAiPromptInput"/> hands over when the user submits.
/// </summary>
/// <param name="Text">The trimmed text.</param>
/// <param name="Attachments">The files staged alongside it.</param>
/// <remarks>
/// The submission owns the attachments from then on: the composer no longer releases their browser streams or previews
/// when it is discarded.
/// </remarks>
public sealed record BitAiPromptSubmission(string Text, IReadOnlyList<BitAiAttachment> Attachments);
