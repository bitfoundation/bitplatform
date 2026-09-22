namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public partial class BitFileInputDemo
{
    private readonly string example1RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" Title=""Pick one file from your device"" />

<BitFileInput Label=""Disabled file input"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitCheckbox @bind-Value=""allowDrop"" Label=""AllowDrop"" />
<BitCheckbox @bind-Value=""allowPaste"" Label=""AllowPaste"" />

<BitFileInput Label=""Browse, drop or paste a file"" AllowDrop=""allowDrop"" AllowPaste=""allowPaste"" />";
    private readonly string example2CsharpCode = @"
private bool allowDrop = true;
private bool allowPaste = true;";

    private readonly string example3RazorCode = @"
<BitFileInput Label=""Browse or drop a document""
              Accept="".pdf,.docx""
              MaxSize=""1024 * 1024 * 5""
              Description=""PDF or DOCX, up to 5 MB."" />

<BitFileInput Label=""Browse or drop an image"" Accept=""image/*"" MaxSize=""1024 * 1024 * 2"">
    <DescriptionTemplate>
        <i class=""bit-icon bit-icon--Info"" />
        <span>Square images look best. Up to <b>2 MB</b>.</span>
    </DescriptionTemplate>
</BitFileInput>";

    private readonly string example4RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple />

<BitFileInput Label=""Browse or drop a folder"" Directory />";

    private readonly string example5RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton RemoveButtonIconName=""Cancel"" />";

    private readonly string example6RazorCode = @"
<BitFileInput Label=""Browse or drop files""
              Multiple
              Append
              ShowRemoveButton
              AllowDuplicates=""false""
              DuplicateErrorMessage=""This file has already been picked."" />

<BitFileInput Label=""Browse or drop a file"" AutoReset />";

    private readonly string example7RazorCode = @"
<BitFileInput Label=""Browse or drop a file""
              MaxSize=""1024 * 1024 * 1""
              MinSize=""1024""
              MaxSizeErrorMessage=""The file is too big! Please select a file smaller than 1 MB."" />

<BitFileInput Label=""Browse or drop files""
              Multiple
              Append
              ShowRemoveButton
              MaxCount=""3""
              MaxTotalSize=""1024 * 1024 * 2"" />";

    private readonly string example8RazorCode = @"
<BitFileInput Label=""Browse images"" Accept=""image/*"" />

<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@(["".gif"", "".jpg"", "".png"", "".bmp""])"" />

<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@([""image/*"", ""application/pdf""])"" />

<BitFileInput Label=""Take a photo"" Accept=""image/*"" Capture=""environment"" />";

    private readonly string example9RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple FileValidator=""@ValidateEmptyFile"" />

<BitFileInput Label=""Browse or drop images""
              Multiple
              Accept=""image/*""
              ReadImageDimensions
              FileValidator=""@ValidateImageDimensions"" />";
    private readonly string example9CsharpCode = @"
private string? ValidateEmptyFile(BitFileInputInfo file)
{
    return file.Size == 0 ? ""Empty files are not allowed"" : null;
}

private string? ValidateImageDimensions(BitFileInputInfo file)
{
    // dropped and pasted files bypass the accept filter, so non-image files reach the validator as well.
    if (file.ContentType.StartsWith(""image/"", StringComparison.OrdinalIgnoreCase) is false) return null;

    if (file.Width is null || file.Height is null) return ""This image could not be decoded"";

    return (file.Width < 300 || file.Height < 300)
        ? $""The image is {file.Width}×{file.Height}, smaller than the required 300×300""
        : null;
}";

    private readonly string example10RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple ShowPreview />

<BitFileInput Label=""Browse or drop files"" Multiple ShowPreview FileIconSelector=""@SelectFileIcon"" />";
    private readonly string example10CsharpCode = @"
private BitIconInfo? SelectFileIcon(BitFileInputInfo file)
{
    // anything the app knows nothing about is left without a glyph rather than given a generic one.
    if (file.ContentType.StartsWith(""video/"", StringComparison.OrdinalIgnoreCase)) return BitIconInfo.Bit(""MyMoviesTV"");
    if (file.ContentType.StartsWith(""audio/"", StringComparison.OrdinalIgnoreCase)) return BitIconInfo.Bit(""Volume3"");

    return Path.GetExtension(file.Name).ToLowerInvariant() switch
    {
        "".pdf"" => BitIconInfo.Bit(""PDF""),
        "".zip"" or "".rar"" or "".7z"" => BitIconInfo.Bit(""ZipFolder""),
        _ => null
    };
}";

    private readonly string example11RazorCode = @"
<BitFileInput Label=""انتخاب یا رها کردن فایل""
              Multiple
              ShowRemoveButton
              MaxSize=""1024 * 1024 * 1""
              RemoveButtonTitle=""حذف""
              MaxSizeErrorMessage=""حجم فایل از حد مجاز بیشتر است.""
              FileSizeFormatter=""@FormatFileSizeInFarsi""
              AnnouncementProvider=""@AnnounceInFarsi"" />

<BitFileInput Label=""Browse or drop files"" Multiple FileSizeFormatter=""@(size => $""{size:N0} bytes"")"" />";
    private readonly string example11CsharpCode = @"
private static readonly string[] farsiUnits = [""بایت"", ""کیلوبایت"", ""مگابایت"", ""گیگابایت""];

private string FormatFileSizeInFarsi(long size)
{
    double value = size;
    var unit = 0;

    while (value >= 1024 && unit < farsiUnits.Length - 1)
    {
        value /= 1024;
        unit++;
    }

    return $""{Math.Round(value, 1)} {farsiUnits[unit]}"";
}

private string? AnnounceInFarsi(IReadOnlyList<BitFileInputInfo> files)
{
    if (files.Count == 0) return ""فایلی انتخاب نشده است."";

    var rejected = files.Count(f => f.IsValid is false);

    return rejected == 0
        ? $""{files.Count} فایل انتخاب شد.""
        : $""{files.Count} فایل انتخاب شد، {rejected} مورد نامعتبر است."";
}";

    private readonly string example12RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple HideFileList OnChange=""@HandleOnHiddenListChange"" />

<div>Custom file list:</div>
@if (hiddenListFiles.Length == 0)
{
    <div>No files selected yet.</div>
}
@foreach (var file in hiddenListFiles)
{
    <div>@file.Name - @FileSizeHumanizer.Humanize(file.Size)</div>
}";
    private readonly string example12CsharpCode = @"
private BitFileInputInfo[] hiddenListFiles = [];

private void HandleOnHiddenListChange(BitFileInputInfo[] files)
{
    hiddenListFiles = files;
}";

    private readonly string example13RazorCode = @"
<BitFileInput @ref=""eventsFileInput""
              Label=""Select or drag and drop files""
              Multiple
              ShowRemoveButton
              MaxSize=""1024 * 1024 * 1""
              OnChange=""@HandleOnChange""
              OnInvalid=""@HandleOnInvalid""
              OnRemove=""@HandleOnRemove"" />

<div>Selected files:</div>
@foreach (var file in eventsFiles)
{
    <div>
        @file.Name (@FileSizeHumanizer.Humanize(file.Size), modified @file.LastModifiedDate.ToString(""yyyy-MM-dd""))
        @if (file.Content is not null)
        {
            <span> - @file.Content.Length bytes loaded</span>
        }
    </div>
}

@if (eventsLog.Count > 0)
{
    <div>Events:</div>
    @foreach (var log in eventsLog)
    {
        <div class=""event-log"">@log</div>
    }
}";
    private readonly string example13CsharpCode = @"
private BitFileInput eventsFileInput = default!;
private BitFileInputInfo[] eventsFiles = [];
private readonly List<string> eventsLog = [];

private async Task HandleOnChange(BitFileInputInfo[] files)
{
    eventsFiles = files;

    AddEventLog($""OnChange: {files.Length} file(s) selected"");

    // reads the content of every valid file of the list.
    await eventsFileInput.ReadContentAsync();
}

private void HandleOnInvalid(BitFileInputInfo[] files)
{
    AddEventLog($""OnInvalid: {string.Join("", "", files.Select(f => $""{f.Name} ({f.Message})""))}"");
}

private void HandleOnRemove(BitFileInputInfo file)
{
    AddEventLog($""OnRemove: {file.Name}"");
}

private void AddEventLog(string log)
{
    eventsLog.Insert(0, log);

    if (eventsLog.Count > 5)
    {
        eventsLog.RemoveAt(eventsLog.Count - 1);
    }
}";

    private readonly string example14RazorCode = @"
<BitFileInput @ref=""bitFileInput""
              Multiple
              Append
              MaxSize=""1024 * 1024 * 2""
              AllowedExtensions=""@(["".jpg"", "".jpeg"", "".png"", "".bmp""])""
              Classes=""@(new() { Dragging = ""custom-drop-zone"" })"">
    <LabelTemplate>
        <button type=""button"" class=""browse-file"" @onclick=""() => bitFileInput.Browse()"">
            <i class=""bit-icon bit-icon--CloudUpload"" aria-hidden=""true"" />
            <div class=""browse-file-title"">Drag and drop or <strong>browse files</strong></div>
            <div class=""browse-file-hint"">JPG, JPEG, PNG or BMP, up to 2 MB each</div>
        </button>
    </LabelTemplate>
    <FileViewTemplate Context=""file"">
        <div class=""file-row @(file.IsValid ? null : ""file-row-invalid"")"">
            <i class=""bit-icon bit-icon--Page"" aria-hidden=""true"" />
            <div class=""file-row-data"">
                <div class=""file-row-name"">@file.Name</div>
                <div class=""file-row-meta"">@FileSizeHumanizer.Humanize(file.Size)</div>
                @if (file.IsValid is false)
                {
                    <div class=""file-row-error"">@file.Message</div>
                }
            </div>
            <button type=""button"" class=""file-row-remove"" aria-label=""@($""Remove {file.Name}"")""
                    @onclick=""() => bitFileInput.RemoveFile(file)"">
                <i class=""bit-icon bit-icon--Cancel"" aria-hidden=""true"" />
            </button>
        </div>
    </FileViewTemplate>
</BitFileInput>";
    private readonly string example14CsharpCode = @"
private BitFileInput bitFileInput = default!;";
    private const string example14ScssCode = @"
/* Every color is a theme token, so the hand-built panel and rows follow the preset and the scheme. */
.browse-file {
    gap: 0.5rem;
    width: 100%;
    display: flex;
    cursor: pointer;
    padding: 1.5rem;
    text-align: center;
    align-items: center;
    font-family: inherit;
    justify-content: center;
    flex-flow: column nowrap;
    color: var(--bit-clr-fg-pri);
    background-color: var(--bit-clr-bg-pri);
    border-radius: var(--bit-shp-radius-surface);
    border: 2px dashed var(--bit-clr-brd-pri);

    &:hover {
        border-color: var(--bit-clr-pri);
        background-color: var(--bit-clr-bg-sec);
    }

    &:focus-visible {
        outline: 2px solid var(--bit-clr-pri-focus);
        outline-offset: 2px;
    }

    i {
        font-size: 1.5rem;
        color: var(--bit-clr-pri);
    }
}

.browse-file-title {
    font-size: 1rem;
}

.browse-file-hint {
    font-size: 0.75rem;
    color: var(--bit-clr-fg-sec);
}

.file-row {
    gap: 0.75rem;
    display: flex;
    padding: 0.5rem;
    margin-top: 0.5rem;
    align-items: center;
    color: var(--bit-clr-fg-pri);
    background-color: var(--bit-clr-bg-sec);
    border-radius: var(--bit-shp-radius-surface);
    border: 1px solid var(--bit-clr-brd-pri);

    > i {
        font-size: 1.5rem;
        color: var(--bit-clr-fg-sec);
    }
}

.file-row-invalid {
    border-color: var(--bit-clr-err);
}

.file-row-data {
    flex-grow: 1;
    min-width: 0;
}

.file-row-name {
    overflow: hidden;
    font-weight: 600;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.file-row-meta {
    font-size: 0.75rem;
    color: var(--bit-clr-fg-sec);
}

.file-row-error {
    font-size: 0.75rem;
    color: var(--bit-clr-err);
}

.file-row-remove {
    width: 2rem;
    height: 2rem;
    display: flex;
    flex-shrink: 0;
    cursor: pointer;
    align-items: center;
    justify-content: center;
    border: none;
    color: var(--bit-clr-err);
    background-color: transparent;
    border-radius: var(--bit-shp-radius-button);

    &:hover {
        background-color: var(--bit-clr-bg-ter);
    }

    &:focus-visible {
        outline: 2px solid var(--bit-clr-pri-focus);
        outline-offset: -2px;
    }
}

/* Classes.Dragging lands on the root while files hover over it, which is where the panel gets its
   drag feedback from now that it no longer carries the component's own drop indicator. */
::deep .custom-drop-zone .browse-file {
    border-color: var(--bit-clr-pri);
    background-color: var(--bit-clr-bg-sec-hover);
}";

    private readonly DemoCodeFile[] example14CodeFiles =
    [
        new("BitFileInputDemo.razor.scss", example14ScssCode),
    ];

    private readonly string example15RazorCode = @"
<BitFileInput @ref=""publicApiFileInput"" HideLabel Multiple OnChange=""@(_ => StateHasChanged())"" />

<div class=""api-buttons"">
    <BitButton OnClick=""() => publicApiFileInput.Browse()"">Browse files</BitButton>
    <BitButton OnClick=""() => publicApiFileInput.Reset()"">Reset</BitButton>
    <BitButton OnClick=""() => publicApiFileInput.RemoveFile()"">Remove all</BitButton>
    <BitButton OnClick=""@HashTheFirstFile"">Hash the first file</BitButton>
</div>

<div>@(publicApiFileInput?.Files.Count ?? 0) file(s) in the list.</div>

@if (streamHash.HasValue())
{
    <div>SHA-256: <code>@streamHash</code></div>
}";
    private readonly string example15CsharpCode = @"
private BitFileInput publicApiFileInput = default!;
private string? streamHash;

private async Task HashTheFirstFile()
{
    streamHash = null;

    var file = publicApiFileInput.Files.FirstOrDefault(f => f.IsValid);

    if (file is null) return;

    // nothing of the file is ever held whole: the stream is read in chunks and folded into the hash.
    await using var stream = await publicApiFileInput.OpenReadStreamAsync(file);

    streamHash = Convert.ToHexString(await SHA256.HashDataAsync(stream));
}";

    private readonly string example16RazorCode = @"
<BitFileInput Label=""Browse or drop a file""
              AriaLabel=""Select a document to attach""
              Description=""Tab to the button, then press Enter or Space."" />";

    private readonly string example17RazorCode = @"
<BitFileInput Variant=""BitVariant.Fill"" Label=""Fill"" />
<BitFileInput Variant=""BitVariant.Outline"" Label=""Outline"" />
<BitFileInput Variant=""BitVariant.Text"" Label=""Text"" />

<BitFileInput Variant=""BitVariant.Fill"" Label=""Fill"" IsEnabled=""false"" />
<BitFileInput Variant=""BitVariant.Outline"" Label=""Outline"" IsEnabled=""false"" />
<BitFileInput Variant=""BitVariant.Text"" Label=""Text"" IsEnabled=""false"" />

<BitFileInput Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Label=""Fill"" />
<BitFileInput Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Label=""Outline"" />
<BitFileInput Variant=""BitVariant.Text"" Color=""BitColor.Success"" Label=""Text"" />";

    private readonly string example18RazorCode = @"
<BitFileInput ShowDropZone Multiple ShowPreview ShowRemoveButton
              Label=""Drag files here or click to browse""
              Description=""Any file type, as many as you like."" />

<BitFileInput ShowDropZone Directory DropZoneIconName=""FabricFolder""
              Label=""Drag a folder here or click to browse"" />";

    private readonly string example19RazorCode = @"
<BitParams Parameters=""@fileInputParams"">
    <BitFileInput />

    <BitFileInput Label=""Its own label, the cascaded rest""
                  MaxSize=""1024 * 1024 * 10""
                  MaxSizeErrorMessage=""This one alone accepts up to 10 MB."" />
</BitParams>

<BitFileInput Label=""Outside the cascade, and back to the defaults"" />";
    private readonly string example19CsharpCode = @"
private readonly BitFileInputParams[] fileInputParams =
[
    new()
    {
        Multiple = true,
        ShowPreview = true,
        ShowRemoveButton = true,
        Label = ""Attach a file"",
        MaxSize = 1024 * 1024 * 1,
        MaxSizeErrorMessage = ""Attachments are limited to 1 MB."",
        Description = ""Anything up to 1 MB. Drop it here or browse."",
        FileSizeFormatter = size => $""{size:N0} bytes""
    }
];";

    private readonly string example20RazorCode = @"
<BitFileInput Color=""BitColor.Primary"" Label=""Primary"" />
<BitFileInput Color=""BitColor.Secondary"" Label=""Secondary"" />
<BitFileInput Color=""BitColor.Tertiary"" Label=""Tertiary"" />
<BitFileInput Color=""BitColor.Info"" Label=""Info"" />
<BitFileInput Color=""BitColor.Success"" Label=""Success"" />
<BitFileInput Color=""BitColor.Warning"" Label=""Warning"" />
<BitFileInput Color=""BitColor.SevereWarning"" Label=""SevereWarning"" />
<BitFileInput Color=""BitColor.Error"" Label=""Error"" />

<BitFileInput Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" />
<BitFileInput Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" />
<BitFileInput Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" />

<BitFileInput Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" />
<BitFileInput Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" />
<BitFileInput Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" />

<BitFileInput Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" />
<BitFileInput Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" />
<BitFileInput Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" />";

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton RemoveButtonIcon=""@(""fa-solid fa-trash-can"")"" />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Css(""fa-solid fa-xmark"")"" />

<BitFileInput Label=""Browse or drop files"" Multiple ShowPreview ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Fa(""solid trash"")""
              FileIconSelector=""@(_ => BitIconInfo.Fa(""solid file-lines""))"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton RemoveButtonIcon=""@(""bi bi-trash"")"" />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Css(""bi bi-x-circle-fill"")"" />

<BitFileInput Label=""Browse or drop files"" Multiple ShowPreview ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Bi(""trash3-fill"")""
              FileIconSelector=""@(_ => BitIconInfo.Bi(""file-earmark""))"" />";

    private readonly string example22RazorCode = @"
<BitFileInput Size=""BitSize.Small"" Label=""Browse or drop a file"" ShowPreview ShowRemoveButton />

<BitFileInput Size=""BitSize.Medium"" Label=""Browse or drop a file"" ShowPreview ShowRemoveButton />

<BitFileInput Size=""BitSize.Large"" Label=""Browse or drop a file"" ShowPreview ShowRemoveButton />";

    private readonly string example23RazorCode = @"
<BitFileInput Label=""Styled file input""
              Style=""box-shadow: dodgerblue 0 0 1rem; border-radius: 1rem; padding: 0.5rem;"" />

<BitFileInput Label=""Classed file input"" Class=""custom-class"" />

<BitFileInput Label=""Styles"" ShowRemoveButton Styles=""@(new()
              {
                  Label = ""border-color: deeppink; background-color: deeppink; color: white;"",
                  Dragging = ""background-color: lavenderblush;"",
                  FileName = ""color: deeppink;"",
                  RemoveButton = ""background-color: deeppink; color: white;""
              })"" />

<BitFileInput Label=""Classes"" ShowRemoveButton Classes=""@(new()
              {
                  Label = ""custom-label"",
                  Dragging = ""custom-dragging"",
                  FileItem = ""custom-item"",
                  RemoveButton = ""custom-remove""
              })"" />


<BitFileInput Label=""Pill button, wide component"" ShowPreview ShowRemoveButton
              Style=""--bit-FileInput-label-radius: 999px; --bit-FileInput-max-width: 100%; --bit-FileInput-label-font-weight: 400;"" />

<BitFileInput Label=""Solid drop indicator in the role color"" ShowRemoveButton
              Style=""--bit-FileInput-drop-border-style: solid; --bit-FileInput-drop-background: var(--bit-clr-suc);"" />

<BitFileInput Label=""Rounder, roomier items"" ShowPreview ShowRemoveButton
              Style=""--bit-FileInput-item-radius: 1rem; --bit-FileInput-item-padding: 1rem; --bit-FileInput-item-gap: 0.75rem; --bit-FileInput-preview-size: 3.5rem;"" />

<BitFileInput Label=""A list that scrolls instead of growing"" Multiple Append ShowRemoveButton
              Style=""--bit-FileInput-file-list-max-height: 10rem; --bit-FileInput-item-hover-background: var(--bit-clr-bg-sec-hover);"" />


<div style=""--bit-FileInput-color: rebeccapurple; --bit-FileInput-hover-color: #5b2d8e; --bit-FileInput-item-border-color: rebeccapurple; --bit-FileInput-file-size-color: rebeccapurple;"">
    <BitFileInput Label=""Attach a résumé"" ShowRemoveButton />
    <BitFileInput Label=""Attach a cover letter"" ShowRemoveButton />
</div>";

    private readonly string example24RazorCode = @"
<div dir=""rtl"">
    <BitFileInput Dir=""BitDir.Rtl""
                  Label=""انتخاب یا رها کردن فایل""
                  Multiple
                  ShowPreview
                  ShowRemoveButton
                  RemoveButtonTitle=""حذف"" />
</div>";
}
