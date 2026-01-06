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
            Description = "Enables the append mode that adds any additional selected file(s) to the current file list."
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
            Name = "HideLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the label of the file input."
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
            Description = "Specifies the maximum allowed file size in bytes (0 for unlimited)."
        },
        new()
        {
            Name = "MaxSizeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the message for the failed validation due to exceeding the maximum size."
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
            Description = "Specifies the message for the failed validation due to the allowed extensions."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback for when file or files selection changes.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
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
            Name = "FileViewTemplate",
            Type = "RenderFragment<BitFileInputInfo>?",
            DefaultValue = "null",
            Description = "The custom file view template.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
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
            Href = "#file-input-info"
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
            Href = "#file-input-info"
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

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "file-input-info",
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



    private BitFileInput bitFileInput = default!;

    private BitFileInputInfo[] selectedFiles = [];
    private void HandleOnChange(BitFileInputInfo[] files)
    {
        selectedFiles = files;
    }


    private BitFileInput publicApiFileInput = default!;



    private readonly string example1RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" />";

    private readonly string example2RazorCode = @"
<BitFileInput Label=""Browse or drop files"" Multiple />";

    private readonly string example3RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AutoReset />";

    private readonly string example4RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" Append />";

    private readonly string example5RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" MaxSize=""1024 * 1024 * 1"" />";

    private readonly string example6RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" AllowedExtensions=""@(["".gif"","".jpg"","".png"","".bmp""])"" />";

    private readonly string example7RazorCode = @"
<BitFileInput Label=""Browse or drop a file"" ShowRemoveButton />";

    private readonly string example8RazorCode = @"
<BitFileInput Label=""Select or drag and drop files"" OnChange=""@HandleOnChange"" />

<div>Selected files:</div>
@foreach (var file in selectedFiles)
{
    <div>@file.Name (@FileSizeHumanizer.Humanize(file.Size))</div>
}";
    private readonly string example8CsharpCode = @"
private BitFileInputInfo[] selectedFiles = [];

private void HandleOnChange(BitFileInputInfo[] files)
{
    selectedFiles = files;
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
    private readonly string example9CsharpCode = @"
private BitFileInput bitFileInput = default!;";

    private readonly string example10RazorCode = @"
<BitFileInput @ref=""publicApiFileInput"" HideLabel />

<BitButton OnClick=""() => publicApiFileInput.Browse()"">Browse file</BitButton>
<BitButton OnClick=""() => publicApiFileInput.Reset()"">Reset</BitButton>";
    private readonly string example10CsharpCode = @"
private BitFileInput publicApiFileInput = default!;";
}
