namespace Bit.BlazorUI;

/// <summary>
/// Renders a fenced code block of a registered language in a <see cref="BitAiMarkdownView"/>, in place of the built-in
/// <see cref="BitAiCodeBlockView"/>.
/// </summary>
/// <param name="context">The code and its info string.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiCodeBlockRenderer(BitAiCodeBlockContext context);
