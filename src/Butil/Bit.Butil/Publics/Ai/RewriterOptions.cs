namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="Rewriter"/> session - how it transforms text it is given. Members left null
/// are not sent, so the model's own defaults apply.
/// </summary>
/// <remarks>
/// The tone and length values are <b>relative</b> here, unlike <see cref="WriterOptions"/>: they say
/// which way to move the text, not what to aim for.
/// </remarks>
public class RewriterOptions
{
    /// <summary>Which way to move the tone: <c>"more-formal"</c>, <c>"as-is"</c> (the default) or <c>"more-casual"</c>.</summary>
    public string? Tone { get; set; }

    /// <summary>Output format: <c>"as-is"</c> (the default), <c>"markdown"</c> or <c>"plain-text"</c>.</summary>
    public string? Format { get; set; }

    /// <summary>Which way to move the length: <c>"shorter"</c>, <c>"as-is"</c> (the default) or <c>"longer"</c>.</summary>
    public string? Length { get; set; }

    /// <summary>Context that applies to every rewrite in this session.</summary>
    public string? SharedContext { get; set; }

    /// <summary>The languages the input will be in, as BCP 47 tags.</summary>
    public string[]? ExpectedInputLanguages { get; set; }

    /// <summary>The language to write in, as a BCP 47 tag.</summary>
    public string? OutputLanguage { get; set; }
}
