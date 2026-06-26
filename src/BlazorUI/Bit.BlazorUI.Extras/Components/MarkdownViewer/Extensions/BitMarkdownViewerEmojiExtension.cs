namespace Bit.BlazorUI;

/// <summary>Enables <c>:shortcode:</c> emoji replacement.</summary>
public sealed class BitMarkdownViewerEmojiExtension : IBitMarkdownViewerExtension
{
    private readonly IReadOnlyDictionary<string, string>? _overrides;

    /// <summary>Uses the built-in emoji map.</summary>
    public BitMarkdownViewerEmojiExtension() { }

    /// <summary>Uses the built-in emoji map plus the supplied per-pipeline overrides.</summary>
    public BitMarkdownViewerEmojiExtension(IReadOnlyDictionary<string, string> overrides) => _overrides = overrides;

    public void Setup(BitMarkdownViewerPipelineBuilder builder)
        => builder.AstProcessors.Add(_overrides is null
            ? new BitMarkdownViewerEmojiAstProcessor()
            : new BitMarkdownViewerEmojiAstProcessor(_overrides));
}
