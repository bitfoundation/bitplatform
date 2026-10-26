using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiMarkdownView renders a parsed Markdown document: the prose of an assistant message, with its code blocks,
/// tables, task lists, footnotes, mathematics and inline citation markers.
/// </summary>
/// <remarks>
/// It takes an already-parsed document rather than a string, so the caller decides how parsing happens:
/// <see cref="BitAiStreamingMarkdown"/> reuses an incremental parser across deltas, while a static caller parses once.
/// <br />
/// Model output is untrusted, so raw HTML is escaped unless <see cref="BitAiMarkdownOptions.AllowRawHtml"/> opts in,
/// link and image URLs of a scheme that can run script are neutralised, and images follow the
/// <see cref="BitAiUiOptions.Images"/> policy.
/// <br />
/// Three things defer to the <see cref="BitAiRendererRegistry"/>: fenced code in a language the host registered
/// (a diagram, a chart), mathematics (<see cref="BitAiRendererRegistry.MathRenderer"/>) and the chip of an inline
/// citation (<see cref="BitAiRendererRegistry.CitationRenderer"/>). Each registered renderer runs inside a
/// <see cref="BitAiRendererBoundary"/>, so one that throws on what the model wrote is replaced by a notice and the rest
/// of the document still renders. Everything else is rendered here, every element under a class of its own, so the
/// prose styles never reach into what a registered renderer draws.
/// </remarks>
public partial class BitAiMarkdownView : BitAiComponentBase
{
    // What the notice of a registered renderer that failed names it after (a code block's renderer is named after its
    // language): the BitAiRendererRegistry members the renderers are registered through.
    private const string MathRendererName = "math";
    private const string CitationRendererName = BitAiMessagePartKinds.Citation;

    // The images the user asked to load under a policy that asks first.
    private readonly HashSet<string> _loadedImages = new(StringComparer.Ordinal);

    // The source the user just loaded, whose image takes the focus once it has replaced the button that loaded it (see
    // LoadImage), and that image - the first of the source, since a view can show one several times.
    private string? _focusImageSource;
    private ElementReference _imageToFocus;

    // Where the streaming caret is drawn in the render under way (see CaretHost): a paragraph, a heading or a list item
    // of the document, the document itself for after its blocks, or null for nowhere.
    private BitAiMarkdownNode? _caretHost;

    // The task-list items drawn so far in the render under way, which numbers the ids their checkboxes are named by.
    private int _taskItemCount;

    private BitAiIconClassesCache<BitActionButtonClassStyles> _imagePlaceholderClasses;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the Markdown view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple Markdown view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiMarkdownViewParams.ParamName)]
    public BitAiMarkdownViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// The citations that the inline citation markers of the document resolve against.
    /// </summary>
    /// <remarks>
    /// A marker whose number matches a citation is drawn as a <see cref="BitAiCitationMarker"/>, or by the
    /// <see cref="BitAiRendererRegistry.CitationRenderer"/> when the app registered one (unless
    /// <see cref="BitAiUiOptions.InlineCitations"/> is off); one that matches none stays the literal text it was.
    /// </remarks>
    [Parameter] public IReadOnlyList<BitAiCitation>? Citations { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the Markdown view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiMarkdownViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The document to render. Nothing is rendered while it is null.
    /// </summary>
    [Parameter] public BitAiMarkdownDocument? Document { get; set; }

    /// <summary>
    /// The callback for when the user activates an inline citation marker.
    /// </summary>
    /// <remarks>
    /// A chip drawn by the <see cref="BitAiRendererRegistry.CitationRenderer"/> is the app's own: the renderer is handed
    /// this callback (<see cref="BitAiCitationContext.OnActivate"/>), which its chip raises through
    /// <see cref="BitAiCitationContext.ActivateAsync"/>.
    /// </remarks>
    [Parameter] public EventCallback<BitAiCitation> OnCitationClick { get; set; }

    /// <summary>
    /// Draws a caret at the end of the text while the document is still being written (parsed with more source
    /// expected), the sign that more text is on its way.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    /// <remarks>
    /// The caret follows the paragraph or the heading the source ends inside, or sits in a list item just started; after
    /// anything else - a code block, a table, a blank line - it is drawn after the blocks, where the next one will start.
    /// A document with no block yet draws none: that a reply has nothing to show yet is its host's to say (a chat message
    /// shows a <see cref="BitAiThinkingIndicator"/>).
    /// <see cref="BitAiUiOptions.ShowStreamingCaret"/> turns the caret off for every view at once.
    /// </remarks>
    [Parameter] public bool ShowCaret { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the Markdown view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiMarkdownViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aimdv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiMarkdownViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // An image the user loaded takes the focus once it has replaced the button that loaded it (see LoadImage).
        if (_focusImageSource is not null)
        {
            _focusImageSource = null;

            await FocusImageAsync();
        }
    }



    // A footnote and its references are named after the view, so several messages of one transcript that each have a
    // footnote 1 keep their links apart.
    private string FootnoteId(int number) => $"{_Id}-fn-{number}";

    private string FootnoteReferenceId(int number, int ordinal) => $"{_Id}-fnref-{number}-{ordinal}";

    // So is a heading: the parser numbers the ids of one document apart (bit-ai-h-<slug>, -2, -3, ...), and the view's
    // own id keeps the same heading in two messages from giving the page two elements of one id.
    private string HeadingId(string id) => $"{_Id}-{id}";

    // A footnote reference and a back-link are links into this view, written as "#<id>" so the address they name is the
    // element's. Followed by the browser, or by Blazor's router, which takes every link inside the base address for its
    // own, "#<id>" is resolved against the <base href> rather than the page and leads to another one (the home page of
    // an app whose base is "/"). So the click is the view's (preventDefault), and the script takes the reader to the
    // element on this page: in view, focused, and named in the address bar. A key press on a link is a click too.
    private async Task FollowAsync(string targetId) => await _js.BitAiMarkdownViewFollow(targetId);

    private static string Join(string ownClass, string? classes) => classes.HasValue() ? $"{ownClass} {classes}" : ownClass;

    /// <summary>
    /// Where the streaming caret of an open document is drawn: at the end of the text the source ends inside, so it
    /// follows the words as they are written.
    /// </summary>
    /// <remarks>
    /// The last block is followed down through the lists (their last item) and the quotes holding it, as long as each is
    /// still open: an open paragraph or heading there takes the caret after its text, and a list item just started, with
    /// nothing in it yet, takes it in its place. Anything else - a code block or a table being written, a blank line, a
    /// block the text has moved on from - leaves it to the document, which draws it after its blocks, where the next one
    /// will start.
    /// </remarks>
    private static BitAiMarkdownNode CaretHost(BitAiMarkdownDocument document)
    {
        var blocks = document.Blocks;

        while (blocks.Count > 0)
        {
            switch (blocks[^1])
            {
                case BitAiParagraphBlock { IsOpen: true } paragraph:
                    return paragraph;

                case BitAiHeadingBlock { IsOpen: true } heading:
                    return heading;

                case BitAiQuoteBlock { IsOpen: true } quote:
                    blocks = quote.Blocks;
                    break;

                case BitAiListBlock { IsOpen: true, Items.Count: > 0 } list:
                    var item = list.Items[^1];
                    if (item.Blocks.Count == 0) return item;
                    blocks = item.Blocks;
                    break;

                default:
                    return document;
            }
        }

        return document;
    }

    // The button that loaded the image is gone once the image is shown, and the focus it held with it - to the body of the
    // document, from where the keyboard and a screen reader start over at the top of the page. So the image takes it,
    // programmatically focusable for that (tabindex="-1") and named by its alternative text.
    private void LoadImage(string source)
    {
        _loadedImages.Add(source);
        _focusImageSource = source;
        _imageToFocus = default;
    }

    private void CaptureLoadedImage(string source, ElementReference image)
    {
        if (source == _focusImageSource && string.IsNullOrEmpty(_imageToFocus.Id))
        {
            _imageToFocus = image;
        }
    }

    private async Task FocusImageAsync()
    {
        // No image was rendered for the source loaded - the document was replaced in the meantime.
        if (string.IsNullOrEmpty(_imageToFocus.Id) || IsDisposing) return;

        try
        {
            await _imageToFocus.FocusAsync();
        }
        catch (JSDisconnectedException) { } // the circuit is gone, and the page with it
        catch (OperationCanceledException) { }
        catch (JSException) { } // an image taken off the page in the meantime has nothing to focus
    }

    // The classes handed to the image placeholder: the app's own for that part with the glyph's added - it ships with
    // this package's own icon subset rather than with the icon font of the core library (see BitAiIconClasses) - built
    // again only when the app's change, so the button is not handed a new instance on every render.
    private BitActionButtonClassStyles ImagePlaceholderClasses =>
        _imagePlaceholderClasses.Get("bit-icon-ai bit-icon-ai--Photo2", Classes?.ImagePlaceholder, BitAiIconClasses.With);

    private static string? AlignmentClass(BitAiTableBlock table, int column) =>
        column < table.Alignments.Count
            ? table.Alignments[column] switch
            {
                BitAiColumnAlignment.Center => "bit-aimdv-alc",
                BitAiColumnAlignment.Right => "bit-aimdv-alr",
                _ => null,
            }
            : null;
}
