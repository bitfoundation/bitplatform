using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitFileInput is a file input component that wraps the HTML file input element and enables file selection
/// with support for validation, drag-and-drop onto a button or a drop zone panel, paste, image previews,
/// file type glyphs, folder selection, and customization.
/// The selected files' metadata is accessible from C# code, and their content can be pulled on demand either
/// as a byte array (<see cref="ReadContentAsync"/>) or as a chunked stream (<see cref="OpenReadStreamAsync"/>).
/// </summary>
public partial class BitFileInput : BitComponentBase
{
    private bool _allowDrop = true;
    private bool _allowPaste = true;
    private bool _expandDirectories;
    private string? _dragClass;
    private string? _dragStyle;
    private string? _announcement;
    private bool _announcementMarker;
    private ElementReference _inputRef;
    private ElementReference _labelRef;
    private string _buttonId = default!;
    private string _descriptionId = default!;
    private int? _focusAfterRemoveIndex;
    private List<BitFileInputInfo> _files = [];
    private IJSObjectReference _dropZoneRef = default!;
    private readonly Dictionary<string, ElementReference> _removeRefs = [];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The cascading parameters of the BitFileInput, provided by a <see cref="BitParams"/> ancestor.
    /// </summary>
    /// <remarks>
    /// The intended use is to allow shared configuration or settings to be applied to multiple file input components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitFileInputParams.ParamName)]
    public BitFileInputParams? CascadingParameters { get; set; }



    /// <summary>
    /// Accepted file types for the file browser using MIME types or file extensions (e.g., "image/*", ".pdf,.doc").
    /// Applied to the underlying HTML input element's accept attribute.
    /// When not set, the accept attribute is generated from <see cref="AllowedExtensions"/>.
    /// </summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>
    /// Whether files can be selected by dragging them from the operating system and dropping them on the component.
    /// The default value is true.
    /// </summary>
    [Parameter] public bool AllowDrop { get; set; } = true;

    /// <summary>
    /// Whether a file that is already in the file list can be selected again.
    /// When disabled, a newly selected file matching an existing one by folder, name, size and last modified time
    /// is marked as invalid with the <see cref="DuplicateErrorMessage"/> instead of being added as a second entry,
    /// becoming valid again once the file it duplicates is removed.
    /// The default value is true.
    /// </summary>
    [Parameter] public bool AllowDuplicates { get; set; } = true;

    /// <summary>
    /// Allowed file types for validation purposes, accepting both file extensions (e.g., [".jpg", ".png", ".pdf"])
    /// and MIME types with an optional wildcard (e.g., ["image/*", "application/pdf"]).
    /// The leading dot of an extension is optional and the matching is case-insensitive.
    /// Use ["*"] to allow all file types. Files not matching any of these entries will be marked as invalid.
    /// </summary>
    [Parameter] public IReadOnlyCollection<string> AllowedExtensions { get; set; } = ["*"];

    /// <summary>
    /// Whether files can be selected by pasting them from the clipboard onto the component.
    /// The paste is only captured while the focus is inside the component, so the browse button must be focused first.
    /// The default value is true.
    /// </summary>
    [Parameter] public bool AllowPaste { get; set; } = true;

    /// <summary>
    /// Custom provider of the text announced by the screen reader through the live region of the component
    /// whenever the file list changes. Receives the current file list and returns the text to announce,
    /// or null to announce nothing. When not set, a built-in English announcement is used.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<BitFileInputInfo>, string?>? AnnouncementProvider { get; set; }

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
    /// A short hint rendered under the browse button and wired to it through aria-describedby,
    /// which is the place to spell out the accepted file types and the size limits so that both sighted
    /// and screen reader users learn the constraints before hitting them.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Custom Razor template of the hint rendered under the browse button, taking precedence over <see cref="Description"/>.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Whether to select folders (directories) instead of files, rendered as the webkitdirectory attribute.
    /// All files inside the selected folder and its subfolders will be added to the file list.
    /// It also makes a dropped folder expand into its contents instead of being ignored.
    /// </summary>
    [Parameter] public bool Directory { get; set; }

    /// <summary>
    /// Gets or sets the glyph of the drop zone panel using custom CSS classes for external icon libraries,
    /// which is only rendered while <see cref="ShowDropZone"/> is enabled.
    /// Takes precedence over <see cref="DropZoneIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DropZoneIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the drop zone panel's glyph from the built-in Fluent UI icons.
    /// Defaults to "CloudUpload", and an empty string leaves the panel without a glyph at all.
    /// </summary>
    [Parameter] public string? DropZoneIconName { get; set; }

    /// <summary>
    /// Custom error message displayed when a file is selected again while <see cref="AllowDuplicates"/> is disabled.
    /// Defaults to "The file is already selected".
    /// </summary>
    [Parameter] public string? DuplicateErrorMessage { get; set; }

    /// <summary>
    /// Custom provider of the glyph shown in the thumbnail's place for a file that has no image preview,
    /// which is rendered while <see cref="ShowPreview"/> is enabled. Receives the file and returns the icon
    /// to draw, or null to leave that file without one. When not set, the icon is picked from the file's
    /// MIME type and extension.
    /// </summary>
    [Parameter] public Func<BitFileInputInfo, BitIconInfo?>? FileIconSelector { get; set; }

    /// <summary>
    /// The accessible name of the file list, which tells a screen reader user walking the lists of the page
    /// what this one holds. Defaults to "Selected files".
    /// </summary>
    [Parameter] public string? FileListAriaLabel { get; set; }

    /// <summary>
    /// Custom formatter of the file size shown under the name of each file item.
    /// Receives the size of the file in bytes and returns the text to display,
    /// which is the place to localize the units or to switch between the binary and the decimal bases.
    /// When not set, a built-in humanizer is used.
    /// </summary>
    [Parameter] public Func<long, string>? FileSizeFormatter { get; set; }

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
    /// Since the browse button is also what turns into the drag-and-drop indicator, hiding it moves that
    /// indicator onto the component itself so that dropping files still shows that it is allowed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HideLabel { get; set; }

    /// <summary>
    /// The text displayed on the browse button. Defaults to "Browse".
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom Razor template for the browse button area, allowing full customization of the file selection trigger UI.
    /// Since it replaces the browse button, which is also what turns into the drag-and-drop indicator, providing it
    /// moves that indicator onto the component itself so that dropping files still shows that it is allowed.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Maximum allowed number of files in the file list.
    /// Files selected beyond this count will be marked as invalid, becoming valid again once removals free up room.
    /// Set to 0 for no count limit.
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
    /// Maximum allowed total size in bytes of all the files in the file list.
    /// Files pushing the accumulated size beyond this limit will be marked as invalid,
    /// becoming valid again once removals free up room. Set to 0 for no total size limit.
    /// </summary>
    [Parameter] public long MaxTotalSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file makes the total size of the file list exceed the maximum total size.
    /// Defaults to "The total size of the files is larger than the max total size".
    /// </summary>
    [Parameter] public string? MaxTotalSizeErrorMessage { get; set; }

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
    /// Callback invoked right after <see cref="OnChange"/> whenever the file list holds at least one invalid file,
    /// providing an array of only the invalid files along with their validation messages.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo[]> OnInvalid { get; set; }

    /// <summary>
    /// Callback invoked for each file that gets removed from the file list,
    /// either through the remove button or the <see cref="RemoveFile"/> method.
    /// </summary>
    [Parameter] public EventCallback<BitFileInputInfo> OnRemove { get; set; }

    /// <summary>
    /// Whether to decode every selected image file to fill the <see cref="BitFileInputInfo.Width"/> and
    /// <see cref="BitFileInputInfo.Height"/> properties with its pixel dimensions, which makes it possible to
    /// enforce resolution rules from a <see cref="FileValidator"/>. Decoding costs time and memory proportional
    /// to the images, so it is disabled by default.
    /// </summary>
    [Parameter] public bool ReadImageDimensions { get; set; }

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
    /// The tooltip of the remove button, which is also used as the prefix of its accessible label
    /// (e.g., "Remove report.pdf"). Defaults to "Remove".
    /// </summary>
    [Parameter] public string? RemoveButtonTitle { get; set; }

    /// <summary>
    /// Whether to render the browse area as a full width drop zone panel - a dashed rule around a glyph and
    /// the label - instead of an ordinary button, which is what makes a drag-and-drop target look like one.
    /// The panel is the same button underneath, so it is still reached with Tab and activated with Enter or Space,
    /// and it carries the drag indicator exactly as the button does.
    /// It also makes <see cref="BitVariant.Outline"/> the default variant, since a panel-sized block of the
    /// role color is rarely what a drop zone wants; set <see cref="Variant"/> to take that back.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ShowDropZone { get; set; }

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
    /// The tooltip of the browse button, rendered as its title attribute.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the browse button, which decides how much of the <see cref="Color"/> it carries:
    /// a full fill, only an outline, or neither.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



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
        if (IsDisposed) return;

        // clearing what is already empty changes nothing, so it is not worth an announcement - otherwise
        // every AutoReset browse would tell the screen reader that no file is selected before the dialog opens.
        var hadFiles = _files.Count > 0;

        _files.Clear();
        _removeRefs.Clear();

        await _js.BitFileInputReset(UniqueId, _inputRef);

        if (hadFiles)
        {
            Announce();
        }

        StateHasChanged();
    }

    /// <summary>
    /// Reads the content of the specified file from the browser and populates its <see cref="BitFileInputInfo.Content"/> property
    /// with the byte array, or reads every valid file of the file list when no file is specified.
    /// Only reads valid files and only while the component is enabled.
    /// The whole file crosses the interop boundary as one message, which on Blazor Server the circuit caps
    /// (SignalR's MaximumReceiveMessageSize, 32 KB by default), so anything larger than a small file is read
    /// with <see cref="OpenReadStreamAsync"/> instead.
    /// </summary>
    /// <param name="fileInfo">The file info whose content should be loaded, or null to load all the valid files.</param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    public async Task ReadContentAsync(BitFileInputInfo? fileInfo = null, CancellationToken cancellationToken = default)
    {
        if (IsDisposed) return;
        if (IsEnabled is false) return;

        if (fileInfo is null)
        {
            foreach (var file in _files.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ReadContentAsync(file, cancellationToken);
            }

            return;
        }

        if (fileInfo.IsValid is false) return;

        fileInfo.Content = await _js.BitFileInputReadContent(UniqueId, fileInfo.FileId, cancellationToken);
    }

    /// <summary>
    /// Opens a stream over the content of the specified file, which the runtime reads from the browser in chunks
    /// instead of materializing the whole file in memory the way <see cref="ReadContentAsync"/> does.
    /// This is what makes a file too large to hold as a byte array - a video, an archive, a database dump -
    /// copyable to disk, hashable or forwardable to a server, and on Blazor Server it is also what gets a file
    /// past the circuit's message size cap, which one byte array in one interop message runs into almost at once.
    /// The stream is forward only and must be disposed by the caller, which is also what releases the
    /// underlying JavaScript reference to the file.
    /// Unlike <see cref="ReadContentAsync"/> it also reads a file the validations rejected, since a file too
    /// large to hold in memory is exactly the one a stream is wanted for.
    /// </summary>
    /// <param name="fileInfo">The file to read, which must still be in the current selection.</param>
    /// <param name="maxAllowedSize">
    /// The largest number of bytes the stream is allowed to yield, which guards against a file swapped
    /// underneath the page after it was picked. It defaults to the size the browser reported for the file.
    /// </param>
    /// <param name="cancellationToken">A token that cancels the read.</param>
    public async Task<Stream> OpenReadStreamAsync(BitFileInputInfo fileInfo,
                                                  long? maxAllowedSize = null,
                                                  CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var streamRef = await _js.BitFileInputOpenReadStream(UniqueId, fileInfo.FileId)
                        ?? throw new InvalidOperationException("The JavaScript runtime is no longer available, so the file cannot be read.");

        try
        {
            var stream = await streamRef.OpenReadStreamAsync(maxAllowedSize ?? fileInfo.Size, cancellationToken);

            // the stream reads through the reference, so the reference is handed to the stream to be released with it.
            return new BitFileInputContentStream(stream, streamRef);
        }
        catch
        {
            await streamRef.DisposeAsync();

            throw;
        }
    }

    /// <summary>
    /// Removes a specific file from the selected files list, or clears all files when no file is specified,
    /// invoking the <see cref="OnRemove"/> callback for each removed file and the <see cref="OnChange"/> callback afterwards.
    /// </summary>
    /// <param name="fileInfo">The file to remove, or null to remove all files.</param>
    public async Task RemoveFile(BitFileInputInfo? fileInfo = null)
    {
        if (IsDisposed) return;
        if (IsEnabled is false) return;
        if (_files.Any() is false) return;

        if (fileInfo is null)
        {
            var removedFiles = _files.ToArray();

            _files.Clear();
            _removeRefs.Clear();

            await _js.BitFileInputReset(UniqueId, _inputRef);

            if (IsDisposed) return;

            foreach (var file in removedFiles)
            {
                await OnRemove.InvokeAsync(file);

                if (IsDisposed) return;
            }
        }
        else
        {
            if (_files.Remove(fileInfo) is false) return;

            _removeRefs.Remove(fileInfo.FileId);

            await _js.BitFileInputRemoveFile(UniqueId, fileInfo.FileId);

            if (IsDisposed) return;

            await OnRemove.InvokeAsync(fileInfo);

            if (IsDisposed) return;
        }

        ApplyListValidations();

        await InvokeChangeCallbacksAsync();

        if (IsDisposed) return;

        StateHasChanged();
    }



    private bool _HasDescription => DescriptionTemplate is not null || Description.HasValue();



    protected override string RootElementClass => "bit-fin";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-fin-fil",
            BitVariant.Outline => "bit-fin-otl",
            BitVariant.Text => "bit-fin-txt",
            // a drop zone is a surface to drop onto rather than the primary action of the page, so it
            // defaults to the outlined variant whose rule is what the dashed drop indicator is drawn on.
            _ => ShowDropZone ? "bit-fin-otl" : "bit-fin-fil"
        });

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

        // the browse button is what carries the drop indicator, so a component rendered without one - hidden
        // or replaced by a LabelTemplate - needs the indicator drawn around itself instead of silently
        // accepting drops with nothing to show for it.
        ClassBuilder.Register(() => LabelTemplate is not null || HideLabel ? "bit-fin-nlb" : string.Empty);

        ClassBuilder.Register(() => ShowDropZone ? "bit-fin-dzn" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        InputId = $"FileInput-{UniqueId}-input";
        _buttonId = $"FileInput-{UniqueId}-label";
        _descriptionId = $"FileInput-{UniqueId}-description";

        return base.OnInitializedAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileInputParams))]
    protected override async Task OnParametersSetAsync()
    {
        CascadingParameters?.UpdateParameters(this);

        await base.OnParametersSetAsync();

        if (_dropZoneRef is null) return;

        await UpdateDropZone();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
        {
            await FocusAfterRemove();

            return;
        }

        _allowDrop = AllowDrop;
        _allowPaste = AllowPaste;
        _expandDirectories = Directory;
        _dragClass = GetDragClass();
        _dragStyle = Styles?.Dragging;

        _dropZoneRef = await _js.BitFileInputSetupDragDrop(RootElement, _inputRef, _dragClass, _dragStyle,
                                                           _allowDrop, _allowPaste, _expandDirectories);

        if (IsDisposed) return;
        if (_dropZoneRef is null) return;

        // a parameter change that arrived while the setup was still awaiting found no drop zone to update yet,
        // so the drop zone is synchronized once more against the parameters as they stand now.
        await UpdateDropZone();
    }



    private string GetDragClass() => $"bit-fin-drg {Classes?.Dragging}".Trim();

    // the remove button the user pressed leaves the DOM along with its file, and the focus with it - a keyboard
    // user would be dropped back onto the document and have to tab through the page again to get back here.
    // So the removal remembers where it happened and the next render hands the focus to the button that took
    // that place, to the last one when the list got shorter, or back to the browse button when nothing is left.
    private async Task HandleRemoveClick(BitFileInputInfo file)
    {
        var index = _files.IndexOf(file);

        await RemoveFile(file);

        if (IsDisposed) return;
        if (index < 0) return;

        _focusAfterRemoveIndex = index;

        StateHasChanged();
    }

    private async Task FocusAfterRemove()
    {
        if (_focusAfterRemoveIndex is null) return;

        var index = _focusAfterRemoveIndex.Value;

        _focusAfterRemoveIndex = null;

        try
        {
            if (ShowRemoveButton && IsEnabled && HideFileList is false &&
                FileViewTemplate is null && _files.Count > 0)
            {
                var target = _files[Math.Min(index, _files.Count - 1)];

                if (_removeRefs.TryGetValue(target.FileId, out var removeRef))
                {
                    await removeRef.FocusAsync();

                    return;
                }
            }

            if (LabelTemplate is null && HideLabel is false && IsEnabled)
            {
                await _labelRef.FocusAsync();
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // the element may already be gone, which is not worth failing a render over
    }

    // the name without its extension, which is the part the file list is allowed to ellipsize.
    private static string GetFileStem(string name)
    {
        var extension = BitFileInputInfo.GetExtension(name);

        return extension.HasNoValue() ? name : name[..^extension.Length];
    }

    // the extension the file list renders unshrunk beside the stem, which together spell the name back out.
    private static string GetFileExtension(string name) => BitFileInputInfo.GetExtension(name);

    // the id of an invalid file's error message, which its remove button is described by.
    private string GetMessageId(BitFileInputInfo file) => $"FileInput-{UniqueId}-{file.FileId}-message";

    private async Task UpdateDropZone()
    {
        var dragClass = GetDragClass();
        var dragStyle = Styles?.Dragging;

        if (_allowDrop == AllowDrop && _allowPaste == AllowPaste && _expandDirectories == Directory &&
            _dragClass == dragClass && _dragStyle == dragStyle) return;

        _allowDrop = AllowDrop;
        _allowPaste = AllowPaste;
        _expandDirectories = Directory;
        _dragClass = dragClass;
        _dragStyle = dragStyle;

        try
        {
            await _dropZoneRef.InvokeVoidAsync("update", _allowDrop, _allowPaste, _expandDirectories, _dragClass, _dragStyle);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private static bool AllowsAllFileTypes(IReadOnlyCollection<string>? allowedExtensions)
    {
        return allowedExtensions is null
            || allowedExtensions.Count == 0
            || allowedExtensions.Any(ext => ext?.Trim() is "*" or "*.*" or "*/*");
    }

    private static IEnumerable<string> GetNormalizedExtensions(IReadOnlyCollection<string> allowedExtensions)
    {
        // an entry is either a MIME type (it contains a slash) or a file extension whose leading dot is optional.
        return allowedExtensions.Select(ext => ext?.Trim())
                                .Where(ext => ext.HasValue())
                                .Select(ext => ext!.Contains('/') || ext.StartsWith('.') ? ext : $".{ext}");
    }

    private string? GetAcceptValue()
    {
        if (Accept.HasValue()) return Accept;

        if (AllowsAllFileTypes(AllowedExtensions)) return null;

        var accept = string.Join(",", GetNormalizedExtensions(AllowedExtensions));

        return accept.HasValue() ? accept : null;
    }

    private static bool IsFileTypeNotAllowed(BitFileInputInfo file, string[]? allowedTypes)
    {
        // a null list means every file type is allowed.
        if (allowedTypes is null) return false;

        var extension = Path.GetExtension(file.Name);

        foreach (var entry in allowedTypes)
        {
            if (entry.Contains('/'))
            {
                if (file.ContentType.HasNoValue()) continue;

                if (entry.EndsWith("/*", StringComparison.Ordinal))
                {
                    // a wildcard MIME type like "image/*" matches every subtype of that group.
                    if (file.ContentType.StartsWith(entry[..^1], StringComparison.OrdinalIgnoreCase)) return false;
                }
                else if (entry.Equals(file.ContentType, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                continue;
            }

            // files without an extension can never match an extension entry.
            if (extension.HasNoValue()) continue;

            if (entry.Equals(extension, StringComparison.OrdinalIgnoreCase)) return false;
        }

        return true;
    }

    // two selections of the same file are indistinguishable by their name, size and last modified time,
    // which is as close to an identity as the browser exposes for a picked file. The folder a file came
    // from joins them, since a folder selection routinely holds the same name twice and two such files
    // are not the same file.
    private static string GetFileIdentity(BitFileInputInfo file)
    {
        return $"{file.RelativePath}|{file.Name}|{file.Size}|{file.LastModified}";
    }

    // the directory part of the relative path a folder selection reports, which is what the file item
    // prints beside the size; an ordinary selection reports no path at all and prints nothing.
    private static string GetFolder(BitFileInputInfo file)
    {
        if (file.RelativePath.HasNoValue()) return string.Empty;

        var index = file.RelativePath.LastIndexOf('/');

        return index <= 0 ? string.Empty : file.RelativePath[..index];
    }

    private BitIconInfo? GetFileIcon(BitFileInputInfo file)
    {
        if (FileIconSelector is not null) return FileIconSelector(file);

        return BitIconInfo.Bit(GetDefaultFileIconName(file));
    }

    // the extension is checked first, for the document families the MIME type cannot tell apart (an Office
    // document is a zip to the browser); for everything else the MIME type the browser reports decides,
    // falling back to a plain page when the browser hands a file over with no type at all.
    private static string GetDefaultFileIconName(BitFileInputInfo file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();

        switch (extension)
        {
            case ".doc" or ".docx" or ".odt" or ".rtf": return "WordDocument";
            case ".xls" or ".xlsx" or ".ods" or ".csv": return "ExcelDocument";
            case ".ppt" or ".pptx" or ".odp": return "PowerPointDocument";
            case ".zip" or ".rar" or ".7z" or ".tar" or ".gz": return "ZipFolder";
            case ".pdf": return "PDF";
        }

        var contentType = file.ContentType;

        if (contentType.HasValue())
        {
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return "FileImage";
            if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return "Video";
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return "MusicInCollection";
            if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return "TextDocument";
        }

        return "Page";
    }

    private void ValidateFile(BitFileInputInfo file, string[]? allowedTypes)
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
        else if (IsFileTypeNotAllowed(file, allowedTypes))
        {
            file.IsValid = false;
            file.Message = NotAllowedExtensionErrorMessage ?? "The file type is not allowed";
        }
        else if (FileValidator is not null)
        {
            string? message;

            try
            {
                message = FileValidator(file);
            }
            catch (Exception ex)
            {
                // a throwing custom validator invalidates its own file instead of aborting the whole selection.
                message = ex.Message;
            }

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
            _removeRefs.Clear();
        }

        var newFiles = await _js.BitFileInputSetup(UniqueId, _inputRef, Append, ShowPreview, ReadImageDimensions);

        if (IsDisposed) return;

        // the allowed types are resolved once per selection instead of once per file,
        // since a folder selection can easily carry thousands of files.
        var allowedTypes = AllowsAllFileTypes(AllowedExtensions)
                            ? null
                            : GetNormalizedExtensions(AllowedExtensions).ToArray();

        foreach (var file in newFiles)
        {
            ValidateFile(file, allowedTypes);

            _files.Add(file);
        }

        ApplyListValidations();

        await InvokeChangeCallbacksAsync();
    }

    private void ApplyListValidations()
    {
        // the list level rules are re-evaluated from scratch so that files invalidated by a previous
        // evaluation can become valid again as soon as removals free up room or drop the original of a duplicate.
        for (var index = 0; index < _files.Count; index++)
        {
            var file = _files[index];

            file.Index = index;

            if (file.ListValidationFailed is false) continue;

            file.ListValidationFailed = false;
            file.IsValid = true;
            file.Message = null;
        }

        if (AllowDuplicates is false)
        {
            var knownFiles = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in _files)
            {
                // every file registers its identity, even an invalid one, so that a re-selection of a file
                // already in the list is caught no matter why that file is invalid.
                if (knownFiles.Add(GetFileIdentity(file))) continue;

                // a file that already failed a validation of its own keeps that message,
                // which would otherwise be lost as soon as the duplication is resolved.
                if (file.IsValid is false) continue;

                Invalidate(file, DuplicateErrorMessage ?? "The file is already selected");
            }
        }

        var count = 0;
        var totalSize = 0L;

        foreach (var file in _files)
        {
            // only the files that are otherwise valid consume the budget of the list level limits.
            if (file.IsValid is false) continue;

            if (MaxCount > 0 && count >= MaxCount)
            {
                Invalidate(file, MaxCountErrorMessage ?? "The maximum number of files is exceeded");
            }
            else if (MaxTotalSize > 0 && totalSize + file.Size > MaxTotalSize)
            {
                Invalidate(file, MaxTotalSizeErrorMessage ?? "The total size of the files is larger than the max total size");
            }
            else
            {
                count++;
                totalSize += file.Size;
            }
        }

        static void Invalidate(BitFileInputInfo file, string message)
        {
            file.ListValidationFailed = true;
            file.IsValid = false;
            file.Message = message;
        }
    }

    private async Task InvokeChangeCallbacksAsync()
    {
        Announce();

        await OnChange.InvokeAsync([.. _files]);

        if (OnInvalid.HasDelegate is false) return;

        var invalidFiles = _files.Where(f => f.IsValid is false).ToArray();

        if (invalidFiles.Length == 0) return;

        await OnInvalid.InvokeAsync(invalidFiles);
    }

    private void Announce()
    {
        var text = GetAnnouncementText();

        // screen readers skip a live region update that repeats the previous text verbatim,
        // so an invisible zero width space is alternated to make every update unique.
        _announcementMarker = !_announcementMarker;

        _announcement = text.HasValue() && _announcementMarker ? text + '\u200B' : text;
    }

    private string? GetAnnouncementText()
    {
        if (AnnouncementProvider is not null) return AnnouncementProvider(_files);

        if (_files.Count == 0) return "No file selected.";

        var invalidCount = _files.Count(f => f.IsValid is false);

        return $"{_files.Count} file{(_files.Count == 1 ? string.Empty : "s")} selected." +
               (invalidCount > 0 ? $" {invalidCount} of them invalid." : string.Empty);
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
