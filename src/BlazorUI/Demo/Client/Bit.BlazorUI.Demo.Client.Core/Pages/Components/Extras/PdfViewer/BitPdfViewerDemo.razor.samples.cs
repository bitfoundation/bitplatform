namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfViewer;

public partial class BitPdfViewerDemo
{
    private readonly string example1RazorCode = @"
<InputFile OnChange=""OnBasicFileChange"" accept="".pdf,application/pdf"" />

<BitPdfViewer Source=""basicSource"" AllowDropFile />";
    private readonly string example1CsharpCode = @"
private BitPdfSource basicSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");

// or from what you already have in memory:
// BitPdfSource.FromBytes(pdfBytes, ""file-name.pdf"");
// BitPdfSource.FromBase64(base64OrDataUri, ""file-name.pdf"");
// await BitPdfSource.FromStreamAsync(stream, ""file-name.pdf"");
// A url source can carry request headers and a known password too:
// BitPdfSource.FromUrl(url).WithHeaders(headers).WithPassword(""secret"");

private async Task OnBasicFileChange(InputFileChangeEventArgs e)
{
    if (e.FileCount == 0) return;

    const long maxSize = 512 * 1024 * 1024;
    if (e.File.Size <= 0 || e.File.Size > maxSize) return;

    using var stream = e.File.OpenReadStream(maxAllowedSize: maxSize);
    var bytes = new byte[(int)e.File.Size];
    await stream.ReadExactlyAsync(bytes);

    basicSource = BitPdfSource.FromBytes(bytes, e.File.Name);
}";

    private readonly string example2RazorCode = @"
<BitButton IsEnabled=""heightSource is null""
           OnClick='() => heightSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""heightSource"" Height=""400px"" Width=""min(100%, 40rem)"" />";
    private readonly string example2CsharpCode = @"
private BitPdfSource? heightSource;";

    private readonly string example3RazorCode = @"
<BitButton IsEnabled=""toolbarSource is null""
           OnClick='() => toolbarSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""toolbarSource"" Height=""400px""
              ToolbarItems=""BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom | BitPdfToolbarItems.OpenFile | BitPdfToolbarItems.Fullscreen"">
    <ToolbarStartTemplate>
        <BitTag Text=""Draft"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"" />
    </ToolbarStartTemplate>
    <ToolbarEndTemplate>
        @* The context is the viewer itself. *@
        <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"" IconName=""@BitIconName.Share""
                   OnClick=""() => Share(context)"">
            Share
        </BitButton>
    </ToolbarEndTemplate>
</BitPdfViewer>

@if (toolbarMessage is not null)
{
    <div role=""status"">@toolbarMessage</div>
}

@* ShowToolbar=""false"" hides the bar; OnFileOpened takes the file the OpenFile picker read:
<BitPdfViewer Source=""toolbarSource"" MaxOpenFileSize=""20 * 1024 * 1024"" OnFileOpened=""s => toolbarSource = s"" /> *@";
    private readonly string example3CsharpCode = @"
private BitPdfSource? toolbarSource;
private string? toolbarMessage;

private void Share(BitPdfViewer viewer)
{
    toolbarMessage = viewer.PageCount == 0 ? null : $""Share requested for page {viewer.CurrentPage} of {viewer.PageCount}"";
}";

    private readonly string example4RazorCode = @"
<BitButton IsEnabled=""sidebarSource is null""
           OnClick='() => sidebarSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""sidebarSource"" Height=""450px"" DefaultSidebar=""BitPdfSidebar.Thumbnails"" />";
    private readonly string example4CsharpCode = @"
private BitPdfSource? sidebarSource;";

    private readonly string example5RazorCode = @"
<BitButton IsEnabled=""zoomSource is null""
           OnClick='() => zoomSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""zoomSource"" Height=""450px""
              InitialZoomMode=""BitPdfZoomMode.Automatic""
              ZoomPresets=""zoomPresets""
              MinZoom=""0.5"" MaxZoom=""3"" ZoomStep=""1.5"" />";
    private readonly string example5CsharpCode = @"
private BitPdfSource? zoomSource;

// The percentages the zoom dropdown offers, replacing the built-in ones.
private readonly double[] zoomPresets = [0.5, 1, 1.5, 2];";

    private readonly string example6RazorCode = @"
<BitDropdown @bind-Value=""scrollMode"" Label=""ScrollMode""
             TItem=""BitDropdownOption<BitPdfScrollMode>"" TValue=""BitPdfScrollMode"">
    <BitDropdownOption Text=""Vertical"" Value=""BitPdfScrollMode.Vertical"" />
    <BitDropdownOption Text=""Horizontal"" Value=""BitPdfScrollMode.Horizontal"" />
    <BitDropdownOption Text=""Wrapped"" Value=""BitPdfScrollMode.Wrapped"" />
    <BitDropdownOption Text=""Page"" Value=""BitPdfScrollMode.Page"" />
</BitDropdown>
<BitDropdown @bind-Value=""spreadMode"" Label=""SpreadMode""
             TItem=""BitDropdownOption<BitPdfSpreadMode>"" TValue=""BitPdfSpreadMode"">
    <BitDropdownOption Text=""None"" Value=""BitPdfSpreadMode.None"" />
    <BitDropdownOption Text=""Odd"" Value=""BitPdfSpreadMode.Odd"" />
    <BitDropdownOption Text=""Even"" Value=""BitPdfSpreadMode.Even"" />
</BitDropdown>
<BitToggle @bind-Value=""panTool"" Label=""Pan tool"" />

<BitButton IsEnabled=""layoutSource is null""
           OnClick='() => layoutSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""layoutSource"" Height=""500px""
              ScrollMode=""scrollMode""
              SpreadMode=""spreadMode""
              CursorTool=""@(panTool ? BitPdfCursorTool.Pan : BitPdfCursorTool.Select)"" />";
    private readonly string example6CsharpCode = @"
private BitPdfSource? layoutSource;

private bool panTool;
private BitPdfScrollMode scrollMode = BitPdfScrollMode.Vertical;
private BitPdfSpreadMode spreadMode = BitPdfSpreadMode.None;";

    private readonly string example7RazorCode = @"
<BitButton IsEnabled=""searchSource is null""
           OnClick='() => searchSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>
<BitButton IsEnabled=""searchSource is not null""
           OnClick=""() => searchViewerRef.Search(searchTerm)"">Search from code</BitButton>
<BitButton IsEnabled=""searchSource is not null""
           OnClick=""() => searchViewerRef.SetSearchOptions(matchCase: true, wholeWord: true)"">Case + whole word</BitButton>

<BitTextField @bind-Value=""searchTerm"" Label=""Term"" />

<BitPdfViewer @ref=""searchViewerRef"" Source=""searchSource"" Height=""450px"" />";
    private readonly string example7CsharpCode = @"
private BitPdfSource? searchSource;

private string searchTerm = ""the"";

private BitPdfViewer searchViewerRef = default!;

// await searchViewerRef.FindNext();
// await searchViewerRef.FindPrevious();
// await searchViewerRef.ClearSearch();
// int count = searchViewerRef.SearchMatchCount;";

    private readonly string example8RazorCode = @"
<BitChoiceGroup @bind-Value=""renderMode"" Horizontal Label=""RenderMode""
                TItem=""BitChoiceGroupOption<BitPdfRenderMode>"" TValue=""BitPdfRenderMode"">
    <BitChoiceGroupOption Text=""Html"" Value=""BitPdfRenderMode.Html"" />
    <BitChoiceGroupOption Text=""Canvas"" Value=""BitPdfRenderMode.Canvas"" />
</BitChoiceGroup>
<BitChoiceGroup @bind-Value=""textCoalescing"" Horizontal Label=""TextCoalescing""
                TItem=""BitChoiceGroupOption<BitPdfTextCoalescing>"" TValue=""BitPdfTextCoalescing"">
    <BitChoiceGroupOption Text=""Exact"" Value=""BitPdfTextCoalescing.Exact"" />
    <BitChoiceGroupOption Text=""Compact"" Value=""BitPdfTextCoalescing.Compact"" />
</BitChoiceGroup>

<BitButton IsEnabled=""renderingSource is null""
           OnClick='() => renderingSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

@* BackgroundRendering is a safe no-op on single-threaded WebAssembly. *@
<BitPdfViewer Source=""renderingSource"" Height=""450px"" BackgroundRendering
              RenderMode=""renderMode""
              TextCoalescing=""textCoalescing"" />";
    private readonly string example8CsharpCode = @"
private BitPdfSource? renderingSource;

private BitPdfRenderMode renderMode = BitPdfRenderMode.Html;
private BitPdfTextCoalescing textCoalescing = BitPdfTextCoalescing.Exact;";

    private readonly string example9RazorCode = @"
<BitButton IsEnabled=""infoSource is null""
           OnClick='() => infoSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer @ref=""infoViewerRef"" Source=""infoSource"" Height=""400px"" OnDocumentLoaded=""StateHasChanged"" />

@if (infoViewerRef?.Document is not null)
{
    <div>Title: @(infoViewerRef.Metadata?.Title ?? ""-"")</div>
    <div>Producer: @(infoViewerRef.Metadata?.Producer ?? ""-"")</div>
    <div>Pdf version: @(infoViewerRef.PdfVersion ?? ""-"")</div>
    <div>Encrypted: @infoViewerRef.IsEncrypted</div>
    <div>Can print: @infoViewerRef.Permissions.CanPrint, can copy: @infoViewerRef.Permissions.CanCopy</div>
    <div>Form fields: @infoViewerRef.FormFields.Count</div>
    <div>File size: @infoViewerRef.FileSize bytes</div>
}";
    private readonly string example9CsharpCode = @"
private BitPdfSource? infoSource;

private BitPdfViewer? infoViewerRef;

// var labels = infoViewerRef.PageLabels;
// var structure = infoViewerRef.StructureTree;";

    private readonly string example10RazorCode = @"
<InputFile OnChange=""OnPasswordFileChange"" accept="".pdf,application/pdf"" />

<BitPdfViewer Source=""passwordSource"" Height=""400px"" RespectPermissions />

@* A known password travels on the source: Source='passwordSource.WithPassword(""secret"")'
   and your own UI can do the asking: OnPasswordRequested=""AskForPassword"" *@";
    private readonly string example10CsharpCode = @"
private BitPdfSource? passwordSource;

private async Task OnPasswordFileChange(InputFileChangeEventArgs e)
{
    if (e.FileCount == 0) return;

    const long maxSize = 512 * 1024 * 1024;
    if (e.File.Size <= 0 || e.File.Size > maxSize) return;

    using var stream = e.File.OpenReadStream(maxAllowedSize: maxSize);
    var bytes = new byte[(int)e.File.Size];
    await stream.ReadExactlyAsync(bytes);

    passwordSource = BitPdfSource.FromBytes(bytes, e.File.Name);
}

// private Task<string?> AskForPassword() => myOwnDialog.ShowAsync();";

    private readonly string example11RazorCode = @"
<BitButton OnClick='() => statesSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load a document</BitButton>
<BitButton OnClick=""LoadBrokenFile"">Load a broken file</BitButton>
<BitButton IsEnabled=""statesSource is not null"" OnClick=""() => statesSource = null"">Clear</BitButton>

<BitPdfViewer Source=""statesSource"" Height=""300px"" AllowDropFile>
    <LoadingTemplate>
        <BitSpinnerLoading Label=""Opening the document..."" />
    </LoadingTemplate>
    <EmptyTemplate>
        <BitText>Drop a pdf file here.</BitText>
    </EmptyTemplate>
    <ErrorTemplate>
        <BitText Color=""BitColor.Error"">This file could not be opened: @context</BitText>
    </ErrorTemplate>
</BitPdfViewer>";
    private readonly string example11CsharpCode = @"
private BitPdfSource? statesSource;

// A file that only claims to be a pdf.
private void LoadBrokenFile()
    => statesSource = BitPdfSource.FromBytes(""%PDF-1.7 this is not a pdf""u8.ToArray(), ""broken.pdf"");";

    private readonly string example12RazorCode = @"
<BitToggle @bind-Value=""blockPrinting"" Label=""Block printing"" />
<BitButton IsEnabled=""eventsSource is null""
           OnClick='() => eventsSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""eventsSource"" Height=""400px""
              OnDocumentLoaded='() => eventsLog.Add(""Document loaded"")'
              OnPageChanged='p => eventsLog.Add($""Page changed: {p}"")'
              OnPageRendered='p => eventsLog.Add($""Page rendered: {p}"")'
              OnZoomChanged='z => eventsLog.Add($""Zoom changed: {z:P0}"")'
              OnRotationChanged='r => eventsLog.Add($""Rotation changed: {r}deg"")'
              OnSidebarChanged='s => eventsLog.Add($""Sidebar changed: {s}"")'
              OnWarnings='w => eventsLog.Add($""Warnings: {w.Count}"")'
              OnProgress='p => eventsLog.Add($""Loading: {p:P0}"")'
              OnError='e => eventsLog.Add($""Error: {e}"")'
              OnDownloading=""HandleDownloading""
              OnPrinting=""HandlePrinting"" />

<div>Events:</div>
<div style=""max-height:8rem;overflow:auto"">
    @foreach (var log in Enumerable.Reverse(eventsLog).Take(8))
    {
        <div>@log</div>
    }
</div>";
    private readonly string example12CsharpCode = @"
private BitPdfSource? eventsSource;
private bool blockPrinting;

private readonly List<string> eventsLog = [];

private void HandleDownloading(BitPdfDownloadArgs args)
{
    args.FileName = $""report-{DateTime.Now:yyyy-MM-dd}.pdf"";
    eventsLog.Add($""Saving as {args.FileName}"");
}

private void HandlePrinting(BitPdfPrintArgs args)
{
    args.Cancel = blockPrinting;
    eventsLog.Add($""Printing pages {args.FromPage}-{args.ToPage}{(args.Cancel ? "" (blocked)"" : """")}"");
}";

    private readonly string example13RazorCode = @"
<BitButton IsEnabled=""bindingSource is null""
           OnClick='() => bindingSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitNumberField @bind-Value=""boundPage"" Label=""CurrentPage"" Min=""1"" />
<BitNumberField @bind-Value=""boundZoom"" Label=""Zoom"" Step=""0.25"" Min=""0.1"" Max=""8"" />
<BitNumberField @bind-Value=""boundRotation"" Label=""Rotation"" Step=""90"" />

<BitPdfViewer Source=""bindingSource"" Height=""400px""
              @bind-CurrentPage=""boundPage""
              @bind-Zoom=""boundZoom""
              @bind-Rotation=""boundRotation"" />";
    private readonly string example13CsharpCode = @"
private BitPdfSource? bindingSource;

private int boundPage = 1;
private double boundZoom = 1;
private int boundRotation;";

    private readonly string example14RazorCode = @"
<BitButton IsEnabled=""publicApiSource is null""
           OnClick='() => publicApiSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitButton OnClick=""() => pdfViewerRef.FirstPage()"">First</BitButton>
<BitButton OnClick=""() => pdfViewerRef.PrevPage()"">Prev</BitButton>
<BitTag Variant=""BitVariant.Outline"" Text=""@PublicApiPosition"" Color=""BitColor.Info"" />
<BitButton OnClick=""() => pdfViewerRef.NextPage()"">Next</BitButton>
<BitButton OnClick=""() => pdfViewerRef.LastPage()"">Last</BitButton>
<BitButton OnClick=""() => pdfViewerRef.ZoomOut()"">Zoom -</BitButton>
<BitButton OnClick=""() => pdfViewerRef.SetZoom(1)"">100%</BitButton>
<BitButton OnClick=""() => pdfViewerRef.ZoomIn()"">Zoom +</BitButton>
<BitButton OnClick=""() => pdfViewerRef.SetZoomMode(BitPdfZoomMode.FitPage)"">Fit page</BitButton>
<BitButton OnClick=""() => pdfViewerRef.RotateClockwise()"">Rotate</BitButton>
<BitButton OnClick=""() => pdfViewerRef.ShowSidebar(BitPdfSidebar.Thumbnails)"">Thumbnails</BitButton>
<BitButton OnClick=""() => pdfViewerRef.GoToDestination(FirstBookmarkDestination())"">First bookmark</BitButton>
<BitButton OnClick=""ShowSelectedText"">Selected text</BitButton>
<BitButton OnClick=""() => pdfViewerRef.Download()"">Download</BitButton>
<BitButton OnClick=""() => pdfViewerRef.Print()"">Print all</BitButton>
<BitButton OnClick=""() => pdfViewerRef.PrintCurrentPage()"">Print page</BitButton>
<BitButton OnClick=""() => pdfViewerRef.Print(1, 2)"">Print 1-2</BitButton>
<BitButton OnClick=""() => pdfViewerRef.TogglePresentationMode()"">Present</BitButton>

<BitPdfViewer @ref=""pdfViewerRef""
              Source=""publicApiSource""
              Height=""400px""
              ShowToolbar=""false""
              OnDocumentLoaded=""StateHasChanged""
              OnPageChanged=""_ => StateHasChanged()"" />

@if (selectedText is not null)
{
    <div>Selected: @(selectedText.Length == 0 ? ""(nothing)"" : selectedText)</div>
}";
    private readonly string example14CsharpCode = @"
private BitPdfSource? publicApiSource;

private BitPdfViewer pdfViewerRef = default!;

private string? selectedText;

// The reference is only set once the viewer has rendered.
private string PublicApiPosition => $""{pdfViewerRef?.CurrentPage}/{pdfViewerRef?.PageCount}"";

// A destination lands on the exact spot a bookmark points at, not just its page.
private BitPdfDestination? FirstBookmarkDestination()
    => pdfViewerRef?.Outline.FirstOrDefault()?.Destination;

private async Task ShowSelectedText()
{
    selectedText = await pdfViewerRef.GetSelectedText();
}

// await pdfViewerRef.OpenAsync(BitPdfSource.FromBytes(bytes, ""other.pdf""));
// await pdfViewerRef.GoToNamedDestination(""chapter-2"");
// string text = pdfViewerRef.ExtractText();
// string html = pdfViewerRef.RenderPageHtml(1);";

    private readonly string example15RazorCode = @"
<BitButton IsEnabled=""localizedSource is null""
           OnClick='() => localizedSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""localizedSource"" Height=""400px"" Texts=""germanTexts"" />";
    private readonly string example15CsharpCode = @"
private BitPdfSource? localizedSource;

// Only the properties you assign are replaced; the rest keep their English defaults.
private readonly BitPdfViewerTexts germanTexts = new()
{
    ToolbarAriaLabel = ""PDF-Werkzeugleiste"",
    DocumentAriaLabel = ""Dokument"",
    Thumbnails = ""Seitenminiaturen"",
    Bookmarks = ""Lesezeichen"",
    Attachments = ""Anhänge"",
    Layers = ""Ebenen"",
    FirstPage = ""Erste Seite"",
    PreviousPage = ""Vorherige Seite"",
    NextPage = ""Nächste Seite"",
    LastPage = ""Letzte Seite"",
    PageNumber = ""Seitenzahl"",
    ThumbnailAriaLabelFormat = ""Seite {0}"",
    PageAriaLabelFormat = ""Seite {0}"",
    LinkAriaLabelFormat = ""Gehe zu Seite {0}"",
    PageAnnouncementFormat = ""Seite {0} von {1}"",
    ZoomIn = ""Vergrößern"",
    ZoomOut = ""Verkleinern"",
    ZoomLevel = ""Zoomstufe"",
    Automatic = ""Automatischer Zoom"",
    FitWidth = ""Seitenbreite"",
    FitPage = ""Ganze Seite"",
    FitHeight = ""Seitenhöhe"",
    ActualSize = ""Originalgröße"",
    Find = ""Im Dokument suchen"",
    FindPlaceholder = ""Im Dokument suchen"",
    PreviousMatch = ""Vorheriger Treffer"",
    NextMatch = ""Nächster Treffer"",
    MatchCase = ""Groß-/Kleinschreibung"",
    WholeWord = ""Ganze Wörter"",
    MatchDiacritics = ""Akzente beachten"",
    HighlightAll = ""Alle hervorheben"",
    PhraseNotFound = ""Nicht gefunden"",
    MatchCountFormat = ""{0} von {1}"",
    RotateClockwise = ""Im Uhrzeigersinn drehen"",
    RotateCounterClockwise = ""Gegen den Uhrzeigersinn drehen"",
    Download = ""Herunterladen"",
    Print = ""Drucken"",
    Fullscreen = ""Vollbild"",
    ExitFullscreen = ""Vollbild beenden"",
    Presentation = ""Präsentationsmodus"",
    Properties = ""Dokumenteigenschaften"",
    OpenFile = ""Datei öffnen"",
    Close = ""Schließen"",
    NoDocument = ""Kein Dokument geladen."",
    PageCountFormat = ""{0} Seite(n)."",
};";

    private readonly string example16RazorCode = @"
<BitToggle @bind-Value=""a11yEnabled"" Label=""IsEnabled"" />
<BitButton IsEnabled=""a11ySource is null""
           OnClick='() => a11ySource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

@* EnableKeyboardShortcuts=""false"" hands every key back to the page. *@
<BitPdfViewer Source=""a11ySource"" Height=""450px"" DefaultSidebar=""BitPdfSidebar.Bookmarks"" IsEnabled=""a11yEnabled"" />";
    private readonly string example16CsharpCode = @"
private BitPdfSource? a11ySource;
private bool a11yEnabled = true;";

    private readonly string example17RazorCode = @"
<BitButton IsEnabled=""cascadeSource is null""
           OnClick='() => cascadeSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitParams Parameters=""pdfViewerParams"">
    <BitPdfViewer Source=""cascadeSource"" />
    <BitPdfViewer Source=""cascadeSource"" ToolbarItems=""BitPdfToolbarItems.All"" />
</BitParams>";
    private readonly string example17CsharpCode = @"
private BitPdfSource? cascadeSource;

private readonly BitPdfViewerParams[] pdfViewerParams =
[
    new()
    {
        Height = ""300px"",
        InitialZoomMode = BitPdfZoomMode.FitPage,
        ToolbarItems = BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom | BitPdfToolbarItems.Search,
        Texts = new() { NoDocument = ""Both viewers share their defaults - load a document."" },
    }
];";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        box-shadow: 0 0 1rem tomato;
    }

    .custom-toolbar {
        font-weight: bold;
    }
</style>

<BitButton IsEnabled=""styleSource is null""
           OnClick='() => styleSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""styleSource"" Height=""300px"" Style=""border-color:tomato"" Class=""custom-class"" />

<BitPdfViewer Source=""styleSource"" Height=""300px""
              Classes=""@(new() { Toolbar = ""custom-toolbar"" })""
              Styles=""@(new() { Toolbar = ""background:var(--bit-clr-pri);color:var(--bit-clr-pri-text)"",
                                ToolbarButton = ""color:var(--bit-clr-pri-text)"" })"" />

<BitPdfViewer Source=""styleSource"" Height=""300px"" Style=""@cssVariablesStyle"" />";
    private readonly string example18CsharpCode = @"
private BitPdfSource? styleSource;

// The same variables set on :root restyle every viewer of the app.
private const string cssVariablesStyle = ""--bit-PdfViewer-border-radius:1rem;"" +
                                         ""--bit-PdfViewer-border-color:var(--bit-clr-pri);"" +
                                         ""--bit-PdfViewer-background:var(--bit-clr-bg-pri);"" +
                                         ""--bit-PdfViewer-toolbar-background:var(--bit-clr-pri-light);"" +
                                         ""--bit-PdfViewer-button-radius:999px;"" +
                                         ""--bit-PdfViewer-page-shadow:none;"" +
                                         ""--bit-PdfViewer-page-gap:0.5rem;"" +
                                         ""--bit-PdfViewer-page-filter:invert(1) hue-rotate(180deg);"" +
                                         ""--bit-PdfViewer-search-match-background:gold;"";";

    private readonly string example19RazorCode = @"
<BitButton IsEnabled=""rtlSource is null""
           OnClick='() => rtlSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Dir=""BitDir.Rtl"" Source=""rtlSource"" Height=""400px"" />";
    private readonly string example19CsharpCode = @"
private BitPdfSource? rtlSource;";
}
