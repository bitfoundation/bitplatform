namespace Bit.BlazorUI;

/// <summary>
/// A file input component that wraps the HTML file input element, enabling file selection with support for validation, drag-and-drop, and customization.
/// The selected files can be accessed and processed from the C# context.
/// </summary>
public partial class BitFileInput : BitComponentBase
{
    private ElementReference _inputRef;
    private string _buttonId = default!;
    private List<BitFileInputInfo> _files = [];
    private IJSObjectReference _dropZoneRef = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Specifies the accepted file types using MIME types or file extensions (e.g., "image/*", ".pdf,.doc").
    /// This value is applied to the HTML input element's accept attribute.
    /// </summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>
    /// Specifies the allowed file extensions for validation (e.g., [".jpg", ".png", ".pdf"]).
    /// Use ["*"] to allow all file types. Files not matching these extensions will be marked as invalid.
    /// </summary>
    [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = ["*"];

    /// <summary>
    /// When enabled, newly selected files are appended to the existing file list instead of replacing it.
    /// </summary>
    [Parameter] public bool Append { get; set; }

    /// <summary>
    /// When enabled, the file input is automatically reset (cleared) before opening the file browser dialog.
    /// This allows selecting the same file multiple times consecutively.
    /// </summary>
    [Parameter] public bool AutoReset { get; set; }

    /// <summary>
    /// When enabled, the file list displaying selected files is hidden from the UI.
    /// </summary>
    [Parameter] public bool HideFileList { get; set; }

    /// <summary>
    /// When enabled, the default browse button label is hidden from the UI.
    /// </summary>
    [Parameter] public bool HideLabel { get; set; }

    /// <summary>
    /// The text displayed on the browse button. Defaults to "Browse" if not specified.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// A custom Razor template for the browse button area, allowing full customization of the file selection UI.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum allowed file size in bytes for validation. 
    /// Files exceeding this size will be marked as invalid. Set to 0 for no size limit.
    /// </summary>
    [Parameter] public long MaxSize { get; set; }

    /// <summary>
    /// The error message displayed when a file exceeds the maximum size limit.
    /// Defaults to "The file size is larger than the max size" if not specified.
    /// </summary>
    [Parameter] public string? MaxSizeErrorMessage { get; set; }

    /// <summary>
    /// When enabled, allows selecting multiple files simultaneously through the file browser dialog.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>
    /// The error message displayed when a file's extension is not in the allowed extensions list.
    /// Defaults to "The file type is not allowed" if not specified.
    /// </summary>
    [Parameter] public string? NotAllowedExtensionErrorMessage { get; set; }

    /// <summary>
    /// Callback invoked when the file selection changes.
    /// Receives an array of <see cref="BitFileInputInfo"/> objects representing all selected files.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnChange { get; set; }

    /// <summary>
    /// When enabled, displays a remove button next to each file in the file list,
    /// allowing users to individually remove files from the selection.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// A custom Razor template for rendering individual file items in the file list.
    /// Receives a <see cref="BitFileInputInfo"/> context for each file.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInputInfo>? FileViewTemplate { get; set; }



    /// <summary>
    /// Gets a read-only list of all currently selected files with their metadata and validation status.
    /// </summary>
    public IReadOnlyList<BitFileInputInfo> Files => _files;

    /// <summary>
    /// Gets the unique identifier of the underlying HTML file input element.
    /// </summary>
    public string? InputId { get; private set; }



    /// <summary>
    /// Programmatically opens the file browser dialog, allowing users to select files.
    /// If <see cref="AutoReset"/> is enabled, the input is reset before opening the dialog.
    /// </summary>
    /// <returns>A task that completes when the browser dialog is opened.</returns>
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
    /// <returns>A task that completes when the reset operation finishes.</returns>
    public async Task Reset()
    {
        _files.Clear();

        await _js.BitFileInputReset(UniqueId, _inputRef);

        StateHasChanged();
    }

    /// <summary>
    /// Removes one or more files from the selected files list.
    /// </summary>
    /// <param name="fileInfo">
    /// The specific file to remove. If null, all files will be removed from the list.
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
