namespace Bit.BlazorUI;

/// <summary>
/// Enables GitHub alerts: <c>&gt; [!NOTE]</c>, <c>&gt; [!TIP]</c>, <c>&gt; [!IMPORTANT]</c>,
/// <c>&gt; [!WARNING]</c> and <c>&gt; [!CAUTION]</c> block quotes render as titled callouts.
/// </summary>
public sealed class BitMarkdownAlertExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new BitMarkdownAlertAstProcessor());
        builder.Renderers.Add(new BitMarkdownAlertRenderer());
    }
}
