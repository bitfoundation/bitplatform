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
    /// Custom delay before each automatic retry, which is what turns the fixed <see cref="AutoRetryDelay"/>
    /// into a backoff: it receives the file - whose <see cref="BitFileInfo.ResponseStatus"/> says what the
    /// server answered - and the number of the attempt about to be made, counting from 1, and returns how
    /// long to wait before it. Returning null falls back to the <see cref="AutoRetryDelay"/>, which is also
    /// what a provider that throws does, so a miscalculated delay never swallows the retry itself.
    /// </summary>
    public Func<BitFileInfo, int, TimeSpan?>? AutoRetryDelayProvider { get; set; }

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
    /// The text of the "Cancel all" button of the batch actions (see <see cref="ShowBatchActions"/>).
    /// </summary>
    public string? CancelAllText { get; set; }

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
    /// The text of the "Clear" button of the batch actions (see <see cref="ShowBatchActions"/>).
    /// </summary>
    public string? ClearText { get; set; }

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
    /// A CSS selector of one or more elements outside the component that accept a drop as well, which is how
    /// a whole form, a card or the page itself becomes the drop target while the browse button stays where it
    /// is. The root element of the component is always a drop zone and needs no selector of its own; the
    /// elements this one names are matched whenever a drag reaches them, so one rendered after the component
    /// - or replaced later on - is a drop zone from the moment it matches. While files are dragged over any
    /// of them, all of them (the root included) carry the Classes.Dragging class and the Styles.Dragging
    /// inline style, and the focus being inside one of them is also what lets a paste land in this component.
    /// </summary>
    public string? DropZoneSelector { get; set; }

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
    /// The icon of the browse button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="LabelIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? LabelIcon { get; set; }

    /// <summary>
    /// The name of the icon of the browse button from the built-in Fluent UI icons.
    /// Defaults to <c>CloudUpload</c> in the <see cref="ShowDropArea"/> mode, and to no icon otherwise.
    /// </summary>
    public string? LabelIconName { get; set; }

    /// <summary>
    /// The position of the icon of the browse button relative to its text: before it (the default) or after it.
    /// In the ShowDropArea mode the icon is stacked above or below the text instead.
    /// </summary>
    /// <remarks>
    /// Only <see cref="BitPlacement.Start"/> and <see cref="BitPlacement.End"/> mean anything here, and they
    /// follow the reading direction. Every other placement leaves the icon where Start would put it.
    /// </remarks>
    public BitPlacement? LabelIconPlacement { get; set; }

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
    /// The status message shown for the files of the PreloadedFiles parameter, which are already on the
    /// server rather than freshly uploaded and would otherwise read as an upload that just succeeded.
    /// </summary>
    public string? PreloadedFileMessage { get; set; }

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
    /// Whether an action bar with "Upload all", "Cancel all" and "Clear" buttons is rendered under the file list.
    /// </summary>
    public bool? ShowBatchActions { get; set; }

    /// <summary>
    /// Whether the browse button is rendered as a large drop area instead of a regular button.
    /// </summary>
    public bool? ShowDropArea { get; set; }

    /// <summary>
    /// Whether a thumbnail of every selected image is shown at the head of its file item, produced
    /// entirely in the browser from an object URL that is handed back as soon as the file is removed or
    /// the component is reset. The same URL is on the <see cref="BitFileInfo.PreviewUrl"/> of each file.
    /// A file that is not an image takes a glyph of its type in a box of the same size instead, so that
    /// the names of a mixed list stay lined up along one edge.
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
    /// The text of the "Upload all" button of the batch actions (see <see cref="ShowBatchActions"/>).
    /// </summary>
    public string? UploadAllText { get; set; }

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

        // The working chunk size is derived from ChunkSize and AutoChunkSize. A changed ChunkSize derives it through its
        // own [CallOnSet] hook; AutoChunkSize has none, so a change to it is followed here.
        var autoChunkSizeChanged = false;

        if (Accept.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(Accept), Accept, static f => f.Accept, static (f, v) => f.Accept = v);
        }

        if (AllowDrop.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AllowDrop), AllowDrop.Value, static f => f.AllowDrop, static (f, v) => f.AllowDrop = v);
        }

        if (AllowDuplicates.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AllowDuplicates), AllowDuplicates.Value, static f => f.AllowDuplicates, static (f, v) => f.AllowDuplicates = v);
        }

        if (AllowPaste.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AllowPaste), AllowPaste.Value, static f => f.AllowPaste, static (f, v) => f.AllowPaste = v);
        }

        if (AllowedExtensions is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(AllowedExtensions), AllowedExtensions, static f => f.AllowedExtensions, static (f, v) => f.AllowedExtensions = v);
        }

        if (AnnouncementProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(AnnouncementProvider), AnnouncementProvider, static f => f.AnnouncementProvider, static (f, v) => f.AnnouncementProvider = v);
        }

        if (Append.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Append), Append.Value, static f => f.Append, static (f, v) => f.Append = v);
        }

        if (AutoChunkSize.HasValue && bitFileUpload.TakeFromCascade(nameof(AutoChunkSize), AutoChunkSize.Value, static f => f.AutoChunkSize, static (f, v) => f.AutoChunkSize = v))
        {
            autoChunkSizeChanged = true;
        }

        if (AutoReset.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AutoReset), AutoReset.Value, static f => f.AutoReset, static (f, v) => f.AutoReset = v);
        }

        if (AutoRetries.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AutoRetries), AutoRetries.Value, static f => f.AutoRetries, static (f, v) => f.AutoRetries = v);
        }

        if (AutoRetryDelay.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AutoRetryDelay), AutoRetryDelay.Value, static f => f.AutoRetryDelay, static (f, v) => f.AutoRetryDelay = v);
        }

        if (AutoRetryDelayProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(AutoRetryDelayProvider), AutoRetryDelayProvider, static f => f.AutoRetryDelayProvider, static (f, v) => f.AutoRetryDelayProvider = v);
        }

        if (AutoUpload.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(AutoUpload), AutoUpload.Value, static f => f.AutoUpload, static (f, v) => f.AutoUpload = v);
        }

        if (CancelButtonTitle.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(CancelButtonTitle), CancelButtonTitle, static f => f.CancelButtonTitle, static (f, v) => f.CancelButtonTitle = v);
        }

        if (CancelAllText.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(CancelAllText), CancelAllText!, static f => f.CancelAllText, static (f, v) => f.CancelAllText = v);
        }

        var ownCancelIcon = bitFileUpload.HasSetAnyOf(nameof(CancelIcon), nameof(CancelIconName));

        if (CancelIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(CancelIcon), CancelIcon, static f => f.CancelIcon, static (f, v) => f.CancelIcon = v, outranked: ownCancelIcon);
        }

        if (CancelIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(CancelIconName), CancelIconName, static f => f.CancelIconName, static (f, v) => f.CancelIconName = v, outranked: ownCancelIcon);
        }

        if (CanceledUploadMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(CanceledUploadMessage), CanceledUploadMessage!, static f => f.CanceledUploadMessage, static (f, v) => f.CanceledUploadMessage = v);
        }

        if (Capture.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(Capture), Capture, static f => f.Capture, static (f, v) => f.Capture = v);
        }

        if (ChunkSize.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ChunkSize), ChunkSize.Value, static f => f.ChunkSize, static (f, v) => f.ChunkSize = v);
        }

        if (ChunkedUpload.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ChunkedUpload), ChunkedUpload.Value, static f => f.ChunkedUpload, static (f, v) => f.ChunkedUpload = v);
        }

        if (Classes is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(Classes), Classes, static f => f.Classes, static (f, v) => f.Classes = v);
        }

        if (ClearText.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(ClearText), ClearText!, static f => f.ClearText, static (f, v) => f.ClearText = v);
        }

        if (Color.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Color), Color.Value, static f => f.Color, static (f, v) => f.Color = v);
        }

        if (ConcurrentUploads.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ConcurrentUploads), ConcurrentUploads.Value, static f => f.ConcurrentUploads, static (f, v) => f.ConcurrentUploads = v);
        }

        if (Description.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(Description), Description, static f => f.Description, static (f, v) => f.Description = v);
        }

        if (Directory.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Directory), Directory.Value, static f => f.Directory, static (f, v) => f.Directory = v);
        }

        if (DropZoneSelector.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(DropZoneSelector), DropZoneSelector, static f => f.DropZoneSelector, static (f, v) => f.DropZoneSelector = v);
        }

        if (DuplicateErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(DuplicateErrorMessage), DuplicateErrorMessage!, static f => f.DuplicateErrorMessage, static (f, v) => f.DuplicateErrorMessage = v);
        }

        if (FailedRemoveMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(FailedRemoveMessage), FailedRemoveMessage!, static f => f.FailedRemoveMessage, static (f, v) => f.FailedRemoveMessage = v);
        }

        if (FailedUploadMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(FailedUploadMessage), FailedUploadMessage!, static f => f.FailedUploadMessage, static (f, v) => f.FailedUploadMessage = v);
        }

        if (FileListAriaLabel.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(FileListAriaLabel), FileListAriaLabel!, static f => f.FileListAriaLabel, static (f, v) => f.FileListAriaLabel = v);
        }

        if (FileSizeFormatter is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(FileSizeFormatter), FileSizeFormatter, static f => f.FileSizeFormatter, static (f, v) => f.FileSizeFormatter = v);
        }

        if (FileValidator is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(FileValidator), FileValidator, static f => f.FileValidator, static (f, v) => f.FileValidator = v);
        }

        if (HideFileView.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(HideFileView), HideFileView.Value, static f => f.HideFileView, static (f, v) => f.HideFileView = v);
        }

        if (HideLabel.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(HideLabel), HideLabel.Value, static f => f.HideLabel, static (f, v) => f.HideLabel = v);
        }

        if (Label.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(Label), Label!, static f => f.Label, static (f, v) => f.Label = v);
        }

        var ownLabelIcon = bitFileUpload.HasSetAnyOf(nameof(LabelIcon), nameof(LabelIconName));

        if (LabelIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(LabelIcon), LabelIcon, static f => f.LabelIcon, static (f, v) => f.LabelIcon = v, outranked: ownLabelIcon);
        }

        if (LabelIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(LabelIconName), LabelIconName, static f => f.LabelIconName, static (f, v) => f.LabelIconName = v, outranked: ownLabelIcon);
        }

        if (LabelIconPlacement.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(LabelIconPlacement), LabelIconPlacement.Value, static f => f.LabelIconPlacement, static (f, v) => f.LabelIconPlacement = v);
        }

        if (MaxCount.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(MaxCount), MaxCount.Value, static f => f.MaxCount, static (f, v) => f.MaxCount = v);
        }

        if (MaxCountErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(MaxCountErrorMessage), MaxCountErrorMessage!, static f => f.MaxCountErrorMessage, static (f, v) => f.MaxCountErrorMessage = v);
        }

        if (MaxSize.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(MaxSize), MaxSize.Value, static f => f.MaxSize, static (f, v) => f.MaxSize = v);
        }

        if (MaxSizeErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(MaxSizeErrorMessage), MaxSizeErrorMessage!, static f => f.MaxSizeErrorMessage, static (f, v) => f.MaxSizeErrorMessage = v);
        }

        if (MaxTotalSize.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(MaxTotalSize), MaxTotalSize.Value, static f => f.MaxTotalSize, static (f, v) => f.MaxTotalSize = v);
        }

        if (MaxTotalSizeErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(MaxTotalSizeErrorMessage), MaxTotalSizeErrorMessage!, static f => f.MaxTotalSizeErrorMessage, static (f, v) => f.MaxTotalSizeErrorMessage = v);
        }

        if (MinSize.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(MinSize), MinSize.Value, static f => f.MinSize, static (f, v) => f.MinSize = v);
        }

        if (MinSizeErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(MinSizeErrorMessage), MinSizeErrorMessage!, static f => f.MinSizeErrorMessage, static (f, v) => f.MinSizeErrorMessage = v);
        }

        if (Multiple.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Multiple), Multiple.Value, static f => f.Multiple, static (f, v) => f.Multiple = v);
        }

        if (NotAllowedExtensionErrorMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(NotAllowedExtensionErrorMessage), NotAllowedExtensionErrorMessage!, static f => f.NotAllowedExtensionErrorMessage, static (f, v) => f.NotAllowedExtensionErrorMessage = v);
        }

        if (PauseButtonTitle.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(PauseButtonTitle), PauseButtonTitle, static f => f.PauseButtonTitle, static (f, v) => f.PauseButtonTitle = v);
        }

        var ownPauseIcon = bitFileUpload.HasSetAnyOf(nameof(PauseIcon), nameof(PauseIconName));

        if (PauseIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(PauseIcon), PauseIcon, static f => f.PauseIcon, static (f, v) => f.PauseIcon = v, outranked: ownPauseIcon);
        }

        if (PauseIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(PauseIconName), PauseIconName, static f => f.PauseIconName, static (f, v) => f.PauseIconName = v, outranked: ownPauseIcon);
        }

        if (PreloadedFileMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(PreloadedFileMessage), PreloadedFileMessage!, static f => f.PreloadedFileMessage, static (f, v) => f.PreloadedFileMessage = v);
        }

        if (QueuedUploadMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(QueuedUploadMessage), QueuedUploadMessage!, static f => f.QueuedUploadMessage, static (f, v) => f.QueuedUploadMessage = v);
        }

        if (ReadImageDimensions.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ReadImageDimensions), ReadImageDimensions.Value, static f => f.ReadImageDimensions, static (f, v) => f.ReadImageDimensions = v);
        }

        if (RemoveButtonTitle.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveButtonTitle), RemoveButtonTitle, static f => f.RemoveButtonTitle, static (f, v) => f.RemoveButtonTitle = v);
        }

        var ownRemoveIcon = bitFileUpload.HasSetAnyOf(nameof(RemoveIcon), nameof(RemoveIconName));

        if (RemoveIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveIcon), RemoveIcon, static f => f.RemoveIcon, static (f, v) => f.RemoveIcon = v, outranked: ownRemoveIcon);
        }

        if (RemoveIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveIconName), RemoveIconName, static f => f.RemoveIconName, static (f, v) => f.RemoveIconName = v, outranked: ownRemoveIcon);
        }

        if (RemoveRequestHttpHeaders is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveRequestHttpHeaders), RemoveRequestHttpHeaders, static f => f.RemoveRequestHttpHeaders, static (f, v) => f.RemoveRequestHttpHeaders = v);
        }

        if (RemoveRequestHttpHeadersProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveRequestHttpHeadersProvider), RemoveRequestHttpHeadersProvider, static f => f.RemoveRequestHttpHeadersProvider, static (f, v) => f.RemoveRequestHttpHeadersProvider = v);
        }

        if (RemoveRequestHttpMethod.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveRequestHttpMethod), RemoveRequestHttpMethod, static f => f.RemoveRequestHttpMethod, static (f, v) => f.RemoveRequestHttpMethod = v);
        }

        if (RemoveRequestQueryStrings is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveRequestQueryStrings), RemoveRequestQueryStrings, static f => f.RemoveRequestQueryStrings, static (f, v) => f.RemoveRequestQueryStrings = v);
        }

        if (RemoveRequestQueryStringsProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveRequestQueryStringsProvider), RemoveRequestQueryStringsProvider, static f => f.RemoveRequestQueryStringsProvider, static (f, v) => f.RemoveRequestQueryStringsProvider = v);
        }

        if (RemoveUrl.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RemoveUrl), RemoveUrl, static f => f.RemoveUrl, static (f, v) => f.RemoveUrl = v);
        }

        if (RetryButtonTitle.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RetryButtonTitle), RetryButtonTitle, static f => f.RetryButtonTitle, static (f, v) => f.RetryButtonTitle = v);
        }

        var ownRetryIcon = bitFileUpload.HasSetAnyOf(nameof(RetryIcon), nameof(RetryIconName));

        if (RetryIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(RetryIcon), RetryIcon, static f => f.RetryIcon, static (f, v) => f.RetryIcon = v, outranked: ownRetryIcon);
        }

        if (RetryIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(RetryIconName), RetryIconName, static f => f.RetryIconName, static (f, v) => f.RetryIconName = v, outranked: ownRetryIcon);
        }

        if (ShouldAutoRetry is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(ShouldAutoRetry), ShouldAutoRetry, static f => f.ShouldAutoRetry, static (f, v) => f.ShouldAutoRetry = v);
        }

        if (ShowBatchActions.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ShowBatchActions), ShowBatchActions.Value, static f => f.ShowBatchActions, static (f, v) => f.ShowBatchActions = v);
        }

        if (ShowDropArea.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ShowDropArea), ShowDropArea.Value, static f => f.ShowDropArea, static (f, v) => f.ShowDropArea = v);
        }

        if (ShowPreview.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ShowPreview), ShowPreview.Value, static f => f.ShowPreview, static (f, v) => f.ShowPreview = v);
        }

        if (ShowRemoveButton.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(ShowRemoveButton), ShowRemoveButton.Value, static f => f.ShowRemoveButton, static (f, v) => f.ShowRemoveButton = v);
        }

        if (Size.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Size), Size.Value, static f => f.Size, static (f, v) => f.Size = v);
        }

        if (Styles is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(Styles), Styles, static f => f.Styles, static (f, v) => f.Styles = v);
        }

        if (SuccessfulUploadMessage.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(SuccessfulUploadMessage), SuccessfulUploadMessage!, static f => f.SuccessfulUploadMessage, static (f, v) => f.SuccessfulUploadMessage = v);
        }

        if (UploadAllText.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadAllText), UploadAllText!, static f => f.UploadAllText, static (f, v) => f.UploadAllText = v);
        }

        if (UploadButtonTitle.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadButtonTitle), UploadButtonTitle, static f => f.UploadButtonTitle, static (f, v) => f.UploadButtonTitle = v);
        }

        if (UploadFormFieldName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadFormFieldName), UploadFormFieldName, static f => f.UploadFormFieldName, static (f, v) => f.UploadFormFieldName = v);
        }

        var ownUploadIcon = bitFileUpload.HasSetAnyOf(nameof(UploadIcon), nameof(UploadIconName));

        if (UploadIcon is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadIcon), UploadIcon, static f => f.UploadIcon, static (f, v) => f.UploadIcon = v, outranked: ownUploadIcon);
        }

        if (UploadIconName.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadIconName), UploadIconName, static f => f.UploadIconName, static (f, v) => f.UploadIconName = v, outranked: ownUploadIcon);
        }

        if (UploadRequestFormFields is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestFormFields), UploadRequestFormFields, static f => f.UploadRequestFormFields, static (f, v) => f.UploadRequestFormFields = v);
        }

        if (UploadRequestHttpHeaders is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestHttpHeaders), UploadRequestHttpHeaders, static f => f.UploadRequestHttpHeaders, static (f, v) => f.UploadRequestHttpHeaders = v);
        }

        if (UploadRequestHttpHeadersProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestHttpHeadersProvider), UploadRequestHttpHeadersProvider, static f => f.UploadRequestHttpHeadersProvider, static (f, v) => f.UploadRequestHttpHeadersProvider = v);
        }

        if (UploadRequestHttpMethod.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestHttpMethod), UploadRequestHttpMethod, static f => f.UploadRequestHttpMethod, static (f, v) => f.UploadRequestHttpMethod = v);
        }

        if (UploadRequestQueryStrings is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestQueryStrings), UploadRequestQueryStrings, static f => f.UploadRequestQueryStrings, static (f, v) => f.UploadRequestQueryStrings = v);
        }

        if (UploadRequestQueryStringsProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadRequestQueryStringsProvider), UploadRequestQueryStringsProvider, static f => f.UploadRequestQueryStringsProvider, static (f, v) => f.UploadRequestQueryStringsProvider = v);
        }

        if (UploadTimeout.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadTimeout), UploadTimeout.Value, static f => f.UploadTimeout, static (f, v) => f.UploadTimeout = v);
        }

        if (UploadUrl.HasValue())
        {
            bitFileUpload.TakeFromCascade(nameof(UploadUrl), UploadUrl, static f => f.UploadUrl, static (f, v) => f.UploadUrl = v);
        }

        if (UploadUrlProvider is not null)
        {
            bitFileUpload.TakeFromCascade(nameof(UploadUrlProvider), UploadUrlProvider, static f => f.UploadUrlProvider, static (f, v) => f.UploadUrlProvider = v);
        }

        if (Variant.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(Variant), Variant.Value, static f => f.Variant, static (f, v) => f.Variant = v);
        }

        if (WithCredentials.HasValue)
        {
            bitFileUpload.TakeFromCascade(nameof(WithCredentials), WithCredentials.Value, static f => f.WithCredentials, static (f, v) => f.WithCredentials = v);
        }

        // Only when AutoChunkSize actually moves: deriving the chunk size on every parameter set would throw away the
        // speed AutoChunkSize has measured, since this runs on each render of whatever holds the BitParams.
        if (autoChunkSizeChanged)
        {
            bitFileUpload.OnSetChunkSize();
        }
    }
}
