namespace Bit.BlazorUI;

/// <summary>
/// BitFileInput is a file input component that wraps the HTML file input element and enables file selection
/// with support for validation, drag-and-drop, and customization.
/// The selected files' metadata and content can be accessed and processed from C# code.
/// </summary>
public partial class BitFileInput : BitComponentBase
{
    private ElementReference _inputRef;
    private string _buttonId = default!;
    private List<BitFileInputInfo> _files = [];
    private IJSObjectReference _dropZoneRef = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Accepted file types for the file browser using MIME types or file extensions (e.g., "image/*", ".pdf,.doc").
    /// Applied to the underlying HTML input element's accept attribute.
    /// </summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>
    /// Allowed file extensions for validation purposes (e.g., [".jpg", ".png", ".pdf"]).
    /// Use ["*"] to allow all file types. Files not matching these extensions will be marked as invalid.
    /// </summary>
    [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = ["*"];

    /// <summary>
    /// Whether to append newly selected files to the existing file list instead of replacing it.
    /// </summary>
    [Parameter] public bool Append { get; set; }

    /// <summary>
    /// Whether the file input is automatically reset (cleared) before opening the file browser dialog,
    /// allowing the same file to be selected multiple times consecutively.
    /// </summary>
    [Parameter] public bool AutoReset { get; set; }

    /// <summary>
    /// Whether to hide the file list that displays the selected files in the UI.
    /// </summary>
    [Parameter] public bool HideFileList { get; set; }

    /// <summary>
    /// Whether to hide the default browse button label from the UI.
    /// </summary>
    [Parameter] public bool HideLabel { get; set; }

    /// <summary>
    /// The text displayed on the browse button. Defaults to "Browse".
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom Razor template for the browse button area, allowing full customization of the file selection trigger UI.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Maximum allowed file size in bytes for validation.
    /// Files exceeding this size will be marked as invalid. Set to 0 for no size limit.
    /// </summary>
    [Parameter] public long MaxSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file exceeds the maximum size limit.
    /// Defaults to "The file size is larger than the max size".
    /// </summary>
    [Parameter] public string? MaxSizeErrorMessage { get; set; }

    /// <summary>
    /// Whether to allow selecting multiple files simultaneously through the file browser dialog.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>
    /// Custom error message displayed when a file's extension is not in the allowed extensions list.
    /// Defaults to "The file type is not allowed".
    /// </summary>
    [Parameter] public string? NotAllowedExtensionErrorMessage { get; set; }

    /// <summary>
    /// Callback invoked when the file selection changes, providing an array of <see cref="BitFileInputInfo"/> representing all selected files.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnChange { get; set; }

    /// <summary>
    /// Whether to display a remove button next to each file in the file list, allowing individual file removal.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// Custom Razor template for rendering individual file items in the file list.
    /// Receives a <see cref="BitFileInputInfo"/> context for each file.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInputInfo>? FileViewTemplate { get; set; }



    /// <summary>
    /// A read-only list of all currently selected files with their metadata, validation status, and content.
    /// </summary>
    public IReadOnlyList<BitFileInputInfo> Files => _files;

    /// <summary>
    /// The unique identifier of the underlying HTML file input element.
    /// </summary>
    public string? InputId { get; private set; }



    /// <summary>
    /// Opens the file browser dialog programmatically, allowing users to select files.
    /// If <see cref="AutoReset"/> is enabled, the input is reset before opening.
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
    /// Clears all selected files and resets the file input to its initial state.
    /// </summary>
    public async Task Reset()
    {
        _files.Clear();

        await _js.BitFileInputReset(UniqueId, _inputRef);

        StateHasChanged();
    }

    /// <summary>
    /// Reads the content of the specified file from the browser and populates its <see cref="BitFileInputInfo.Content"/> property
    /// with the byte array. Only reads valid and enabled files.
    /// </summary>
    /// <param name="fileInfo">The file info whose content should be loaded.</param>
    public async Task ReadContentAsync(BitFileInputInfo fileInfo)
    {
        if (IsEnabled is false) return;
        if (fileInfo.IsValid is false) return;

        fileInfo.Content = await _js.BitFileInputReadContent(UniqueId, fileInfo.FileId);
    }

    /// <summary>
    /// Removes a specific file from the selected files list, or clears all files when no file is specified.
    /// </summary>
    /// <param name="fileInfo">The file to remove, or null to remove all files.</param>
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
        _buttonId = $"FileInput-{UniqueId}-label";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        _dropZoneRef = await _js.BitFileInputSetupDragDrop(RootElement, _inputRef);
    }



    private bool IsFileTypeNotAllowed(BitFileInputInfo file)
    {
        //if (Accept.HasNoValue()) return false;

        // If AllowedExtensions only contains "*", all files are allowed
        if (AllowedExtensions.Count == 0 || AllowedExtensions.All(ext => ext == "*")) return false;

        var fileSections = file.Name.Split('.');

        // Handle files without an extension
        if (fileSections.Length < 2) return true; // No extension, not in allowed list

        var extension = $".{fileSections?.Last()}";

        return AllowedExtensions.All(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase) is false);
    }

    private async Task HandleOnChange()
    {
        if (Append is false)
        {
            _files.Clear();
        }

        if (IsDisposed) return;

        var newFiles = await _js.BitFileInputSetup(UniqueId, _inputRef, Append);

        foreach (var file in newFiles)
        {
            // Validate file size
            if (MaxSize > 0 && file.Size > MaxSize)
            {
                file.IsValid = false;
                file.Message = MaxSizeErrorMessage ?? "The file size is larger than the max size";
            }
            // Validate file extension
            else if (IsFileTypeNotAllowed(file))
            {
                file.IsValid = false;
                file.Message = NotAllowedExtensionErrorMessage ?? "The file type is not allowed";
            }
        }

        _files.AddRange(newFiles);

        await OnChange.InvokeAsync([.. _files]);
    }

    private string GetFileElClass(bool isValid)
    {
        return isValid ? $"bit-fin-vld" : $"bit-fin-inv";
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

        try
        {
            await _js.BitFileInputClear(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
