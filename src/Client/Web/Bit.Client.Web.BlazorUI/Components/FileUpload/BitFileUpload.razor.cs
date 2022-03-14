using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
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
        private const int MIN_CHUNK_SIZE = 512 * 1024; // 512 kb
        private const int MAX_CHUNK_SIZE = 10 * 1024 * 1024; // 10 mb
        private DotNetObjectReference<BitFileUpload>? dotnetObjectReference;
        private ElementReference inputFileElement;

        [Inject] public IJSRuntime? JSRuntime { get; set; }

        [Inject] public HttpClient? HttpClient { get; set; }


        /// <summary>
        /// Filters files by extension.
        /// </summary>
        [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = new List<string> { "*" };

        /// <summary>
        /// Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB.
        /// </summary>
        [Parameter] public bool AutoChunkSizeEnabled { get; set; }

        /// <summary>
        /// Automatically starts the upload file(s) process immediately after selecting the file(s).
        /// </summary>
        [Parameter] public bool AutoUploadEnabled { get; set; }

        /// <summary>
        /// The size of each chunk of file upload in bytes.
        /// </summary>
        [Parameter] public long ChunkSize { get; set; } = FileSizeHumanizer.OneMegaByte * 10;

        /// <summary>
        /// Enables multi-file select & upload.
        /// </summary>
        [Parameter] public bool IsMultiSelect { get; set; }

        /// <summary>
        /// The text of select file button.
        /// </summary>
        [Parameter] public string Label { get; set; } = "Browse";

        /// <summary>
        /// A custom razor fragment for select button.
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        /// <summary>
        /// Specifies the maximum size of the file (0 for unlimited).
        /// </summary>
        [Parameter] public long MaxSize { get; set; }

        /// <summary>
        /// Specifies the message for the failed uploading progress due to exceeding the maximum size.
        /// </summary>
        [Parameter] public string MaxSizeErrorMessage { get; set; } = "The file size is larger than the max size";

        /// <summary>
        /// Specifies the message for the failed uploading progress due to the allowed extensions.
        /// </summary>
        [Parameter] public string NotAllowedExtensionErrorMessage { get; set; } = "The file type is not allowed";

        /// <summary>
        /// Callback for when all files are uploaded.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo[]> OnAllUploadsComplete { get; set; }

        /// <summary>
        /// Callback for when file or files status change.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo[]> OnChange { get; set; }

        /// <summary>
        /// Callback for when the file upload is progressed.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo> OnProgress { get; set; }

        /// <summary>
        /// Callback for when a remove file is done.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo> OnRemoveComplete { get; set; }

        /// <summary>
        /// Callback for when a remove file is failed.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo> OnRemoveFailed { get; set; }

        /// <summary>
        /// Callback for when a file upload is done.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo> OnUploadComplete { get; set; }

        /// <summary>
        /// Callback for when an upload file is failed.
        /// </summary>
        [Parameter] public EventCallback<BitFileInfo> OnUploadFailed { get; set; }

        /// <summary>
        /// Custom http headers for remove request.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<string, string> RemoveRequestHttpHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Custom query strings for remove request.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<string, string> RemoveRequestQueryStrings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// URL of the server endpoint removing the files.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        [Parameter] public string? RemoveUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

        /// <summary>
        /// Show/Hide after upload remove button.
        /// </summary>
        [Parameter] public bool ShowRemoveButton { get; set; }

        /// <summary>
        /// The message shown for successful file uploads.
        /// </summary>
        [Parameter] public string SuccessfullUploadMessage { get; set; } = "File upload succesfull";

        /// <summary>
        /// The message shown for failed file uploads.
        /// </summary>
        [Parameter] public string FailedUploadMessage { get; set; } = "File upload failed";

        /// <summary>
        /// Custom http headers for upload request.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<string, string> UploadRequestHttpHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Custom query strings for upload request.
        /// </summary>
        [Parameter] public IReadOnlyDictionary<string, string> UploadRequestQueryStrings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// General upload status.
        /// </summary>
        public BitFileUploadStatus UploadStatus { get; set; }

        /// <summary>
        /// URL of the server endpoint receiving the files.
        /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
        [Parameter] public string? UploadUrl { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings


        /// <summary>
        /// All selected files.
        /// </summary>
        public IReadOnlyList<BitFileInfo>? Files { get; private set; }

        /// <summary>
        /// The id of the input element
        /// </summary>
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

            if (dotnetObjectReference != null)
            {
                dotnetObjectReference.Dispose();
            }

            dotnetObjectReference = DotNetObjectReference.Create(this);

            var url = AddQueryString(UploadUrl, UploadRequestQueryStrings);

            Files = await JSRuntime.InitUploader(inputFileElement, dotnetObjectReference, url, UploadRequestHttpHeaders);

            if (Files is not null)
            {
                await OnChange.InvokeAsync(Files.ToArray());
            }

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

            if (UploadStatus != BitFileUploadStatus.InProgress)
            {
                UploadStatus = BitFileUploadStatus.InProgress;
            }

            await UpdateStatus(BitFileUploadStatus.InProgress, index);
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
            if (Files[index].Status == BitFileUploadStatus.NotAllowed) return;

            var uploadedSize = Files[index].TotalSizeOfUploaded;
            if (Files[index].Size != 0 && uploadedSize >= Files[index].Size) return;

            if (MaxSize > 0 && Files[index].Size > MaxSize)
            {
                await UpdateStatus(BitFileUploadStatus.NotAllowed, index);
                return;
            }

            if (IsFileTypeNotAllowed(Files[index]))
            {
                await UpdateStatus(BitFileUploadStatus.NotAllowed, index);
                return;
            }

            if (Files[index].RequestToPause)
            {
                await PauseUpload(index);
                return;
            }

            if (Files[index].RequestToCancel)
            {
                await CancelUpload(index);
                return;
            }

            long from = Files[index].TotalSizeOfUploaded;
            long to;
            if (Files[index].Size > ChunkSize)
            {
                to = ChunkSize + from;
            }
            else
            {
                to = Files[index].Size;
            }

            Files[index].StartTimeUpload = DateTime.UtcNow;
            Files[index].SizeOfLastChunkUploaded = 0;

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
            await UpdateStatus(BitFileUploadStatus.Paused, index);
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
                Files.ToList().ForEach(c => c.RequestToCancel = true);
            }
            else
            {
                Files[index].RequestToCancel = true;
            }
        }

        /// <summary>
		/// Receive upload progress notification from underlying javascript.
		/// </summary>
		[JSInvokable("HandleUploadProgress")]
        public async Task HandleUploadProgress(int index, long loaded)
        {
            if (Files is null || Files[index].Status != BitFileUploadStatus.InProgress) return;

            Files[index].SizeOfLastChunkUploaded = loaded;
            await UpdateStatus(BitFileUploadStatus.InProgress, index);
            StateHasChanged();
        }

        /// <summary>
        /// Receive upload finished notification from underlying javascript.
        /// </summary>
        [JSInvokable("HandleFileUpload")]
        public async Task HandleFileUpload(int fileIndex, int responseStatus, string responseText)
        {
            if (Files is null
                || UploadStatus == BitFileUploadStatus.Paused
                || Files[fileIndex].Status != BitFileUploadStatus.InProgress)
                return;

            Files[fileIndex].TotalSizeOfUploaded += ChunkSize;
            Files[fileIndex].SizeOfLastChunkUploaded = 0;
            UpdateChunkSize(fileIndex);
            if (Files[fileIndex].TotalSizeOfUploaded < Files[fileIndex].Size)
            {
                await Upload(index: fileIndex);
            }
            else
            {
                Files[fileIndex].Message = responseText;
                await UpdateStatus(GetUploadStatus(responseStatus), fileIndex);
                var allFilesUploaded = Files.All(c => c.Status is BitFileUploadStatus.Completed or BitFileUploadStatus.Failed);

                if (allFilesUploaded)
                {
                    UploadStatus = BitFileUploadStatus.Completed;
                    await OnAllUploadsComplete.InvokeAsync(Files.ToArray());
                }
            }

            StateHasChanged();
        }

        private void UpdateChunkSize(int fileIndex)
        {
            if (Files is null || AutoChunkSizeEnabled is false) return;

            var dtNow = DateTime.UtcNow;
            var duration = (dtNow - Files[fileIndex].StartTimeUpload.GetValueOrDefault(dtNow)).TotalMilliseconds;

            if (duration is >= 1000 and <= 1500) return;

            ChunkSize = Convert.ToInt64(ChunkSize / (duration / 1000));

            if (ChunkSize > MAX_CHUNK_SIZE)
            {
                ChunkSize = MAX_CHUNK_SIZE;
            }

            if (ChunkSize < MIN_CHUNK_SIZE)
            {
                ChunkSize = MIN_CHUNK_SIZE;
            }
        }

        private async Task UpdateStatus(BitFileUploadStatus uploadStatus, int index)
        {
            if (Files is null) return;

            if (index < 0)
            {
                UploadStatus = uploadStatus;

                var files = Files.Where(c => c.Status != BitFileUploadStatus.NotAllowed).ToArray();
                foreach (var file in files)
                {
                    file.Status = uploadStatus;
                }

                await OnChange.InvokeAsync(files);
            }
            else
            {
                if (Files[index].Status != uploadStatus)
                {
                    Files[index].Status = uploadStatus;
                    await OnChange.InvokeAsync(new[] { Files[index] });
                }

                if (uploadStatus == BitFileUploadStatus.InProgress)
                {
                    await OnProgress.InvokeAsync(Files[index]);
                }

                if (uploadStatus == BitFileUploadStatus.Completed)
                {
                    await OnUploadComplete.InvokeAsync(Files[index]);
                }

                if (uploadStatus == BitFileUploadStatus.Removed)
                {
                    await OnRemoveComplete.InvokeAsync(Files[index]);
                }

                if (uploadStatus == BitFileUploadStatus.Failed)
                {
                    await OnUploadFailed.InvokeAsync(Files[index]);
                }

                if (uploadStatus == BitFileUploadStatus.RemoveFailed)
                {
                    await OnRemoveFailed.InvokeAsync(Files[index]);
                }
            }
        }

        private static string GetFileIcon(string fileName)
        {
            var fileSections = fileName.Split('.').ToList();
            var extension = fileSections.Last();

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

                _ => string.Empty
            };
        }

        private static BitFileUploadStatus GetUploadStatus(int responseStatus)
        {
            return responseStatus is >= 200 and <= 299 ?
                    BitFileUploadStatus.Completed :
                    (responseStatus == 0 ? BitFileUploadStatus.Paused : BitFileUploadStatus.Failed);
        }

        private async Task CancelUpload(int index)
        {
            if (JSRuntime is null || Files is null) return;

            await JSRuntime.PauseFile(index);
            await UpdateStatus(BitFileUploadStatus.Canceled, index);
            Files[index].RequestToCancel = false;
        }

        private async Task RemoveFile(int index)
        {
            if (JSRuntime is null || Files is null) return;

            try
            {
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

                await UpdateStatus(BitFileUploadStatus.Removed, index);
            }
            catch (Exception ex)
            {
                Files[index].Message = ex.ToString();
                await UpdateStatus(BitFileUploadStatus.RemoveFailed, index);
            }
        }

        private async Task RemoveOneFile(int index)
        {
            if (Files is null || RemoveUrl.HasNoValue() || HttpClient is null) return;

            var url = AddQueryString(RemoveUrl!, "fileName", Files[index].Name);
            url = AddQueryString(url, RemoveRequestQueryStrings);

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);

            foreach (var header in RemoveRequestHttpHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            await HttpClient.SendAsync(request);
        }

        private static string GetFileElClass(BitFileUploadStatus status)
        {
            switch (status)
            {
                case BitFileUploadStatus.Completed:
                    return "uploaded";
                case BitFileUploadStatus.Failed:
                case BitFileUploadStatus.NotAllowed:
                    return "failed";
                case BitFileUploadStatus.Paused:
                    return "paused";
                default:
                    return "in-progress";
            }
        }

        private string GetUploadMessageStr(BitFileInfo file)
        {
            switch (file.Status)
            {
                case BitFileUploadStatus.Completed:
                    return SuccessfullUploadMessage;
                case BitFileUploadStatus.Failed:
                    return FailedUploadMessage;
                case BitFileUploadStatus.NotAllowed:
                    return IsFileTypeNotAllowed(file) ? NotAllowedExtensionErrorMessage : MaxSizeErrorMessage;
                default:
                    return string.Empty;
            }
        }

        private bool IsFileTypeNotAllowed(BitFileInfo file)
        {
            var fileSections = file.Name.Split('.').ToList();
            var extension = $".{fileSections?.Last()}";
            return AllowedExtensions.Count > 0 && AllowedExtensions.All(ext => ext != "*") && AllowedExtensions.All(ext => ext != extension);
        }

        private static int GetFileUploadPercent(BitFileInfo file)
        {
            int uploadedPercent;
            if (file.TotalSizeOfUploaded >= file.Size)
            {
                uploadedPercent = 100;
            }
            else
            {
                uploadedPercent = (int)((file.TotalSizeOfUploaded + file.SizeOfLastChunkUploaded) / (float)file.Size * 100);
            }

            return uploadedPercent;
        }

        private static string GetFileUploadSize(BitFileInfo file)
        {
            long uploadedSize;
            if (file.TotalSizeOfUploaded >= file.Size)
            {
                uploadedSize = file.Size;
            }
            else
            {
                uploadedSize = file.TotalSizeOfUploaded + file.SizeOfLastChunkUploaded;
            }

            return uploadedSize.Humanize();
        }

        private static string AddQueryString(string uri, string name, string value)
        {
            return AddQueryString(uri, new Dictionary<string, string> { { name, value } });
        }

        private static string AddQueryString(string uri, IReadOnlyDictionary<string, string> queryStrings)
        {
            // this method is copied from:
            // https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.WebUtilities/QueryHelpers.cs

            var anchorIndex = uri.IndexOf('#', StringComparison.InvariantCultureIgnoreCase);
            var uriToBeAppended = uri;
            var anchorText = "";

            // If there is an anchor, then the query string must be inserted before its first occurence.
            if (anchorIndex != -1)
            {
                anchorText = uri[anchorIndex..];
                uriToBeAppended = uri[..anchorIndex];
            }

            var queryIndex = uriToBeAppended.IndexOf('?', StringComparison.InvariantCultureIgnoreCase);
            var hasQuery = queryIndex != -1;

            var sb = new StringBuilder();
            sb.Append(uriToBeAppended);

            foreach (var parameter in queryStrings)
            {
                sb.Append(hasQuery ? '&' : '?');
                sb.Append(UrlEncoder.Default.Encode(parameter.Key));
                sb.Append('=');
                sb.Append(UrlEncoder.Default.Encode(parameter.Value));
                hasQuery = true;
            }

            sb.Append(anchorText);
            return sb.ToString();
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
