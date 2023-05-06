using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.FileUpload;

public partial class BitFileUploadDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Accept",
            Type = "string?",
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
            DefaultValue = "",
            Description = "Enables or disables the chunked upload feature."
        },
        new()
        {
            Name = "ChunkSize",
            Type = "long",
            DefaultValue = "10485760 (10 MB)",
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
            Type = "RenderFragment",
            DefaultValue = "null",
            Description = "A custom razor fragment for select button."
        },
        new()
        {
            Name = "MaxSize (byte)",
            Type = "long",
            DefaultValue = "0",
            Description = "Specifies the maximum size of the file (0 for unlimited)."
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
            DefaultValue = "null",
            Description = "Callback for when all files are uploaded."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInfo[]>",
            DefaultValue = "null",
            Description = "Callback for when file or files status change."
        },
        new()
        {
            Name = "OnProgress",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when the file upload is progressed."
        },
        new()
        {
            Name = "OnRemoveComplete",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a remove file is done."
        },
        new()
        {
            Name = "OnRemoveFailed",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a remove file is failed."
        },
        new()
        {
            Name = "OnUploadComplete",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a file upload is done."
        },
        new()
        {
            Name = "OnUploadFailed",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
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
            Type = "string",
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
            Type = "string",
            DefaultValue = "",
            Description = "URL of the server endpoint receiving the files."
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "uploadstatus-enum",
            Name = "BitFileUploadStatus",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
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
            }
        }
    };



    private string onAllUploadsCompleteText = "No File";
    private string ChunkedUploadUrl => $"{Configuration.GetApiServerAddress()}FileUpload/UploadChunkedFile";
    private string NonChunkedUploadUrl => $"{Configuration.GetApiServerAddress()}FileUpload/UploadNonChunkedFile";
    private string RemoveUrl => $"FileUpload/RemoveFile";

    [Inject] public IConfiguration Configuration { get; set; }


    private readonly string example1HtmlCode = @"
<BitFileUpload Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";
    private readonly string example1CSharpCode = @"
private string UploadUrl = $""/Upload"";
";

    private readonly string example2HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";
    private readonly string example2CSharpCode = @"
private string UploadUrl = $""/Upload"";
private string RemoveUrl = $""/Remove"";
";

    private readonly string example3HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               MaxSize=""1024 * 1024 * 100""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";

    private readonly string example4HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""false""
               AllowedExtensions=""@(new List<string> { "".gif"","".jpg"","".mp4"" })""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";

    private readonly string example5HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
            Label=""Select or drag and drop files""
            UploadUrl=""@UploadUrl""
            RemoveUrl=""@RemoveUrl""
            ShowRemoveButton=""true"">
</BitFileUpload>
";

    private readonly string example6HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               OnAllUploadsComplete=""@(() => onAllUploadsCompleteText = ""All File Uploaded"")""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";

    private readonly string example7HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl""
               UploadRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header1"", ""value1"" } })""
               UploadRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs1"", ""qsValue1"" } })""
               RemoveUrl=""@RemoveUrl""
               RemoveRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header2"", ""value2"" } })""
               RemoveRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs2"", ""qsValue2"" } })"">
</BitFileUpload>
";

    private readonly string example8HtmlCode = @"
<BitFileUpload Label=""Select or drag and drop files""
               ChunkedUploadEnabled=""false""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";
}
