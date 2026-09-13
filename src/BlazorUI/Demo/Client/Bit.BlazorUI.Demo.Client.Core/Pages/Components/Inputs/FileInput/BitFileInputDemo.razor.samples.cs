namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public partial class BitFileInputDemo
{
    private readonly string example1RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" />

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
<BitFileInput Label=""Browse or drop files"" Multiple />";

    private readonly string example5RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AutoReset />";

    private readonly string example6RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" Append />";

    private readonly string example7RazorCode = @"
<BitFileInput Label=""Browse or drop files""
              Multiple
              Append
              ShowRemoveButton
              AllowDuplicates=""false""
              DuplicateErrorMessage=""This file has already been picked."" />";

    private readonly string example8RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" MaxSize=""1024 * 1024 * 1"" />

<BitFileInput Label=""Browse or drop a file"" MinSize=""1024"" />

<BitFileInput Label=""Browse or drop files"" Multiple Append ShowRemoveButton MaxTotalSize=""1024 * 1024 * 2"" />

<BitFileInput Label=""Browse or drop a file""
              MaxSize=""1024 * 1024 * 1""
              MaxSizeErrorMessage=""The file is too big! Please select a file smaller than 1 MB."" />";

    private readonly string example9RazorCode = @"
<BitFileInput Label=""Browse images"" Accept=""image/*"" />

<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@(["".gif"","".jpg"","".png"","".bmp""])"" />

<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@([""image/*"", ""application/pdf""])"" />";

    private readonly string example10RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple Append MaxCount=""3"" ShowRemoveButton />";

    private readonly string example11RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple FileValidator=""@ValidateEmptyFile"" />";
    private readonly string example11CsharpCode = @"
private string? ValidateEmptyFile(BitFileInputInfo file)
{
    return file.Size == 0 ? ""Empty files are not allowed"" : null;
}";

    private readonly string example12RazorCode = @"
<BitFileInput Label=""Browse or drop images""
              Multiple
              ShowPreview
              Accept=""image/*""
              ReadImageDimensions
              FileValidator=""@ValidateImageDimensions"" />";
    private readonly string example12CsharpCode = @"
private string? ValidateImageDimensions(BitFileInputInfo file)
{
    // dropped and pasted files bypass the accept filter, so non-image files reach the validator as well.
    if (file.ContentType.StartsWith(""image/"", StringComparison.OrdinalIgnoreCase) is false) return null;

    if (file.Width is null || file.Height is null) return ""This image could not be decoded"";

    return (file.Width < 300 || file.Height < 300)
        ? $""The image is {file.Width}×{file.Height}, smaller than the required 300×300""
        : null;
}";

    private readonly string example13RazorCode = @"
<BitFileInput Label=""Browse or drop a folder"" Directory />";

    private readonly string example14RazorCode = @"
<BitFileInput Label=""Take a photo"" Accept=""image/*"" Capture=""environment"" />";

    private readonly string example15RazorCode = @"
<BitFileInput Label=""Browse or drop images"" Multiple ShowPreview Accept=""image/*"" />";

    private readonly string example16RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton RemoveButtonIconName=""Cancel"" />

<BitFileInput Label=""انتخاب یا رها کردن فایل"" ShowRemoveButton RemoveButtonTitle=""حذف"" />";

    private readonly string example17RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple FileSizeFormatter=""@FormatFileSizeInFarsi"" />

<BitFileInput Label=""Browse or drop files"" Multiple FileSizeFormatter=""@(size => $""{size:N0} bytes"")"" />";
    private readonly string example17CsharpCode = @"
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
}";

    private readonly string example18RazorCode = @"
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
    private readonly string example18CsharpCode = @"
private BitFileInputInfo[] hiddenListFiles = [];

private void HandleOnHiddenListChange(BitFileInputInfo[] files)
{
    hiddenListFiles = files;
}";

    private readonly string example19RazorCode = @"
<BitFileInput @ref=""eventsFileInput"" Label=""Select or drag and drop files""
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
    private readonly string example19CsharpCode = @"
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

    private readonly string example20RazorCode = @"
<style>
    .browse-file {
        border: 1px solid #D2D2D7;
        border-radius: 2px;
        padding: 24px;
        width: 420px;
        height: 200px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 50px;
        cursor: pointer;
    }

    .browse-file:hover {
        border-color: #0072CE;
        background-color: #f8f9fa;
    }

    .browse-file-header {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        font-size: 16px;
    }

    .browse-file-header i {
        font-size: 24px;
        font-weight: 700;
        color: #0072CE;
    }

    .browse-file-header strong {
        color: #0072CE;
    }

    .browse-file-footer {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        font-size: 12px;
        color: #78787D;
    }

    .file-list {
        border: 1px solid #D2D2D7;
        border-radius: 2px;
        padding: 24px;
        width: 420px;
        height: 200px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
    }

    .file-list-header {
        display: flex;
        flex-direction: column;
        gap: 8px;
    }

    .file-info {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .file-info-main {
        display: flex;
        justify-content: flex-start;
        align-items: center;
        gap: 12px;
    }

    .file-info-main i {
        font-size: 24px;
    }

    .file-info-name {
        font-weight: 600;
        margin-bottom: 4px;
    }

    .file-info-data {
        width: 275px;
    }

    .file-info-btns {
        display: flex;
        justify-content: space-between;
        gap: 8px;
    }

    .file-info-btns i {
        display: block;
        cursor: pointer;
    }

    .file-info-btns .remove-ico {
        color: #F9423A;
    }

    .file-info-btns .remove-ico:hover {
        color: #d32f2f;
    }

    .file-info-e-msg {
        margin-top: 12px;
        color: #F9423A;
    }

    .file-list-footer {
        font-size: 12px;
        color: #78787D;
    }

    .custom-drop-zone .browse-file {
        border-style: dashed;
        border-color: #0072CE;
        background-color: #eaf4fd;
    }
</style>

<BitFileInput @ref=""bitFileInput"" Multiple
              MaxSize=""1024 * 1024 * 2""
              AllowedExtensions=""@(["".jpg"", "".jpeg"", "".png"", "".bmp""])""
              Classes=""@(new() { Dragging = ""custom-drop-zone"" })"">
    <LabelTemplate>
        @if (bitFileInput.Files?.Any() is not true)
        {
            <div class=""browse-file"" @onclick=""() => bitFileInput.Browse()"">
                <div class=""browse-file-header"">
                    <i class=""bit-icon bit-icon--CloudUpload"" />
                    <div>
                        Drag and drop or
                    </div>
                    <div>
                        <strong>
                            Browse files
                        </strong>
                    </div>
                </div>

                <div class=""browse-file-footer"">
                    <div>Max file size: 2 MB</div>
                    <div>Supported file types: jpg, jpeg, png, bmp</div>
                </div>
            </div>
        }
    </LabelTemplate>
    <FileViewTemplate Context=""file"">
        @if (!string.IsNullOrEmpty(file.Name))
        {
            <div class=""file-list"">
                <div class=""file-list-header"">
                    <div class=""file-info"">
                        <div class=""file-info-main"">
                            <i class=""bit-icon bit-icon--Page"" />
                            <div class=""file-info-data"">
                                <div class=""file-info-name"">
                                    @file.Name
                                </div>
                                <div>
                                    @FileSizeHumanizer.Humanize(file.Size)
                                </div>
                            </div>
                        </div>

                        <div class=""file-info-btns"">
                            <i class=""bit-icon bit-icon--Cancel remove-ico""
                                @onclick=""() => bitFileInput.RemoveFile(file)"" />
                        </div>
                    </div>

                    @if (!file.IsValid)
                    {
                        <div class=""file-info-e-msg"">@file.Message</div>
                    }
                </div>

                <div class=""file-list-footer"">
                    <div>Max file size: 2 MB</div>
                    <div>Supported file types: jpg, jpeg, png, bmp</div>
                </div>
            </div>
        }
    </FileViewTemplate>
</BitFileInput>";
    private readonly string example20CsharpCode = @"
private BitFileInput bitFileInput = default!;";

    private readonly string example21RazorCode = @"
<BitFileInput @ref=""publicApiFileInput"" HideLabel Multiple OnChange=""@(_ => StateHasChanged())"" />

<BitButton OnClick=""() => publicApiFileInput.Browse()"">Browse files</BitButton>
<BitButton OnClick=""() => publicApiFileInput.Reset()"">Reset</BitButton>
<BitButton OnClick=""() => publicApiFileInput.RemoveFile()"">Remove all</BitButton>

<div>@(publicApiFileInput?.Files.Count ?? 0) file(s) in the list.</div>";
    private readonly string example21CsharpCode = @"
private BitFileInput publicApiFileInput = default!;";

    private readonly string example22RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AriaLabel=""Select a document to attach"" />

<BitFileInput Label=""Browse or drop files""
              Multiple
              MaxSize=""1024 * 1024 * 1""
              AnnouncementProvider=""@AnnounceAttachments"" />";
    private readonly string example22CsharpCode = @"
private string? AnnounceAttachments(IReadOnlyList<BitFileInputInfo> files)
{
    if (files.Count == 0) return ""No attachment yet."";

    var rejected = files.Count(f => f.IsValid is false);

    return rejected == 0
        ? $""{files.Count} attachment(s) ready to send.""
        : $""{files.Count - rejected} attachment(s) ready to send, {rejected} rejected as too large."";
}";

    private readonly string example23RazorCode = @"
<BitFileInput Variant=""BitVariant.Fill"" Label=""Fill"" />
<BitFileInput Variant=""BitVariant.Outline"" Label=""Outline"" />
<BitFileInput Variant=""BitVariant.Text"" Label=""Text"" />

<BitFileInput Variant=""BitVariant.Fill"" Label=""Fill"" IsEnabled=""false"" />
<BitFileInput Variant=""BitVariant.Outline"" Label=""Outline"" IsEnabled=""false"" />
<BitFileInput Variant=""BitVariant.Text"" Label=""Text"" IsEnabled=""false"" />

<BitFileInput Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Label=""Fill"" />
<BitFileInput Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Label=""Outline"" />
<BitFileInput Variant=""BitVariant.Text"" Color=""BitColor.Success"" Label=""Text"" />";

    private readonly string example24RazorCode = @"
<BitFileInput Color=""BitColor.Primary"" Label=""Primary"" />
<BitFileInput Color=""BitColor.Secondary"" Label=""Secondary"" />
<BitFileInput Color=""BitColor.Tertiary"" Label=""Tertiary"" />
<BitFileInput Color=""BitColor.Info"" Label=""Info"" />
<BitFileInput Color=""BitColor.Success"" Label=""Success"" />
<BitFileInput Color=""BitColor.Warning"" Label=""Warning"" />
<BitFileInput Color=""BitColor.SevereWarning"" Label=""SevereWarning"" />
<BitFileInput Color=""BitColor.Error"" Label=""Error"" />

<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
    <BitFileInput Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" />
    <BitFileInput Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" />
    <BitFileInput Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" />
</div>

<BitFileInput Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" />
<BitFileInput Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" />
<BitFileInput Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" />

<BitFileInput Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" />
<BitFileInput Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" />
<BitFileInput Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" />";

    private readonly string example25RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@(""fa-solid fa-trash-can"")"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Css(""fa-solid fa-xmark"")"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Fa(""solid trash"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@(""bi bi-trash"")"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Css(""bi bi-x-circle-fill"")"" />

<BitFileInput Label=""Browse or drop a file""
              ShowRemoveButton
              RemoveButtonIcon=""@BitIconInfo.Bi(""trash3-fill"")"" />";

    private readonly string example26RazorCode = @"
<BitFileInput Size=""BitSize.Small"" Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Size=""BitSize.Medium"" Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Size=""BitSize.Large"" Label=""Browse or drop a file"" ShowRemoveButton />";

    private readonly string example27RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.25rem;
        border: 2px dashed mediumseagreen;
    }

    .custom-label {
        color: white;
        border-color: mediumseagreen;
        background-color: mediumseagreen;
    }

    .custom-dragging {
        background-color: honeydew;
    }

    .custom-item {
        border-color: mediumseagreen;
    }

    .custom-remove {
        color: white;
        background-color: mediumseagreen;
    }
</style>

<BitFileInput Label=""Styled file input"" Style=""box-shadow: dodgerblue 0 0 1rem; border-radius: 1rem; padding: 0.5rem;"" />

<BitFileInput Label=""Classed file input"" Class=""custom-class"" />


<BitFileInput Label=""Styles""
              ShowRemoveButton
              Styles=""@(new() { Label = ""border-color: deeppink; background-color: deeppink; color: white;"",
                                Dragging = ""background-color: lavenderblush;"",
                                FileName = ""color: deeppink;"",
                                RemoveButton = ""background-color: deeppink; color: white;"" })"" />

<BitFileInput Label=""Classes""
              ShowRemoveButton
              Classes=""@(new() { Label = ""custom-label"",
                                 Dragging = ""custom-dragging"",
                                 FileItem = ""custom-item"",
                                 RemoveButton = ""custom-remove"" })"" />";

    private readonly string example28RazorCode = @"
<div dir=""rtl"">
    <BitFileInput Dir=""BitDir.Rtl"" Label=""انتخاب یا رها کردن فایل"" ShowRemoveButton RemoveButtonTitle=""حذف"" />
</div>";
}
