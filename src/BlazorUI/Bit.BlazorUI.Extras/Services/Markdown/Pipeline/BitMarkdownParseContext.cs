namespace Bit.BlazorUI;

/// <summary>
/// The per-parse state shared by every block and inline parser taking part in a single
/// <see cref="BitMarkdownPipeline.Parse(string?)"/> call. It carries the safety limits
/// in effect plus the set of reference labels discovered by the pre-scan, so an inline
/// parser can tell a real reference (<c>[text][ref]</c>, <c>[^1]</c>) from ordinary
/// bracketed prose before the definitions themselves have been parsed.
/// </summary>
internal sealed class BitMarkdownParseContext
{
    public BitMarkdownParseContext(BitMarkdownParseOptions options, IReadOnlySet<string> referenceLabels)
    {
        Options = options;
        ReferenceLabels = referenceLabels;
    }

    /// <summary>The safety limits in effect for this parse.</summary>
    public BitMarkdownParseOptions Options { get; }

    /// <summary>
    /// The normalized labels of every <c>[label]:</c> definition line found anywhere in the
    /// source. Definitions may appear after the references that use them, so this cheap
    /// pre-scan is what lets the inline parsers decide - while scanning - whether a pair of
    /// brackets can possibly be a reference at all. When a document declares no definitions
    /// the set is empty and bracketed text is scanned exactly as it was before references
    /// were supported.
    /// </summary>
    public IReadOnlySet<string> ReferenceLabels { get; }

    /// <summary>An empty context carrying the default limits, used by the parameterless parse overloads.</summary>
    public static BitMarkdownParseContext Empty { get; } =
        new(BitMarkdownParseOptions.Default, new HashSet<string>(StringComparer.Ordinal));
}
