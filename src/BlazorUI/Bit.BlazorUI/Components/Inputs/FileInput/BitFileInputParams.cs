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
    /// Custom error message displayed when a file is selected again while <see cref="AllowDuplicates"/> is disabled.
    /// </summary>
    public string? DuplicateErrorMessage { get; set; }

    /// <summary>
    /// Custom provider of the glyph shown in the thumbnail's place for a file that has no image preview.
    /// </summary>
    public Func<BitFileInputInfo, BitIconInfo?>? FileIconSelector { get; set; }

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
    /// Whether to display a preview thumbnail for image files, and a file type glyph for everything else.
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

        if (Accept.HasValue() && bitFileInput.HasNotBeenSet(nameof(Accept)))
        {
            bitFileInput.Accept = Accept;
        }

        if (AllowDrop.HasValue && bitFileInput.HasNotBeenSet(nameof(AllowDrop)))
        {
            bitFileInput.AllowDrop = AllowDrop.Value;
        }

        if (AllowDuplicates.HasValue && bitFileInput.HasNotBeenSet(nameof(AllowDuplicates)))
        {
            bitFileInput.AllowDuplicates = AllowDuplicates.Value;
        }

        if (AllowedExtensions is not null && bitFileInput.HasNotBeenSet(nameof(AllowedExtensions)))
        {
            bitFileInput.AllowedExtensions = AllowedExtensions;
        }

        if (AllowPaste.HasValue && bitFileInput.HasNotBeenSet(nameof(AllowPaste)))
        {
            bitFileInput.AllowPaste = AllowPaste.Value;
        }

        if (AnnouncementProvider is not null && bitFileInput.HasNotBeenSet(nameof(AnnouncementProvider)))
        {
            bitFileInput.AnnouncementProvider = AnnouncementProvider;
        }

        if (Append.HasValue && bitFileInput.HasNotBeenSet(nameof(Append)))
        {
            bitFileInput.Append = Append.Value;
        }

        if (AutoReset.HasValue && bitFileInput.HasNotBeenSet(nameof(AutoReset)))
        {
            bitFileInput.AutoReset = AutoReset.Value;
        }

        if (Capture.HasValue() && bitFileInput.HasNotBeenSet(nameof(Capture)))
        {
            bitFileInput.Capture = Capture;
        }

        if (Classes is not null && bitFileInput.HasNotBeenSet(nameof(Classes)))
        {
            bitFileInput.Classes = Classes;

            bitFileInput.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitFileInput.HasNotBeenSet(nameof(Color)))
        {
            bitFileInput.Color = Color.Value;

            bitFileInput.ClassBuilder.Reset();
        }

        if (Description.HasValue() && bitFileInput.HasNotBeenSet(nameof(Description)))
        {
            bitFileInput.Description = Description;
        }

        if (Directory.HasValue && bitFileInput.HasNotBeenSet(nameof(Directory)))
        {
            bitFileInput.Directory = Directory.Value;
        }

        if (DuplicateErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(DuplicateErrorMessage)))
        {
            bitFileInput.DuplicateErrorMessage = DuplicateErrorMessage;
        }

        if (FileIconSelector is not null && bitFileInput.HasNotBeenSet(nameof(FileIconSelector)))
        {
            bitFileInput.FileIconSelector = FileIconSelector;
        }

        if (FileSizeFormatter is not null && bitFileInput.HasNotBeenSet(nameof(FileSizeFormatter)))
        {
            bitFileInput.FileSizeFormatter = FileSizeFormatter;
        }

        if (FileValidator is not null && bitFileInput.HasNotBeenSet(nameof(FileValidator)))
        {
            bitFileInput.FileValidator = FileValidator;
        }

        if (HideFileList.HasValue && bitFileInput.HasNotBeenSet(nameof(HideFileList)))
        {
            bitFileInput.HideFileList = HideFileList.Value;
        }

        if (HideLabel.HasValue && bitFileInput.HasNotBeenSet(nameof(HideLabel)))
        {
            bitFileInput.HideLabel = HideLabel.Value;
        }

        if (Label.HasValue() && bitFileInput.HasNotBeenSet(nameof(Label)))
        {
            bitFileInput.Label = Label;
        }

        if (MaxCount.HasValue && bitFileInput.HasNotBeenSet(nameof(MaxCount)))
        {
            bitFileInput.MaxCount = MaxCount.Value;
        }

        if (MaxCountErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(MaxCountErrorMessage)))
        {
            bitFileInput.MaxCountErrorMessage = MaxCountErrorMessage;
        }

        if (MaxSize.HasValue && bitFileInput.HasNotBeenSet(nameof(MaxSize)))
        {
            bitFileInput.MaxSize = MaxSize.Value;
        }

        if (MaxSizeErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(MaxSizeErrorMessage)))
        {
            bitFileInput.MaxSizeErrorMessage = MaxSizeErrorMessage;
        }

        if (MaxTotalSize.HasValue && bitFileInput.HasNotBeenSet(nameof(MaxTotalSize)))
        {
            bitFileInput.MaxTotalSize = MaxTotalSize.Value;
        }

        if (MaxTotalSizeErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(MaxTotalSizeErrorMessage)))
        {
            bitFileInput.MaxTotalSizeErrorMessage = MaxTotalSizeErrorMessage;
        }

        if (MinSize.HasValue && bitFileInput.HasNotBeenSet(nameof(MinSize)))
        {
            bitFileInput.MinSize = MinSize.Value;
        }

        if (MinSizeErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(MinSizeErrorMessage)))
        {
            bitFileInput.MinSizeErrorMessage = MinSizeErrorMessage;
        }

        if (Multiple.HasValue && bitFileInput.HasNotBeenSet(nameof(Multiple)))
        {
            bitFileInput.Multiple = Multiple.Value;
        }

        if (NotAllowedExtensionErrorMessage.HasValue() && bitFileInput.HasNotBeenSet(nameof(NotAllowedExtensionErrorMessage)))
        {
            bitFileInput.NotAllowedExtensionErrorMessage = NotAllowedExtensionErrorMessage;
        }

        if (ReadImageDimensions.HasValue && bitFileInput.HasNotBeenSet(nameof(ReadImageDimensions)))
        {
            bitFileInput.ReadImageDimensions = ReadImageDimensions.Value;
        }

        if (RemoveButtonIcon is not null && bitFileInput.HasNotBeenSet(nameof(RemoveButtonIcon)))
        {
            bitFileInput.RemoveButtonIcon = RemoveButtonIcon;
        }

        if (RemoveButtonIconName.HasValue() && bitFileInput.HasNotBeenSet(nameof(RemoveButtonIconName)))
        {
            bitFileInput.RemoveButtonIconName = RemoveButtonIconName;
        }

        if (RemoveButtonTitle.HasValue() && bitFileInput.HasNotBeenSet(nameof(RemoveButtonTitle)))
        {
            bitFileInput.RemoveButtonTitle = RemoveButtonTitle;
        }

        if (ShowPreview.HasValue && bitFileInput.HasNotBeenSet(nameof(ShowPreview)))
        {
            bitFileInput.ShowPreview = ShowPreview.Value;
        }

        if (ShowRemoveButton.HasValue && bitFileInput.HasNotBeenSet(nameof(ShowRemoveButton)))
        {
            bitFileInput.ShowRemoveButton = ShowRemoveButton.Value;
        }

        if (Size.HasValue && bitFileInput.HasNotBeenSet(nameof(Size)))
        {
            bitFileInput.Size = Size.Value;

            bitFileInput.ClassBuilder.Reset();
        }

        if (Styles is not null && bitFileInput.HasNotBeenSet(nameof(Styles)))
        {
            bitFileInput.Styles = Styles;

            bitFileInput.StyleBuilder.Reset();
        }

        if (Title.HasValue() && bitFileInput.HasNotBeenSet(nameof(Title)))
        {
            bitFileInput.Title = Title;
        }

        if (Variant.HasValue && bitFileInput.HasNotBeenSet(nameof(Variant)))
        {
            bitFileInput.Variant = Variant.Value;

            bitFileInput.ClassBuilder.Reset();
        }
    }
}
