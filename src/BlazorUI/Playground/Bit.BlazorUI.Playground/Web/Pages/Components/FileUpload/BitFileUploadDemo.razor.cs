using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.FileUpload;

public partial class BitFileUploadDemo
{
    private string onAllUploadsCompleteText = "No File";

    string UploadUrl => $"{GetBaseUrl()}FileUpload/UploadStreamedFile";
    string RemoveUrl => $"{GetBaseUrl()}FileUpload/RemoveFile";

    [Inject] public IConfiguration Configuration { get; set; }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AllowedExtensions",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "new List<string> { \"*\" }",
            Description = "Filters files by extension.",
        },
        new ComponentParameter
        {
            Name = "AutoChunkSizeEnabled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB."
        },
        new ComponentParameter
        {
            Name = "AutoUploadEnabled",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically starts the upload file(s) process immediately after selecting the file(s)."
        },
        new ComponentParameter
        {
            Name = "ChunkSize",
            Type = "long",
            DefaultValue = "10485760 (10 MB)",
            Description = "The size of each chunk of file upload in bytes."
        },
        new ComponentParameter
        {
            Name = "IsMultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables multi-file select & upload."
        },
        new ComponentParameter
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "Browse",
            Description = "The text of select file button."
        },
        new ComponentParameter
        {
            Name = "LabelFragment",
            Type = "RenderFragment",
            DefaultValue = "null",
            Description = "A custom razor fragment for select button."
        },
        new ComponentParameter
        {
            Name = "MaxSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Specifies the maximum size of the file (0 for unlimited)."
        },
        new ComponentParameter
        {
            Name = "MaxSizeErrorMessage",
            Type = "string",
            DefaultValue = "The file size is larger than the max size",
            Description = "Specifies the message for the failed uploading progress due to exceeding the maximum size."
        },
        new ComponentParameter
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string",
            DefaultValue = "The file type is not allowed",
            Description = "Specifies the message for the failed uploading progress due to the allowed extensions."
        },
        new ComponentParameter
        {
            Name = "OnAllUploadsComplete",
            Type = "EventCallback<BitFileInfo[]>",
            DefaultValue = "null",
            Description = "Callback for when all files are uploaded."
        },
        new ComponentParameter
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInfo[]>",
            DefaultValue = "null",
            Description = "Callback for when file or files status change."
        },
        new ComponentParameter
        {
            Name = "OnProgress",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when the file upload is progressed."
        },
        new ComponentParameter
        {
            Name = "OnRemoveComplete",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a remove file is done."
        },
        new ComponentParameter
        {
            Name = "OnRemoveFailed",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a remove file is failed."
        },
        new ComponentParameter
        {
            Name = "OnUploadComplete",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when a file upload is done."
        },
        new ComponentParameter
        {
            Name = "OnUploadFailed",
            Type = "EventCallback<BitFileInfo>",
            DefaultValue = "null",
            Description = "Callback for when an upload file is failed."
        },
        new ComponentParameter
        {
            Name = "RemoveRequestHttpHeaders",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom http headers for remove request."
        },
        new ComponentParameter
        {
            Name = "RemoveRequestQueryStrings",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom query strings for remove request."
        },
        new ComponentParameter
        {
            Name = "RemoveUrl",
            Type = "string",
            DefaultValue = "null",
            Description = "URL of the server endpoint removing the files."
        },
        new ComponentParameter
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "URL of the server endpoint removing the files."
        },
        new ComponentParameter
        {
            Name = "SuccessfullUploadMessage",
            Type = "string",
            DefaultValue = "File upload succesfull",
            Description = "The message shown for successful file uploads."
        },
        new ComponentParameter
        {
            Name = "FailedUploadMessage",
            Type = "string",
            DefaultValue = "File upload failed",
            Description = "The message shown for failed file uploads."
        },
        new ComponentParameter
        {
            Name = "UploadRequestHttpHeaders",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom http headers for upload request."
        },
        new ComponentParameter
        {
            Name = "UploadRequestQueryStrings",
            Type = "IReadOnlyDictionary<string, string>",
            DefaultValue = "new Dictionary<string, string>()",
            Description = "Custom query strings for upload request."
        },
        new ComponentParameter
        {
            Name = "UploadUrl",
            Type = "string",
            DefaultValue = "",
            Description = "URL of the server endpoint receiving the files."
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "uploadstatus-enum",
            Title = "BitFileUploadStatus Enum",
            Description = "",
            EnumList = new List<EnumItem>()
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

    private readonly string example1CSharpCode = @"
private string UploadUrl = $""/Upload"";
";

    private readonly string example2CSharpCode = @"
private string UploadUrl = $""/Upload"";
private string RemoveUrl = $""/Remove"";
";

    private readonly string example1HtmlCode = @"
<BitFileUpload Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
";

    private readonly string example2HtmlCode = @"
<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>
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

}
