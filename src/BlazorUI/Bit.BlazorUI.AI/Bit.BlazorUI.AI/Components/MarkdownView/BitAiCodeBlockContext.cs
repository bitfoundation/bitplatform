namespace Bit.BlazorUI;

/// <summary>
/// What a custom code block renderer (<see cref="BitAiCodeBlockRenderer"/>) is given.
/// </summary>
/// <param name="Code">The source inside the fence.</param>
/// <param name="Language">The language from the fence info string, as written.</param>
/// <param name="Meta">The rest of the info string after the language, when there was one.</param>
/// <param name="IsStreaming">Whether the block is still receiving content.</param>
/// <remarks>
/// A renderer for a diagram language should wait for <paramref name="IsStreaming"/> to turn false:
/// a half-written diagram is a syntax error, and a syntax error flashing on every token is worse
/// than the source text.
/// </remarks>
public sealed record BitAiCodeBlockContext(string Code, string? Language, string? Meta, bool IsStreaming);
