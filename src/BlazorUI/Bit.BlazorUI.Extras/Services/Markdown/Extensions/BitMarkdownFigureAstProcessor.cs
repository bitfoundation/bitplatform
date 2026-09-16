namespace Bit.BlazorUI;

/// <summary>
/// Turns a paragraph that holds nothing but a titled image into a
/// <see cref="BitMarkdownFigureNode"/>, so the picture and its caption are one element instead of
/// a picture with a tooltip nobody sees.
/// </summary>
/// <remarks>
/// The caption comes from the image's title (<c>![alt](/url "the caption")</c>), never from its
/// alt text: alt describes the image for someone who cannot see it and a caption is read by
/// everyone, so using the alt twice would have a screen reader say the same sentence twice. An
/// image with no title is left as it was.
/// </remarks>
public sealed class BitMarkdownFigureAstProcessor : BitMarkdownAstProcessor
{
    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not BitMarkdownParagraphNode paragraph) continue;
                if (TryConvert(paragraph) is { } figure) list[i] = figure;
            }
        });
    }

    private static BitMarkdownFigureNode? TryConvert(BitMarkdownParagraphNode paragraph)
    {
        BitMarkdownImageNode? image = null;

        foreach (var inline in paragraph.Inlines)
        {
            switch (inline)
            {
                // Only the image, plus whatever whitespace surrounds it on its own line.
                case BitMarkdownImageNode found when image is null:
                    image = found;
                    break;

                case BitMarkdownTextNode text when text.Text.Trim().Length == 0:
                case BitMarkdownLineBreakNode:
                    break;

                default:
                    return null;
            }
        }

        if (image is null || string.IsNullOrWhiteSpace(image.Title)) return null;

        var figure = new BitMarkdownFigureNode { Caption = image.Title! };
        figure.Children.Add(image);
        return figure;
    }
}
