namespace Bit.BlazorUI;

/// <summary>
/// Renders a mathematical expression of a <see cref="BitAiMarkdownView"/>, in place of the built-in monospace fallback.
/// </summary>
/// <param name="context">The expression and whether it is displayed or inline.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiMathRenderer(BitAiMathContext context);
