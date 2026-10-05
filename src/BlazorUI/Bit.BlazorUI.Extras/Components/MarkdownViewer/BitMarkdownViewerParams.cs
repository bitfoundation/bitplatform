namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitMarkdownViewer"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the documents of an application agree on: the flavors they are parsed with, how
/// untrusted input is bounded, the templates that draw their code blocks, images and links (a highlighter set once
/// for the whole site, above all) and how they are styled. The source, the inline layout and the events are left out
/// on purpose: they are what makes one document the one it is.
/// </remarks>
public class BitMarkdownViewerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitMarkdownViewer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitMarkdownViewer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitMarkdownViewer)}";



    public string Name => ParamName;



    /// <summary>
    /// Renders every fenced or indented code block, instead of the default pre/code.
    /// </summary>
    public RenderFragment<BitMarkdownCodeBlockNode>? CodeBlockTemplate { get; set; }

    /// <summary>
    /// Shifts every heading down by this many levels (a # heading renders as h(1 + offset)), clamped to h1-h6.
    /// </summary>
    public int? HeadingLevelOffset { get; set; }

    /// <summary>
    /// Controls whether remote images are allowed to load.
    /// </summary>
    public BitMarkdownViewerImageRendering? ImageRendering { get; set; }

    /// <summary>
    /// Renders every image, instead of the default img.
    /// </summary>
    public RenderFragment<BitMarkdownImageNode>? ImageTemplate { get; set; }

    /// <summary>
    /// Renders every link, instead of the default anchor.
    /// </summary>
    public RenderFragment<BitMarkdownLinkNode>? LinkTemplate { get; set; }

    /// <summary>
    /// When greater than zero, the Markdown source is truncated to this many characters before parsing.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// The maximum block/inline nesting depth allowed while parsing.
    /// </summary>
    public int? MaxNestingDepth { get; set; }

    /// <summary>
    /// The processing pipeline (flavor set).
    /// </summary>
    public BitMarkdownPipeline? Pipeline { get; set; }

    /// <summary>
    /// Strips the Unicode bidirectional control characters from the source before parsing.
    /// </summary>
    public bool? StripBidiControlCharacters { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitMarkdownViewer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitMarkdownViewer"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitMarkdownViewer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitMarkdownViewer"/>.
    /// </remarks>
    /// <param name="bitMarkdownViewer">
    /// The <see cref="BitMarkdownViewer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitMarkdownViewer bitMarkdownViewer)
    {
        if (bitMarkdownViewer is null) return;

        UpdateBaseParameters(bitMarkdownViewer);

        if (CodeBlockTemplate is not null && bitMarkdownViewer.HasNotBeenSet(nameof(CodeBlockTemplate)))
        {
            bitMarkdownViewer.CodeBlockTemplate = CodeBlockTemplate;
        }

        if (HeadingLevelOffset.HasValue && bitMarkdownViewer.HasNotBeenSet(nameof(HeadingLevelOffset)))
        {
            bitMarkdownViewer.HeadingLevelOffset = HeadingLevelOffset.Value;
        }

        if (ImageRendering.HasValue && bitMarkdownViewer.HasNotBeenSet(nameof(ImageRendering)))
        {
            bitMarkdownViewer.ImageRendering = ImageRendering.Value;
        }

        if (ImageTemplate is not null && bitMarkdownViewer.HasNotBeenSet(nameof(ImageTemplate)))
        {
            bitMarkdownViewer.ImageTemplate = ImageTemplate;
        }

        if (LinkTemplate is not null && bitMarkdownViewer.HasNotBeenSet(nameof(LinkTemplate)))
        {
            bitMarkdownViewer.LinkTemplate = LinkTemplate;
        }

        if (MaxLength.HasValue && bitMarkdownViewer.HasNotBeenSet(nameof(MaxLength)))
        {
            bitMarkdownViewer.MaxLength = MaxLength.Value;
        }

        if (MaxNestingDepth.HasValue && bitMarkdownViewer.HasNotBeenSet(nameof(MaxNestingDepth)))
        {
            bitMarkdownViewer.MaxNestingDepth = MaxNestingDepth.Value;
        }

        if (Pipeline is not null && bitMarkdownViewer.HasNotBeenSet(nameof(Pipeline)))
        {
            bitMarkdownViewer.Pipeline = Pipeline;
        }

        if (StripBidiControlCharacters.HasValue && bitMarkdownViewer.HasNotBeenSet(nameof(StripBidiControlCharacters)))
        {
            bitMarkdownViewer.StripBidiControlCharacters = StripBidiControlCharacters.Value;
        }
    }
}
