namespace Bit.BlazorUI;

/// <summary>
/// The <see cref="IBitAiMessagePart.Kind"/> discriminators the framework ships with.
/// </summary>
/// <remarks>
/// Use these constants rather than string literals when registering renderers, so a typo becomes a
/// compile error instead of a part that silently falls back to the default renderer.
/// </remarks>
public static class BitAiMessagePartKinds
{
    /// <summary>Plain or Markdown text, the ordinary body of a response.</summary>
    public const string Text = "text";

    /// <summary>Text explicitly marked as Markdown.</summary>
    public const string Markdown = "markdown";

    /// <summary>Model reasoning, rendered in a collapsed panel.</summary>
    public const string Reasoning = "reasoning";

    /// <summary>A standalone code block with a language.</summary>
    public const string Code = "code";

    /// <summary>An image.</summary>
    public const string Image = "image";

    /// <summary>A file.</summary>
    public const string File = "file";

    /// <summary>A single citation rendered inline.</summary>
    public const string Citation = "citation";

    /// <summary>The source list for the message, rendered where the part sits.</summary>
    public const string Sources = "sources";

    /// <summary>A tool call, including its arguments, status, and result.</summary>
    public const string ToolCall = "tool-call";

    /// <summary>A tool result carried on its own, for providers that emit tool messages.</summary>
    public const string ToolResult = "tool-result";

    /// <summary>A reference to an artifact held by the conversation.</summary>
    public const string Artifact = "artifact";

    /// <summary>A structured JSON value, possibly still streaming.</summary>
    public const string StructuredData = "structured-data";

    /// <summary>A component chosen by the model and bound to model-supplied properties.</summary>
    public const string GenerativeUi = "generative-ui";

    /// <summary>A form the user fills in and submits back.</summary>
    public const string Form = "form";

    /// <summary>An error surfaced inside the message body.</summary>
    public const string Error = "error";
}
