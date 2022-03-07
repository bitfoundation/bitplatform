using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.FileUpload
{
    public partial class BitFileUploadDemo
    {
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
                Name = "AcceptedExtensions",
                Type = "IReadOnlyCollection<string>",
                DefaultValue = "*",
                Description = "Filters files by extension.",
            },
            new ComponentParameter
            {
                Name = "AutoUploadEnabled",
                Type = "bool",
                DefaultValue = "true",
                Description = "Uploads immediately after selecting the files."
            },
            new ComponentParameter
            {
                Name = "ChunkSize",
                Type = "long",
                DefaultValue = "10485760",
                Description = "Upload is done in the form of chunks and this property shows the progress of upload in each chunk."
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
                Name = "FailedUploadedResultMessage",
                Type = "string",
                DefaultValue = "Uploading failed",
                Description = "Filters files by extension."
            },
            new ComponentParameter
            {
                Name = "Files",
                Type = "IReadOnlyList<BitFileInfo>",
                DefaultValue = "",
                Description = "All selected files."
            },
            new ComponentParameter
            {
                Name = "FileCount",
                Type = "int",
                DefaultValue = "0",
                Description = "Total count of files uploaded."
            },
            new ComponentParameter
            {
                Name = "IsMultiFile",
                Type = "bool",
                DefaultValue = "false",
                Description = "Single is false or multiple is true files upload."
            },
            new ComponentParameter
            {
                Name = "Label",
                Type = "string",
                DefaultValue = "Browse",
                Description = "Custom label for browse button."
            },
            new ComponentParameter
            {
                Name = "MaxSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Specifies the maximum size of the file."
            },
            new ComponentParameter
            {
                Name = "MaxSizeMessage",
                Type = "string",
                DefaultValue = "File size is too large",
                Description = "Specifies the message for the failed uploading progress due to exceeding the maximum size."
            },
            new ComponentParameter
            {
                Name = "SuccessfulUploadedResultMessage",
                Type = "string",
                DefaultValue = "File uploaded",
                Description = "Custom label for Failed Status."
            },
            new ComponentParameter
            {
                Name = "TotalSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Total size of files."
            },
            new ComponentParameter
            {
                Name = "UploadUrl",
                Type = "string",
                DefaultValue = "",
                Description = "URL of the server endpoint receiving the files."
            },
            new ComponentParameter
            {
                Name = "UploadedSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Total size of uploaded files."
            },
            new ComponentParameter
            {
                Name = "UploadStatus",
                Type = "BitUploadStatus",
                LinkType = LinkType.Link,
                Href = "#uploadstatus-enum",
                DefaultValue = "BitUploadStatus.Pending",
                Description = "General upload status.",
            },
            new ComponentParameter
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "uploadstatus-enum",
                Title = "BitUploadStatus Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new()
                    {
                        Name= "Pending",
                        Description="File uploading progress is pended because the server cannot be contacted.",
                        Value="0",
                    },
                    new()
                    {
                        Name= "InProgress",
                        Description="File uploading is in progress.",
                        Value="1",
                    },
                    new()
                    {
                        Name= "Paused",
                        Description="File uploading progress is paused by the user.",
                        Value="2",
                    },
                    new()
                    {
                        Name= "Canceled",
                        Description="File uploading progress is canceled by the user.",
                        Value="3",
                    },
                    new()
                    {
                        Name= "Completed",
                        Description="The file is successfully uploaded.",
                        Value="4",
                    },
                    new()
                    {
                        Name= "Failed",
                        Description="The file has a problem and progress is failed.",
                        Value="5",
                    },
                    new()
                    {
                        Name= "Removed",
                        Description="The uploaded file removed by the user.",
                        Value="6",
                    },
                    new()
                    {
                        Name= "Unaccepted",
                        Description="The type of uploaded file is not acceptable.",
                        Value="7",
                    }
                }
            },
            new EnumParameter()
            {
                Id = "component-visibility-enum",
                Title = "BitComponentVisibility Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new()
                    {
                        Name= "Visible",
                        Description="Show content of the component.",
                        Value="0",
                    },
                    new()
                    {
                        Name= "Hidden",
                        Description="Hide content of the component,though the space it takes on the page remains.",
                        Value="1",
                    },
                    new()
                    {
                        Name= "Collapsed",
                        Description="Hide content of the component,though the space it takes on the page gone.",
                        Value="2",
                    }
                }
            }
        };

        private readonly string example1CSharpCode = @"
private string UploadUrl;";

        private readonly string example2CSharpCode = @"
private string UploadUrl;
private string RemoveUrl;";

        private readonly string example1HtmlCode = @"<BitFileUpload Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>";

        private readonly string example2HtmlCode = @"<BitFileUpload IsMultiSelect=""true""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>";

        private readonly string example3HtmlCode = @"<BitFileUpload IsMultiSelect=""true""
               Label=""Select or drag and drop files""
               MaxSize=""1024 * 1024 * 100""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>";

        private readonly string example4HtmlCode = @"<BitFileUpload IsMultiSelect=""true""
               AutoUploadEnabled=""false""
               AllowedExtensions=""@(new List<string> { "".gif"","".jpg"","".mp4"" })""
               Label=""Select or drag and drop files""
               UploadUrl=""@UploadUrl"">
</BitFileUpload>";

        private readonly string example5HtmlCode = @"<BitFileUpload IsMultiSelect=""true""
            Label=""Select or drag and drop files""
            UploadUrl=""@UploadUrl""
            RemoveUrl=""@RemoveUrl""
            ShowRemoveButton=""true"">
</BitFileUpload>";
    }
}
