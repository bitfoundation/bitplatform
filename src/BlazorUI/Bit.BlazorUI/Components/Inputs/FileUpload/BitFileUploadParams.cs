namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitFileUpload"/> component.
/// </summary>
public class BitFileUploadParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitFileUpload"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitFileUpload value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitFileUpload)}";



    public string Name => ParamName;



    /// <summary>
    /// Accepted file types for the file browser using MIME types or file extensions (e.g., "image/*", ".pdf,.doc").
    /// Applied to the underlying HTML input element's accept attribute.
    /// When not set, the accept attribute is generated from <see cref="AllowedExtensions"/>.
    /// </summary>
    public string? Accept { get; set; }

    /// <summary>
    /// Whether files can be selected by dragging them from the operating system and dropping them on the component.
    /// The default value is true.
    /// </summary>
    public bool? AllowDrop { get; set; }

    /// <summary>
    /// Whether a file that is already in the file list can be selected again.
    /// When disabled, a newly selected file matching an existing one by name, size and last modified time
    /// is rejected with the <see cref="DuplicateErrorMessage"/> instead of being uploaded a second time,
    /// becoming eligible again once the file it duplicates is removed.
    /// The default value is true.
    /// </summary>
    public bool? AllowDuplicates { get; set; }

    /// <summary>
    /// Whether files can be selected by pasting them from the clipboard onto the component.
    /// The paste is only captured while the focus is inside the component, so the browse button must be focused first.
    /// The default value is true.
    /// </summary>
    public bool? AllowPaste { get; set; }

    /// <summary>
    /// Allowed file types for validation purposes, accepting both file extensions (e.g., [".jpg", ".png", ".pdf"])
    /// and MIME types with an optional wildcard (e.g., ["image/*", "application/pdf"]).
    /// The leading dot of an extension is optional and the matching is case-insensitive.
    /// Use ["*"] to allow all file types. Files not matching any of these entries will not be uploaded.
    /// </summary>
    public IReadOnlyCollection<string>? AllowedExtensions { get; set; }

    /// <summary>
    /// Custom provider of the text announced by the screen reader through the live region of the component
    /// whenever the file list or an upload outcome changes. Receives the current file list and returns the text
    /// to announce, or null to announce nothing. When not set, a built-in English announcement is used.
    /// </summary>
    public Func<IReadOnlyList<BitFileInfo>, string?>? AnnouncementProvider { get; set; }

    /// <summary>
    /// Whether a new selection is added to the end of the current file list instead of replacing it,
    /// which is what lets the user build a batch up over several rounds of browsing, dropping or pasting.
    /// The files already in the list keep their upload state.
    /// </summary>
    public bool? Append { get; set; }

    /// <summary>
    /// Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB.
    /// </summary>
    public bool? AutoChunkSize { get; set; }

    /// <summary>
    /// Whether the file list and the upload state are cleared right before the file dialog opens, so that
    /// every browse starts from a clean slate - the list empties even if the dialog is then cancelled.
    /// </summary>
    public bool? AutoReset { get; set; }

    /// <summary>
    /// The number of times a failed upload of a file gets retried automatically before it is reported as failed.
    /// In the chunked mode each retry resumes from the last successfully uploaded chunk.
    /// Set to 0 (the default) to disable the automatic retries.
    /// </summary>
    public int? AutoRetries { get; set; }

    /// <summary>
    /// The delay before each automatic retry of a failed upload.
    /// Set to null (the default) to retry immediately.
    /// </summary>
    public TimeSpan? AutoRetryDelay { get; set; }

    /// <summary>
    /// Whether the selected files start uploading the moment they are selected, skipping the per-file
    /// upload button entirely, for the cases where the selection itself expresses the intent to upload.
    /// </summary>
    public bool? AutoUpload { get; set; }

    /// <summary>
    /// The tooltip of the cancel upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Cancel report.pdf"). Defaults to "Cancel".
    /// </summary>
    public string? CancelButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the cancel upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CancelIconName"/> when both are set.
    /// Defaults to the built-in Cancel icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom cancel icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CancelIconName"/> instead.
    /// </remarks>
    public BitIconInfo? CancelIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the cancel upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Cancel</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Cancel</c>).
    /// <br />
    /// For external icon libraries, use <see cref="CancelIcon"/> instead.
    /// </remarks>
    public string? CancelIconName { get; set; }

    /// <summary>
    /// The message shown for canceled file uploads.
    /// </summary>
    public string? CanceledUploadMessage { get; set; }

    /// <summary>
    /// The capture behavior of the file input on devices with a camera or microphone,
    /// rendered as the capture attribute of the input element (e.g., "user" for the front camera,
    /// "environment" for the rear camera).
    /// </summary>
    public string? Capture { get; set; }

    /// <summary>
    /// The size in bytes of each chunk of a chunked upload. When not set - and whenever
    /// <see cref="AutoChunkSize"/> is enabled, which takes the decision over - it starts at 512 KB.
    /// </summary>
    public long? ChunkSize { get; set; }

    /// <summary>
    /// Whether each file is sliced and sent as a series of sequential requests instead of one monolithic
    /// one, which is what makes a paused or failed file resume from the last chunk that made it through
    /// rather than starting over, so a dropped connection costs one chunk instead of the whole transfer.
    /// </summary>
    public bool? ChunkedUpload { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitFileUpload.
    /// </summary>
    public BitFileUploadClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the file upload, applied to the browse button, the drag-and-drop indicator,
    /// the progress bars and the hovered action buttons.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The maximum number of files uploading at the same time, the remaining ones waiting in a queue
    /// and starting as soon as a slot frees up. Set to 0 (the default) to start every file at once.
    /// </summary>
    public int? ConcurrentUploads { get; set; }

    /// <summary>
    /// A short hint rendered under the browse button and wired to it through aria-describedby,
    /// which is the place to spell out the accepted file types and the size limits so that both sighted
    /// and screen reader users learn the constraints before hitting them.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to select folders (directories) instead of files, rendered as the webkitdirectory attribute.
    /// All files inside the selected folder and its subfolders will be added to the file list.
    /// It also makes a dropped folder expand into its contents instead of being ignored.
    /// </summary>
    public bool? Directory { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being already in the file list
    /// while <see cref="AllowDuplicates"/> is disabled.
    /// </summary>
    public string? DuplicateErrorMessage { get; set; }

    /// <summary>
    /// The message shown for failed file removes.
    /// </summary>
    public string? FailedRemoveMessage { get; set; }

    /// <summary>
    /// The message shown for failed file uploads.
    /// </summary>
    public string? FailedUploadMessage { get; set; }

    /// <summary>
    /// The accessible name of the file list, so that a screen reader user landing on it is told what the list
    /// they are in holds instead of only how many items it has. Set it to an empty string to leave the list
    /// unnamed. The default value is "Selected files".
    /// </summary>
    public string? FileListAriaLabel { get; set; }

    /// <summary>
    /// Custom formatter of the file size shown under the name of each file item.
    /// Receives the size of the file in bytes and returns the text to display,
    /// which is the place to localize the units or to switch between the binary and the decimal bases.
    /// When not set, a built-in humanizer is used.
    /// </summary>
    public Func<long, string>? FileSizeFormatter { get; set; }

    /// <summary>
    /// Custom validation function called for each newly selected file after the built-in validations pass.
    /// Return an error message to reject the file so it will not be uploaded, or null to accept it.
    /// </summary>
    public Func<BitFileInfo, string?>? FileValidator { get; set; }

    /// <summary>
    /// Whether the built-in file list is left unrendered. The files are still selected, validated,
    /// uploaded and reported through <see cref="BitFileUpload.Files"/> and the callbacks - they are simply not drawn,
    /// which is what the surrounding page needs when it shows the attachments in a layout of its own.
    /// </summary>
    public bool? HideFileView { get; set; }

    /// <summary>
    /// Whether to hide the default browse button label from the UI.
    /// </summary>
    public bool? HideLabel { get; set; }

    /// <summary>
    /// The text of the browse button. Setting it to an empty string hides the button altogether.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Maximum allowed number of files in the file list (0 for unlimited).
    /// Files selected beyond this count are rejected at selection time and will not be uploaded.
    /// Only files that pass the other validations consume a slot.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Specifies the message shown for the files rejected due to exceeding the maximum number of files.
    /// </summary>
    public string? MaxCountErrorMessage { get; set; }

    /// <summary>
    /// The maximum allowed size in bytes of each file (0 for unlimited). A larger file is rejected at
    /// selection time with the <see cref="MaxSizeErrorMessage"/> and will not be uploaded.
    /// </summary>
    public long? MaxSize { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being larger than the <see cref="MaxSize"/>.
    /// </summary>
    public string? MaxSizeErrorMessage { get; set; }

    /// <summary>
    /// Maximum allowed total size in bytes of all the files of the file list (0 for unlimited).
    /// Files pushing the accumulated size beyond this limit are rejected at selection time and will not be
    /// uploaded, becoming eligible again once removals free up room.
    /// Only files that pass the other validations consume the budget.
    /// </summary>
    public long? MaxTotalSize { get; set; }

    /// <summary>
    /// Specifies the message shown for the files rejected for making the total size of the file list
    /// exceed the maximum total size.
    /// </summary>
    public string? MaxTotalSizeErrorMessage { get; set; }

    /// <summary>
    /// The minimum allowed size in bytes of each file (0 for no limit). A smaller file is rejected at
    /// selection time with the <see cref="MinSizeErrorMessage"/> and will not be uploaded.
    /// </summary>
    public long? MinSize { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being smaller than the <see cref="MinSize"/>.
    /// </summary>
    public string? MinSizeErrorMessage { get; set; }

    /// <summary>
    /// Whether several files can be handed over at once, both through the file dialog and through a
    /// single drop or paste. Without it a multi-file drop or paste is trimmed down to its first file.
    /// </summary>
    public bool? Multiple { get; set; }

    /// <summary>
    /// The message shown for the files rejected for not matching any entry of <see cref="AllowedExtensions"/>.
    /// </summary>
    public string? NotAllowedExtensionErrorMessage { get; set; }

    /// <summary>
    /// The tooltip of the pause upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Pause report.pdf"). Defaults to "Pause".
    /// </summary>
    public string? PauseButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the pause upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PauseIconName"/> when both are set.
    /// Defaults to the built-in Pause icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom pause icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="PauseIconName"/> instead.
    /// </remarks>
    public BitIconInfo? PauseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the pause upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Pause</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Pause</c>).
    /// <br />
    /// For external icon libraries, use <see cref="PauseIcon"/> instead.
    /// </remarks>
    public string? PauseIconName { get; set; }

    /// <summary>
    /// The message shown for the files waiting in the queue for a free slot of the <see cref="ConcurrentUploads"/>
    /// limit, which is what tells a file that is about to start apart from one that was never asked to upload.
    /// </summary>
    public string? QueuedUploadMessage { get; set; }

    /// <summary>
    /// Whether to read the pixel dimensions of the selected image files, filling the
    /// <see cref="BitFileInfo.Width"/> and <see cref="BitFileInfo.Height"/> of each of them before the
    /// validations run, so that a <see cref="FileValidator"/> can reject an image by its dimensions.
    /// Reading them means decoding every image in the browser, which costs time and memory on a large
    /// selection, so it is off by default.
    /// </summary>
    public bool? ReadImageDimensions { get; set; }

    /// <summary>
    /// The tooltip of the remove file button, which is also used as the prefix of its accessible label
    /// (e.g., "Remove report.pdf"). Defaults to "Remove".
    /// </summary>
    public string? RemoveButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the remove file button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="RemoveIconName"/> when both are set.
    /// Defaults to the built-in Delete icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom remove icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="RemoveIconName"/> instead.
    /// </remarks>
    public BitIconInfo? RemoveIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the remove file button from the built-in Fluent UI icons.
    /// Defaults to <c>Delete</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Delete</c>).
    /// <br />
    /// For external icon libraries, use <see cref="RemoveIcon"/> instead.
    /// </remarks>
    public string? RemoveIconName { get; set; }

    /// <summary>
    /// Custom HTTP headers attached to the remove request.
    /// </summary>
    public Dictionary<string, string>? RemoveRequestHttpHeaders { get; set; }

    /// <summary>
    /// The provider function creating the HTTP headers of the remove request, invoked right before the
    /// request goes out and taking precedence over <see cref="RemoveRequestHttpHeaders"/>.
    /// </summary>
    public Func<Task<Dictionary<string, string>>>? RemoveRequestHttpHeadersProvider { get; set; }

    /// <summary>
    /// The HTTP method of the remove request (e.g., "POST"). Defaults to "DELETE".
    /// </summary>
    public string? RemoveRequestHttpMethod { get; set; }

    /// <summary>
    /// Custom query strings appended to the URL of the remove request.
    /// </summary>
    public Dictionary<string, string>? RemoveRequestQueryStrings { get; set; }

    /// <summary>
    /// The provider function creating the query strings of the remove request, invoked right before the
    /// request goes out and taking precedence over <see cref="RemoveRequestQueryStrings"/>.
    /// </summary>
    public Func<Task<Dictionary<string, string>>>? RemoveRequestQueryStringsProvider { get; set; }

    /// <summary>
    /// URL of the server endpoint removing the files. A file whose bytes already reached the server is
    /// deleted from it through a request to this URL carrying its name as a query string and its id in
    /// the BIT_FILE_ID header; a file that never uploaded is simply dropped from the list without one.
    /// </summary>
    public string? RemoveUrl { get; set; }

    /// <summary>
    /// The tooltip of the retry button of a failed or canceled file, which is also used as the prefix of its
    /// accessible label (e.g., "Retry report.pdf"). Defaults to "Retry".
    /// </summary>
    public string? RetryButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the retry button of a failed or canceled file using custom CSS classes
    /// for external icon libraries. Takes precedence over <see cref="RetryIconName"/> when both are set.
    /// Defaults to the built-in Refresh icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom retry icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="RetryIconName"/> instead.
    /// </remarks>
    public BitIconInfo? RetryIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the retry button of a failed or canceled file
    /// from the built-in Fluent UI icons. Defaults to <c>Refresh</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Refresh</c>).
    /// <br />
    /// For external icon libraries, use <see cref="RetryIcon"/> instead.
    /// </remarks>
    public string? RetryIconName { get; set; }

    /// <summary>
    /// Decides whether a failed upload is worth retrying automatically, receiving the file and the HTTP status
    /// code of the failed request (0 for a network error, a timeout or an aborted request) and returning true
    /// to spend one of the <see cref="AutoRetries"/> attempts on it.
    /// When not set, a built-in rule retries the failures a second attempt can plausibly survive - network
    /// errors, timeouts, 408, 429 and the 5xx server errors - and gives up right away on the other 4xx,
    /// which say that the request itself is the problem and would fail again just the same.
    /// </summary>
    public Func<BitFileInfo, int, bool>? ShouldAutoRetry { get; set; }

    /// <summary>
    /// Whether a thumbnail of every selected image is shown at the head of its file item, produced
    /// entirely in the browser from an object URL that is handed back as soon as the file is removed or
    /// the component is reset. The same URL is on the <see cref="BitFileInfo.PreviewUrl"/> of each file.
    /// </summary>
    public bool? ShowPreview { get; set; }

    /// <summary>
    /// Whether each settled file item offers a remove button, which drops a file that never uploaded from
    /// the list and deletes an uploaded one from the server through the <see cref="RemoveUrl"/>.
    /// </summary>
    public bool? ShowRemoveButton { get; set; }

    /// <summary>
    /// The size of the file upload, applied to the browse button and the file list items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitFileUpload.
    /// </summary>
    public BitFileUploadClassStyles? Styles { get; set; }

    /// <summary>
    /// The message shown for successful file uploads.
    /// </summary>
    public string? SuccessfulUploadMessage { get; set; }

    /// <summary>
    /// The tooltip of the upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Upload report.pdf"). Defaults to "Upload".
    /// </summary>
    public string? UploadButtonTitle { get; set; }

    /// <summary>
    /// The name of the form field carrying the file content in the upload request. Defaults to "file".
    /// </summary>
    public string? UploadFormFieldName { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UploadIconName"/> when both are set.
    /// Defaults to the built-in Play icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom upload icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="UploadIconName"/> instead.
    /// </remarks>
    public BitIconInfo? UploadIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Play</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Play</c>).
    /// <br />
    /// For external icon libraries, use <see cref="UploadIcon"/> instead.
    /// </remarks>
    public string? UploadIconName { get; set; }

    /// <summary>
    /// Additional multipart form fields sent alongside the content of every file in its upload requests,
    /// which is what carries the metadata a server needs next to the bytes - a target folder, an album id,
    /// a caption - for the endpoints that read it from the form rather than from the query string.
    /// The <see cref="BitFileInfo.FormFields"/> of a file is merged over these for that file.
    /// </summary>
    public Dictionary<string, string>? UploadRequestFormFields { get; set; }

    /// <summary>
    /// Custom HTTP headers attached to the upload requests, fixed at selection time.
    /// </summary>
    public Dictionary<string, string>? UploadRequestHttpHeaders { get; set; }

    /// <summary>
    /// The provider function to create the http headers for upload request.
    /// Unlike <see cref="UploadRequestHttpHeaders"/>, it is invoked right before every single request -
    /// each file and each chunk - which is what lets it hand over a freshly minted access token.
    /// </summary>
    public Func<Task<Dictionary<string, string>>>? UploadRequestHttpHeadersProvider { get; set; }

    /// <summary>
    /// The HTTP method of the upload request (e.g., "PUT"). Defaults to "POST".
    /// </summary>
    public string? UploadRequestHttpMethod { get; set; }

    /// <summary>
    /// Custom query strings appended to the URL of the upload requests, fixed at selection time.
    /// </summary>
    public Dictionary<string, string>? UploadRequestQueryStrings { get; set; }

    /// <summary>
    /// The provider function to create the query strings for upload request.
    /// Unlike <see cref="UploadRequestQueryStrings"/>, it is invoked right before every single request -
    /// each file and each chunk - which is what lets it hand over a value that does not survive a batch.
    /// </summary>
    public Func<Task<Dictionary<string, string>>>? UploadRequestQueryStringsProvider { get; set; }

    /// <summary>
    /// The timeout of the upload request for each file or chunk. When it elapses the upload of the file fails.
    /// Set to null (the default) for no timeout.
    /// </summary>
    public TimeSpan? UploadTimeout { get; set; }

    /// <summary>
    /// URL of the server endpoint receiving the files, fixed at selection time. Use
    /// <see cref="UploadUrlProvider"/> instead for an endpoint that has to be minted per request.
    /// </summary>
    public string? UploadUrl { get; set; }

    /// <summary>
    /// The provider function to create the URL of the server endpoint receiving the files.
    /// Unlike <see cref="UploadUrl"/>, it is invoked right before every single request - each file and
    /// each chunk - which is what lets it hand over a presigned URL that expires.
    /// </summary>
    public Func<Task<string?>>? UploadUrlProvider { get; set; }

    /// <summary>
    /// The visual variant of the browse button, which decides how much of the <see cref="Color"/> it carries:
    /// a full fill, only an outline, or neither.
    /// </summary>
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Whether the upload request is sent with credentials such as cookies and authorization headers
    /// for cross-origin requests (the withCredentials flag of the underlying XMLHttpRequest).
    /// </summary>
    public bool? WithCredentials { get; set; }


    /// <summary>
    /// Updates the properties of the specified <see cref="BitFileUpload"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitFileUpload"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitFileUpload"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitFileUpload"/>.
    /// </remarks>
    /// <param name="bitFileUpload">
    /// The <see cref="BitFileUpload"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitFileUpload bitFileUpload)
    {
        if (bitFileUpload is null) return;

        UpdateBaseParameters(bitFileUpload);

        if (Accept.HasValue() && bitFileUpload.HasNotBeenSet(nameof(Accept)))
        {
            bitFileUpload.Accept = Accept;
        }

        if (AllowDrop.HasValue && bitFileUpload.HasNotBeenSet(nameof(AllowDrop)))
        {
            bitFileUpload.AllowDrop = AllowDrop.Value;
        }

        if (AllowDuplicates.HasValue && bitFileUpload.HasNotBeenSet(nameof(AllowDuplicates)))
        {
            bitFileUpload.AllowDuplicates = AllowDuplicates.Value;
        }

        if (AllowPaste.HasValue && bitFileUpload.HasNotBeenSet(nameof(AllowPaste)))
        {
            bitFileUpload.AllowPaste = AllowPaste.Value;
        }

        if (AllowedExtensions is not null && bitFileUpload.HasNotBeenSet(nameof(AllowedExtensions)))
        {
            bitFileUpload.AllowedExtensions = AllowedExtensions;
        }

        if (AnnouncementProvider is not null && bitFileUpload.HasNotBeenSet(nameof(AnnouncementProvider)))
        {
            bitFileUpload.AnnouncementProvider = AnnouncementProvider;
        }

        if (Append.HasValue && bitFileUpload.HasNotBeenSet(nameof(Append)))
        {
            bitFileUpload.Append = Append.Value;
        }

        if (AutoChunkSize.HasValue && bitFileUpload.HasNotBeenSet(nameof(AutoChunkSize)))
        {
            bitFileUpload.AutoChunkSize = AutoChunkSize.Value;
        }

        if (AutoReset.HasValue && bitFileUpload.HasNotBeenSet(nameof(AutoReset)))
        {
            bitFileUpload.AutoReset = AutoReset.Value;
        }

        if (AutoRetries.HasValue && bitFileUpload.HasNotBeenSet(nameof(AutoRetries)))
        {
            bitFileUpload.AutoRetries = AutoRetries.Value;
        }

        if (AutoRetryDelay.HasValue && bitFileUpload.HasNotBeenSet(nameof(AutoRetryDelay)))
        {
            bitFileUpload.AutoRetryDelay = AutoRetryDelay.Value;
        }

        if (AutoUpload.HasValue && bitFileUpload.HasNotBeenSet(nameof(AutoUpload)))
        {
            bitFileUpload.AutoUpload = AutoUpload.Value;
        }

        if (CancelButtonTitle.HasValue() && bitFileUpload.HasNotBeenSet(nameof(CancelButtonTitle)))
        {
            bitFileUpload.CancelButtonTitle = CancelButtonTitle;
        }

        if (CancelIcon is not null && bitFileUpload.HasNotBeenSet(nameof(CancelIcon)))
        {
            bitFileUpload.CancelIcon = CancelIcon;
        }

        if (CancelIconName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(CancelIconName)))
        {
            bitFileUpload.CancelIconName = CancelIconName;
        }

        if (CanceledUploadMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(CanceledUploadMessage)))
        {
            bitFileUpload.CanceledUploadMessage = CanceledUploadMessage!;
        }

        if (Capture.HasValue() && bitFileUpload.HasNotBeenSet(nameof(Capture)))
        {
            bitFileUpload.Capture = Capture;
        }

        if (ChunkSize.HasValue && bitFileUpload.HasNotBeenSet(nameof(ChunkSize)))
        {
            bitFileUpload.ChunkSize = ChunkSize.Value;
        }

        if (ChunkedUpload.HasValue && bitFileUpload.HasNotBeenSet(nameof(ChunkedUpload)))
        {
            bitFileUpload.ChunkedUpload = ChunkedUpload.Value;
        }

        if (Classes is not null && bitFileUpload.HasNotBeenSet(nameof(Classes)))
        {
            bitFileUpload.Classes = Classes;

            bitFileUpload.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitFileUpload.HasNotBeenSet(nameof(Color)))
        {
            bitFileUpload.Color = Color.Value;

            bitFileUpload.ClassBuilder.Reset();
        }

        if (ConcurrentUploads.HasValue && bitFileUpload.HasNotBeenSet(nameof(ConcurrentUploads)))
        {
            bitFileUpload.ConcurrentUploads = ConcurrentUploads.Value;
        }

        if (Description.HasValue() && bitFileUpload.HasNotBeenSet(nameof(Description)))
        {
            bitFileUpload.Description = Description;
        }

        if (Directory.HasValue && bitFileUpload.HasNotBeenSet(nameof(Directory)))
        {
            bitFileUpload.Directory = Directory.Value;
        }

        if (DuplicateErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(DuplicateErrorMessage)))
        {
            bitFileUpload.DuplicateErrorMessage = DuplicateErrorMessage!;
        }

        if (FailedRemoveMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(FailedRemoveMessage)))
        {
            bitFileUpload.FailedRemoveMessage = FailedRemoveMessage!;
        }

        if (FailedUploadMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(FailedUploadMessage)))
        {
            bitFileUpload.FailedUploadMessage = FailedUploadMessage!;
        }

        if (FileListAriaLabel.HasValue() && bitFileUpload.HasNotBeenSet(nameof(FileListAriaLabel)))
        {
            bitFileUpload.FileListAriaLabel = FileListAriaLabel!;
        }

        if (FileSizeFormatter is not null && bitFileUpload.HasNotBeenSet(nameof(FileSizeFormatter)))
        {
            bitFileUpload.FileSizeFormatter = FileSizeFormatter;
        }

        if (FileValidator is not null && bitFileUpload.HasNotBeenSet(nameof(FileValidator)))
        {
            bitFileUpload.FileValidator = FileValidator;
        }

        if (HideFileView.HasValue && bitFileUpload.HasNotBeenSet(nameof(HideFileView)))
        {
            bitFileUpload.HideFileView = HideFileView.Value;
        }

        if (HideLabel.HasValue && bitFileUpload.HasNotBeenSet(nameof(HideLabel)))
        {
            bitFileUpload.HideLabel = HideLabel.Value;
        }

        if (Label.HasValue() && bitFileUpload.HasNotBeenSet(nameof(Label)))
        {
            bitFileUpload.Label = Label!;
        }

        if (MaxCount.HasValue && bitFileUpload.HasNotBeenSet(nameof(MaxCount)))
        {
            bitFileUpload.MaxCount = MaxCount.Value;
        }

        if (MaxCountErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(MaxCountErrorMessage)))
        {
            bitFileUpload.MaxCountErrorMessage = MaxCountErrorMessage!;
        }

        if (MaxSize.HasValue && bitFileUpload.HasNotBeenSet(nameof(MaxSize)))
        {
            bitFileUpload.MaxSize = MaxSize.Value;
        }

        if (MaxSizeErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(MaxSizeErrorMessage)))
        {
            bitFileUpload.MaxSizeErrorMessage = MaxSizeErrorMessage!;
        }

        if (MaxTotalSize.HasValue && bitFileUpload.HasNotBeenSet(nameof(MaxTotalSize)))
        {
            bitFileUpload.MaxTotalSize = MaxTotalSize.Value;
        }

        if (MaxTotalSizeErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(MaxTotalSizeErrorMessage)))
        {
            bitFileUpload.MaxTotalSizeErrorMessage = MaxTotalSizeErrorMessage!;
        }

        if (MinSize.HasValue && bitFileUpload.HasNotBeenSet(nameof(MinSize)))
        {
            bitFileUpload.MinSize = MinSize.Value;
        }

        if (MinSizeErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(MinSizeErrorMessage)))
        {
            bitFileUpload.MinSizeErrorMessage = MinSizeErrorMessage!;
        }

        if (Multiple.HasValue && bitFileUpload.HasNotBeenSet(nameof(Multiple)))
        {
            bitFileUpload.Multiple = Multiple.Value;
        }

        if (NotAllowedExtensionErrorMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(NotAllowedExtensionErrorMessage)))
        {
            bitFileUpload.NotAllowedExtensionErrorMessage = NotAllowedExtensionErrorMessage!;
        }

        if (PauseButtonTitle.HasValue() && bitFileUpload.HasNotBeenSet(nameof(PauseButtonTitle)))
        {
            bitFileUpload.PauseButtonTitle = PauseButtonTitle;
        }

        if (PauseIcon is not null && bitFileUpload.HasNotBeenSet(nameof(PauseIcon)))
        {
            bitFileUpload.PauseIcon = PauseIcon;
        }

        if (PauseIconName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(PauseIconName)))
        {
            bitFileUpload.PauseIconName = PauseIconName;
        }

        if (QueuedUploadMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(QueuedUploadMessage)))
        {
            bitFileUpload.QueuedUploadMessage = QueuedUploadMessage!;
        }

        if (ReadImageDimensions.HasValue && bitFileUpload.HasNotBeenSet(nameof(ReadImageDimensions)))
        {
            bitFileUpload.ReadImageDimensions = ReadImageDimensions.Value;
        }

        if (RemoveButtonTitle.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RemoveButtonTitle)))
        {
            bitFileUpload.RemoveButtonTitle = RemoveButtonTitle;
        }

        if (RemoveIcon is not null && bitFileUpload.HasNotBeenSet(nameof(RemoveIcon)))
        {
            bitFileUpload.RemoveIcon = RemoveIcon;
        }

        if (RemoveIconName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RemoveIconName)))
        {
            bitFileUpload.RemoveIconName = RemoveIconName;
        }

        if (RemoveRequestHttpHeaders is not null && bitFileUpload.HasNotBeenSet(nameof(RemoveRequestHttpHeaders)))
        {
            bitFileUpload.RemoveRequestHttpHeaders = RemoveRequestHttpHeaders;
        }

        if (RemoveRequestHttpHeadersProvider is not null && bitFileUpload.HasNotBeenSet(nameof(RemoveRequestHttpHeadersProvider)))
        {
            bitFileUpload.RemoveRequestHttpHeadersProvider = RemoveRequestHttpHeadersProvider;
        }

        if (RemoveRequestHttpMethod.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RemoveRequestHttpMethod)))
        {
            bitFileUpload.RemoveRequestHttpMethod = RemoveRequestHttpMethod;
        }

        if (RemoveRequestQueryStrings is not null && bitFileUpload.HasNotBeenSet(nameof(RemoveRequestQueryStrings)))
        {
            bitFileUpload.RemoveRequestQueryStrings = RemoveRequestQueryStrings;
        }

        if (RemoveRequestQueryStringsProvider is not null && bitFileUpload.HasNotBeenSet(nameof(RemoveRequestQueryStringsProvider)))
        {
            bitFileUpload.RemoveRequestQueryStringsProvider = RemoveRequestQueryStringsProvider;
        }

        if (RemoveUrl.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RemoveUrl)))
        {
            bitFileUpload.RemoveUrl = RemoveUrl;
        }

        if (RetryButtonTitle.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RetryButtonTitle)))
        {
            bitFileUpload.RetryButtonTitle = RetryButtonTitle;
        }

        if (RetryIcon is not null && bitFileUpload.HasNotBeenSet(nameof(RetryIcon)))
        {
            bitFileUpload.RetryIcon = RetryIcon;
        }

        if (RetryIconName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(RetryIconName)))
        {
            bitFileUpload.RetryIconName = RetryIconName;
        }

        if (ShouldAutoRetry is not null && bitFileUpload.HasNotBeenSet(nameof(ShouldAutoRetry)))
        {
            bitFileUpload.ShouldAutoRetry = ShouldAutoRetry;
        }

        if (ShowPreview.HasValue && bitFileUpload.HasNotBeenSet(nameof(ShowPreview)))
        {
            bitFileUpload.ShowPreview = ShowPreview.Value;
        }

        if (ShowRemoveButton.HasValue && bitFileUpload.HasNotBeenSet(nameof(ShowRemoveButton)))
        {
            bitFileUpload.ShowRemoveButton = ShowRemoveButton.Value;
        }

        if (Size.HasValue && bitFileUpload.HasNotBeenSet(nameof(Size)))
        {
            bitFileUpload.Size = Size.Value;

            bitFileUpload.ClassBuilder.Reset();
        }

        if (Styles is not null && bitFileUpload.HasNotBeenSet(nameof(Styles)))
        {
            bitFileUpload.Styles = Styles;

            bitFileUpload.StyleBuilder.Reset();
        }

        if (SuccessfulUploadMessage.HasValue() && bitFileUpload.HasNotBeenSet(nameof(SuccessfulUploadMessage)))
        {
            bitFileUpload.SuccessfulUploadMessage = SuccessfulUploadMessage!;
        }

        if (UploadButtonTitle.HasValue() && bitFileUpload.HasNotBeenSet(nameof(UploadButtonTitle)))
        {
            bitFileUpload.UploadButtonTitle = UploadButtonTitle;
        }

        if (UploadFormFieldName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(UploadFormFieldName)))
        {
            bitFileUpload.UploadFormFieldName = UploadFormFieldName;
        }

        if (UploadIcon is not null && bitFileUpload.HasNotBeenSet(nameof(UploadIcon)))
        {
            bitFileUpload.UploadIcon = UploadIcon;
        }

        if (UploadIconName.HasValue() && bitFileUpload.HasNotBeenSet(nameof(UploadIconName)))
        {
            bitFileUpload.UploadIconName = UploadIconName;
        }

        if (UploadRequestFormFields is not null && bitFileUpload.HasNotBeenSet(nameof(UploadRequestFormFields)))
        {
            bitFileUpload.UploadRequestFormFields = UploadRequestFormFields;
        }

        if (UploadRequestHttpHeaders is not null && bitFileUpload.HasNotBeenSet(nameof(UploadRequestHttpHeaders)))
        {
            bitFileUpload.UploadRequestHttpHeaders = UploadRequestHttpHeaders;
        }

        if (UploadRequestHttpHeadersProvider is not null && bitFileUpload.HasNotBeenSet(nameof(UploadRequestHttpHeadersProvider)))
        {
            bitFileUpload.UploadRequestHttpHeadersProvider = UploadRequestHttpHeadersProvider;
        }

        if (UploadRequestHttpMethod.HasValue() && bitFileUpload.HasNotBeenSet(nameof(UploadRequestHttpMethod)))
        {
            bitFileUpload.UploadRequestHttpMethod = UploadRequestHttpMethod;
        }

        if (UploadRequestQueryStrings is not null && bitFileUpload.HasNotBeenSet(nameof(UploadRequestQueryStrings)))
        {
            bitFileUpload.UploadRequestQueryStrings = UploadRequestQueryStrings;
        }

        if (UploadRequestQueryStringsProvider is not null && bitFileUpload.HasNotBeenSet(nameof(UploadRequestQueryStringsProvider)))
        {
            bitFileUpload.UploadRequestQueryStringsProvider = UploadRequestQueryStringsProvider;
        }

        if (UploadTimeout.HasValue && bitFileUpload.HasNotBeenSet(nameof(UploadTimeout)))
        {
            bitFileUpload.UploadTimeout = UploadTimeout.Value;
        }

        if (UploadUrl.HasValue() && bitFileUpload.HasNotBeenSet(nameof(UploadUrl)))
        {
            bitFileUpload.UploadUrl = UploadUrl;
        }

        if (UploadUrlProvider is not null && bitFileUpload.HasNotBeenSet(nameof(UploadUrlProvider)))
        {
            bitFileUpload.UploadUrlProvider = UploadUrlProvider;
        }

        if (Variant.HasValue && bitFileUpload.HasNotBeenSet(nameof(Variant)))
        {
            bitFileUpload.Variant = Variant.Value;

            bitFileUpload.ClassBuilder.Reset();
        }

        if (WithCredentials.HasValue && bitFileUpload.HasNotBeenSet(nameof(WithCredentials)))
        {
            bitFileUpload.WithCredentials = WithCredentials.Value;
        }

        // The working chunk size is derived from ChunkSize and AutoChunkSize by a setter that assigning
        // the properties here goes around, so it is derived again once both of them are in place.
        if (ChunkSize.HasValue || AutoChunkSize.HasValue)
        {
            bitFileUpload.OnSetChunkSize();
        }
    }
}
