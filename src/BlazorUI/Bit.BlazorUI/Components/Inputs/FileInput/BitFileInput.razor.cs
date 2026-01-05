namespace Bit.BlazorUI;

/// <summary>
/// BitFileInput component wraps the HTML file input element(s) and allows file selection. The selected files can be accessed from the C# context for further processing.
/// </summary>
public partial class BitFileInput : BitComponentBase
{
    private ElementReference _inputRef;
    private List<BitFileInputInfo> _files = [];
    private IJSObjectReference _dropZoneRef = default!;
    private DotNetObjectReference<BitFileInput> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The value of the accept attribute of the input element.
    /// </summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>
    /// Filters files by extension.
    /// </summary>
    [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = ["*"];

    /// <summary>
    /// Enables the append mode that appends any additional selected file(s) to the current file list.
    /// </summary>
    [Parameter] public bool Append { get; set; }

    /// <summary>
    /// Automatically resets the file input before starting to browse for files.
    /// </summary>
    [Parameter] public bool AutoReset { get; set; }

    /// <summary>
    /// Hides the file list section of the file input.
    /// </summary>
    [Parameter] public bool HideFileList { get; set; }

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
    /// Specifies the message for the failed validation due to exceeding the maximum size.
    /// </summary>
    [Parameter] public string MaxSizeErrorMessage { get; set; } = "The file size is larger than the max size";

    /// <summary>
    /// Enables multi-file selection.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>
    /// Specifies the message for the failed validation due to the allowed extensions.
    /// </summary>
    [Parameter] public string NotAllowedExtensionErrorMessage { get; set; } = "The file type is not allowed";

    /// <summary>
    /// Callback for when file or files selection changes.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnChange { get; set; }

    /// <summary>
    /// Callback for when files are selected and validated.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnSelectComplete { get; set; }

    /// <summary>
    /// Show/Hide remove button.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// The custom file view template.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInputInfo>? FileViewTemplate { get; set; }



    /// <summary>
    /// A list of all of the selected files.
    /// </summary>
    public IReadOnlyList<BitFileInputInfo> Files => _files;

    /// <summary>
    /// The id of the file input element.
    /// </summary>
    public string? InputId { get; private set; }



    /// <summary>
    /// Opens a file selection dialog.
    /// </summary>
    public async Task Browse()
    {
        if (IsEnabled is false) return;

        if (AutoReset)
        {
            await Reset();
        }

        await _js.BitFileInputBrowse(_inputRef);
    }

    /// <summary>
    /// Resets the file input.
    /// </summary>
    public async Task Reset()
    {
        _files.Clear();
        await _js.BitFileInputReset(UniqueId, _inputRef);
    }

    /// <summary>
    /// Removes a file from the selected files list.
    /// </summary>
    /// <param name="fileInfo">
    /// null => all files | else => specific file
    /// </param>
    public void RemoveFile(BitFileInputInfo? fileInfo = null)
    {
        if (_files.Any() is false) return;

        if (fileInfo is null)
        {
            _files.Clear();
        }
        else
        {
            _files.Remove(fileInfo);
        }

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-fin";

    protected override Task OnInitializedAsync()
    {
        InputId = $"FileInput-{UniqueId}-input";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        _dropZoneRef = await _js.BitFileInputSetupDragDrop(RootElement, _inputRef);
    }



    internal bool IsFileTypeNotAllowed(BitFileInputInfo file)
    {
        if (Accept.HasNoValue()) return false;

        var fileSections = file.Name.Split('.');
        var extension = $".{fileSections?.Last()}";
        return AllowedExtensions.Count > 0 && AllowedExtensions.All(ext => ext != "*") && AllowedExtensions.All(ext => ext != extension);
    }

    private async Task HandleOnChange()
    {
        if (Append is false)
        {
            _files.Clear();
        }

        if (IsDisposed) return;

        var newFiles = await _js.BitFileInputSetup(UniqueId, _dotnetObj, _inputRef, Append);

        foreach (var file in newFiles)
        {
            // Validate file size
            if (MaxSize > 0 && file.Size > MaxSize)
            {
                file.IsValid = false;
                file.Message = MaxSizeErrorMessage;
            }
            // Validate file extension
            else if (IsFileTypeNotAllowed(file))
            {
                file.IsValid = false;
                file.Message = NotAllowedExtensionErrorMessage;
            }
        }

        _files.AddRange(newFiles);

        if (_files.Any() is false) return;

        await OnChange.InvokeAsync([.. _files]);
        await OnSelectComplete.InvokeAsync([.. _files]);
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        if (_dropZoneRef is not null)
        {
            try
            {
                await _dropZoneRef.InvokeVoidAsync("dispose");
                await _dropZoneRef.DisposeAsync();
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
            catch (JSException ex)
            {
                // it seems it's safe to just ignore this exception here.
                // otherwise it will blow up the MAUI app in a page refresh for example.
                Console.WriteLine(ex.Message);
            }
        }

        if (_dotnetObj is not null)
        {
            _dotnetObj.Dispose();

            try
            {
                await _js.BitFileInputClear(UniqueId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
