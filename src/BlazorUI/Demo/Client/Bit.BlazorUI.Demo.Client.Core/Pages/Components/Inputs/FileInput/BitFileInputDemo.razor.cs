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
            Description = "Accepted file types for the file browser using MIME types or file extensions (e.g., \"image/*\", \".pdf,.doc\"). Applied to the underlying HTML input element's accept attribute.",
        },
        new()
        {
            Name = "AllowedExtensions",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "[\"*\"]",
            Description = "Allowed file extensions for validation purposes (e.g., [\".jpg\", \".png\", \".pdf\"]). Use [\"*\"] to allow all file types. Files not matching these extensions will be marked as invalid.",
        },
        new()
        {
            Name = "Append",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to append newly selected files to the existing file list instead of replacing it."
        },
        new()
        {
            Name = "AutoReset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the file input is automatically reset (cleared) before opening the file browser dialog, allowing the same file to be selected multiple times consecutively."
        },
        new()
        {
            Name = "HideFileList",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the file list that displays the selected files in the UI."
        },
        new()
        {
            Name = "HideLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the default browse button label from the UI."
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "Browse",
            Description = "The text displayed on the browse button. Defaults to \"Browse\"."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Razor template for the browse button area, allowing full customization of the file selection trigger UI."
        },
        new()
        {
            Name = "MaxSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Maximum allowed file size in bytes for validation. Files exceeding this size will be marked as invalid. Set to 0 for no size limit."
        },
        new()
        {
            Name = "MaxSizeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file exceeds the maximum size limit. Defaults to \"The file size is larger than the max size\"."
        },
        new()
        {
            Name = "Multiple",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to allow selecting multiple files simultaneously through the file browser dialog."
        },
        new()
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string",
            DefaultValue = "File type not allowed",
            Description = "Custom error message displayed when a file's extension is not in the allowed extensions list. Defaults to \"The file type is not allowed\"."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback invoked when the file selection changes, providing an array of BitFileInputInfo representing all selected files.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to display a remove button next to each file in the file list, allowing individual file removal."
        },
        new()
        {
            Name = "FileViewTemplate",
            Type = "RenderFragment<BitFileInputInfo>?",
            DefaultValue = "null",
            Description = "Custom Razor template for rendering individual file items in the file list. Receives a BitFileInputInfo context for each file.",
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
            Description = "A read-only list of all currently selected files with their metadata, validation status, and content.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "InputId",
            Type = "string?",
            DefaultValue = "",
            Description = "The unique identifier of the underlying HTML file input element.",
        },
        new()
        {
            Name = "RemoveFile",
            Type = "(BitFileInputInfo? fileInfo = null) => void",
            DefaultValue = "",
            Description = "Removes a specific file from the selected files list, or clears all files when no file is specified.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "Browse",
            Type = "Task",
            DefaultValue = "",
            Description = "Opens the file browser dialog programmatically, allowing users to select files. If AutoReset is enabled, the input is reset before opening.",
        },
        new()
        {
            Name = "ReadContentAsync",
            Type = "(BitFileInputInfo fileInfo) => Task",
            DefaultValue = "",
            Description = "Reads the content of the specified file from the browser and populates its Content property with the byte array. Only reads valid and enabled files.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "Reset",
            Type = "Task",
            DefaultValue = "",
            Description = "Clears all selected files and resets the file input to its initial state.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "file-input-info",
            Title = "BitFileInputInfo",
            Description = "Represents metadata, validation state, and content of a file selected through BitFileInput.",
            Parameters =
            [
               new()
               {
                   Name = "ContentType",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "The MIME content type of the file (e.g., \"image/png\", \"application/pdf\")."
               },
               new()
               {
                   Name = "Name",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "The name of the file including its extension (e.g., \"document.pdf\")."
               },
               new()
               {
                   Name = "Size",
                   Type = "long",
                   Description = "The size of the file in bytes."
               },
               new()
               {
                   Name = "FileId",
                   Type = "String",
                   DefaultValue = "string.Empty",
                   Description = "A unique identifier (GUID) assigned to the file upon selection, used to reference the file in JavaScript interop."
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   Description = "The zero-based index of the file in the current selection list."
               },
               new()
               {
                   Name = "IsValid",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether the file has passed all validation checks including size constraints and allowed extensions."
               },
               new()
               {
                   Name = "Message",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The validation error message when the file has failed a validation check (e.g., size or extension). This is null when the file is valid."
               },
               new()
               {
                   Name = "Content",
                   Type = "byte[]?",
                   DefaultValue = "null",
                   Description = "The file content as a byte array, populated by calling ReadContentAsync. This is null by default and only loaded on demand."
               }
            ]
        }
    ];



    private BitFileInput bitFileInput = default!;

    private BitFileInput eventsFileInput = default!;
    private BitFileInputInfo[] selectedFiles = [];
    private async Task HandleOnChange(BitFileInputInfo[] files)
    {
        selectedFiles = files;

        foreach (var file in files)
        {
            if (file.IsValid && file.Content is null)
            {
                await eventsFileInput.ReadContentAsync(file);
            }
        }
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
<BitFileInput @ref=""eventsFileInput"" Label=""Select or drag and drop files"" OnChange=""@HandleOnChange"" />

<div>Selected files:</div>
@foreach (var file in selectedFiles)
{
    <div>
        @file.Name (@FileSizeHumanizer.Humanize(file.Size))
        @if (file.Content is not null)
        {
            <span> - @file.Content.Length bytes loaded</span>
        }
    </div>
}";
    private readonly string example8CsharpCode = @"
private BitFileInput eventsFileInput = default!;
private BitFileInputInfo[] selectedFiles = [];

private async Task HandleOnChange(BitFileInputInfo[] files)
{
    selectedFiles = files;

    foreach (var file in files)
    {
        if (file.IsValid && file.Content is null)
        {
            await eventsFileInput.ReadContentAsync(file);
        }
    }
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
