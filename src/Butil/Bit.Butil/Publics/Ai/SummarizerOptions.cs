namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="Summarizer"/> session. Members left null are not sent, so the model's own
/// defaults apply.
/// </summary>
public class SummarizerOptions
{
    /// <summary>
    /// What kind of summary: <c>"key-points"</c> (the default), <c>"tldr"</c>, <c>"teaser"</c> or
    /// <c>"headline"</c>.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>Output format: <c>"markdown"</c> (the default) or <c>"plain-text"</c>.</summary>
    public string? Format { get; set; }

    /// <summary>How long: <c>"short"</c>, <c>"medium"</c> (the default) or <c>"long"</c>.</summary>
    public string? Length { get; set; }

    /// <summary>
    /// Context that applies to everything summarized in this session - what the documents are and
    /// who the summary is for.
    /// </summary>
    public string? SharedContext { get; set; }

    /// <summary>The languages the input will be in, as BCP 47 tags.</summary>
    public string[]? ExpectedInputLanguages { get; set; }

    /// <summary>The language the summary should be in, as a BCP 47 tag.</summary>
    public string? OutputLanguage { get; set; }
}
