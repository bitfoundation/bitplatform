using System.Text;
using System.Text.Encodings.Web;

namespace Bit.BlazorUI;

/// <summary>
/// A component that wraps the HTML file input element and uploads them.
/// </summary>
public partial class BitFileUpload : IDisposable
{
    private const int MIN_CHUNK_SIZE = 512 * 1024; // 512 kb
    private const int MAX_CHUNK_SIZE = 10 * 1024 * 1024; // 10 mb

    private bool _disposed;
    private long? chunkSize;
    private ElementReference inputFileElement;
    private IJSObjectReference? dropZoneInstance;
    private long _internalChunkSize = MIN_CHUNK_SIZE;
    private DotNetObjectReference<BitFileUpload>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private HttpClient _httpClient { get; set; } = default!;



    /// <summary>
    /// The value of the accept attribute of the input element.
    /// </summary>
    [Parameter] public string? Accept { get; set; }

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
    /// Enables or disables the chunked upload feature.
    /// </summary>
    [Parameter] public bool ChunkedUploadEnabled { get; set; } = true;

    /// <summary>
    /// The size of each chunk of file upload in bytes.
    /// </summary>
    [Parameter]
    public long? ChunkSize
    {
        get => chunkSize;
        set
        {
            chunkSize = value;

            if (chunkSize.HasValue is false || AutoChunkSizeEnabled)
            {
                _internalChunkSize = MIN_CHUNK_SIZE;
            }
            else
            {
                _internalChunkSize = chunkSize.Value;
            }
        }
    }

    /// <summary>
    /// The message shown for failed file uploads.
    /// </summary>
    [Parameter] public string FailedUploadMessage { get; set; } = "File upload failed";

    /// <summary>
    /// Enables multi-file select and upload.
    /// </summary>
    [Parameter] public bool IsMultiSelect { get; set; }

    /// <summary>
    /// The text of select file button.
    /// </summary>
    [Parameter] public string Label { get; set; } = "Browse";

    /// <summary>
    /// A custom razor template for select button.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Specifies the maximum size (byte) of the file (0 for unlimited).
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
    [Parameter] public string? RemoveUrl { get; set; }

    /// <summary>
    /// Show/Hide after upload remove button.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// The message shown for successful file uploads.
    /// </summary>
    [Parameter] public string SuccessfulUploadMessage { get; set; } = "File upload succeed";

    /// <summary>
    /// Custom http headers for upload request.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string> UploadRequestHttpHeaders { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Custom query strings for upload request.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, string> UploadRequestQueryStrings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// URL of the server endpoint receiving the files.
    /// </summary>
    [Parameter] public string? UploadUrl { get; set; }

    /// <summary>
    /// Template to render each file info.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInfo>? FileViewTemplate { get; set; }


    /// <summary>
    /// All selected files.
    /// </summary>
    public IReadOnlyList<BitFileInfo>? Files { get; private set; }

    /// <summary>
    /// General upload status.
    /// </summary>
    public BitFileUploadStatus UploadStatus { get; private set; }

    /// <summary>
    /// File input id.
    /// </summary>
    public string? InputId { get; private set; }

    /// <summary>
    /// Loading when do remove a file
    /// </summary>
    public bool IsRemoving { get; private set; }

    protected override string RootElementClass => "bit-upl";

    /// <summary>
    /// Starts Uploading the file(s).
    /// </summary>
    public async Task Upload(BitFileInfo? fileInfo = null)
    {
        if (Files is null) return;

        if (UploadStatus != BitFileUploadStatus.InProgress)
        {
            UploadStatus = BitFileUploadStatus.InProgress;
        }

        await UpdateStatus(BitFileUploadStatus.InProgress, fileInfo);
        if (fileInfo is null)
        {
            foreach (var file in Files)
            {
                await UploadOneFile(file);
            }
        }
        else
        {
            await UploadOneFile(fileInfo);
        }
    }

    /// <summary>
    /// Pause upload.
    /// </summary>
    /// <param name="fileInfo">
    /// null => all files | else => specific file
    /// </param>
    /// <returns></returns>
    public void PauseUpload(BitFileInfo? fileInfo = null)
    {
        if (Files is null) return;

        if (fileInfo is null)
        {
            foreach (var file in Files)
            {
                file.PauseUploadRequested = true;
            }
        }
        else
        {
            fileInfo.PauseUploadRequested = true;
        }
    }

    /// <summary>
    /// Cancel upload.
    /// </summary>
    /// <param name="fileInfo">
    /// null => all files | else => specific file
    /// </param>
    /// <returns></returns>
    public void CancelUpload(BitFileInfo? fileInfo = null)
    {
        if (Files is null) return;

        if (fileInfo is null)
        {
            foreach (var file in Files)
            {
                file.CancelUploadRequested = true;
            }
        }
        else
        {
            fileInfo.CancelUploadRequested = true;
        }
    }

    /// <summary>
    /// Remove file.
    /// </summary>
    /// <param name="fileInfo">
    /// null => all files | else => specific file
    /// </param>
    /// <returns></returns>
    public async Task RemoveFile(BitFileInfo? fileInfo = null)
    {
        if (Files is null) return;
        if (IsRemoving) return;

        IsRemoving = true;

        if (fileInfo is null)
        {
            foreach (var file in Files)
            {
                await RemoveOneFile(file);
            }
        }
        else
        {
            await RemoveOneFile(fileInfo);
        }

        IsRemoving = false;
    }

    protected override Task OnInitializedAsync()
    {
        InputId = $"FileUpload-{UniqueId}-input";

        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dropZoneInstance = await _js.SetupFileUploadDropzone(RootElement, inputFileElement);
        }
    }

    private async Task HandleOnChange()
    {
        if (UploadUrl is null) return;

        var url = AddQueryString(UploadUrl, UploadRequestQueryStrings);

        Files = await _js.InitFileUpload(inputFileElement, _dotnetObj, url, UploadRequestHttpHeaders);

        if (Files is null) return;

        await OnChange.InvokeAsync([.. Files]);

        if (AutoUploadEnabled)
        {
            await Upload();
        }
    }

    private async Task UploadOneFile(BitFileInfo fileInfo)
    {
        if (Files is null || fileInfo.Status == BitFileUploadStatus.NotAllowed) return;

        var uploadedSize = fileInfo.TotalSizeOfUploaded;
        if (fileInfo.Size != 0 && uploadedSize >= fileInfo.Size) return;

        if (MaxSize > 0 && fileInfo.Size > MaxSize)
        {
            await UpdateStatus(BitFileUploadStatus.NotAllowed, fileInfo);
            return;
        }

        if (IsFileTypeNotAllowed(fileInfo))
        {
            await UpdateStatus(BitFileUploadStatus.NotAllowed, fileInfo);
            return;
        }

        if (fileInfo.PauseUploadRequested)
        {
            await PauseUploadOneFile(fileInfo.Index);
            return;
        }

        if (fileInfo.CancelUploadRequested)
        {
            await CancelUploadOneFile(fileInfo.Index);
            return;
        }

        long to;
        long from = 0;
        if (ChunkedUploadEnabled)
        {
            from = fileInfo.TotalSizeOfUploaded;
            if (fileInfo.Size > _internalChunkSize)
            {
                to = from + _internalChunkSize;
            }
            else
            {
                to = fileInfo.Size;
            }

            fileInfo.StartTimeUpload = DateTime.UtcNow;
            fileInfo.SizeOfLastChunkUploaded = 0;
        }
        else
        {
            to = fileInfo.Size;
        }

        await _js.UploadFile(from, to, fileInfo.Index);
    }

    private async Task PauseUploadOneFile(int index)
    {
        if (Files is null) return;

        await _js.PauseFile(index);
        var file = Files[index];
        await UpdateStatus(BitFileUploadStatus.Paused, file);
        file.PauseUploadRequested = false;
    }

    private void UpdateChunkSize(int fileIndex)
    {
        if (Files is null || AutoChunkSizeEnabled is false) return;

        var dtNow = DateTime.UtcNow;
        var duration = (dtNow - Files[fileIndex].StartTimeUpload.GetValueOrDefault(dtNow)).TotalMilliseconds;

        if (duration is >= 1000 and <= 1500) return;

        _internalChunkSize = Convert.ToInt64(_internalChunkSize / (duration / 1000));

        if (_internalChunkSize > MAX_CHUNK_SIZE)
        {
            _internalChunkSize = MAX_CHUNK_SIZE;
        }

        if (_internalChunkSize < MIN_CHUNK_SIZE)
        {
            _internalChunkSize = MIN_CHUNK_SIZE;
        }
    }

    private async Task UpdateStatus(BitFileUploadStatus uploadStatus, BitFileInfo? fileInfo = null)
    {
        if (Files is null) return;

        if (fileInfo is null)
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
            if (fileInfo.Status != uploadStatus)
            {
                fileInfo.Status = uploadStatus;
                await OnChange.InvokeAsync([fileInfo]);
            }

            switch (uploadStatus)
            {
                case BitFileUploadStatus.InProgress:
                    await OnProgress.InvokeAsync(fileInfo);
                    break;

                case BitFileUploadStatus.Completed:
                    await OnUploadComplete.InvokeAsync(fileInfo);
                    break;

                case BitFileUploadStatus.Failed:
                    await OnUploadFailed.InvokeAsync(fileInfo);
                    break;

                case BitFileUploadStatus.Removed:
                    await OnRemoveComplete.InvokeAsync(fileInfo);
                    break;

                case BitFileUploadStatus.RemoveFailed:
                    await OnRemoveFailed.InvokeAsync(fileInfo);
                    break;
            }
        }
    }

    private async Task CancelUploadOneFile(int index)
    {
        if (Files is null) return;

        await _js.PauseFile(index);
        var file = Files[index];
        await UpdateStatus(BitFileUploadStatus.Canceled, file);
        file.CancelUploadRequested = false;
    }

    private async Task RemoveOneFile(BitFileInfo fileInfo)
    {
        if (fileInfo.Status is BitFileUploadStatus.Removed) return;

        if (fileInfo.TotalSizeOfUploaded > 0)
        {
            await RemoveOneFileFromServer(fileInfo);
        }
        else
        {
            await UpdateStatus(BitFileUploadStatus.Removed, fileInfo);
        }
    }

    private async Task RemoveOneFileFromServer(BitFileInfo fileInfo)
    {
        if (RemoveUrl.HasNoValue()) return;

        try
        {
            var url = AddQueryString(RemoveUrl!, "fileName", fileInfo.Name);
            url = AddQueryString(url, RemoveRequestQueryStrings);

            using var request = new HttpRequestMessage(HttpMethod.Delete, url);

            foreach (var header in RemoveRequestHttpHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            await _httpClient.SendAsync(request);

            await UpdateStatus(BitFileUploadStatus.Removed, fileInfo);
        }
        catch (Exception ex)
        {
            fileInfo.Message = ex.ToString();
            await UpdateStatus(BitFileUploadStatus.RemoveFailed, fileInfo);
        }
    }

    internal bool IsFileTypeNotAllowed(BitFileInfo file)
    {
        if (Accept.HasNoValue()) return false;

        var fileSections = file.Name.Split('.');
        var extension = $".{fileSections?.Last()}";
        return AllowedExtensions.Count > 0 && AllowedExtensions.All(ext => ext != "*") && AllowedExtensions.All(ext => ext != extension);
    }

    private static string AddQueryString(string uri, string name, string value)
    {
        return AddQueryString(uri, new Dictionary<string, string> { { name, value } });
    }

    private static string AddQueryString(string uri, IReadOnlyDictionary<string, string> queryStrings)
    {
        // this method is copied from:
        // https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.WebUtilities/QueryHelpers.cs

        int anchorIndex = uri.IndexOf('#', StringComparison.InvariantCultureIgnoreCase);
        string uriToBeAppended = uri;
        string? anchorText = null;

        // If there is an anchor, then the query string must be inserted before its first occurrence.
        if (anchorIndex != -1)
        {
            anchorText = uri[anchorIndex..];
            uriToBeAppended = uri[..anchorIndex];
        }

        var queryIndex = uriToBeAppended.IndexOf('?', StringComparison.InvariantCultureIgnoreCase);
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder(uriToBeAppended);

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

    /// <summary>
    /// Receive upload progress notification from underlying javascript.
    /// </summary>
    [JSInvokable("HandleUploadProgress")]
    public async Task HandleUploadProgress(int index, long loaded)
    {
        if (Files is null) return;

        var file = Files[index];
        if (file.Status != BitFileUploadStatus.InProgress) return;

        file.SizeOfLastChunkUploaded = loaded;
        await UpdateStatus(BitFileUploadStatus.InProgress, file);
        StateHasChanged();
    }

    /// <summary>
    /// Receive upload finished notification from underlying JavaScript.
    /// </summary>
    [JSInvokable("HandleFileUpload")]
    public async Task HandleFileUpload(int fileIndex, int responseStatus, string responseText)
    {
        if (Files is null || UploadStatus == BitFileUploadStatus.Paused) return;

        var file = Files[fileIndex];
        if (file.Status != BitFileUploadStatus.InProgress) return;

        file.TotalSizeOfUploaded += ChunkedUploadEnabled ? _internalChunkSize : file.Size;
        file.SizeOfLastChunkUploaded = 0;

        UpdateChunkSize(fileIndex);

        if (file.TotalSizeOfUploaded < file.Size)
        {
            await Upload(file);
        }
        else
        {
            file.Message = responseText;
            if (responseStatus is >= 200 and <= 299)
            {
                await UpdateStatus(BitFileUploadStatus.Completed, file);
            }
            else if ((responseStatus is 0 && (file.Status is BitFileUploadStatus.Paused or BitFileUploadStatus.Canceled)) is false)
            {
                await UpdateStatus(BitFileUploadStatus.Failed, file);
            }

            var allFilesUploaded = Files.All(c => c.Status is BitFileUploadStatus.Completed or BitFileUploadStatus.Failed);
            if (allFilesUploaded)
            {
                UploadStatus = BitFileUploadStatus.Completed;
                await OnAllUploadsComplete.InvokeAsync(Files.ToArray());
            }
        }

        StateHasChanged();
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (dropZoneInstance is not null)
        {
            _ = dropZoneInstance.InvokeVoidAsync("dispose").AsTask();
            _ = dropZoneInstance.DisposeAsync().AsTask();
            dropZoneInstance = null;
        }

        if (_dotnetObj != null)
        {
            _dotnetObj.Dispose();
            _dotnetObj = null;
        }

        _disposed = true;
    }
}
