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
        private static readonly HttpClient client = new();
        private DotNetObjectReference<BitFileUpload>? dotnetObjectReference;
        private ElementReference inputFileElement;

        [Inject] protected IJSRuntime? JSRuntime { get; set; }

        /// <summary>
        /// URL of the server endpoint receiving the files.
        /// </summary>
        [Parameter] public string? UploadUrl { get; set; }

        /// <summary>
        /// URL of the server endpoint removing the files.
        /// </summary>
        [Parameter] public string? RemoveUrl { get; set; }

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
        [Parameter] public string SuccessfulUploadedResultMessage { get; set; } = "File uploaded";

        /// <summary>
        /// Custom label for Failed Status.
        /// </summary>
        [Parameter] public string FailedUploadedResultMessage { get; set; } = "Uploading failed";

        /// <summary>
        /// Filters files by extension.
        /// </summary>
        [Parameter] public IReadOnlyCollection<string> AcceptedExtensions { get; set; } = new List<string> { "*" };

        /// <summary>
        /// Single <c>false</c> or multiple <c>true</c> files upload.
        /// </summary>
        [Parameter] public bool IsMultiFile { get; set; }

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
        [Parameter] public string MaxSizeMessage { get; set; } = "File size is too large";

        /// <summary>
        /// Total count of files uploaded.
        /// </summary>
        public int FileCount => Files?.Count ?? 0;

        /// <summary>
        /// Total size of files.
        /// </summary>
        public long TotalSize => Files?.Sum(c => c.Size) ?? 0;

        /// <summary>
        /// Total size of uploaded files.
        /// </summary>
        public long UpLoadedSize { get; set; }

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

        /// <summary>
        /// Select file(s) by browse button or drag and drop.
        /// </summary>
        /// <returns></returns>
        public async Task HandleOnChange()
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
        public async Task Upload(int index = -1)
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

        protected override Task OnInitializedAsync()
        {
            InputId = $"{UniqueId}FileInput";
            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-file-upload";

        private async Task UploadOneFile(int index)
        {
            if (JSRuntime is null || Files is null) return;
            if (Files[index].UploadStatus == BitUploadStatus.Unaccepted) return;

            var uploadedSize = Files[index].ChunkesUpLoadedSize.Sum();
            if (Files[index].Size != 0 && uploadedSize >= Files[index].Size) return;

            if (MaxSize > 0 && Files[index].Size > MaxSize)
            {
                Files[index].UploadStatus = BitUploadStatus.Unaccepted;
                return;
            }

            if (Files[index].RequestToPause)
            {
                await PauseAsync(index);
                return;
            }

            if (Files[index].RequestToCancell)
            {
                await CancelAsync(index);
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

        private async Task PauseAsync(int index)
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
                Files.Where(c => c.UploadStatus != BitUploadStatus.Unaccepted).ToList().ForEach(c => c.UploadStatus = uploadStatus);
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

        private async Task CancelAsync(int index)
        {
            if (JSRuntime is null || Files is null) return;

            await JSRuntime.PauseFile(index);
            UpdateStatus(BitUploadStatus.Canceled, index);
            Files[index].RequestToCancell = false;
        }

        private async Task Remove(int index)
        {
            if (JSRuntime is null || Files is null) return;

            if (index < 0)
            {
                for (int i = 0; i < Files.Count; i++)
                {
                    await RemoveOneFileAsync(i);
                }
            }
            else
            {
                await RemoveOneFileAsync(index);
            }

            UpdateStatus(BitUploadStatus.Removed, index);
        }

        private async Task RemoveOneFileAsync(int index)
        {
            if (Files is null || RemoveUrl is null) return;

            var uri = new Uri($"{RemoveUrl}?fileName={Files[index].Name}");
            _ = await client.GetAsync(uri);
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
