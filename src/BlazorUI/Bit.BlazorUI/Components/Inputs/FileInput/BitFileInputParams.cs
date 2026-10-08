namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitFileInput"/> component.
/// </summary>
public class BitFileInputParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitFileInput"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitFileInput value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitFileInput)}";



    public string Name => ParamName;



    /// <summary>
    /// Accepted file types for the file browser using MIME types or file extensions (e.g., "image/*", ".pdf,.doc").
    /// </summary>
    public string? Accept { get; set; }

    /// <summary>
    /// Whether files can be selected by dragging them from the operating system and dropping them on the component.
    /// </summary>
    public bool? AllowDrop { get; set; }

    /// <summary>
    /// Whether a file that is already in the file list can be selected again.
    /// </summary>
    public bool? AllowDuplicates { get; set; }

    /// <summary>
    /// Allowed file types for validation purposes, accepting both file extensions and MIME types with an optional wildcard.
    /// </summary>
    public IReadOnlyCollection<string>? AllowedExtensions { get; set; }

    /// <summary>
    /// Whether files can be selected by pasting them from the clipboard onto the component.
    /// </summary>
    public bool? AllowPaste { get; set; }

    /// <summary>
    /// Custom provider of the text announced by the screen reader whenever the file list changes.
    /// </summary>
    public Func<IReadOnlyList<BitFileInputInfo>, string?>? AnnouncementProvider { get; set; }

    /// <summary>
    /// Whether to append newly selected files to the existing file list instead of replacing it.
    /// </summary>
    public bool? Append { get; set; }

    /// <summary>
    /// Whether the file input is automatically reset (cleared) before opening the file browser dialog.
    /// </summary>
    public bool? AutoReset { get; set; }

    /// <summary>
    /// The capture behavior of the file input on devices with a camera or microphone (e.g., "user", "environment").
    /// </summary>
    public string? Capture { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the file input.
    /// </summary>
    public BitFileInputClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the file input, applied to the browse button and the drag-and-drop indicator.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// A short hint rendered under the browse button and wired to it through <c>aria-describedby</c>.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to select folders (directories) instead of files, rendered as the <c>webkitdirectory</c> attribute.
    /// </summary>
    public bool? Directory { get; set; }

    /// <summary>
    /// Gets or sets the glyph of the drop zone panel using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? DropZoneIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the drop zone panel's glyph from the built-in Fluent UI icons.
    /// </summary>
    public string? DropZoneIconName { get; set; }

    /// <summary>
    /// Custom error message displayed when a file is selected again while <see cref="AllowDuplicates"/> is disabled.
    /// </summary>
    public string? DuplicateErrorMessage { get; set; }

    /// <summary>
    /// Custom provider of the glyph shown in the thumbnail's place for a file that has no image preview.
    /// </summary>
    public Func<BitFileInputInfo, BitIconInfo?>? FileIconSelector { get; set; }

    /// <summary>
    /// The accessible name of the file list, which is a piece of English a whole app localizes once.
    /// </summary>
    public string? FileListAriaLabel { get; set; }

    /// <summary>
    /// Custom formatter of the file size shown under the name of each file item, which is where a localized
    /// unit name belongs - the kind of decision a whole app makes once rather than per file input.
    /// </summary>
    public Func<long, string>? FileSizeFormatter { get; set; }

    /// <summary>
    /// Custom validation function called for each newly selected file after the built-in validations pass.
    /// </summary>
    public Func<BitFileInputInfo, string?>? FileValidator { get; set; }

    /// <summary>
    /// Whether to hide the file list that displays the selected files in the UI.
    /// </summary>
    public bool? HideFileList { get; set; }

    /// <summary>
    /// Whether to hide the default browse button label from the UI.
    /// </summary>
    public bool? HideLabel { get; set; }

    /// <summary>
    /// The text displayed on the browse button.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Maximum allowed number of files in the file list. Set to 0 for no count limit.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Custom error message displayed when the number of files exceeds the maximum count limit.
    /// </summary>
    public string? MaxCountErrorMessage { get; set; }

    /// <summary>
    /// Maximum allowed file size in bytes for validation. Set to 0 for no size limit.
    /// </summary>
    public long? MaxSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file exceeds the maximum size limit.
    /// </summary>
    public string? MaxSizeErrorMessage { get; set; }

    /// <summary>
    /// Maximum allowed total size in bytes of all the files in the file list. Set to 0 for no total size limit.
    /// </summary>
    public long? MaxTotalSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file makes the total size of the file list exceed the maximum total size.
    /// </summary>
    public string? MaxTotalSizeErrorMessage { get; set; }

    /// <summary>
    /// Minimum allowed file size in bytes for validation. Set to 0 for no size limit.
    /// </summary>
    public long? MinSize { get; set; }

    /// <summary>
    /// Custom error message displayed when a file is smaller than the minimum size limit.
    /// </summary>
    public string? MinSizeErrorMessage { get; set; }

    /// <summary>
    /// Whether to allow selecting multiple files simultaneously through the file browser dialog.
    /// </summary>
    public bool? Multiple { get; set; }

    /// <summary>
    /// Custom error message displayed when a file's extension is not in the allowed extensions list.
    /// </summary>
    public string? NotAllowedExtensionErrorMessage { get; set; }

    /// <summary>
    /// Whether to decode every selected image file to fill its pixel dimensions before validation runs.
    /// </summary>
    public bool? ReadImageDimensions { get; set; }

    /// <summary>
    /// The remove button icon, using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? RemoveButtonIcon { get; set; }

    /// <summary>
    /// The name of the remove button icon from the built-in Fluent UI icons.
    /// </summary>
    public string? RemoveButtonIconName { get; set; }

    /// <summary>
    /// The tooltip of the remove button, which is also used as the prefix of its accessible label.
    /// </summary>
    public string? RemoveButtonTitle { get; set; }

    /// <summary>
    /// Whether to render the browse area as a full width drop zone panel instead of an ordinary button.
    /// </summary>
    public bool? ShowDropZone { get; set; }

    /// <summary>
    /// Whether to display a preview thumbnail for image files in the file list.
    /// </summary>
    public bool? ShowPreview { get; set; }

    /// <summary>
    /// Whether to display a remove button next to each file in the file list.
    /// </summary>
    public bool? ShowRemoveButton { get; set; }

    /// <summary>
    /// The size of the file input, applied to the browse button and the file list items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the file input.
    /// </summary>
    public BitFileInputClassStyles? Styles { get; set; }

    /// <summary>
    /// The tooltip of the browse button, rendered as its title attribute.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The visual variant of the browse button: a full fill, only an outline, or neither.
    /// </summary>
    public BitVariant? Variant { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitFileInput"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitFileInput"/> itself.
    /// </summary>
    /// <param name="bitFileInput">
    /// The <see cref="BitFileInput"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitFileInput bitFileInput)
    {
        if (bitFileInput is null) return;

        UpdateBaseParameters(bitFileInput);

        if (Accept.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(Accept), Accept, static f => f.Accept, static (f, v) => f.Accept = v);
        }

        if (AllowDrop.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(AllowDrop), AllowDrop.Value, static f => f.AllowDrop, static (f, v) => f.AllowDrop = v);
        }

        if (AllowDuplicates.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(AllowDuplicates), AllowDuplicates.Value, static f => f.AllowDuplicates, static (f, v) => f.AllowDuplicates = v);
        }

        if (AllowedExtensions is not null)
        {
            bitFileInput.TakeFromCascade(nameof(AllowedExtensions), AllowedExtensions, static f => f.AllowedExtensions, static (f, v) => f.AllowedExtensions = v);
        }

        if (AllowPaste.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(AllowPaste), AllowPaste.Value, static f => f.AllowPaste, static (f, v) => f.AllowPaste = v);
        }

        if (AnnouncementProvider is not null)
        {
            bitFileInput.TakeFromCascade(nameof(AnnouncementProvider), AnnouncementProvider, static f => f.AnnouncementProvider, static (f, v) => f.AnnouncementProvider = v);
        }

        if (Append.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Append), Append.Value, static f => f.Append, static (f, v) => f.Append = v);
        }

        if (AutoReset.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(AutoReset), AutoReset.Value, static f => f.AutoReset, static (f, v) => f.AutoReset = v);
        }

        if (Capture.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(Capture), Capture, static f => f.Capture, static (f, v) => f.Capture = v);
        }

        if (Classes is not null)
        {
            bitFileInput.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (Color.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Color), Color.Value, static f => f.Color, static (f, v) => f.Color = v);
        }

        if (Description.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(Description), Description, static f => f.Description, static (f, v) => f.Description = v);
        }

        if (Directory.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Directory), Directory.Value, static f => f.Directory, static (f, v) => f.Directory = v);
        }

        if (DropZoneIcon is not null)
        {
            bitFileInput.TakeFromCascade(nameof(DropZoneIcon), DropZoneIcon, static f => f.DropZoneIcon, static (f, v) => f.DropZoneIcon = v);
        }

        if (DropZoneIconName.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(DropZoneIconName), DropZoneIconName, static f => f.DropZoneIconName, static (f, v) => f.DropZoneIconName = v);
        }

        if (DuplicateErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(DuplicateErrorMessage), DuplicateErrorMessage, static f => f.DuplicateErrorMessage, static (f, v) => f.DuplicateErrorMessage = v);
        }

        if (FileIconSelector is not null)
        {
            bitFileInput.TakeFromCascade(nameof(FileIconSelector), FileIconSelector, static f => f.FileIconSelector, static (f, v) => f.FileIconSelector = v);
        }

        if (FileListAriaLabel.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(FileListAriaLabel), FileListAriaLabel, static f => f.FileListAriaLabel, static (f, v) => f.FileListAriaLabel = v);
        }

        if (FileSizeFormatter is not null)
        {
            bitFileInput.TakeFromCascade(nameof(FileSizeFormatter), FileSizeFormatter, static f => f.FileSizeFormatter, static (f, v) => f.FileSizeFormatter = v);
        }

        if (FileValidator is not null)
        {
            bitFileInput.TakeFromCascade(nameof(FileValidator), FileValidator, static f => f.FileValidator, static (f, v) => f.FileValidator = v);
        }

        if (HideFileList.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(HideFileList), HideFileList.Value, static f => f.HideFileList, static (f, v) => f.HideFileList = v);
        }

        if (HideLabel.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(HideLabel), HideLabel.Value, static f => f.HideLabel, static (f, v) => f.HideLabel = v);
        }

        if (Label.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(Label), Label, static f => f.Label, static (f, v) => f.Label = v);
        }

        if (MaxCount.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(MaxCount), MaxCount.Value, static f => f.MaxCount, static (f, v) => f.MaxCount = v);
        }

        if (MaxCountErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(MaxCountErrorMessage), MaxCountErrorMessage, static f => f.MaxCountErrorMessage, static (f, v) => f.MaxCountErrorMessage = v);
        }

        if (MaxSize.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(MaxSize), MaxSize.Value, static f => f.MaxSize, static (f, v) => f.MaxSize = v);
        }

        if (MaxSizeErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(MaxSizeErrorMessage), MaxSizeErrorMessage, static f => f.MaxSizeErrorMessage, static (f, v) => f.MaxSizeErrorMessage = v);
        }

        if (MaxTotalSize.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(MaxTotalSize), MaxTotalSize.Value, static f => f.MaxTotalSize, static (f, v) => f.MaxTotalSize = v);
        }

        if (MaxTotalSizeErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(MaxTotalSizeErrorMessage), MaxTotalSizeErrorMessage, static f => f.MaxTotalSizeErrorMessage, static (f, v) => f.MaxTotalSizeErrorMessage = v);
        }

        if (MinSize.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(MinSize), MinSize.Value, static f => f.MinSize, static (f, v) => f.MinSize = v);
        }

        if (MinSizeErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(MinSizeErrorMessage), MinSizeErrorMessage, static f => f.MinSizeErrorMessage, static (f, v) => f.MinSizeErrorMessage = v);
        }

        if (Multiple.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Multiple), Multiple.Value, static f => f.Multiple, static (f, v) => f.Multiple = v);
        }

        if (NotAllowedExtensionErrorMessage.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(NotAllowedExtensionErrorMessage), NotAllowedExtensionErrorMessage, static f => f.NotAllowedExtensionErrorMessage, static (f, v) => f.NotAllowedExtensionErrorMessage = v);
        }

        if (ReadImageDimensions.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(ReadImageDimensions), ReadImageDimensions.Value, static f => f.ReadImageDimensions, static (f, v) => f.ReadImageDimensions = v);
        }

        if (RemoveButtonIcon is not null)
        {
            bitFileInput.TakeFromCascade(nameof(RemoveButtonIcon), RemoveButtonIcon, static f => f.RemoveButtonIcon, static (f, v) => f.RemoveButtonIcon = v);
        }

        if (RemoveButtonIconName.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(RemoveButtonIconName), RemoveButtonIconName, static f => f.RemoveButtonIconName, static (f, v) => f.RemoveButtonIconName = v);
        }

        if (RemoveButtonTitle.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(RemoveButtonTitle), RemoveButtonTitle, static f => f.RemoveButtonTitle, static (f, v) => f.RemoveButtonTitle = v);
        }

        if (ShowDropZone.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(ShowDropZone), ShowDropZone.Value, static f => f.ShowDropZone, static (f, v) => f.ShowDropZone = v);
        }

        if (ShowPreview.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(ShowPreview), ShowPreview.Value, static f => f.ShowPreview, static (f, v) => f.ShowPreview = v);
        }

        if (ShowRemoveButton.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(ShowRemoveButton), ShowRemoveButton.Value, static f => f.ShowRemoveButton, static (f, v) => f.ShowRemoveButton = v);
        }

        if (Size.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Size), Size.Value, static f => f.Size, static (f, v) => f.Size = v);
        }

        if (Styles is not null)
        {
            bitFileInput.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }

        if (Title.HasValue())
        {
            bitFileInput.TakeFromCascade(nameof(Title), Title, static f => f.Title, static (f, v) => f.Title = v);
        }

        if (Variant.HasValue)
        {
            bitFileInput.TakeFromCascade(nameof(Variant), Variant.Value, static f => f.Variant, static (f, v) => f.Variant = v);
        }
    }
}
