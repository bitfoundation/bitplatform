namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public partial class BitFileInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accept",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the accept attribute of the input element.",
        },
        new()
        {
            Name = "AllowedExtensions",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "[\"*\"]",
            Description = "Filters files by extension.",
        },
        new()
        {
            Name = "Append",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the append mode that appends any additional selected file(s) to the current file list."
        },
        new()
        {
            Name = "AutoReset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically resets the file-input before starting to browse for files."
        },
        new()
        {
            Name = "HideFileList",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the file list section of the file input."
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "Browse",
            Description = "The text of select file button."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "A custom razor fragment for select button."
        },
        new()
        {
            Name = "MaxSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Specifies the maximum size (byte) of the file (0 for unlimited)."
        },
        new()
        {
            Name = "Multiple",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables multi-file selection."
        },
        new()
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string",
            DefaultValue = "File type not allowed",
            Description = "The message shown for files with not allowed extensions."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback for when file or files change.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnSelectComplete",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback for when file selection is completed.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the remove button for each selected file."
        },
        new()
        {
            Name = "SizeTooLargeErrorMessage",
            Type = "string",
            DefaultValue = "File size is too large",
            Description = "The message shown for files that exceed the max size."
        },
        new()
        {
            Name = "FileViewTemplate",
            Type = "RenderFragment<BitFileInputInfo>?",
            DefaultValue = "null",
            Description = "The custom file view template."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "file-info",
            Title = "BitFileInputInfo",
            Parameters =
            [
               new()
               {
                   Name = "ContentType",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "The Content-Type of the selected file."
               },
               new()
               {
                   Name = "Name",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "The name of the selected file."
               },
               new()
               {
                   Name = "Size",
                   Type = "long",
                   Description = "The size of the selected file."
               },
               new()
               {
                   Name = "FileId",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "The file ID of the selected file, this is a GUID."
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   Description = "The index of the selected file."
               },
               new()
               {
                   Name = "IsValid",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Indicates whether the file passed validation (size and extension)."
               },
               new()
               {
                   Name = "Message",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The validation message if the file is not valid."
               }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Files",
            Type = "BitFileInputInfo[]?",
            DefaultValue = "null",
            Description = "A list of all of the selected files.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "InputId",
            Type = "string?",
            DefaultValue = "",
            Description = "The id of the file input element.",
        },
        new()
        {
            Name = "RemoveFile",
            Type = "(BitFileInputInfo? fileInfo = null) => void",
            DefaultValue = "",
            Description = "Removes a file from the selected files list.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "Browse",
            Type = "Task",
            DefaultValue = "",
            Description = "Opens a file selection dialog.",
        },
        new()
        {
            Name = "Reset",
            Type = "Task",
            DefaultValue = "",
            Description = "Resets the file input.",
        }
    ];



    private BitFileInput bitFileInput = default!;
    private BitFileInput bitFileInputWithBrowse = default!;

    private List<BitFileInputInfo> selectedFiles1 = [];
    private List<BitFileInputInfo> selectedFiles2 = [];
    private List<BitFileInputInfo> selectedFiles3 = [];
    private List<BitFileInputInfo> selectedFiles4 = [];
    private List<BitFileInputInfo> selectedFiles5 = [];
    private List<BitFileInputInfo> selectedFiles6 = [];
    private List<BitFileInputInfo> selectedFiles7 = [];
    private List<BitFileInputInfo> selectedFiles8 = [];
    private List<BitFileInputInfo> selectedFiles9 = [];
    private List<BitFileInputInfo> selectedFiles10 = [];

    private string selectCompleteText = "";

    private bool FileInputIsEmpty() => bitFileInput.Files?.Any() is not true;

    private void HandleOnChange1(BitFileInputInfo[] files)
    {
        selectedFiles1 = files.ToList();
    }

    private void HandleOnChange2(BitFileInputInfo[] files)
    {
        selectedFiles2 = files.ToList();
    }

    private void HandleOnChange3(BitFileInputInfo[] files)
    {
        selectedFiles3 = files.ToList();
    }

    private void HandleOnChange4(BitFileInputInfo[] files)
    {
        selectedFiles4 = files.ToList();
    }

    private void HandleOnChange5(BitFileInputInfo[] files)
    {
        selectedFiles5 = files.ToList();
    }

    private void HandleOnChange6(BitFileInputInfo[] files)
    {
        selectedFiles6 = files.ToList();
    }

    private void HandleOnChange7(BitFileInputInfo[] files)
    {
        selectedFiles7 = files.ToList();
    }

    private void HandleOnChange8(BitFileInputInfo[] files)
    {
        selectedFiles8 = files.ToList();
    }

    private void HandleOnChange9(BitFileInputInfo[] files)
    {
        selectedFiles9 = files.ToList();
    }

    private void HandleOnChange10(BitFileInputInfo[] files)
    {
        selectedFiles10 = files.ToList();
    }

    private async Task HandleBrowseClick()
    {
        await bitFileInputWithBrowse.Browse();
    }

    private async Task HandleResetClick()
    {
        await bitFileInputWithBrowse.Reset();
    }



    private readonly string example1RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" OnChange=""@HandleOnChange1"" />

@if (selectedFiles1.Any())
{
    <div>
        <strong>Selected files:</strong>
        @foreach (var file in selectedFiles1)
        {
            <div>@file.Name (@FileSizeHumanizer.Humanize(file.Size))</div>
        }
    </div>
}";
    private readonly string example1CsharpCode = @"
private List<BitFileInputInfo> selectedFiles1 = [];

private void HandleOnChange1(BitFileInputInfo[] files)
{
    selectedFiles1 = files.ToList();
}";

    private readonly string example2RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" Multiple OnChange=""@HandleOnChange2"" />

@if (selectedFiles2.Any())
{
    <div>
        <strong>Selected @selectedFiles2.Count file(s):</strong>
        @foreach (var file in selectedFiles2)
        {
            <div>@file.Name (@FileSizeHumanizer.Humanize(file.Size))</div>
        }
    </div>
}";
    private readonly string example2CsharpCode = @"
private List<BitFileInputInfo> selectedFiles2 = [];

private void HandleOnChange2(BitFileInputInfo[] files)
{
    selectedFiles2 = files.ToList();
}";

    private readonly string example3RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" AutoReset OnChange=""@HandleOnChange3"" />";
    private readonly string example3CsharpCode = @"
private List<BitFileInputInfo> selectedFiles3 = [];

private void HandleOnChange3(BitFileInputInfo[] files)
{
    selectedFiles3 = files.ToList();
}";

    private readonly string example4RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" Append OnChange=""@HandleOnChange4"" />";
    private readonly string example4CsharpCode = @"
private List<BitFileInputInfo> selectedFiles4 = [];

private void HandleOnChange4(BitFileInputInfo[] files)
{
    selectedFiles4 = files.ToList();
}";

    private readonly string example5RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" MaxSize=""1024 * 1024 * 1"" OnChange=""@HandleOnChange5"" />

@if (selectedFiles5.Any())
{
    <div>
        <strong>Selected files:</strong>
        @foreach (var file in selectedFiles5)
        {
            <div class=""@(file.IsValid ? """" : ""invalid-file"")"">
                @file.Name (@FileSizeHumanizer.Humanize(file.Size))
                @if (!file.IsValid)
                {
                    <span> - @file.Message</span>
                }
            </div>
        }
    </div>
}";
    private readonly string example5CsharpCode = @"
private List<BitFileInputInfo> selectedFiles5 = [];

private void HandleOnChange5(BitFileInputInfo[] files)
{
    selectedFiles5 = files.ToList();
}";

    private readonly string example6RazorCode = @"
<BitFileInput Label=""Select or drag and drop files""
              AllowedExtensions=""@(new List<string> { "".gif"","".jpg"","".png"","".bmp"" })""
              OnChange=""@HandleOnChange6"" />";
    private readonly string example6CsharpCode = @"
private List<BitFileInputInfo> selectedFiles6 = [];

private void HandleOnChange6(BitFileInputInfo[] files)
{
    selectedFiles6 = files.ToList();
}";

    private readonly string example7RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" ShowRemoveButton OnChange=""@HandleOnChange7"" />";
    private readonly string example7CsharpCode = @"
private List<BitFileInputInfo> selectedFiles7 = [];

private void HandleOnChange7(BitFileInputInfo[] files)
{
    selectedFiles7 = files.ToList();
}";

    private readonly string example8RazorCode = @"
<BitFileInput Label=""Select or drag and drop files""
              OnChange=""@HandleOnChange8""
              OnSelectComplete=""@(() => selectCompleteText = ""File selection completed"")"" />

<div>@selectCompleteText</div>";
    private readonly string example8CsharpCode = @"
private List<BitFileInputInfo> selectedFiles8 = [];
private string selectCompleteText = """";

private void HandleOnChange8(BitFileInputInfo[] files)
{
    selectedFiles8 = files.ToList();
}";

    private readonly string example9RazorCode = @"
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

<BitFileInput @ref=""bitFileInput"" OnChange=""@HandleOnChange9"">
    <LabelTemplate>
        @if (FileInputIsEmpty())
        {
            <div class=""browse-file"" @onclick=""() => bitFileInput.Browse()"">
                <div class=""browse-file-header"">
                    <i class=""bit-icon bit-icon--CloudUpload"" />
                    <div>
                        Drag and drop or
                    </div>
                    <div>
                        <strong>
                            Browse file
                        </strong>
                    </div>
                </div>

                <div class=""browse-file-footer"">
                    <div>
                        Max file size: 2 MB
                    </div>
                    <div>
                        Supported file types: jpg, jpeg, png, bpm
                    </div>
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
                    <div>
                        Max file size: 2 MB
                    </div>
                    <div>
                        Supported file types: jpg, jpeg, png, bpm
                    </div>
                </div>
            </div>
        }
    </FileViewTemplate>
</BitFileInput>";
    private readonly string example9CsharpCode = @"
private BitFileInput bitFileInput = default!;
private List<BitFileInputInfo> selectedFiles9 = [];

private bool FileInputIsEmpty() => bitFileInput.Files?.Any() is not true;

private void HandleOnChange9(BitFileInputInfo[] files)
{
    selectedFiles9 = files.ToList();
}";

    private readonly string example10RazorCode = @"
<BitFileInput @ref=""bitFileInputWithBrowse""
              Label=""""
              OnChange=""@HandleOnChange10"" />
<br />
<BitButton OnClick=""HandleBrowseClick"">Browse file</BitButton>
<BitButton OnClick=""HandleResetClick"">Reset</BitButton>";
    private readonly string example10CsharpCode = @"
private BitFileInput bitFileInputWithBrowse = default!;
private List<BitFileInputInfo> selectedFiles10 = [];

private void HandleOnChange10(BitFileInputInfo[] files)
{
    selectedFiles10 = files.ToList();
}

private async Task HandleBrowseClick()
{
    await bitFileInputWithBrowse.Browse();
}

private async Task HandleResetClick()
{
    await bitFileInputWithBrowse.Reset();
}";
}
