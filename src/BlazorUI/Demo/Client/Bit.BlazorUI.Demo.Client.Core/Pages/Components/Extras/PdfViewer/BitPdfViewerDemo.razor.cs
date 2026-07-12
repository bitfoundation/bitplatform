namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfViewer;

public partial class BitPdfViewerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Source",
            Type = "BitPdfSource?",
            DefaultValue = "null",
            Description = "The document to display.",
            LinkType = LinkType.Link,
            Href = "#pdf-source"
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "780px",
            Description = "The CSS height of the viewer container.",
        },
        new()
        {
            Name = "ShowToolbar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the toolbar is shown.",
        },
        new()
        {
            Name = "InitialZoomMode",
            Type = "BitPdfZoomMode",
            DefaultValue = "BitPdfZoomMode.FitWidth",
            Description = "The initial zoom behavior.",
            LinkType = LinkType.Link,
            Href = "#pdf-zoom-mode-enum"
        },
        new()
        {
            Name = "TextCoalescing",
            Type = "BitPdfTextCoalescing",
            DefaultValue = "BitPdfTextCoalescing.Exact",
            Description = "How painted text is emitted. Compact merges same-line, same-style runs into one span per visual line (far fewer DOM nodes on per-glyph pdfs).",
            LinkType = LinkType.Link,
            Href = "#pdf-text-coalescing-enum"
        },
        new()
        {
            Name = "RenderMode",
            Type = "BitPdfRenderMode",
            DefaultValue = "BitPdfRenderMode.Html",
            Description = "How page content is painted. Canvas replays a display list onto a per-page canvas, while Html (the default) renders prerenderable positioned DOM.",
            LinkType = LinkType.Link,
            Href = "#pdf-render-mode-enum"
        },
        new()
        {
            Name = "OnDocumentLoaded",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when a document has finished loading.",
        },
        new()
        {
            Name = "OnPageChanged",
            Type = "EventCallback<int>",
            DefaultValue = "",
            Description = "The callback for when the focused page changes (with the 1-based page number).",
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "The callback for when loading or rendering fails, with the error message.",
        },
        new()
        {
            Name = "OnWarnings",
            Type = "EventCallback<IReadOnlyList<string>>",
            DefaultValue = "",
            Description = "The callback raised after a document loads with any non-fatal diagnostics (e.g. a damaged file whose cross-reference table had to be rebuilt).",
        },
        new()
        {
            Name = "OnPasswordRequested",
            Type = "Func<Task<string?>>?",
            DefaultValue = "null",
            Description = "Invoked when an encrypted document needs a password. Return the password to retry, or null/empty to cancel. If unset, a password error surfaces through OnError instead.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "PageCount",
            Type = "int",
            Description = "The number of pages of the current document.",
        },
        new()
        {
            Name = "CurrentPage",
            Type = "int",
            Description = "The currently focused page (1-based).",
        },
        new()
        {
            Name = "Zoom",
            Type = "double",
            Description = "The current zoom factor (1 means 100%).",
        },
        new()
        {
            Name = "HasOutline",
            Type = "bool",
            Description = "Whether the document exposes any bookmarks.",
        },
        new()
        {
            Name = "GoToPage",
            Type = "Task GoToPage(int pageNumber)",
            Description = "Navigates to the provided page number (1-based).",
        },
        new()
        {
            Name = "NextPage",
            Type = "Task NextPage()",
            Description = "Navigates to the next page.",
        },
        new()
        {
            Name = "PrevPage",
            Type = "Task PrevPage()",
            Description = "Navigates to the previous page.",
        },
        new()
        {
            Name = "ZoomIn",
            Type = "Task ZoomIn()",
            Description = "Zooms in by 20%.",
        },
        new()
        {
            Name = "ZoomOut",
            Type = "Task ZoomOut()",
            Description = "Zooms out by 20%.",
        },
        new()
        {
            Name = "SetZoomMode",
            Type = "Task SetZoomMode(BitPdfZoomMode mode)",
            Description = "Sets the zoom mode (fit-width, fit-page, actual size or custom).",
        },
        new()
        {
            Name = "RotateClockwise",
            Type = "Task RotateClockwise()",
            Description = "Rotates all pages 90 degrees clockwise.",
        },
        new()
        {
            Name = "Download",
            Type = "Task Download()",
            Description = "Downloads the original document bytes.",
        },
        new()
        {
            Name = "Print",
            Type = "Task Print()",
            Description = "Opens the browser print dialog with all pages of the document.",
        },
        new()
        {
            Name = "ToggleFullscreen",
            Type = "Task ToggleFullscreen()",
            Description = "Toggles the fullscreen mode of the viewer.",
        },
        new()
        {
            Name = "RenderPageHtml",
            Type = "string RenderPageHtml(int pageNumber)",
            Description = "Renders a single page (1-based) to self-contained HTML, or an empty string when no document is loaded or the number is out of range.",
        },
        new()
        {
            Name = "ExtractPageText",
            Type = "string ExtractPageText(int pageNumber)",
            Description = "Extracts the visible text of a single page (1-based) for search or copy, or an empty string when unavailable.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pdf-source",
            Title = "BitPdfSource",
            Description = "Identifies where a pdf document is loaded from. A source is either a byte buffer already in memory (BitPdfSource.FromBytes), or a URL the document can be fetched from (BitPdfSource.FromUrl).",
            Parameters =
            [
                new()
                {
                    Name = "Bytes",
                    Type = "byte[]?",
                    DefaultValue = "null",
                    Description = "Raw document bytes, when the source is an in-memory buffer.",
                },
                new()
                {
                    Name = "Url",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The URL to fetch the document from, when the source is remote.",
                },
                new()
                {
                    Name = "FileName",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "An optional display name (e.g. the original file name).",
                },
                new()
                {
                    Name = "Password",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The password to open an encrypted document, if known up front (also see the WithPassword method).",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "pdf-zoom-mode-enum",
            Name = "BitPdfZoomMode",
            Description = "How the viewer scales pages to the available space.",
            Items =
            [
                new()
                {
                    Name = "Custom",
                    Value = "0",
                    Description = "An explicit zoom factor is applied (the user picked a percentage).",
                },
                new()
                {
                    Name = "FitWidth",
                    Value = "1",
                    Description = "Each page is scaled so its width fills the viewport.",
                },
                new()
                {
                    Name = "FitPage",
                    Value = "2",
                    Description = "Each page is scaled so the whole page fits in the viewport.",
                },
                new()
                {
                    Name = "ActualSize",
                    Value = "3",
                    Description = "Pages are shown at their natural size (one CSS pixel per point).",
                },
            ]
        },
        new()
        {
            Id = "pdf-render-mode-enum",
            Name = "BitPdfRenderMode",
            Description = "How page content is rendered.",
            Items =
            [
                new()
                {
                    Name = "Html",
                    Value = "0",
                    Description = "Pages render to positioned HTML/CSS DOM (the default). Fully prerenderable and crisp at any zoom.",
                },
                new()
                {
                    Name = "Canvas",
                    Value = "1",
                    Description = "Page content is painted onto a per-page canvas by replaying a display list produced by the C# engine. Far fewer DOM nodes; selection, search and links still work through the DOM text layer, and zoom changes re-rasterize the canvases so text stays crisp. Requires JavaScript, so no prerender.",
                },
            ]
        },
        new()
        {
            Id = "pdf-text-coalescing-enum",
            Name = "BitPdfTextCoalescing",
            Description = "How painted text runs are emitted into the page HTML.",
            Items =
            [
                new()
                {
                    Name = "Exact",
                    Value = "0",
                    Description = "Every show-text run keeps its own positioned span, so each glyph run lands at its exact pdf-computed position. Highest fidelity, but per-glyph pdfs emit one span per character.",
                },
                new()
                {
                    Name = "Compact",
                    Value = "1",
                    Description = "Adjacent runs on the same baseline with identical style are merged into one span per visual line. Dramatically fewer DOM nodes on per-glyph pdfs, at the cost of small intra-line position drift. Rotated text is never coalesced and stays exact.",
                },
            ]
        },
    ];



    private readonly BitPdfSource basicSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf", "article.pdf");
    private readonly BitPdfSource plainSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/hello-world.pdf", "hello-world.pdf");
    private readonly BitPdfSource canvasSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf", "article.pdf");
    private readonly BitPdfSource eventsSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf", "article.pdf");
    private readonly BitPdfSource publicApiSource = BitPdfSource.FromUrl("/_content/Bit.BlazorUI.Demo.Client.Core/samples/article.pdf", "article.pdf");

    private readonly List<string> eventsLog = [];

    private BitPdfViewer pdfViewerRef = default!;



    private readonly string example1RazorCode = @"
<BitPdfViewer Source=""basicSource"" />";
    private readonly string example1CsharpCode = @"
private readonly BitPdfSource basicSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");

// or from in-memory bytes:
// private readonly BitPdfSource basicSource = BitPdfSource.FromBytes(pdfBytes, ""file-name.pdf"");";

    private readonly string example2RazorCode = @"
<BitPdfViewer Source=""plainSource"" ShowToolbar=""false"" Height=""420px"" />";
    private readonly string example2CsharpCode = @"
private readonly BitPdfSource plainSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");";

    private readonly string example3RazorCode = @"
<BitPdfViewer Source=""canvasSource"" RenderMode=""BitPdfRenderMode.Canvas"" />";
    private readonly string example3CsharpCode = @"
private readonly BitPdfSource canvasSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");";

    private readonly string example4RazorCode = @"
<BitPdfViewer Source=""eventsSource""
              OnDocumentLoaded='() => eventsLog.Add(""Document loaded"")'
              OnPageChanged='p => eventsLog.Add($""Page changed: {p}"")'
              OnError='e => eventsLog.Add($""Error: {e}"")' />

<div>Events:</div>
<div style=""max-height:8rem;overflow:auto"">
    @foreach (var log in Enumerable.Reverse(eventsLog).Take(5))
    {
        <div>@log</div>
    }
</div>";
    private readonly string example4CsharpCode = @"
private readonly BitPdfSource eventsSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");

private readonly List<string> eventsLog = [];";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => pdfViewerRef.GoToPage(1)"">First</BitButton>
<BitButton OnClick=""() => pdfViewerRef.PrevPage()"">Prev</BitButton>
<BitTag Variant=""BitVariant.Outline"" Text=""@($""{pdfViewerRef?.CurrentPage}/{pdfViewerRef?.PageCount}"")"" Color=""BitColor.Info"" />
<BitButton OnClick=""() => pdfViewerRef.NextPage()"">Next</BitButton>
<BitButton OnClick=""() => pdfViewerRef.GoToPage(pdfViewerRef.PageCount)"">Last</BitButton>
<BitButton OnClick=""() => pdfViewerRef.ZoomOut()"">Zoom -</BitButton>
<BitButton OnClick=""() => pdfViewerRef.ZoomIn()"">Zoom +</BitButton>
<BitButton OnClick=""() => pdfViewerRef.RotateClockwise()"">Rotate</BitButton>
<BitButton OnClick=""() => pdfViewerRef.Print()"">Print</BitButton>

<BitPdfViewer @ref=""pdfViewerRef""
              Source=""publicApiSource""
              ShowToolbar=""false""
              OnDocumentLoaded=""StateHasChanged""
              OnPageChanged=""_ => StateHasChanged()"" />";
    private readonly string example5CsharpCode = @"
private readonly BitPdfSource publicApiSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");

private BitPdfViewer pdfViewerRef = default!;";
}
