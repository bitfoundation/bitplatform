namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public partial class BitFileInputDemo
{
    private readonly string example1RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" />

<BitFileInput Label=""Disabled file input"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple />";

    private readonly string example3RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AutoReset />";

    private readonly string example4RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" Append />";

    private readonly string example5RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" MaxSize=""1024 * 1024 * 1"" />

<BitFileInput Label=""Browse or drop a file"" MinSize=""1024"" />

<BitFileInput Label=""Browse or drop a file""
              MaxSize=""1024 * 1024 * 1""
              MaxSizeErrorMessage=""The file is too big! Please select a file smaller than 1 MB."" />";

    private readonly string example6RazorCode = @"
<BitFileInput Label=""Browse images"" Accept=""image/*"" />

<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@(["".gif"","".jpg"","".png"","".bmp""])"" />";

    private readonly string example7RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple Append MaxCount=""3"" />";

    private readonly string example8RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple FileValidator=""@ValidateEmptyFile"" />";
    private readonly string example8CsharpCode = @"
private string? ValidateEmptyFile(BitFileInputInfo file)
{
    return file.Size == 0 ? ""Empty files are not allowed"" : null;
}";

    private readonly string example9RazorCode = @"
<BitFileInput Label=""Browse a folder"" Directory />";

    private readonly string example10RazorCode = @"
<BitFileInput Label=""Take a photo"" Accept=""image/*"" Capture=""environment"" />";

    private readonly string example11RazorCode = @"
<BitFileInput Label=""Browse or drop images"" Multiple ShowPreview Accept=""image/*"" />";

    private readonly string example12RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton RemoveButtonIconName=""Cancel"" />";

    private readonly string example13RazorCode = @"
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
    private readonly string example13CsharpCode = @"
private BitFileInputInfo[] hiddenListFiles = [];

private void HandleOnHiddenListChange(BitFileInputInfo[] files)
{
    hiddenListFiles = files;
}";

    private readonly string example14RazorCode = @"
<BitFileInput @ref=""eventsFileInput"" Label=""Select or drag and drop files""
              Multiple
              ShowRemoveButton
              OnChange=""@HandleOnChange""
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
    private readonly string example14CsharpCode = @"
private BitFileInput eventsFileInput = default!;
private BitFileInputInfo[] eventsFiles = [];
private readonly List<string> eventsLog = [];

private async Task HandleOnChange(BitFileInputInfo[] files)
{
    eventsFiles = files;

    AddEventLog($""OnChange: {files.Length} file(s) selected"");

    foreach (var file in files)
    {
        if (file.IsValid && file.Content is null)
        {
            await eventsFileInput.ReadContentAsync(file);
        }
    }
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

    private readonly string example15RazorCode = @"
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

    .file-info-e-msg {
        margin-top: 12px;
        color: #F9423A;
    }

    .file-list-footer {
        font-size: 12px;
        color: #78787D;
    }
</style>

<BitFileInput @ref=""bitFileInput"" Multiple
              MaxSize=""1024 * 1024 * 2""
              AllowedExtensions=""@(["".jpg"", "".jpeg"", "".png"", "".bmp""])"">
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
    private readonly string example15CsharpCode = @"
private BitFileInput bitFileInput = default!;";

    private readonly string example16RazorCode = @"
<BitFileInput @ref=""publicApiFileInput"" HideLabel Multiple />

<BitButton OnClick=""() => publicApiFileInput.Browse()"">Browse files</BitButton>
<BitButton OnClick=""() => publicApiFileInput.Reset()"">Reset</BitButton>
<BitButton OnClick=""() => publicApiFileInput.RemoveFile()"">Remove all</BitButton>";
    private readonly string example16CsharpCode = @"
private BitFileInput publicApiFileInput = default!;";

    private readonly string example17RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AriaLabel=""Select a document to attach"" />";

    private readonly string example18RazorCode = @"
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

    private readonly string example19RazorCode = @"
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

    private readonly string example20RazorCode = @"
<BitFileInput Size=""BitSize.Small"" Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Size=""BitSize.Medium"" Label=""Browse or drop a file"" ShowRemoveButton />

<BitFileInput Size=""BitSize.Large"" Label=""Browse or drop a file"" ShowRemoveButton />";

    private readonly string example21RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.25rem;
        border: 2px dashed mediumseagreen;
    }

    .custom-label {
        color: mediumseagreen;
        border-color: mediumseagreen;
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
              Styles=""@(new() { Label = ""border-color: deeppink; color: deeppink;"",
                                Dragging = ""background-color: lavenderblush;"",
                                FileName = ""color: deeppink;"",
                                RemoveButton = ""background-color: deeppink; color: white;"" })"" />

<BitFileInput Label=""Classes""
              ShowRemoveButton
              Classes=""@(new() { Label = ""custom-label"",
                                 Dragging = ""custom-dragging"",
                                 FileItem = ""custom-item"",
                                 RemoveButton = ""custom-remove"" })"" />";

    private readonly string example22RazorCode = @"
<div dir=""rtl"">
    <BitFileInput Dir=""BitDir.Rtl"" Label=""انتخاب یا رها کردن فایل"" ShowRemoveButton />
</div>";
}
