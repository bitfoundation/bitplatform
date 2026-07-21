namespace Bit.BlazorUI;

/// <summary>
/// BitFileInput is a file input component that wraps the HTML file input element and enables file selection
/// with support for validation, drag-and-drop, paste, image previews, and customization.
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
    /// When not set, the accept attribute is generated from <see cref="AllowedExtensions"/>.
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
    /// The capture behavior of the file input on devices with a camera or microphone,
    /// rendered as the capture attribute of the input element (e.g., "user" for the front camera,
    /// "environment" for the rear camera).
    /// </summary>
    [Parameter] public string? Capture { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitFileInput.
    /// </summary>
    [Parameter] public BitFileInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the file input, applied to the browse button and the drag-and-drop indicator.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Whether to select folders (directories) instead of files, rendered as the webkitdirectory attribute.
    /// All files inside the selected folder and its subfolders will be added to the file list.
    /// </summary>
    [Parameter] public bool Directory { get; set; }

    /// <summary>
    /// Custom validation function called for each newly selected file after the built-in validations pass.
    /// Return an error message to mark the file as invalid, or null to accept it.
    /// </summary>
    [Parameter] public Func<BitFileInputInfo, string?>? FileValidator { get; set; }

    /// <summary>
    /// Custom Razor template for rendering individual file items in the file list.
    /// Receives a <see cref="BitFileInputInfo"/> context for each file.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInputInfo>? FileViewTemplate { get; set; }

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
    /// Maximum allowed number of files in the file list.
    /// Files selected beyond this count will be marked as invalid. Set to 0 for no count limit.
    /// </summary>
    [Parameter] public int MaxCount { get; set; }

    /// <summary>
    /// Custom error message displayed when the number of files exceeds the maximum count limit.
    /// Defaults to "The maximum number of files is exceeded".
    /// </summary>
    [Parameter] public string? MaxCountErrorMessage { get; set; }

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
    /// Minimum allowed file size in bytes for validation.
    /// Files smaller than this size will be marked as invalid. Set to 0 for no size limit.
    /// </summary>
    [Parameter] public long MinSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file is smaller than the minimum size limit.
    /// Defaults to "The file size is smaller than the min size".
    /// </summary>
    [Parameter] public string? MinSizeErrorMessage { get; set; }

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
    /// It is also invoked after removing a file through the remove button or the <see cref="RemoveFile"/> method.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnChange { get; set; }

    /// <summary>
    /// Callback invoked for each file that gets removed from the file list,
    /// either through the remove button or the <see cref="RemoveFile"/> method.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo> OnRemove { get; set; }

    /// <summary>
    /// Gets or sets the remove button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="RemoveButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? RemoveButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the remove button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? RemoveButtonIconName { get; set; }

    /// <summary>
    /// Whether to display a preview thumbnail for image files in the file list.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; }

    /// <summary>
    /// Whether to display a remove button next to each file in the file list, allowing individual file removal.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// The size of the file input, applied to the browse button and the file list items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitFileInput.
    /// </summary>
    [Parameter] public BitFileInputClassStyles? Styles { get; set; }



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
    /// Clears all selected files and resets the file input to its initial state without invoking any callback.
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
    /// Removes a specific file from the selected files list, or clears all files when no file is specified,
    /// invoking the <see cref="OnRemove"/> callback for each removed file and the <see cref="OnChange"/> callback afterwards.
    /// </summary>
    /// <param name="fileInfo">The file to remove, or null to remove all files.</param>
    public async Task RemoveFile(BitFileInputInfo? fileInfo = null)
    {
        if (_files.Any() is false) return;

        if (fileInfo is null)
        {
            var removedFiles = _files.ToArray();

            _files.Clear();

            await _js.BitFileInputClear(UniqueId);

            foreach (var file in removedFiles)
            {
                await OnRemove.InvokeAsync(file);
            }
        }
        else
        {
            if (_files.Remove(fileInfo) is false) return;

            await _js.BitFileInputRemoveFile(UniqueId, fileInfo.FileId);

            await OnRemove.InvokeAsync(fileInfo);
        }

        await OnChange.InvokeAsync([.. _files]);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-fin";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-fin-pri",
            BitColor.Secondary => "bit-fin-sec",
            BitColor.Tertiary => "bit-fin-ter",
            BitColor.Info => "bit-fin-inf",
            BitColor.Success => "bit-fin-suc",
            BitColor.Warning => "bit-fin-wrn",
            BitColor.SevereWarning => "bit-fin-swr",
            BitColor.Error => "bit-fin-err",
            BitColor.PrimaryBackground => "bit-fin-pbg",
            BitColor.SecondaryBackground => "bit-fin-sbg",
            BitColor.TertiaryBackground => "bit-fin-tbg",
            BitColor.PrimaryForeground => "bit-fin-pfg",
            BitColor.SecondaryForeground => "bit-fin-sfg",
            BitColor.TertiaryForeground => "bit-fin-tfg",
            BitColor.PrimaryBorder => "bit-fin-pbr",
            BitColor.SecondaryBorder => "bit-fin-sbr",
            BitColor.TertiaryBorder => "bit-fin-tbr",
            _ => "bit-fin-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-fin-sm",
            BitSize.Medium => "bit-fin-md",
            BitSize.Large => "bit-fin-lg",
            _ => "bit-fin-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        InputId = $"FileInput-{UniqueId}-input";
        _buttonId = $"FileInput-{UniqueId}-label";

        return base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false) return;

        var dragClass = $"bit-fin-drg {Classes?.Dragging}".Trim();

        _dropZoneRef = await _js.BitFileInputSetupDragDrop(RootElement, _inputRef, dragClass);
    }



    private string? GetAcceptValue()
    {
        if (Accept.HasValue()) return Accept;

        if (AllowedExtensions.Count == 0 || AllowedExtensions.Any(ext => ext == "*")) return null;

        return string.Join(",", AllowedExtensions);
    }

    private bool IsFileTypeNotAllowed(BitFileInputInfo file)
    {
        // If AllowedExtensions contains "*", all file types are allowed
        if (AllowedExtensions.Count == 0 || AllowedExtensions.Any(ext => ext == "*")) return false;

        var extension = Path.GetExtension(file.Name);

        // Files without an extension are not in the allowed list
        if (extension.HasNoValue()) return true;

        return AllowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)) is false;
    }

    private void ValidateFile(BitFileInputInfo file)
    {
        if (MaxSize > 0 && file.Size > MaxSize)
        {
            file.IsValid = false;
            file.Message = MaxSizeErrorMessage ?? "The file size is larger than the max size";
        }
        else if (MinSize > 0 && file.Size < MinSize)
        {
            file.IsValid = false;
            file.Message = MinSizeErrorMessage ?? "The file size is smaller than the min size";
        }
        else if (IsFileTypeNotAllowed(file))
        {
            file.IsValid = false;
            file.Message = NotAllowedExtensionErrorMessage ?? "The file type is not allowed";
        }
        else if (FileValidator is not null)
        {
            var message = FileValidator(file);

            if (message.HasValue())
            {
                file.IsValid = false;
                file.Message = message;
            }
        }
    }

    private async Task HandleOnChange()
    {
        if (IsDisposed) return;

        if (Append is false)
        {
            _files.Clear();
        }

        var newFiles = await _js.BitFileInputSetup(UniqueId, _inputRef, Append, ShowPreview);

        foreach (var file in newFiles)
        {
            ValidateFile(file);
        }

        _files.AddRange(newFiles);

        if (MaxCount > 0 && _files.Count > MaxCount)
        {
            foreach (var file in _files.Skip(MaxCount))
            {
                if (file.IsValid is false) continue;

                file.IsValid = false;
                file.Message = MaxCountErrorMessage ?? "The maximum number of files is exceeded";
            }
        }

        await OnChange.InvokeAsync([.. _files]);
    }

    private string GetFileElClass(bool isValid)
    {
        return isValid ? "bit-fin-vld" : "bit-fin-inv";
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
