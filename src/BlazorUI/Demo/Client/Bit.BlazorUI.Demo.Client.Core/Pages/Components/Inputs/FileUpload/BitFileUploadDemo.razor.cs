﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileUpload;

public partial class BitFileUploadDemo
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
            DefaultValue = "new List<string> { \"*\" }",
            Description = "Filters files by extension.",
        },
        new()
        {
            Name = "AutoChunkSizeEnabled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB."
        },
        new()
        {
            Name = "AutoUploadEnabled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically starts the upload file(s) process immediately after selecting the file(s)."
        },
        new()
        {
            Name = "ChunkedUploadEnabled",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables or disables the chunked upload feature."
        },
        new()
        {
            Name = "ChunkSize",
            Type = "long?",
            DefaultValue = "null",
            Description = "The size of each chunk of file upload in bytes."
        },
        new()
        {
            Name = "IsMultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables multi-file select & upload."
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
            Name = "MaxSizeErrorMessage",
            Type = "string",
            DefaultValue = "The file size is larger than the max size",
            Description = "Specifies the message for the failed uploading progress due to exceeding the maximum size."
        },
        new()
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string",
            DefaultValue = "The file type is not allowed",
            Description = "Specifies the message for the failed uploading progress due to the allowed extensions."
        },
        new()
        {
            Name = "OnAllUploadsComplete",
            Type = "EventCallback<BitFileInfo[]>",
            Description = "Callback for when all files are uploaded."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInfo[]>",
            Description = "Callback for when file or files status change."
        },
        new()
        {
            Name = "OnProgress",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when the file upload is progressed."
        },
        new()
        {
            Name = "OnRemoveComplete",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a remove file is done."
        },
        new()
        {
            Name = "OnRemoveFailed",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a remove file is failed."
        },
        new()
        {
            Name = "OnUploading",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a file upload is about to start."
        },
        new()
        {
            Name = "OnUploadComplete",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a file upload is done."
        },
        new()
        {
            Name = "OnUploadFailed",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when an upload file is failed."
        },
        new()
        {
            Name = "RemoveRequestHttpHeaders",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom http headers for remove request."
        },
        new()
        {
            Name = "RemoveRequestQueryStrings",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom query strings for remove request."
        },
        new()
        {
            Name = "RemoveUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL of the server endpoint removing the files."
        },
        new()
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "URL of the server endpoint removing the files."
        },
        new()
        {
            Name = "SuccessfulUploadMessage",
            Type = "string",
            DefaultValue = "File upload successful",
            Description = "The message shown for successful file uploads."
        },
        new()
        {
            Name = "FailedUploadMessage",
            Type = "string",
            DefaultValue = "File upload failed",
            Description = "The message shown for failed file uploads."
        },
        new()
        {
            Name = "FailedRemoveMessage",
            Type = "string",
            DefaultValue = "File remove failed",
            Description = "The message shown for failed file removes."
        },
        new()
        {
            Name = "UploadRequestHttpHeaders",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom http headers for upload request."
        },
        new()
        {
            Name = "UploadRequestQueryStrings",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom query strings for upload request."
        },
        new()
        {
            Name = "UploadUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL of the server endpoint receiving the files."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "nav-class-styles",
            Title = "BitFileInfo",
            Parameters = new()
            {
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
                   Name = "LastChunkUploadedSize",
                   Type = "long",
                   Description = "The size of the last uploaded chunk of the file."
               },
               new()
               {
                   Name = "TotalUploadedSize",
                   Type = "long",
                   Description = "The total uploaded size of the file."
               },
               new()
               {
                   Name = "Message",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The error message is issued during file validation before uploading the file or at the time of uploading."
               },
               new()
               {
                   Name = "Status",
                   Type = "BitFileUploadStatus",
                   DefaultValue = "Pending",
                   Description = "The status of the file in the BitFileUpload.",
                   LinkType = LinkType.Link,
                   Href = "#uploadstatus-enum"
               },
               new()
               {
                   Name = "HttpHeaders",
                   Type = "IReadOnlyDictionary<string, string>?",
                   DefaultValue = "null",
                   Description = "The HTTP header at upload file."
               }
            }
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "uploadstatus-enum",
            Name = "BitFileUploadStatus",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Pending",
                    Description = "File uploading progress is pended because the server cannot be contacted.",
                    Value = "0",
                },
                new()
                {
                    Name = "InProgress",
                    Description = "File uploading is in progress.",
                    Value = "1",
                },
                new()
                {
                    Name = "Paused",
                    Description = "File uploading progress is paused by the user.",
                    Value = "2",
                },
                new()
                {
                    Name = "Canceled",
                    Description = "File uploading progress is canceled by the user.",
                    Value = "3",
                },
                new()
                {
                    Name = "Completed",
                    Description = "The file is successfully uploaded.",
                    Value = "4",
                },
                new()
                {
                    Name = "Failed",
                    Description = "The file has a problem and progress is failed.",
                    Value = "5",
                },
                new()
                {
                    Name = "Removed",
                    Description = "The uploaded file removed by the user.",
                    Value = "6",
                },
                new()
                {
                    Name = "RemoveFailed",
                    Description = "The file removal failed.",
                    Value = "7",
                },
                new()
                {
                    Name = "NotAllowed",
                    Description = "The type of uploaded file is not allowed.",
                    Value = "8",
                }
            ]
        }
    ];



    [Inject] private IJSRuntime _js { get; set; } = default!;
    [Inject] private IConfiguration _configuration { get; set; } = default!;

    private string onAllUploadsCompleteText = "No File";
    private string ChunkedUploadUrl => $"{_configuration.GetApiServerAddress()}FileUpload/UploadChunkedFile";
    private string NonChunkedUploadUrl => $"{_configuration.GetApiServerAddress()}FileUpload/UploadNonChunkedFile";
    private string RemoveUrl => $"{_configuration.GetApiServerAddress()}FileUpload/RemoveFile";

    private BitFileUpload bitFileUpload = default!;
    private BitFileUpload bitFileUploadWithBrowseFile = default!;

    private bool FileUploadIsEmpty() => !bitFileUpload.Files?.Any(f => f.Status != BitFileUploadStatus.Removed) ?? true;

    private async Task HandleUploadOnClick()
    {
        if (bitFileUpload.Files is null) return;

        await bitFileUpload.Upload();
    }

    private async Task HandleRemoveOnClick()
    {
        if (bitFileUpload.Files is null) return;

        await bitFileUpload.RemoveFile();
    }

    private static int GetFileUploadPercent(BitFileInfo file)
    {
        int uploadedPercent;
        if (file.TotalUploadedSize >= file.Size)
        {
            uploadedPercent = 100;
        }
        else
        {
            uploadedPercent = (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100);
        }

        return uploadedPercent;
    }

    private static string GetFileUploadSize(BitFileInfo file)
    {
        long totalSize = file.Size / 1024;
        long uploadSize;
        if (file.TotalUploadedSize >= file.Size)
        {
            uploadSize = totalSize;
        }
        else
        {
            uploadSize = (file.TotalUploadedSize + file.LastChunkUploadedSize) / 1024;
        }

        return $"{uploadSize}KB / {totalSize}KB";
    }

    private string GetUploadMessageStr(BitFileInfo file) => file.Status switch
    {
        BitFileUploadStatus.Completed => bitFileUpload.SuccessfulUploadMessage,
        BitFileUploadStatus.Failed => bitFileUpload.FailedUploadMessage,
        BitFileUploadStatus.NotAllowed => IsFileTypeNotAllowed(file) ? bitFileUpload.NotAllowedExtensionErrorMessage : bitFileUpload.MaxSizeErrorMessage,
        _ => string.Empty,
    };

    private bool IsFileTypeNotAllowed(BitFileInfo file)
    {
        if (bitFileUpload.Accept is not null) return false;

        var fileSections = file.Name.Split('.');
        var extension = $".{fileSections?.Last()}";
        return bitFileUpload.AllowedExtensions.Count > 0 && bitFileUpload.AllowedExtensions.All(ext => ext != "*") && bitFileUpload.AllowedExtensions.All(ext => ext != extension);
    }

    private async Task HandleBrowseFileOnClick()
    {
        await bitFileUploadWithBrowseFile.Browse();
    }



    private readonly string example1RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@ChunkedUploadUrl"" MaxSize=""1024 * 1024 * 500"" />";
    private readonly string example1CsharpCode = @"
private string UploadUrl = $""/Upload"";";

    private readonly string example2RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"" />";
    private readonly string example2CsharpCode = @"
private string UploadUrl = $""/Upload"";
private string RemoveUrl = $""/Remove"";";

    private readonly string example3RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               MaxSize=""1024 * 1024 * 100""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"" />";

    private readonly string example4RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""false""
               AllowedExtensions=""@(new List<string> { "".gif"","".jpg"","".mp4"" })""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"" />";

    private readonly string example5RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl""
               RemoveUrl=""@RemoveUrl""
               ShowRemoveButton=""true"" />";

    private readonly string example6RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               MaxSize=""1024 * 1024 * 500"" 
               UploadUrl=""@ChunkedUploadUrl""
               Label=""Select or drag and drop files""
               OnAllUploadsComplete=""@(() => onAllUploadsCompleteText = ""All File Uploaded"")""
               OnUploading=""@(info => info.HttpHeaders = new Dictionary<string, string> { {""key1"", ""value1""} })"" />";

    private readonly string example7RazorCode = @"
<BitFileUpload IsMultiSelect=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl""
               UploadRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header1"", ""value1"" } })""
               UploadRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs1"", ""qsValue1"" } })""
               RemoveUrl=""@RemoveUrl""
               RemoveRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header2"", ""value2"" } })""
               RemoveRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs2"", ""qsValue2"" } })"" />";

    private readonly string example8RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files""
               ChunkedUploadEnabled=""false""
               UploadUrl=""@UploadUrl"" />";

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

    .file-info {
        display: flex;
        justify-content: space-between;
    }

    .file-info-name {
        overflow: hidden;
        margin-right: 10px;
    }

    .file-info-title {
        color: #5A5A5F;
        line-height: 22px;
        display: flex;
        justify-content: space-between;
    }

    .file-info-subtitle {
        color: #909096;
    }

    .file-info-ico {
        border: 1px solid #F3F3F8;
        border-radius: 2px;
        background-color: #F3F3F8;
        width: 80px;
        height: 80px;
        display: flex;
        justify-content: center;
        align-items: center;
    }

    .file-info-ico i {
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

    .file-info-btns .upload-ico {
        color: #0072CE;
    }

    .file-info-btns .remove-ico {
        color: #F9423A;
    }

    .file-info-progressbar-container {
        width: 100%;
        overflow: hidden;
        height: 2px;
        margin-top: 24px;
        background-color: #D9D9D9;
    }

    .file-info-progressbar {
        height: 2px;
        transition: width 0.15s linear 0s;
        background-color: #0072CE;
    }

    .file-info-s-msg {
        margin-top: 12px;
        color: #5EB227;
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


<BitFileUpload @ref=""bitFileUpload""
               Label=""""
               UploadUrl=""@NonChunkedUploadUrl""
               RemoveUrl=""@RemoveUrl""
               MaxSize=""1024 * 1024 * 2""
               AllowedExtensions=""@(new List<string> { "".jpeg"", "".jpg"", "".png"", "".bpm"" })""
               SuccessfulUploadMessage=""File upload succeeded""
               NotAllowedExtensionErrorMessage=""File type not supported"">
    <LabelTemplate>
        @if (FileUploadIsEmpty())
        {
            <div class=""browse-file"">
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
        @if (file.Status != BitFileUploadStatus.Removed)
        {
            <div class=""file-list"">
                <div class=""file-info"">
                    <div class=""file-info-ico"">
                        <i class=""bit-icon bit-icon--FileImage"" />
                    </div>
                    <div class=""file-info-data"">
                        <div class=""file-info-title"">
                            <div class=""file-info-name"">@file.Name</div>
                            <div class=""file-info-btns"">
                                <label for=""@bitFileUpload.InputId""><i class=""bit-icon bit-icon--CloudUpload upload-ico"" /></label>
                                <i class=""bit-icon bit-icon--ChromeClose remove-ico"" @onclick=""HandleRemoveOnClick"" />
                            </div>
                        </div>
                        @if (file.Status is BitFileUploadStatus.InProgress or BitFileUploadStatus.Pending)
                        {
                            var fileUploadPercent = GetFileUploadPercent(file);
                            <div class=""file-info-subtitle"">@GetFileUploadSize(file) - @fileUploadPercent%</div>
                            <div class=""file-info-progressbar-container"">
                                <div class=""file-info-progressbar"" role=""progressbar"" style=""width:@fileUploadPercent%;"" aria-valuemin=""0"" aria-valuemax=""100"" aria-valuenow=""@fileUploadPercent""></div>
                            </div>
                        }
                        else
                        {
                            <div class=""@(file.Status == BitFileUploadStatus.Completed ? ""file-info-s-msg"" : ""file-info-e-msg"")"">@GetUploadMessageStr(file)</div>
                        }
                    </div>
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
</BitFileUpload>

<BitButton OnClick=""HandleUploadOnClick"">Upload</BitButton>";
    private readonly string example9CsharpCode = @"
[Inject] public IJSRuntime JSRuntime { get; set; } = default!;

private string NonChunkedUploadUrl => ""FileUpload/UploadNonChunkedFile"";
private string RemoveUrl => $""FileUpload/RemoveFile"";

private BitFileUpload bitFileUpload;
private bool FileUploadIsEmpty() => !bitFileUpload.Files?.Any(f => f.Status != BitFileUploadStatus.Removed) ?? true;

private async Task HandleUploadOnClick()
{
    if (bitFileUpload.Files is null) return;

    await bitFileUpload.Upload();
}

private async Task HandleRemoveOnClick()
{
    if (bitFileUpload.Files is null) return;

    await bitFileUpload.RemoveFile();
}

private static int GetFileUploadPercent(BitFileInfo file)
{
    int uploadedPercent;
    if (file.TotalUploadedSize >= file.Size)
    {
        uploadedPercent = 100;
    }
    else
    {
        uploadedPercent = (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100);
    }

    return uploadedPercent;
}

private static string GetFileUploadSize(BitFileInfo file)
{
    long totalSize = file.Size / 1024;
    long uploadSize;
    if (file.TotalUploadedSize >= file.Size)
    {
        uploadSize = totalSize;
    }
    else
    {
        uploadSize = (file.TotalUploadedSize + file.LastChunkUploadedSize) / 1024;
    }

    return $""{uploadSize}KB / {totalSize}KB"";
}

private string GetUploadMessageStr(BitFileInfo file) => file.Status switch
{
    BitFileUploadStatus.Completed => bitFileUpload.SuccessfulUploadMessage,
    BitFileUploadStatus.Failed => bitFileUpload.FailedUploadMessage,
    BitFileUploadStatus.NotAllowed => IsFileTypeNotAllowed(file) ? bitFileUpload.NotAllowedExtensionErrorMessage : bitFileUpload.MaxSizeErrorMessage,
    _ => string.Empty,
};

private bool IsFileTypeNotAllowed(BitFileInfo file)
{
    if (bitFileUpload.Accept is not null) return false;

    var fileSections = file.Name.Split('.');
    var extension = $"".{fileSections?.Last()}"";
    return bitFileUpload.AllowedExtensions.Count > 0 && bitFileUpload.AllowedExtensions.All(ext => ext != ""*"") && bitFileUpload.AllowedExtensions.All(ext => ext != extension);
}";

    private readonly string example10RazorCode = @"
<BitFileUpload @ref=""bitFileUploadWithBrowseFile""
               Label=""""
               UploadUrl=""@NonChunkedUploadUrl""
               RemoveUrl=""@RemoveUrl""
               MaxSize=""1024 * 1024 * 500"" />

<BitButton OnClick=""HandleBrowseFileOnClick"">Browse file</BitButton>";
    private readonly string example10CsharpCode = @"
private string NonChunkedUploadUrl = ""/Upload"";
private string RemoveUrl => ""/RemoveFile"";
private BitFileUpload bitFileUploadWithBrowseFile;

private async Task HandleBrowseFileOnClick()
{
    await bitFileUploadWithBrowseFile.Browse();
}";
}
