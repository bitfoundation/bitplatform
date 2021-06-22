using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    /// <summary>
    /// A component that wraps the HTML file input element and upload them.
    /// </summary>
    public partial class BitFileUpload : IDisposable
    {
        protected override string RootElementClass => "bit-fu";

        private static readonly HttpClient client = new();

        /// <summary>
        /// Refrence to input file element
        /// </summary>
        private ElementReference inputFileElement { get; set; }

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
        /// General upload status
        /// </summary>
        public UploadStatus UploadStatus { get; set; }

        /// <summary>
        /// Upload is done in the form of Chunks and this property shows the amount of upload in each Chunk
        /// </summary>
        public static long ChunkSize => FileSizeHumanizer.OneMegaByte * 10;

        /// <summary>
        /// All selected files
        /// </summary>
        public IReadOnlyList<BitFileInfo>? Files { get; private set; }

        /// <summary>
        /// Refrence to this component for js accessibility
        /// </summary>
        private DotNetObjectReference<BitFileUpload>? dotnetObjectReference;

        [Inject] protected IJSRuntime? JSRuntime { get; set; }

        /// <summary>
        /// URL of the server endpoint receiving the files.
        /// </summary>
        [Parameter] public Uri? UploadUrl { get; set; }

        /// <summary>
        /// URL of the server endpoint removing the files.
        /// </summary>
        [Parameter] public Uri? RemoveUrl { get; set; }

        /// <summary>
        /// Custom label for Browse button
        /// </summary>
        [Parameter] public string BrowseButtonLabel { get; set; } = "Browse";

        /// <summary>
        /// Custom label for Uploaded Status
        /// </summary>
        [Parameter] public string SuccessfulUploadedResultMessage { get; set; } = "File uploaded";

        /// <summary>
        /// Custom label for Failed Status
        /// </summary>
        [Parameter] public string FailedUploadedResultMessage { get; set; } = "Uploading failed";

        /// <summary>
        /// Filter files by extension
        /// </summary>
        [Parameter] public IReadOnlyCollection<string> AcceptedExtensions { get; set; } = new List<string> { "*" };

        /// <summary>
        /// Single <c>false</c> or multiple <c>true</c> files upload.
        /// </summary>
        [Parameter] public bool IsMultiFile { get; set; }

        /// <summary>
        /// If you want to upload immediately after selecting the files, you need to set this parameter true
        /// </summary>
        [Parameter] public bool AutoUploadEnabled { get; set; } = true;

        /// <summary>
        /// Select file(s) by brows button or drag and drop.
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

            if (UploadStatus != UploadStatus.InProgress)
            {
                UploadStatus = UploadStatus.InProgress;
            }
            UpdateStatus(UploadStatus.InProgress, index);
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

        /// <summary>
        /// Upload one file with specific index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task UploadOneFile(int index)
        {
            if (JSRuntime is null || Files is null) return;
            var uploadedSize = Files[index].ChunkesUpLoadedSize.Sum();
            if (uploadedSize >= Files[index].Size)
            {
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
        /// pause upload
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
            UpdateStatus(UploadStatus.Paused, index);
            Files[index].RequestToPause = false;
        }

        /// <summary>
        /// cancel upload
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

        private async Task CancelAsync(int index)
        {
            if (JSRuntime is null || Files is null) return;
            await JSRuntime.PauseFile(index);
            UpdateStatus(UploadStatus.Canceled, index);
            Files[index].RequestToCancell = false;
        }

        /// <summary>
        /// remove files
        /// </summary>
        /// <param name="index">
        /// -1 => all files | else => specific file
        /// </param>
        /// <returns></returns>
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
            UpdateStatus(UploadStatus.Removed, index);
        }

        private async Task RemoveOneFileAsync(int index)
        {
            if (Files is null || RemoveUrl is null) return;
            var uri = new Uri($"{RemoveUrl.AbsoluteUri}?fileName={Files[index].Name}");

            _ = await client.GetAsync(uri);
        }

        /// <summary>
		/// Receive upload progress notification from underlying javascript.
		/// </summary>
		[JSInvokable("BitHandleUploadProgress")]
        public void HandleUploadProgress(int index, long loaded)
        {
            if (Files is null || Files[index].UploadStatus != UploadStatus.InProgress) return;

            Files[index].ChunkesUpLoadedSize[Files[index].ChunkCount - 1] = loaded;
            UpdateStatus(UploadStatus.InProgress, index);
            StateHasChanged();
        }

        /// <summary>
        /// Receive upload finished notification from underlying javascript.
        /// </summary>
        [JSInvokable("BitHandleFileUploaded")]
        public async Task HandleFileUploaded(int fileIndex, int responseStatus)
        {
            if (Files is null
                || UploadStatus == UploadStatus.Paused
                || Files[fileIndex].UploadStatus != UploadStatus.InProgress)
                return;

            if (Files[fileIndex].ChunkesUpLoadedSize.Sum() < Files[fileIndex].Size)
            {
                await Upload(index: fileIndex);
            }
            else
            {
                Files[fileIndex].UploadStatus = GetUploadStatus(responseStatus);
                var allFilesUploaded = Files.All(c => c.UploadStatus == UploadStatus.Completed || c.UploadStatus == UploadStatus.Failed);

                if (allFilesUploaded)
                {
                    UploadStatus = UploadStatus.Completed;
                }
            }
            StateHasChanged();
        }

        /// <summary>
        /// Update files status
        /// </summary>
        /// <param name="uploadStatus"></param>
        /// <param name="index">
        ///  -1 => all files | else => specific file
        /// </param>
        private void UpdateStatus(UploadStatus uploadStatus, int index)
        {
            if (Files is null) return;
            if (index < 0)
            {
                UploadStatus = uploadStatus;
                Files.ToList().ForEach(c => c.UploadStatus = uploadStatus);
            }
            else
            {
                if (Files[index].UploadStatus == uploadStatus) return;
                Files[index].UploadStatus = uploadStatus;
            }
        }

        /// <summary>
        /// Get updload status from XHR response status
        /// </summary>
        /// <param name="responseStatus"></param>
        /// <returns></returns>
        private static UploadStatus GetUploadStatus(int responseStatus)
        {
            return responseStatus >= 200 && responseStatus <= 299 ?
                    UploadStatus.Completed :
                    (responseStatus == 0 ? UploadStatus.Paused : UploadStatus.Failed);
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
