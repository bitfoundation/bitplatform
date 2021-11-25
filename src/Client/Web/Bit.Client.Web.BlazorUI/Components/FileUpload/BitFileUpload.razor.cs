using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    /// <summary>
    /// A component that wraps the HTML file input element and uploads them.
    /// </summary>
    public partial class BitFileUpload : IDisposable
    {
        private DotNetObjectReference<BitFileUpload>? dotnetObjectReference;
        private ElementReference inputFileElement;

        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Inject] public HttpClient? HttpClient { get; set; }

        /// <summary>
        /// URL of the server endpoint receiving the files.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        [Parameter] public string? UploadUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// URL of the server endpoint removing the files.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        [Parameter] public string? RemoveUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// label text for browse button.
        /// </summary>
        [Parameter] public string Label { get; set; } = "Browse";

        /// <summary>
        /// Custom label for browse button.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Custom label for Uploaded Status.
        /// </summary>
        [Parameter] public string SuccessfullyUploadedMessage { get; set; } = "File uploaded";

        /// <summary>
        /// Custom label for Failed Status.
        /// </summary>
        [Parameter] public string UploadingFailedMessage { get; set; } = "Uploading failed";

        /// <summary>
        /// Filters files by extension.
        /// </summary>
        [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = new List<string> { "*" };

        /// <summary>
        /// Single <c>false</c> or multiple <c>true</c> files upload.
        /// </summary>
        [Parameter] public bool IsMultiSelect { get; set; } = false;

        /// <summary>
        /// Uploads immediately after selecting the files.
        /// </summary>
        [Parameter] public bool AutoUploadEnabled { get; set; } = true;

        /// <summary>
        /// Specifies the maximum size of the file.
        /// </summary>
        [Parameter] public long MaxSize { get; set; }

        /// <summary>
        /// Specifies the message for the failed uploading progress due to exceeding the maximum size.
        /// </summary>
        [Parameter] public string MaxSizeErrorMessage { get; set; } = "The file size is larger than the max size";

        /// <summary>
        /// Specifies the message for the failed uploading progress due to the allowed extentions.
        /// </summary>
        [Parameter] public string NotAllowedExtentionErrorMessage { get; set; } = "The file type is not allowed";

        /// <summary>
        /// General upload status.
        /// </summary>
        public BitUploadStatus UploadStatus { get; set; }

        /// <summary>
        /// Upload is done in the form of chunks and this property shows the progress of upload in each chunk.
        /// </summary>
        public static long ChunkSize => FileSizeHumanizer.OneMegaByte * 10;

        /// <summary>
        /// All selected files.
        /// </summary>
        public IReadOnlyList<BitFileInfo>? Files { get; private set; }

        public string InputId { get; set; } = string.Empty;

        protected override Task OnInitializedAsync()
        {
            InputId = $"{UniqueId}FileInput";
            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-file-upload";

        /// <summary>
        /// Select file(s) by browse button or drag and drop.
        /// </summary>
        /// <returns></returns>
        private async Task HandleOnChange()
        {
            if (JSRuntime is null || UploadUrl is null) return;
            dotnetObjectReference = DotNetObjectReference.Create(this);
            Files = await JSRuntime.InitUploader(inputFileElement, dotnetObjectReference, UploadUrl);

            if (AutoUploadEnabled)
            {
                await Upload();
            }
        }

        /// <summary>
        /// Uploads the file(s).
        /// </summary>
        private async Task Upload(int index = -1)
        {
            if (JSRuntime is null || Files is null) return;

            if (UploadStatus != BitUploadStatus.InProgress)
            {
                UploadStatus = BitUploadStatus.InProgress;
            }

            UpdateStatus(BitUploadStatus.InProgress, index);
            if (index >= 0)
            {
                await UploadOneFile(index);
            }
            else
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    await UploadOneFile(i);
                }
            }
        }

        private async Task UploadOneFile(int index)
        {
            if (JSRuntime is null || Files is null) return;
            if (Files[index].UploadStatus == BitUploadStatus.NotAllowed) return;

            var uploadedSize = Files[index].ChunkesUpLoadedSize.Sum();
            if (Files[index].Size != 0 && uploadedSize >= Files[index].Size) return;

            if (MaxSize > 0 && Files[index].Size > MaxSize)
            {
                Files[index].UploadStatus = BitUploadStatus.NotAllowed;
                return;
            }

            if (IsFileTypeNotAllowed(Files[index]))
            {
                Files[index].UploadStatus = BitUploadStatus.NotAllowed;
                return;
            }

            if (Files[index].RequestToPause)
            {
                await PauseUpload(index);
                return;
            }

            if (Files[index].RequestToCancell)
            {
                await CancelUpload(index);
                return;
            }

            long from = ChunkSize * Files[index].ChunkCount;
            long to;
            if (Files[index].Size > ChunkSize)
            {
                to = ChunkSize * (Files[index].ChunkCount + 1);
            }
            else
            {
                to = Files[index].Size;
            }

            Files[index].ChunkesUpLoadedSize.Add(0);
            Files[index].ChunkCount += 1;

            await JSRuntime.UploadFile(from, to, index);
        }

        /// <summary>
        /// Pause upload.
        /// </summary>
        /// <param name="index">
        /// -1 => all files | else => specific file
        /// </param>
        /// <returns></returns>
        public void RequestToPause(int index = -1)
        {
            if (JSRuntime is null || Files is null) return;

            if (index < 0)
            {
                Files.ToList().ForEach(c => c.RequestToPause = true);
            }
            else
            {
                Files[index].RequestToPause = true;
            }
        }

        private async Task PauseUpload(int index)
        {
            if (JSRuntime is null || Files is null) return;

            await JSRuntime.PauseFile(index);
            UpdateStatus(BitUploadStatus.Paused, index);
            Files[index].RequestToPause = false;
        }

        /// <summary>
        /// Cancel upload.
        /// </summary>
        /// <param name="index">
        /// -1 => all files | else => specific file
        /// </param>
        /// <returns></returns>
        public void RequestToCancel(int index = -1)
        {
            if (JSRuntime is null || Files is null) return;

            if (index < 0)
            {
                Files.ToList().ForEach(c => c.RequestToCancell = true);
            }
            else
            {
                Files[index].RequestToCancell = true;
            }
        }

        /// <summary>
		/// Receive upload progress notification from underlying javascript.
		/// </summary>
		[JSInvokable("BitHandleUploadProgress")]
        public void HandleUploadProgress(int index, long loaded)
        {
            if (Files is null || Files[index].UploadStatus != BitUploadStatus.InProgress) return;

            Files[index].ChunkesUpLoadedSize[Files[index].ChunkCount - 1] = loaded;
            UpdateStatus(BitUploadStatus.InProgress, index);
            StateHasChanged();
        }

        /// <summary>
        /// Receive upload finished notification from underlying javascript.
        /// </summary>
        [JSInvokable("BitHandleFileUploaded")]
        public async Task HandleFileUploaded(int fileIndex, int responseStatus)
        {
            if (Files is null
                || UploadStatus == BitUploadStatus.Paused
                || Files[fileIndex].UploadStatus != BitUploadStatus.InProgress)
                return;

            if (Files[fileIndex].ChunkesUpLoadedSize.Sum() < Files[fileIndex].Size)
            {
                await Upload(index: fileIndex);
            }
            else
            {
                Files[fileIndex].UploadStatus = GetUploadStatus(responseStatus);
                var allFilesUploaded = Files.All(c => c.UploadStatus == BitUploadStatus.Completed || c.UploadStatus == BitUploadStatus.Failed);

                if (allFilesUploaded)
                {
                    UploadStatus = BitUploadStatus.Completed;
                }
            }

            StateHasChanged();
        }

        private void UpdateStatus(BitUploadStatus uploadStatus, int index)
        {
            if (Files is null) return;

            if (index < 0)
            {
                UploadStatus = uploadStatus;
                Files.Where(c => c.UploadStatus != BitUploadStatus.NotAllowed).ToList().ForEach(c => c.UploadStatus = uploadStatus);
            }
            else
            {
                if (Files[index].UploadStatus == uploadStatus) return;
                Files[index].UploadStatus = uploadStatus;
            }
        }

        private static string GetFileIcon(string fileName)
        {
            var fileSections = fileName?.Split('.')?.ToList();
            var extension = fileSections?.Last();

            return extension switch
            {
                "jpg" => "FileImage",
                "gif" => "FileImage",
                "png" => "FileImage",
                "bmp" => "FileImage",
                "webp" => "FileImage",

                "mp4" => "Video",
                "mov" => "Video",
                "wmv" => "Video",
                "avi" => "Video",
                "avchd" => "Video",
                "flv" => "Video",
                "f4v" => "Video",
                "swf" => "Video",
                "mkv" => "Video",
                "webm" => "Video",

                "zip" => "Zip",
                "rar" => "Zip",

                "pdf" => "PDF",
                "txt" => "InsertTextBox",

                _ => ""
            };
        }

        private static BitUploadStatus GetUploadStatus(int responseStatus)
        {
            return responseStatus >= 200 && responseStatus <= 299 ?
                    BitUploadStatus.Completed :
                    (responseStatus == 0 ? BitUploadStatus.Paused : BitUploadStatus.Failed);
        }

        private async Task CancelUpload(int index)
        {
            if (JSRuntime is null || Files is null) return;

            await JSRuntime.PauseFile(index);
            UpdateStatus(BitUploadStatus.Canceled, index);
            Files[index].RequestToCancell = false;
        }

        private async Task RemoveFile(int index)
        {
            if (JSRuntime is null || Files is null) return;

            if (index < 0)
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    await RemoveOneFile(i);
                }
            }
            else
            {
                await RemoveOneFile(index);
            }

            UpdateStatus(BitUploadStatus.Removed, index);
        }

        private async Task RemoveOneFile(int index)
        {
            if (Files is null || RemoveUrl is null || HttpClient is null) return;

            var url = $"{RemoveUrl}?fileName={Files[index].Name}";
#pragma warning disable CA2234 // Pass system uri objects instead of strings
            _ = await HttpClient.DeleteAsync(url);
#pragma warning restore CA2234 // Pass system uri objects instead of strings
        }

        private static string GetFileElClass(BitUploadStatus status)
        {
            switch (status)
            {
                case BitUploadStatus.Completed:
                    return "uploaded";
                case BitUploadStatus.Failed:
                case BitUploadStatus.NotAllowed:
                    return "failed";
                case BitUploadStatus.Paused:
                    return "paused";
                default:
                    return "in-progress";
            }
        }

        private string GetUploadMessageStr(BitFileInfo file)
        {
            switch (file.UploadStatus)
            {
                case BitUploadStatus.Completed:
                    return SuccessfullyUploadedMessage;
                case BitUploadStatus.Failed:
                    return UploadingFailedMessage;
                case BitUploadStatus.NotAllowed:
                    if (IsFileTypeNotAllowed(file))
                    {
                        return NotAllowedExtentionErrorMessage;
                    }
                    else
                    {
                        return MaxSizeErrorMessage;
                    }
                default:
                    return "";
            }
        }

        private bool IsFileTypeNotAllowed(BitFileInfo file)
        {
            var fileSections = file.Name?.Split('.')?.ToList();
            var extension = $".{fileSections?.Last()}";
            return AllowedExtensions.Count > 0 && !AllowedExtensions.Any(ext => ext == "*") && !AllowedExtensions.Any(ext => ext == extension);
        }

        private static int GetFileUploadPercent(BitFileInfo file)
        {
            var uploadedPercent = 0;
            if (file.ChunkesUpLoadedSize.Sum() >= file.Size)
            {
                uploadedPercent = 100;
            }
            else
            {
                uploadedPercent = (int)(file.ChunkesUpLoadedSize.Sum() / (float)file.Size * 100);
            }

            return uploadedPercent;
        }

        private static string GetFileUploadSize(BitFileInfo file)
        {
            long uploadedSize;
            if (file.ChunkesUpLoadedSize.Sum() >= file.Size)
            {
                uploadedSize = file.Size;
            }
            else
            {
                uploadedSize = file.ChunkesUpLoadedSize.Sum();
            }

            return uploadedSize.Humanize();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || dotnetObjectReference is null) return;
            dotnetObjectReference.Dispose();
            dotnetObjectReference = null;
        }
    }
}
