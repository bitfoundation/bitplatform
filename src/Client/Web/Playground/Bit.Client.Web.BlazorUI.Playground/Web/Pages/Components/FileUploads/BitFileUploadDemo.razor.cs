using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.FileUploads
{
    public partial class BitFileUploadDemo
    {
        private string UploadUrl = "/FileUpload/UploadStreamedFile";
        private string RemoveUrl = "/FileUpload/RemoveFile";

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "acceptedExtensions",
                Type = "IReadOnlyCollection<string>",
                DefaultValue = "*",
                Description = "Filters files by extension.",
            },
            new ComponentParameter()
            {
                Name = "autoUploadEnabled",
                Type = "bool",
                DefaultValue = "false",
                Description = "Uploads immediately after selecting the files.",
            },
            new ComponentParameter()
            {
                Name = "chunkSize",
                Type = "long",
                DefaultValue = "",
                Description = "Upload is done in the form of chunks and this property shows the progress of upload in each chunk.",
            },
            new ComponentParameter()
            {
                Name = "failedUploadedResultMessage",
                Type = "string",
                DefaultValue = "Uploading failed",
                Description = "Filters files by extension.",
            },
            new ComponentParameter()
            {
                Name = "files",
                Type = "IReadOnlyList<BitFileInfo>",
                DefaultValue = "",
                Description = "All selected files.",
            },
            new ComponentParameter()
            {
                Name = "fileCount",
                Type = "int",
                DefaultValue = "0",
                Description = "Total count of files uploaded.",
            },
            new ComponentParameter()
            {
                Name = "isMultiFile",
                Type = "bool",
                DefaultValue = "false",
                Description = "Single is false or multiple is true files upload.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "Browse",
                Description = "Custom label for browse button.",
            },
            new ComponentParameter()
            {
                Name = "maxSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Specifies the maximum size of the file.",
            },
            new ComponentParameter()
            {
                Name = "maxSizeMessage",
                Type = "string",
                DefaultValue = "File size is too large",
                Description = "Specifies the message for the failed uploading progress due to exceeding the maximum size.",
            },
            new ComponentParameter()
            {
                Name = "successfulUploadedResultMessage",
                Type = "string",
                DefaultValue = "File uploaded",
                Description = "Custom label for Failed Status.",
            },
            new ComponentParameter()
            {
                Name = "totalSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Total size of files.",
            },
            new ComponentParameter()
            {
                Name = "uploadUrl",
                Type = "string",
                DefaultValue = "",
                Description = "URL of the server endpoint receiving the files.",
            },
            new ComponentParameter()
            {
                Name = "uploadedSize",
                Type = "long",
                DefaultValue = "0",
                Description = "Total size of uploaded files.",
            },
            new ComponentParameter()
            {
                Name = "uploadStatus",
                Type = "BitUploadStatus",
                LinkType = LinkType.Link,
                Href = "#uploadstatus-enum",
                DefaultValue = "BitUploadStatus.pending",
                Description = "General upload status.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "uploadstatus-enum",
                Title = "uploadstatus-enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "pending",
                        Description="File uploading progress is pended because the server cannot be contacted",
                        Value="pending = 0",
                    },
                    new EnumItem()
                    {
                        Name= "inProgress",
                        Description="File uploading is in progress",
                        Value="inProgress = 1",
                    },
                    new EnumItem()
                    {
                        Name= "paused",
                        Description="File uploading progress is paused by the user",
                        Value="paused = 2",
                    },
                    new EnumItem()
                    {
                        Name= "canceled",
                        Description="File uploading progress is canceled by the user",
                        Value="canceled = 3",
                    },
                    new EnumItem()
                    {
                        Name= "completed",
                        Description="The file is successfully uploaded",
                        Value="completed = 4",
                    },
                    new EnumItem()
                    {
                        Name= "failed",
                        Description="The file has a problem and progress is failed",
                        Value="failed = 5",
                    },
                    new EnumItem()
                    {
                        Name= "removed",
                        Description="The uploaded file removed by the user",
                        Value="removed = 6",
                    },
                    new EnumItem()
                    {
                        Name= "unaccepted",
                        Description="The type of uploaded file is not acceptable",
                        Value="unaccepted = 7",
                    }
                }
            }
        };
    }
}
