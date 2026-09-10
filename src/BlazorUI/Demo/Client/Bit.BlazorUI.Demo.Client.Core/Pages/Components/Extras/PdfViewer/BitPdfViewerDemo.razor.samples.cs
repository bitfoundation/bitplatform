namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.PdfViewer;

public partial class BitPdfViewerDemo
{
    private readonly string example1RazorCode = @"
<InputFile OnChange=""OnBasicFileChange"" accept="".pdf,application/pdf"" />

<BitPdfViewer Source=""basicSource"" />";
    private readonly string example1CsharpCode = @"
private BitPdfSource basicSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"");

// or from in-memory bytes:
// private BitPdfSource basicSource = BitPdfSource.FromBytes(pdfBytes, ""file-name.pdf"");

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

<BitPdfViewer Source=""heightSource"" Height=""400px"" />";
    private readonly string example2CsharpCode = @"
private BitPdfSource? heightSource;";

    private readonly string example3RazorCode = @"
<BitButton IsEnabled=""toolbarSource is null""
           OnClick='() => toolbarSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

@* No toolbar at all: *@
<BitPdfViewer Source=""toolbarSource"" ShowToolbar=""false"" />

@* Or only some of its groups: *@
<BitPdfViewer Source=""toolbarSource"" Height=""400px""
              ToolbarItems=""BitPdfToolbarItems.Navigation | BitPdfToolbarItems.Zoom | BitPdfToolbarItems.Fullscreen"" />";
    private readonly string example3CsharpCode = @"
private BitPdfSource? toolbarSource;";

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
              InitialZoomMode=""BitPdfZoomMode.FitPage""
              MinZoom=""0.5"" MaxZoom=""3"" ZoomStep=""1.5"" />";
    private readonly string example5CsharpCode = @"
private BitPdfSource? zoomSource;";

    private readonly string example6RazorCode = @"
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
private BitPdfSpreadMode spreadMode = BitPdfSpreadMode.None;

// The same three can be driven from the component reference:
// await pdfViewerRef.SetScrollMode(BitPdfScrollMode.Wrapped);
// await pdfViewerRef.SetSpreadMode(BitPdfSpreadMode.Odd);
// pdfViewerRef.SetCursorTool(BitPdfCursorTool.Pan);";

    private readonly string example7RazorCode = @"
<BitButton IsEnabled=""searchSource is null""
           OnClick='() => searchSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>
<BitButton IsEnabled=""searchSource is not null""
           OnClick=""() => searchViewerRef.Search(searchTerm)"">Search from code</BitButton>

<BitTextField @bind-Value=""searchTerm"" Label=""Term"" />

<BitPdfViewer @ref=""searchViewerRef"" Source=""searchSource"" Height=""450px"" />";
    private readonly string example7CsharpCode = @"
private BitPdfSource? searchSource;

private string searchTerm = ""the"";

private BitPdfViewer searchViewerRef = default!;

// The matches can also be stepped through from code:
// await searchViewerRef.FindNext();
// await searchViewerRef.FindPrevious();
// await searchViewerRef.ClearSearch();
// int count = searchViewerRef.SearchMatchCount;";

    private readonly string example8RazorCode = @"
<BitButton IsEnabled=""keyboardSource is null""
           OnClick='() => keyboardSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

@* Shortcuts are on by default; set the parameter to false to hand every key back to the page. *@
<BitPdfViewer Source=""keyboardSource"" Height=""450px"" EnableKeyboardShortcuts=""true"" />";
    private readonly string example8CsharpCode = @"
private BitPdfSource? keyboardSource;";

    private readonly string example9RazorCode = @"
<BitChoiceGroup @bind-Value=""renderMode"" Horizontal Label=""RenderMode""
                TItem=""BitChoiceGroupOption<BitPdfRenderMode>"" TValue=""BitPdfRenderMode"">
    <BitChoiceGroupOption Text=""Html"" Value=""BitPdfRenderMode.Html"" />
    <BitChoiceGroupOption Text=""Canvas"" Value=""BitPdfRenderMode.Canvas"" />
</BitChoiceGroup>

<BitButton IsEnabled=""canvasSource is null""
           OnClick='() => canvasSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<InputFile OnChange=""OnCanvasFileChange"" accept="".pdf,application/pdf"" />

<BitPdfViewer Source=""canvasSource"" Height=""450px"" RenderMode=""renderMode"" />";
    private readonly string example9CsharpCode = @"
private BitPdfSource? canvasSource;

private BitPdfRenderMode renderMode = BitPdfRenderMode.Html;

private async Task OnCanvasFileChange(InputFileChangeEventArgs e)
{
    if (e.FileCount == 0) return;

    const long maxSize = 512 * 1024 * 1024;
    if (e.File.Size <= 0 || e.File.Size > maxSize) return;

    using var stream = e.File.OpenReadStream(maxAllowedSize: maxSize);
    var bytes = new byte[(int)e.File.Size];
    await stream.ReadExactlyAsync(bytes);

    canvasSource = BitPdfSource.FromBytes(bytes, e.File.Name);
}";

    private readonly string example10RazorCode = @"
<BitChoiceGroup @bind-Value=""textCoalescing"" Horizontal Label=""TextCoalescing""
                TItem=""BitChoiceGroupOption<BitPdfTextCoalescing>"" TValue=""BitPdfTextCoalescing"">
    <BitChoiceGroupOption Text=""Exact"" Value=""BitPdfTextCoalescing.Exact"" />
    <BitChoiceGroupOption Text=""Compact"" Value=""BitPdfTextCoalescing.Compact"" />
</BitChoiceGroup>

<BitButton IsEnabled=""coalescingSource is null""
           OnClick='() => coalescingSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""coalescingSource"" Height=""450px"" TextCoalescing=""textCoalescing"" />";
    private readonly string example10CsharpCode = @"
private BitPdfSource? coalescingSource;

private BitPdfTextCoalescing textCoalescing = BitPdfTextCoalescing.Exact;";

    private readonly string example11RazorCode = @"
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
    private readonly string example11CsharpCode = @"
private BitPdfSource? infoSource;

private BitPdfViewer? infoViewerRef;

// The whole parsed document is available too, e.g. for indexing or reflow:
// var labels = infoViewerRef.PageLabels;
// var structure = infoViewerRef.StructureTree;
// var text = infoViewerRef.ExtractText();";

    private readonly string example12RazorCode = @"
<InputFile OnChange=""OnPasswordFileChange"" accept="".pdf,application/pdf"" />

<BitPdfViewer Source=""passwordSource"" Height=""400px"" OnError='e => passwordError = e' />

@* A known password can travel on the source instead of being asked for: *@
@* <BitPdfViewer Source=""passwordSource.WithPassword(""secret"")"" /> *@

@* Or replace the built-in dialog with your own UI: *@
@* <BitPdfViewer Source=""passwordSource"" OnPasswordRequested=""AskForPassword"" /> *@";
    private readonly string example12CsharpCode = @"
private string? passwordError;
private BitPdfSource? passwordSource;

private async Task OnPasswordFileChange(InputFileChangeEventArgs e)
{
    if (e.FileCount == 0) return;

    passwordError = null;

    using var stream = e.File.OpenReadStream(maxAllowedSize: 512 * 1024 * 1024);
    passwordSource = await BitPdfSource.FromStreamAsync(stream, e.File.Name);
}

// The host can own the asking instead of the built-in dialog:
// private Task<string?> AskForPassword() => myOwnDialog.ShowAsync();";

    private readonly string example13RazorCode = @"
<BitButton IsEnabled=""eventsSource is null""
           OnClick='() => eventsSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""eventsSource"" Height=""400px""
              OnDocumentLoaded='() => eventsLog.Add(""Document loaded"")'
              OnPageChanged='p => eventsLog.Add($""Page changed: {p}"")'
              OnZoomChanged='z => eventsLog.Add($""Zoom changed: {z:P0}"")'
              OnRotationChanged='r => eventsLog.Add($""Rotation changed: {r}deg"")'
              OnSidebarChanged='s => eventsLog.Add($""Sidebar changed: {s}"")'
              OnWarnings='w => eventsLog.Add($""Warnings: {w.Count}"")'
              OnProgress='p => eventsLog.Add($""Downloading: {p:P0}"")'
              OnError='e => eventsLog.Add($""Error: {e}"")' />

<div>Events:</div>
<div style=""max-height:8rem;overflow:auto"">
    @foreach (var log in Enumerable.Reverse(eventsLog).Take(8))
    {
        <div>@log</div>
    }
</div>";
    private readonly string example13CsharpCode = @"
private BitPdfSource? eventsSource;

private readonly List<string> eventsLog = [];";

    private readonly string example14RazorCode = @"
<BitButton IsEnabled=""publicApiSource is null""
           OnClick='() => publicApiSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;align-items:center"">
    <BitButton OnClick=""() => pdfViewerRef.FirstPage()"">First</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.PrevPage()"">Prev</BitButton>
    <BitTag Variant=""BitVariant.Outline"" Text=""@($""{pdfViewerRef?.CurrentPage}/{pdfViewerRef?.PageCount}"")"" Color=""BitColor.Info"" />
    <BitButton OnClick=""() => pdfViewerRef.NextPage()"">Next</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.LastPage()"">Last</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.ZoomOut()"">Zoom -</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.SetZoom(1)"">100%</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.ZoomIn()"">Zoom +</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.SetZoomMode(BitPdfZoomMode.FitPage)"">Fit page</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.RotateCounterClockwise()"">Rotate ccw</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.RotateClockwise()"">Rotate cw</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.ShowSidebar(BitPdfSidebar.Thumbnails)"">Thumbnails</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.Download()"">Download</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.Print()"">Print</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.TogglePresentationMode()"">Present</BitButton>
    <BitButton OnClick=""() => pdfViewerRef.ToggleFullscreen()"">Fullscreen</BitButton>
</div>

@* BackgroundRendering offloads parse/render to a worker thread when the runtime
   has one (Blazor Server, or a WASM app built with WasmEnableThreads). *@
<BitPdfViewer @ref=""pdfViewerRef"" BackgroundRendering
              Source=""publicApiSource""
              Height=""400px""
              ShowToolbar=""false""
              OnDocumentLoaded=""StateHasChanged""
              OnPageChanged=""_ => StateHasChanged()"" />";
    private readonly string example14CsharpCode = @"
private BitPdfSource? publicApiSource;

private BitPdfViewer pdfViewerRef = default!;";

    private readonly string example15RazorCode = @"
<BitButton IsEnabled=""bindingSource is null""
           OnClick='() => bindingSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitNumberField @bind-Value=""boundPage"" Label=""CurrentPage"" Min=""1"" />
<BitNumberField @bind-Value=""boundZoom"" Label=""Zoom"" Step=""0.25"" Min=""0.1"" Max=""8"" />
<BitNumberField @bind-Value=""boundRotation"" Label=""Rotation"" Step=""90"" />

<BitPdfViewer Source=""bindingSource"" Height=""400px""
              @bind-CurrentPage=""boundPage""
              @bind-Zoom=""boundZoom""
              @bind-Rotation=""boundRotation"" />";
    private readonly string example15CsharpCode = @"
private BitPdfSource? bindingSource;

private int boundPage = 1;
private double boundZoom = 1;
private int boundRotation;";

    private readonly string example16RazorCode = @"
<BitButton IsEnabled=""localizedSource is null""
           OnClick='() => localizedSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Dir=""BitDir.Rtl"" Source=""localizedSource"" Height=""400px"" Texts=""persianTexts"" />";
    private readonly string example16CsharpCode = @"
private BitPdfSource? localizedSource;

// Only the properties you assign are replaced; the rest keep their English defaults.
private readonly BitPdfViewerTexts persianTexts = new()
{
    Thumbnails = ""بندانگشتی صفحات"",
    Bookmarks = ""نشانک‌ها"",
    FirstPage = ""صفحه اول"",
    PreviousPage = ""صفحه قبل"",
    NextPage = ""صفحه بعد"",
    LastPage = ""صفحه آخر"",
    PageNumber = ""شماره صفحه"",
    ZoomIn = ""بزرگ‌نمایی"",
    ZoomOut = ""کوچک‌نمایی"",
    ZoomLevel = ""میزان بزرگ‌نمایی"",
    FitWidth = ""اندازه عرض"",
    FitPage = ""اندازه صفحه"",
    ActualSize = ""اندازه واقعی"",
    Find = ""جستجو در سند"",
    FindPlaceholder = ""جستجو در سند"",
    PreviousMatch = ""مورد قبلی"",
    NextMatch = ""مورد بعدی"",
    MatchCase = ""حساس به حروف"",
    WholeWord = ""کلمه کامل"",
    RotateClockwise = ""چرخش ساعتگرد"",
    RotateCounterClockwise = ""چرخش پادساعتگرد"",
    Download = ""دانلود سند"",
    Print = ""چاپ سند"",
    Fullscreen = ""تمام صفحه"",
    Properties = ""مشخصات سند"",
    Close = ""بستن"",
    NoDocument = ""سندی بارگذاری نشده است."",
    PageCountFormat = ""{0} صفحه"",
};";

    private readonly string example17RazorCode = @"
<BitButton IsEnabled=""styleSource is null""
           OnClick='() => styleSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Source=""styleSource"" Height=""400px""
              Styles=""@(new() { Toolbar = ""background:var(--bit-clr-pri);color:var(--bit-clr-pri-text)"",
                                ToolbarButton = ""color:var(--bit-clr-pri-text)"",
                                Surface = ""background:#2b2b30"" })"" />

@* Classes works the same way, with your own class names: *@
<BitPdfViewer Source=""styleSource"" Class=""custom-viewer""
              Classes=""@(new() { Toolbar = ""custom-toolbar"", Page = ""custom-page"" })"" />";
    private readonly string example17CsharpCode = @"
private BitPdfSource? styleSource;";

    private readonly string example18RazorCode = @"
<BitButton IsEnabled=""rtlSource is null""
           OnClick='() => rtlSource = BitPdfSource.FromUrl(""url-to-the-pdf-file.pdf"", ""file-name.pdf"")'>Load document</BitButton>

<BitPdfViewer Dir=""BitDir.Rtl"" Source=""rtlSource"" Height=""400px"" />";
    private readonly string example18CsharpCode = @"
private BitPdfSource? rtlSource;";
}
