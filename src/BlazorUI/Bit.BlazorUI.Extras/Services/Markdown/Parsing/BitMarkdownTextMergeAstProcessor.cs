namespace Bit.BlazorUI;

/// <summary>
/// Merges every run of adjacent text nodes into one.
/// </summary>
/// <remarks>
/// <para>
/// The inline scanner emits a separate token for each delimiter run it collects, and a run
/// that never pairs up is turned back into text - so a single piece of prose can reach the
/// AST as several text nodes. That is invisible in the rendered HTML, but it is not
/// invisible to the AST processors that read text: an autolink literal like
/// <c>https://example.com/a_(b)</c> arrives split around the <c>_</c> and only its first
/// fragment gets linked, and a task marker split around its brackets stops looking like one.
/// </para>
/// <para>
/// Running this first gives every later processor the text the author actually wrote.
/// </para>
/// </remarks>
public sealed class BitMarkdownTextMergeAstProcessor : BitMarkdownAstProcessor
{
    // After link references are resolved (0), before any flavor.
    public override int Order => 1;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                if (list[i] is BitMarkdownTextNode current && list[i - 1] is BitMarkdownTextNode previous)
                {
                    previous.Text += current.Text;
                    list.RemoveAt(i);
                }
            }
        });
    }
}
