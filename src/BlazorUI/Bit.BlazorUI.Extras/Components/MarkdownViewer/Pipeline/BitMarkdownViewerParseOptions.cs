namespace Bit.BlazorUI;

/// <summary>
/// Per-parse limits that keep parsing of untrusted Markdown bounded and safe from
/// resource-exhaustion (DoS) attacks. These are threaded through the recursive block
/// and inline parsers so a single hostile document cannot exhaust the stack or CPU.
/// </summary>
internal sealed class BitMarkdownViewerParseOptions
{
    /// <summary>
    /// Maximum block/inline nesting depth. Beyond this, nested content is emitted as
    /// plain text instead of recursing further. This prevents a (process-killing,
    /// uncatchable) <see cref="System.StackOverflowException"/> from pathological input
    /// such as thousands of nested blockquotes or lists.
    /// </summary>
    public int MaxDepth { get; init; } = DefaultMaxDepth;

    /// <summary>The default nesting-depth ceiling; far deeper than any legitimate document.</summary>
    public const int DefaultMaxDepth = 100;

    /// <summary>Shared instance carrying the default limits.</summary>
    public static readonly BitMarkdownViewerParseOptions Default = new();
}
