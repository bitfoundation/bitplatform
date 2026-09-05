namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="Writer"/> session - what new text it produces from a prompt. Members left
/// null are not sent, so the model's own defaults apply.
/// </summary>
public class WriterOptions
{
    /// <summary>Tone: <c>"formal"</c>, <c>"neutral"</c> (the default) or <c>"casual"</c>.</summary>
    public string? Tone { get; set; }

    /// <summary>Output format: <c>"markdown"</c> (the default) or <c>"plain-text"</c>.</summary>
    public string? Format { get; set; }

    /// <summary>How long: <c>"short"</c>, <c>"medium"</c> (the default) or <c>"long"</c>.</summary>
    public string? Length { get; set; }

    /// <summary>Context that applies to everything written in this session - the product, the audience, the house style.</summary>
    public string? SharedContext { get; set; }

    /// <summary>The languages the prompt will be in, as BCP 47 tags.</summary>
    public string[]? ExpectedInputLanguages { get; set; }

    /// <summary>The language to write in, as a BCP 47 tag.</summary>
    public string? OutputLanguage { get; set; }
}
