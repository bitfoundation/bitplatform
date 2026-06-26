namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a native, SEO friendly Blazor component that renders Markdown
/// to HTML entirely in C#. There is no JavaScript interop and no third-party packages.
/// </summary>
/// <remarks>
/// <para>
/// By default the component understands only the basic CommonMark core. Richer flavors
/// (GitHub tables, strikethrough, task lists, autolinks, emoji, ...) are opt-in: supply
/// a <see cref="Pipeline"/> built with the desired extensions (for example
/// <see cref="BitMarkdownViewerPipelines.GitHub"/>).
/// </para>
/// <para>
/// Parsing produces an AST which is walked with a <see cref="Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder"/>,
/// so the output is real DOM rather than an <c>innerHTML</c> blob. Raw HTML in the source
/// is treated as text and link / image URLs are sanitized, keeping the output safe from
/// script injection by default.
/// </para>
/// </remarks>
public partial class BitMarkdownViewer : BitComponentBase
{
    private BitMarkdownViewerDocumentNode _document = new();
    private string? _parsedSource;
    private BitMarkdownViewerPipeline? _parsedWith;



    /// <summary>
    /// The Markdown string value to render as html elements.
    /// </summary>
    [Parameter] public string? Markdown { get; set; }

    /// <summary>
    /// The processing pipeline (flavor set). Defaults to <see cref="BitMarkdownViewerPipeline.Basic"/>,
    /// i.e. the basic CommonMark core with no extensions.
    /// </summary>
    [Parameter] public BitMarkdownViewerPipeline? Pipeline { get; set; }



    protected override string RootElementClass => "bit-mdv";

    private BitMarkdownViewerPipeline EffectivePipeline => Pipeline ?? BitMarkdownViewerPipeline.Basic;

    protected override void OnParametersSet()
    {
        var pipeline = EffectivePipeline;

        // Re-parse only when the source or the pipeline reference changes.
        if (_parsedSource != Markdown || ReferenceEquals(_parsedWith, pipeline) is false)
        {
            _document = pipeline.Parse(Markdown);
            _parsedSource = Markdown;
            _parsedWith = pipeline;
        }

        base.OnParametersSet();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var renderer = EffectivePipeline.CreateRenderer();

        builder.OpenElement(0, "div");

        builder.AddMultipleAttributes(1, HtmlAttributes);
        builder.AddAttribute(2, "id", _Id);
        builder.AddAttribute(3, "style", StyleBuilder.Value);
        builder.AddAttribute(4, "class", ClassBuilder.Value);
        if (Dir is not null)
        {
            builder.AddAttribute(5, "dir", Dir.Value.ToString().ToLower());
        }
        builder.AddElementReferenceCapture(6, v => RootElement = v);

        renderer.WriteNodes(builder, _document.Children);

        builder.CloseElement();
    }
}
