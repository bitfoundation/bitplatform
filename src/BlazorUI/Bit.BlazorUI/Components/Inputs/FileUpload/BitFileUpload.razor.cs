using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

namespace Bit.BlazorUI;

/// <summary>
/// BitFileUpload wraps the HTML file input element(s) and uploads them to a given URL, with support for
/// drag-and-drop onto the component or onto drop zones of the app's own, clipboard paste, folder and camera
/// capture selection, image previews, chunked and resumable uploads, a concurrency limit, pause/cancel,
/// automatic retries, validation, and server-side removal.
/// </summary>
public partial class BitFileUpload : BitComponentBase
{
    private const int MIN_CHUNK_SIZE = 512 * 1024; // 512 kb
    private const int MAX_CHUNK_SIZE = 10 * 1024 * 1024; // 10 mb

    // A preloaded file has no picked file behind it and so no uploader in JavaScript to be named by an
    // index. It gets one no uploader can ever have, so that a call meant for it reaches no other file's.
    private const int PRELOADED_FILE_INDEX = -1;

    // roughly three repaints a second, which is as often as a progress bar is worth redrawing and
    // far less often than the browser reports the progress of the requests behind it.
    private static readonly TimeSpan PROGRESS_RENDER_INTERVAL = TimeSpan.FromMilliseconds(300);



    private bool _allowDrop = true;
    private DateTime _lastProgressRender = DateTime.MinValue;
    private bool _allowPaste = true;
    private bool _expandDirectories;
    private string? _dropZoneSelector;
    private string? _dragClass;
    private string? _dragStyle;
    private string? _announcement;
    private bool _announcementMarker;
    private int _removingCount;
    // The collection of preloaded files the list was last built from, and what was in it, so that the same
    // files arriving again with every render do not put the ones the user has since removed back.
    private IReadOnlyCollection<BitFileInfo>? _appliedPreloadedFiles;
    private string[] _appliedPreloadedSignature = [];
    private string _buttonId = default!;
    private string _descriptionId = default!;
    private ElementReference _inputRef;
    private ElementReference _labelRef;
    // The button of which file the focus is to be put on after the next render, with the neighbour to fall
    // back to. An action taken from inside a file item - removing it, starting, pausing or canceling its
    // upload - takes the very button that was pressed out of the DOM, and without this the focus would go
    // with it and land back on the document body, dropping a keyboard user at the top of the page.
    private bool _pendingFocus;
    private string? _pendingFocusFileId;
    private string? _pendingFocusFallbackFileId;
    private BitFileUploadFocusTarget _pendingFocusTarget;
    private readonly Dictionary<string, _BitFileUploadItem> _itemRefs = [];
    private List<BitFileInfo> _files = [];
    private List<BitFileInfo> _uploadQueue = [];
    private long _internalChunkSize = MIN_CHUNK_SIZE;
    private IJSObjectReference _dropZoneRef = default!;
    private DotNetObjectReference<BitFileUpload> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private HttpClient _httpClient { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the file upload component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple file upload components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitFileUploadParams.ParamName)]
    public BitFileUploadParams? CascadingParameters { get; set; }



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
    /// When disabled, a newly selected file matching an existing one by name, size and last modified time
    /// is rejected with the <see cref="DuplicateErrorMessage"/> instead of being uploaded a second time,
    /// becoming eligible again once the file it duplicates is removed. A file of <see cref="PreloadedFiles"/>
    /// counts as one already in the list, matched by name and size alone since a server reports no
    /// modification time, which is what keeps an attachment the record already has from being sent again.
    /// The default value is true.
    /// </summary>
    [Parameter] public bool AllowDuplicates { get; set; } = true;

    /// <summary>
    /// Allowed file types for validation purposes, accepting both file extensions (e.g., [".jpg", ".png", ".pdf"])
    /// and MIME types with an optional wildcard (e.g., ["image/*", "application/pdf"]).
    /// The leading dot of an extension is optional and the matching is case-insensitive.
    /// Use ["*"] to allow all file types. Files not matching any of these entries will not be uploaded.
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
    /// whenever the file list or an upload outcome changes. Receives the current file list and returns the text
    /// to announce, or null to announce nothing. When not set, a built-in English announcement is used.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<BitFileInfo>, string?>? AnnouncementProvider { get; set; }

    /// <summary>
    /// Whether a new selection is added to the end of the current file list instead of replacing it,
    /// which is what lets the user build a batch up over several rounds of browsing, dropping or pasting.
    /// The files already in the list keep their upload state.
    /// </summary>
    [Parameter] public bool Append { get; set; }

    /// <summary>
    /// The number of times a failed upload of a file gets retried automatically before it is reported as failed.
    /// In the chunked mode each retry resumes from the last successfully uploaded chunk.
    /// Set to 0 (the default) to disable the automatic retries.
    /// </summary>
    [Parameter] public int AutoRetries { get; set; }

    /// <summary>
    /// The delay before each automatic retry of a failed upload.
    /// Set to null (the default) to retry immediately.
    /// </summary>
    [Parameter] public TimeSpan? AutoRetryDelay { get; set; }

    /// <summary>
    /// Custom delay before each automatic retry, which is what turns the fixed <see cref="AutoRetryDelay"/>
    /// into a backoff: it receives the file - whose <see cref="BitFileInfo.ResponseStatus"/> says what the
    /// server answered - and the number of the attempt about to be made, counting from 1, and returns how
    /// long to wait before it. Returning null falls back to the <see cref="AutoRetryDelay"/>, which is also
    /// what a provider that throws does, so a miscalculated delay never swallows the retry itself.
    /// </summary>
    [Parameter] public Func<BitFileInfo, int, TimeSpan?>? AutoRetryDelayProvider { get; set; }

    /// <summary>
    /// Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB.
    /// </summary>
    [Parameter] public bool AutoChunkSize { get; set; }

    /// <summary>
    /// Whether the file list and the upload state are cleared right before the file dialog opens, so that
    /// every browse starts from a clean slate - the list empties even if the dialog is then cancelled.
    /// </summary>
    [Parameter] public bool AutoReset { get; set; }

    /// <summary>
    /// Whether the selected files start uploading the moment they are selected, skipping the per-file
    /// upload button entirely, for the cases where the selection itself expresses the intent to upload.
    /// </summary>
    [Parameter] public bool AutoUpload { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the cancel upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CancelIconName"/> when both are set.
    /// Defaults to the built-in Cancel icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom cancel icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CancelIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? CancelIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the cancel upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Cancel</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Cancel</c>).
    /// <br />
    /// For external icon libraries, use <see cref="CancelIcon"/> instead.
    /// </remarks>
    [Parameter] public string? CancelIconName { get; set; }

    /// <summary>
    /// The tooltip of the cancel upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Cancel report.pdf"). Defaults to "Cancel".
    /// </summary>
    [Parameter] public string? CancelButtonTitle { get; set; }

    /// <summary>
    /// The message shown for canceled file uploads.
    /// </summary>
    [Parameter] public string CanceledUploadMessage { get; set; } = "File upload canceled";

    /// <summary>
    /// The capture behavior of the file input on devices with a camera or microphone,
    /// rendered as the capture attribute of the input element (e.g., "user" for the front camera,
    /// "environment" for the rear camera).
    /// </summary>
    [Parameter] public string? Capture { get; set; }

    /// <summary>
    /// Whether each file is sliced and sent as a series of sequential requests instead of one monolithic
    /// one, which is what makes a paused or failed file resume from the last chunk that made it through
    /// rather than starting over, so a dropped connection costs one chunk instead of the whole transfer.
    /// </summary>
    [Parameter] public bool ChunkedUpload { get; set; }

    /// <summary>
    /// The size in bytes of each chunk of a chunked upload. When not set - and whenever
    /// <see cref="AutoChunkSize"/> is enabled, which takes the decision over - it starts at 512 KB.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetChunkSize))]
    public long? ChunkSize { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitFileUpload.
    /// </summary>
    [Parameter] public BitFileUploadClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the file upload, applied to the browse button, the drag-and-drop indicator,
    /// the progress bars and the hovered action buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The maximum number of files uploading at the same time, the remaining ones waiting in a queue
    /// and starting as soon as a slot frees up. Set to 0 (the default) to start every file at once.
    /// </summary>
    [Parameter] public int ConcurrentUploads { get; set; }

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
    /// A CSS selector of one or more elements outside the component that accept a drop as well, which is how
    /// a whole form, a card or the page itself becomes the drop target while the browse button stays where it
    /// is. The root element of the component is always a drop zone and needs no selector of its own; the
    /// elements this one names are matched whenever a drag reaches them, so one rendered after the component
    /// - or replaced later on - is a drop zone from the moment it matches. While files are dragged over any
    /// of them, all of them (the root included) carry the <see cref="Classes"/>.Dragging class and the
    /// <see cref="Styles"/>.Dragging inline style, and the focus being inside one of them is also what lets
    /// a paste land in this component.
    /// </summary>
    [Parameter] public string? DropZoneSelector { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being already in the file list
    /// while <see cref="AllowDuplicates"/> is disabled.
    /// </summary>
    [Parameter] public string DuplicateErrorMessage { get; set; } = "The file is already selected";

    /// <summary>
    /// The message shown for failed file removes.
    /// </summary>
    [Parameter] public string FailedRemoveMessage { get; set; } = "File remove failed";

    /// <summary>
    /// The message shown for failed file uploads.
    /// </summary>
    [Parameter] public string FailedUploadMessage { get; set; } = "File upload failed";

    /// <summary>
    /// Custom formatter of the file size shown under the name of each file item.
    /// Receives the size of the file in bytes and returns the text to display,
    /// which is the place to localize the units or to switch between the binary and the decimal bases.
    /// When not set, a built-in humanizer is used.
    /// </summary>
    [Parameter] public Func<long, string>? FileSizeFormatter { get; set; }

    /// <summary>
    /// Custom validation function called for each newly selected file after the built-in validations pass.
    /// Return an error message to reject the file so it will not be uploaded, or null to accept it.
    /// </summary>
    [Parameter] public Func<BitFileInfo, string?>? FileValidator { get; set; }

    /// <summary>
    /// The accessible name of the file list, so that a screen reader user landing on it is told what the list
    /// they are in holds instead of only how many items it has. Set it to an empty string to leave the list
    /// unnamed. The default value is "Selected files".
    /// </summary>
    [Parameter] public string FileListAriaLabel { get; set; } = "Selected files";

    /// <summary>
    /// Custom Razor template rendering each item of the file list in place of the built-in one, receiving
    /// the file as its context with its name, size, progress, speed and status all available. It is only
    /// asked for the files that are actually in the list, so a removed file leaves no empty item behind.
    /// </summary>
    [Parameter] public RenderFragment<BitFileInfo>? FileViewTemplate { get; set; }

    /// <summary>
    /// Whether the built-in file list is left unrendered. The files are still selected, validated,
    /// uploaded and reported through <see cref="Files"/> and the callbacks - they are simply not drawn,
    /// which is what the surrounding page needs when it shows the attachments in a layout of its own.
    /// </summary>
    [Parameter] public bool HideFileView { get; set; }

    /// <summary>
    /// Whether to hide the default browse button label from the UI.
    /// </summary>
    [Parameter] public bool HideLabel { get; set; }

    /// <summary>
    /// The text of the browse button. Setting it to an empty string hides the button altogether.
    /// </summary>
    [Parameter] public string Label { get; set; } = "Browse";

    /// <summary>
    /// Custom Razor template rendered in place of the browse button, which also replaces the built-in
    /// dashed drop indicator living on that button - a custom label should bring its own drag feedback
    /// through the Dragging entry of <see cref="Classes"/> or <see cref="Styles"/>.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Maximum allowed number of files in the file list (0 for unlimited).
    /// Files selected beyond this count are rejected at selection time and will not be uploaded.
    /// Only files that pass the other validations consume a slot.
    /// </summary>
    [Parameter] public int MaxCount { get; set; }

    /// <summary>
    /// Specifies the message shown for the files rejected due to exceeding the maximum number of files.
    /// </summary>
    [Parameter] public string MaxCountErrorMessage { get; set; } = "The maximum number of files is exceeded";

    /// <summary>
    /// The maximum allowed size in bytes of each file (0 for unlimited). A larger file is rejected at
    /// selection time with the <see cref="MaxSizeErrorMessage"/> and will not be uploaded.
    /// </summary>
    [Parameter] public long MaxSize { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being larger than the <see cref="MaxSize"/>.
    /// </summary>
    [Parameter] public string MaxSizeErrorMessage { get; set; } = "The file size is larger than the max size";

    /// <summary>
    /// Maximum allowed total size in bytes of all the files of the file list (0 for unlimited).
    /// Files pushing the accumulated size beyond this limit are rejected at selection time and will not be
    /// uploaded, becoming eligible again once removals free up room.
    /// Only files that pass the other validations consume the budget.
    /// </summary>
    [Parameter] public long MaxTotalSize { get; set; }

    /// <summary>
    /// Specifies the message shown for the files rejected for making the total size of the file list
    /// exceed the maximum total size.
    /// </summary>
    [Parameter] public string MaxTotalSizeErrorMessage { get; set; } = "The total size of the files is larger than the max total size";

    /// <summary>
    /// The minimum allowed size in bytes of each file (0 for no limit). A smaller file is rejected at
    /// selection time with the <see cref="MinSizeErrorMessage"/> and will not be uploaded.
    /// </summary>
    [Parameter] public long MinSize { get; set; }

    /// <summary>
    /// The message shown for the files rejected for being smaller than the <see cref="MinSize"/>.
    /// </summary>
    [Parameter] public string MinSizeErrorMessage { get; set; } = "The file size is smaller than the min size";

    /// <summary>
    /// Whether several files can be handed over at once, both through the file dialog and through a
    /// single drop or paste. Without it a multi-file drop or paste is trimmed down to its first file.
    /// </summary>
    [Parameter] public bool Multiple { get; set; }

    /// <summary>
    /// The message shown for the files rejected for not matching any entry of <see cref="AllowedExtensions"/>.
    /// </summary>
    [Parameter] public string NotAllowedExtensionErrorMessage { get; set; } = "The file type is not allowed";

    /// <summary>
    /// Callback for when every file of a batch that actually started uploading has reached a terminal
    /// state - completed, failed, canceled, removed or rejected by the validations. A selection that was
    /// never asked to upload never settles, so it never reports itself as complete.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo[]> OnAllUploadsComplete { get; set; }

    /// <summary>
    /// Callback for when file or files status change. It is invoked with the whole file list right after
    /// a selection, and with only the file that changed whenever a single status changes afterwards, so
    /// the current state of the batch is better read back from <see cref="Files"/> than from the argument.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo[]> OnChange { get; set; }

    /// <summary>
    /// Callback invoked right after <see cref="OnChange"/> whenever a selection carries at least one file
    /// rejected by the validations, providing an array of only the rejected files along with their messages.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo[]> OnInvalid { get; set; }

    /// <summary>
    /// Callback for when the upload of a file makes progress, invoked on every progress report of the
    /// browser with the file whose <see cref="BitFileInfo.TotalUploadedSize"/>,
    /// <see cref="BitFileInfo.UploadSpeed"/> and <see cref="BitFileInfo.RemainingTime"/> have just moved.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnProgress { get; set; }

    /// <summary>
    /// Callback for when a file has been removed, whether it was dropped from the list on this side or
    /// deleted from the server through the <see cref="RemoveUrl"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnRemoveComplete { get; set; }

    /// <summary>
    /// Callback for when the removal of a file from the server failed, leaving the file in the list with
    /// the <see cref="FailedRemoveMessage"/> rather than pretending it is gone.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnRemoveFailed { get; set; }

    /// <summary>
    /// Callback for when a file upload is about to start, invoked before the request that carries its
    /// first byte and therefore once per run of the file rather than once per chunk. It is the place to
    /// attach the <see cref="BitFileInfo.HttpHeaders"/> and the <see cref="BitFileInfo.FormFields"/>
    /// that belong to this one file, both of which are read again for every request it makes.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnUploading { get; set; }

    /// <summary>
    /// Callback for when a file has been uploaded successfully, with the body of the server response of
    /// its last request on its <see cref="BitFileInfo.Message"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnUploadComplete { get; set; }

    /// <summary>
    /// Callback for when the upload of a file failed for good - after the automatic retries, if any, have
    /// all been spent - with the body of the failed response on its <see cref="BitFileInfo.Message"/>.
    /// </summary>
    [Parameter] public EventCallback<BitFileInfo> OnUploadFailed { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the pause upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PauseIconName"/> when both are set.
    /// Defaults to the built-in Pause icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom pause icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="PauseIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? PauseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the pause upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Pause</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Pause</c>).
    /// <br />
    /// For external icon libraries, use <see cref="PauseIcon"/> instead.
    /// </remarks>
    [Parameter] public string? PauseIconName { get; set; }

    /// <summary>
    /// The tooltip of the pause upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Pause report.pdf"). Defaults to "Pause".
    /// </summary>
    [Parameter] public string? PauseButtonTitle { get; set; }

    /// <summary>
    /// The status message shown for the files of <see cref="PreloadedFiles"/>, which are already on the
    /// server rather than freshly uploaded and would otherwise read as an upload that just succeeded.
    /// </summary>
    [Parameter] public string PreloadedFileMessage { get; set; } = "Already uploaded";

    /// <summary>
    /// The files that are already on the server, shown in the list from the start so that a form editing a
    /// record can present the attachments it already has next to the ones the user is adding. They carry no
    /// content on this side, so they are never uploaded: each of them starts out completed, counts towards
    /// <see cref="MaxCount"/> and <see cref="MaxTotalSize"/> like any other file, and offers the same remove
    /// button, which deletes it from the server through the <see cref="RemoveUrl"/> with the
    /// <see cref="BitFileInfo.FileId"/> in the BIT_FILE_ID header - so that id is what the server's own id
    /// belongs in. A <see cref="BitFileInfo.PreviewUrl"/> pointing at a thumbnail the server serves is
    /// rendered by <see cref="ShowPreview"/> just like a locally generated one.
    /// The list is built from the collection again whenever the files in it change - which files, by their
    /// <see cref="BitFileInfo.FileId"/>, rather than which array they arrive in, so one written inline in the
    /// markup does not undo what the user has done on every render. The instances are the very ones the list
    /// then holds, so their <see cref="BitFileInfo.Status"/> is where to read what became of each of them.
    /// </summary>
    [Parameter] public IReadOnlyCollection<BitFileInfo>? PreloadedFiles { get; set; }

    /// <summary>
    /// The message shown for the files waiting in the queue for a free slot of the <see cref="ConcurrentUploads"/>
    /// limit, which is what tells a file that is about to start apart from one that was never asked to upload.
    /// </summary>
    [Parameter] public string QueuedUploadMessage { get; set; } = "Waiting to upload";

    /// <summary>
    /// Whether to read the pixel dimensions of the selected image files, filling the
    /// <see cref="BitFileInfo.Width"/> and <see cref="BitFileInfo.Height"/> of each of them before the
    /// validations run, so that a <see cref="FileValidator"/> can reject an image by its dimensions.
    /// Reading them means decoding every image in the browser, which costs time and memory on a large
    /// selection, so it is off by default.
    /// </summary>
    [Parameter] public bool ReadImageDimensions { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the remove file button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="RemoveIconName"/> when both are set.
    /// Defaults to the built-in Delete icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom remove icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="RemoveIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? RemoveIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the remove file button from the built-in Fluent UI icons.
    /// Defaults to <c>Delete</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Delete</c>).
    /// <br />
    /// For external icon libraries, use <see cref="RemoveIcon"/> instead.
    /// </remarks>
    [Parameter] public string? RemoveIconName { get; set; }

    /// <summary>
    /// The tooltip of the remove file button, which is also used as the prefix of its accessible label
    /// (e.g., "Remove report.pdf"). Defaults to "Remove".
    /// </summary>
    [Parameter] public string? RemoveButtonTitle { get; set; }

    /// <summary>
    /// Custom HTTP headers attached to the remove request.
    /// </summary>
    [Parameter] public Dictionary<string, string>? RemoveRequestHttpHeaders { get; set; }

    /// <summary>
    /// The provider function creating the HTTP headers of the remove request, invoked right before the
    /// request goes out and taking precedence over <see cref="RemoveRequestHttpHeaders"/>.
    /// </summary>
    [Parameter] public Func<Task<Dictionary<string, string>>>? RemoveRequestHttpHeadersProvider { get; set; }

    /// <summary>
    /// The HTTP method of the remove request (e.g., "POST"). Defaults to "DELETE".
    /// </summary>
    [Parameter] public string? RemoveRequestHttpMethod { get; set; }

    /// <summary>
    /// Custom query strings appended to the URL of the remove request.
    /// </summary>
    [Parameter] public Dictionary<string, string>? RemoveRequestQueryStrings { get; set; }

    /// <summary>
    /// The provider function creating the query strings of the remove request, invoked right before the
    /// request goes out and taking precedence over <see cref="RemoveRequestQueryStrings"/>.
    /// </summary>
    [Parameter] public Func<Task<Dictionary<string, string>>>? RemoveRequestQueryStringsProvider { get; set; }

    /// <summary>
    /// URL of the server endpoint removing the files. A file whose bytes already reached the server is
    /// deleted from it through a request to this URL carrying its name as a query string and its id in
    /// the BIT_FILE_ID header; a file that never uploaded is simply dropped from the list without one.
    /// </summary>
    [Parameter] public string? RemoveUrl { get; set; }

    /// <summary>
    /// Gets or sets the icon to use for the retry button of a failed or canceled file using custom CSS classes
    /// for external icon libraries. Takes precedence over <see cref="RetryIconName"/> when both are set.
    /// Defaults to the built-in Refresh icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom retry icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="RetryIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? RetryIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the retry button of a failed or canceled file
    /// from the built-in Fluent UI icons. Defaults to <c>Refresh</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Refresh</c>).
    /// <br />
    /// For external icon libraries, use <see cref="RetryIcon"/> instead.
    /// </remarks>
    [Parameter] public string? RetryIconName { get; set; }

    /// <summary>
    /// The tooltip of the retry button of a failed or canceled file, which is also used as the prefix of its
    /// accessible label (e.g., "Retry report.pdf"). Defaults to "Retry".
    /// </summary>
    [Parameter] public string? RetryButtonTitle { get; set; }

    /// <summary>
    /// Decides whether a failed upload is worth retrying automatically, receiving the file and the HTTP status
    /// code of the failed request (0 for a network error, a timeout or an aborted request) and returning true
    /// to spend one of the <see cref="AutoRetries"/> attempts on it.
    /// When not set, a built-in rule retries the failures a second attempt can plausibly survive - network
    /// errors, timeouts, 408, 429 and the 5xx server errors - and gives up right away on the other 4xx,
    /// which say that the request itself is the problem and would fail again just the same.
    /// </summary>
    [Parameter] public Func<BitFileInfo, int, bool>? ShouldAutoRetry { get; set; }

    /// <summary>
    /// Whether a thumbnail of every selected image is shown at the head of its file item, produced
    /// entirely in the browser from an object URL that is handed back as soon as the file is removed or
    /// the component is reset. The same URL is on the <see cref="BitFileInfo.PreviewUrl"/> of each file.
    /// A file that is not an image takes a glyph of its type in a box of the same size instead, so that
    /// the names of a mixed list stay lined up along one edge.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; }

    /// <summary>
    /// Whether each settled file item offers a remove button, which drops a file that never uploaded from
    /// the list and deletes an uploaded one from the server through the <see cref="RemoveUrl"/>.
    /// </summary>
    [Parameter] public bool ShowRemoveButton { get; set; }

    /// <summary>
    /// The size of the file upload, applied to the browse button and the file list items.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitFileUpload.
    /// </summary>
    [Parameter] public BitFileUploadClassStyles? Styles { get; set; }

    /// <summary>
    /// The message shown for successful file uploads.
    /// </summary>
    [Parameter] public string SuccessfulUploadMessage { get; set; } = "File upload succeeded";

    /// <summary>
    /// Gets or sets the icon to use for the upload button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UploadIconName"/> when both are set.
    /// Defaults to the built-in Play icon when neither is set.
    /// </summary>
    /// <remarks>
    /// Use this property to render a custom upload icon from external libraries like FontAwesome or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="UploadIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? UploadIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to use for the upload button from the built-in Fluent UI icons.
    /// Defaults to <c>Play</c> when not set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Play</c>).
    /// <br />
    /// For external icon libraries, use <see cref="UploadIcon"/> instead.
    /// </remarks>
    [Parameter] public string? UploadIconName { get; set; }

    /// <summary>
    /// The tooltip of the upload button, which is also used as the prefix of its accessible label
    /// (e.g., "Upload report.pdf"). Defaults to "Upload".
    /// </summary>
    [Parameter] public string? UploadButtonTitle { get; set; }

    /// <summary>
    /// The name of the form field carrying the file content in the upload request. Defaults to "file".
    /// </summary>
    [Parameter] public string? UploadFormFieldName { get; set; }

    /// <summary>
    /// Additional multipart form fields sent alongside the content of every file in its upload requests,
    /// which is what carries the metadata a server needs next to the bytes - a target folder, an album id,
    /// a caption - for the endpoints that read it from the form rather than from the query string.
    /// The <see cref="BitFileInfo.FormFields"/> of a file is merged over these for that file.
    /// </summary>
    [Parameter] public Dictionary<string, string>? UploadRequestFormFields { get; set; }

    /// <summary>
    /// Custom HTTP headers attached to the upload requests, fixed at selection time.
    /// </summary>
    [Parameter] public Dictionary<string, string>? UploadRequestHttpHeaders { get; set; }

    /// <summary>
    /// The provider function to create the http headers for upload request.
    /// Unlike <see cref="UploadRequestHttpHeaders"/>, it is invoked right before every single request -
    /// each file and each chunk - which is what lets it hand over a freshly minted access token.
    /// </summary>
    [Parameter] public Func<Task<Dictionary<string, string>>>? UploadRequestHttpHeadersProvider { get; set; }

    /// <summary>
    /// The HTTP method of the upload request (e.g., "PUT"). Defaults to "POST".
    /// </summary>
    [Parameter] public string? UploadRequestHttpMethod { get; set; }

    /// <summary>
    /// Custom query strings appended to the URL of the upload requests, fixed at selection time.
    /// </summary>
    [Parameter] public Dictionary<string, string>? UploadRequestQueryStrings { get; set; }

    /// <summary>
    /// The provider function to create the query strings for upload request.
    /// Unlike <see cref="UploadRequestQueryStrings"/>, it is invoked right before every single request -
    /// each file and each chunk - which is what lets it hand over a value that does not survive a batch.
    /// </summary>
    [Parameter] public Func<Task<Dictionary<string, string>>>? UploadRequestQueryStringsProvider { get; set; }

    /// <summary>
    /// The timeout of the upload request for each file or chunk. When it elapses the upload of the file fails.
    /// Set to null (the default) for no timeout.
    /// </summary>
    [Parameter] public TimeSpan? UploadTimeout { get; set; }

    /// <summary>
    /// URL of the server endpoint receiving the files, fixed at selection time. Use
    /// <see cref="UploadUrlProvider"/> instead for an endpoint that has to be minted per request.
    /// </summary>
    [Parameter] public string? UploadUrl { get; set; }

    /// <summary>
    /// The provider function to create the URL of the server endpoint receiving the files.
    /// Unlike <see cref="UploadUrl"/>, it is invoked right before every single request - each file and
    /// each chunk - which is what lets it hand over a presigned URL that expires.
    /// </summary>
    [Parameter] public Func<Task<string?>>? UploadUrlProvider { get; set; }

    /// <summary>
    /// The visual variant of the browse button, which decides how much of the <see cref="Color"/> it carries:
    /// a full fill, only an outline, or neither.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Whether the upload request is sent with credentials such as cookies and authorization headers
    /// for cross-origin requests (the withCredentials flag of the underlying XMLHttpRequest).
    /// </summary>
    [Parameter] public bool WithCredentials { get; set; }



    /// <summary>
    /// A list of all of the selected files to upload.
    /// </summary>
    public IReadOnlyList<BitFileInfo> Files => _files;

    /// <summary>
    /// The status of the batch as a whole: <see cref="BitFileUploadStatus.Pending"/> while nothing has been
    /// uploaded yet, <see cref="BitFileUploadStatus.InProgress"/> from the moment an upload is started, and
    /// <see cref="BitFileUploadStatus.Completed"/> once every file has reached a terminal state, whichever it is.
    /// The outcome of each individual file is on its own <see cref="BitFileInfo.Status"/>.
    /// </summary>
    public BitFileUploadStatus UploadStatus { get; private set; }

    /// <summary>
    /// The id of the file input element.
    /// </summary>
    public string? InputId { get; private set; }

    /// <summary>
    /// Indicates that the file upload is in the middle of removing at least one file.
    /// </summary>
    public bool IsRemoving => _removingCount > 0;

    /// <summary>
    /// The total size in bytes of all the files of the batch, excluding the removed ones
    /// and the ones rejected by the validations.
    /// </summary>
    public long TotalSize => _files.Where(IsCountedInOverallProgress).Sum(f => f.Size);

    /// <summary>
    /// The total uploaded size in bytes across all the files of the batch, excluding the removed ones
    /// and the ones rejected by the validations.
    /// </summary>
    public long TotalUploadedSize => _files.Where(IsCountedInOverallProgress)
                                           .Sum(f => Math.Min(f.Size, f.TotalUploadedSize + f.LastChunkUploadedSize));

    /// <summary>
    /// The overall upload progress of the batch as a percentage (0 to 100),
    /// combining the progress of all the files weighted by their size.
    /// </summary>
    public int OverallUploadProgress
    {
        get
        {
            var totalSize = TotalSize;

            return totalSize == 0 ? 0 : (int)(TotalUploadedSize * 100 / totalSize);
        }
    }

    /// <summary>
    /// The combined speed in bytes per second of every file of the batch that is uploading right now,
    /// which is what the connection as a whole is carrying. It is null while nothing is on the wire.
    /// </summary>
    public double? TotalUploadSpeed
    {
        get
        {
            var speeds = _files.Where(f => f.UploadSpeed is > 0).Sum(f => f.UploadSpeed!.Value);

            return speeds > 0 ? speeds : null;
        }
    }

    /// <summary>
    /// The estimated time left before the whole batch is uploaded, derived from the
    /// <see cref="TotalUploadSpeed"/> and the bytes of the batch that are still to be sent.
    /// It is null whenever nothing is uploading and the speed is therefore unknown.
    /// </summary>
    public TimeSpan? OverallRemainingTime
    {
        get
        {
            if (TotalUploadSpeed is not { } speed) return null;

            // the progress reports count the multipart overhead too, so the bytes reported as sent can
            // run slightly past the batch itself, which must not turn into a negative time left.
            var remaining = Math.Max(0, TotalSize - TotalUploadedSize);

            return TimeSpan.FromSeconds(remaining / speed);
        }
    }

    /// <summary>
    /// Starts uploading the file(s), resuming a paused or chunked file from the last chunk that made it
    /// through and retrying a failed or canceled one with a fresh budget of automatic retries. A file whose
    /// request is already on the wire is left running rather than being started over, and a file that has
    /// completed, been removed or been rejected by the validations has nothing left to send.
    /// </summary>
    /// <param name="fileInfo">
    /// null (default) => all files | else => specific file
    /// </param>
    /// <param name="uploadUrl">A custom URL to upload to, overriding the <see cref="UploadUrl"/> for this call.</param>
    public async Task Upload(BitFileInfo? fileInfo = null, string? uploadUrl = null)
    {
        if (_files.Any() is false) return;

        BitFileInfo[] targets = fileInfo is null ? [.. _files] : [fileInfo];

        // an upload call with nothing left to upload must not disturb the state of the settled batch,
        // and in particular must not report its completion once more.
        if (targets.Any(HasPendingWork) is false) return;

        UploadStatus = BitFileUploadStatus.InProgress;

        foreach (var file in targets)
        {
            // a file already in flight is walking through its own sequence of chunks and keeps the slot
            // it is holding, so it never goes back to the end of the queue between two chunks.
            if (ConcurrentUploads > 0 && file.Status != BitFileUploadStatus.InProgress)
            {
                if (HasPendingWork(file) is false) continue;

                file.QueuedUploadUrl = uploadUrl;

                if (_uploadQueue.Contains(file) is false)
                {
                    _uploadQueue.Add(file);
                    file.IsQueued = true;
                }

                continue;
            }

            await UploadOneFile(file, uploadUrl);
        }

        await PumpUploadQueue();

        RequestRender();
    }

    /// <summary>
    /// Pauses the upload of the files that are on their way: an in-progress file aborts its in-flight request
    /// immediately and keeps the bytes that made it, and a file waiting in the queue of the
    /// <see cref="ConcurrentUploads"/> limit is taken out of that queue. Both can be resumed later through
    /// <see cref="Upload"/>, which picks a chunked file up from its last completed chunk. A file that was never
    /// asked to upload, and one that has already settled, are left exactly as they are.
    /// </summary>
    /// <param name="fileInfo">
    /// null (default) => all files | else => specific file
    /// </param>
    public async Task PauseUpload(BitFileInfo? fileInfo = null)
    {
        if (_files.Any() is false) return;

        if (fileInfo is null)
        {
            foreach (var file in _files.ToArray())
            {
                await PauseOneFile(file);
            }
        }
        else
        {
            await PauseOneFile(fileInfo);
        }

        // pausing a running file frees its slot up for the next file waiting in the queue.
        await PumpUploadQueue();

        RequestRender();
    }

    /// <summary>
    /// Cancels the upload of every file that is still in play - running, waiting in the queue, paused or
    /// merely selected - settling each of them as canceled right away rather than recording an intention
    /// nobody can see, and aborting the in-flight request of a running one. A file that has already
    /// settled is left alone, and a canceled file can be started again later through <see cref="Upload"/>.
    /// </summary>
    /// <param name="fileInfo">
    /// null (default) => all files | else => specific file
    /// </param>
    public async Task CancelUpload(BitFileInfo? fileInfo = null)
    {
        if (_files.Any() is false) return;

        if (fileInfo is null)
        {
            foreach (var file in _files.ToArray())
            {
                await CancelOneFile(file);
            }
        }
        else
        {
            await CancelOneFile(fileInfo);
        }

        // canceling a running file frees its slot up for the next file waiting in the queue,
        // and canceling the last file still running settles the batch as a whole.
        await SettleFile();

        RequestRender();
    }

    /// <summary>
    /// Removes a file by calling the RemoveUrl if the file upload is already started.
    /// </summary>
    /// <param name="fileInfo">
    /// null => all files | else => specific file
    /// </param>
    public async Task RemoveFile(BitFileInfo? fileInfo = null)
    {
        if (_files.Any() is false) return;

        // a removal already running for this very file must not be started a second time, but the removal
        // of another file has no reason to be dropped just because one is already on its way.
        if (fileInfo is null ? IsRemoving : fileInfo.IsRemoving) return;

        _removingCount++;

        try
        {
            if (fileInfo is null)
            {
                foreach (var file in _files.ToArray())
                {
                    await RemoveOneFile(file);
                }
            }
            else
            {
                await RemoveOneFile(fileInfo);
            }
        }
        finally
        {
            // a reset landing in the middle of a removal already dropped the counter to zero,
            // and this removal must not push it below that into a state nothing recovers from.
            _removingCount = Math.Max(0, _removingCount - 1);
        }

        // the room the removed files gave back can take in the files a list level limit had rejected.
        ApplyListValidations();

        Announce();

        // and it can also free a slot up for the next file waiting to be uploaded, or settle the
        // batch as a whole when the file taken away was the last one still running.
        await SettleFile();

        RequestRender();
    }

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

        await _js.BitFileUploadBrowse(_inputRef);
    }

    /// <summary>
    /// Resets the file upload, taking the whole selection and its upload state back to where they started.
    /// The files of <see cref="PreloadedFiles"/> stay: they belong to the record rather than to this
    /// selection - except the ones already deleted from the server, which nothing on this side brings back.
    /// </summary>
    public async Task Reset()
    {
        if (IsDisposed) return;

        // a reset undoes what was picked here, and what is on the server was never picked here.
        var preloaded = _files.Where(f => f.IsPreloaded && f.Status is not BitFileUploadStatus.Removed).ToArray();

        _files.Clear();
        _files.AddRange(preloaded);
        _uploadQueue.Clear();
        // the removals of the files that just went away have nobody left to report to, so the counter
        // they were holding is dropped with them rather than leaving the component removing forever.
        _removingCount = 0;
        UploadStatus = BitFileUploadStatus.Pending;

        await _js.BitFileUploadReset(UniqueId, _inputRef);

        Announce();

        StateHasChanged();
    }



    /// <summary>
    /// Receive upload progress notification from underlying JavaScript.
    /// </summary>
    [JSInvokable("HandleChunkUploadProgress")]
    public async Task __HandleChunkUploadProgress(int index, long loaded)
    {
        if (GetFileByUploaderIndex(index) is not { } file) return;
        if (file.Status != BitFileUploadStatus.InProgress) return;

        file.LastChunkUploadedSize = loaded;

        UpdateTransferRate(file);

        await UpdateStatus(BitFileUploadStatus.InProgress, file);

        // a browser reports the progress of a request many times a second, and every file being uploaded
        // reports its own, so repainting the whole file list on each of them would spend more time
        // rendering than uploading on a large batch. the settling of a chunk or of a file renders on its
        // own anyway, so the only thing a skipped repaint costs is a progress bar a few frames behind.
        var now = DateTime.UtcNow;
        if (now - _lastProgressRender < PROGRESS_RENDER_INTERVAL) return;

        _lastProgressRender = now;

        StateHasChanged();
    }

    /// <summary>
    /// Receive upload finished notification from underlying JavaScript.
    /// </summary>
    [JSInvokable("HandleChunkUpload")]
    public async Task __HandleChunkUpload(int fileIndex, int responseStatus, string responseText)
    {
        if (GetFileByUploaderIndex(fileIndex) is not { } file) return;

        // whatever this response says, the request it answers is over and the file is free again.
        file.IsRequestInFlight = false;
        file.ResponseStatus = responseStatus;

        if (file.Status != BitFileUploadStatus.InProgress) return;

        file.LastChunkUploadedSize = 0;

        if (responseStatus is >= 200 and <= 299)
        {
            file.TotalUploadedSize += file.PendingChunkSize;
            file.AutoRetryAttempts = 0;

            UpdateChunkSize(file);

            if (file.TotalUploadedSize < file.Size)
            {
                await Upload(file);
            }
            else
            {
                file.Message = responseText;
                await UpdateStatus(BitFileUploadStatus.Completed, file);
                await SettleFile();
            }
        }
        else
        {
            // a failed chunk fails the whole file right away instead of blindly moving on to the next chunk.
            // its size is not counted as uploaded, so a retry - automatic or through the Upload method -
            // resumes from the last successfully uploaded chunk.
            if (AutoRetries > 0 && file.AutoRetryAttempts < AutoRetries && IsWorthRetrying(file, responseStatus))
            {
                file.AutoRetryAttempts++;

                if (GetAutoRetryDelay(file) is { } delay && delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }

                if (IsDisposed) return;

                // the file may have been paused, canceled, removed or reset while the delay was pending,
                // in which case that outcome stands instead of being overwritten by this stale failure.
                if (file.Status != BitFileUploadStatus.InProgress || _files.Contains(file) is false)
                {
                    await PumpUploadQueue();
                    StateHasChanged();
                    return;
                }

                await Upload(file);
                StateHasChanged();
                return;
            }

            file.Message = responseText;
            await UpdateStatus(BitFileUploadStatus.Failed, file);
            await SettleFile();
        }

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-upl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-upl-fil",
            BitVariant.Outline => "bit-upl-otl",
            BitVariant.Text => "bit-upl-txt",
            _ => "bit-upl-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-upl-pri",
            BitColor.Secondary => "bit-upl-sec",
            BitColor.Tertiary => "bit-upl-ter",
            BitColor.Info => "bit-upl-inf",
            BitColor.Success => "bit-upl-suc",
            BitColor.Warning => "bit-upl-wrn",
            BitColor.SevereWarning => "bit-upl-swr",
            BitColor.Error => "bit-upl-err",
            BitColor.PrimaryBackground => "bit-upl-pbg",
            BitColor.SecondaryBackground => "bit-upl-sbg",
            BitColor.TertiaryBackground => "bit-upl-tbg",
            BitColor.PrimaryForeground => "bit-upl-pfg",
            BitColor.SecondaryForeground => "bit-upl-sfg",
            BitColor.TertiaryForeground => "bit-upl-tfg",
            BitColor.PrimaryBorder => "bit-upl-pbr",
            BitColor.SecondaryBorder => "bit-upl-sbr",
            BitColor.TertiaryBorder => "bit-upl-tbr",
            _ => "bit-upl-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-upl-sm",
            BitSize.Medium => "bit-upl-md",
            BitSize.Large => "bit-upl-lg",
            _ => "bit-upl-md"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnInitializedAsync()
    {
        InputId = $"FileUpload-{UniqueId}-input";
        _buttonId = $"FileUpload-{UniqueId}-label";
        _descriptionId = $"FileUpload-{UniqueId}-description";

        return base.OnInitializedAsync();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitFileUploadParams))]
    protected override async Task OnParametersSetAsync()
    {
        CascadingParameters?.UpdateParameters(this);

        await base.OnParametersSetAsync();

        if (ShouldApplyPreloadedFiles())
        {
            ApplyPreloadedFiles();

            // the preloaded files take their share of the count and total size budgets, so the files the
            // user had already picked are judged against the list as it stands with them in it.
            ApplyListValidations();

            Announce();
        }

        if (_dropZoneRef is null) return;

        await UpdateDropZone();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false)
        {
            await RestorePendingFocus();
            return;
        }

        _dotnetObj = DotNetObjectReference.Create(this);

        _allowDrop = AllowDrop;
        _allowPaste = AllowPaste;
        _expandDirectories = Directory;
        _dropZoneSelector = DropZoneSelector;
        _dragClass = GetDragClass();
        _dragStyle = Styles?.Dragging;

        _dropZoneRef = await _js.BitFileUploadSetupDragDrop(RootElement, _inputRef, _dragClass, _dragStyle,
                                                           _allowDrop, _allowPaste, _expandDirectories, _dropZoneSelector);

        if (IsDisposed) return;
        if (_dropZoneRef is null) return;

        // a parameter change that arrived while the setup was still awaiting found no drop zone to update yet,
        // so the drop zone is synchronized once more against the parameters as they stand now.
        await UpdateDropZone();
    }



    // The built-in file items report themselves here so that the focus can be handed to a button of another
    // item once the button that was pressed is gone. A custom FileViewTemplate registers nothing, which is
    // exactly right: the focus then falls back to the browse button rather than into markup this component
    // knows nothing about.
    internal void RegisterItem(string fileId, _BitFileUploadItem item) => _itemRefs[fileId] = item;

    internal void UnregisterItem(string fileId, _BitFileUploadItem item)
    {
        if (_itemRefs.TryGetValue(fileId, out var registered) && ReferenceEquals(registered, item))
        {
            _itemRefs.Remove(fileId);
        }
    }

    internal async Task HandleItemUpload(BitFileInfo file)
    {
        // the upload button is replaced by the pause and cancel pair of a running file.
        RequestFocus(file, BitFileUploadFocusTarget.Pause);

        await Upload(file);
    }

    internal async Task HandleItemPause(BitFileInfo file)
    {
        // the pause button is replaced by the resume (upload) button of a paused file.
        RequestFocus(file, BitFileUploadFocusTarget.Upload);

        await PauseUpload(file);
    }

    internal async Task HandleItemCancel(BitFileInfo file)
    {
        // the cancel button is replaced by the retry (upload) button of a canceled file.
        RequestFocus(file, BitFileUploadFocusTarget.Upload);

        await CancelUpload(file);
    }

    internal async Task HandleItemRemove(BitFileInfo file)
    {
        // the whole item goes away with its remove button, so the focus moves to the item that takes its
        // place - or to the one before it at the end of the list. A removal that fails leaves the item
        // where it was, and the focus then stays on the very button that was pressed.
        var visible = _files.Where(f => f.Status != BitFileUploadStatus.Removed).ToList();
        var index = visible.IndexOf(file);
        var neighbor = index < 0 ? null
                     : index + 1 < visible.Count ? visible[index + 1]
                     : index > 0 ? visible[index - 1]
                     : null;

        RequestFocus(file, BitFileUploadFocusTarget.Remove, neighbor);

        await RemoveFile(file);
    }

    internal bool IsFileTypeNotAllowed(BitFileInfo file)
    {
        if (AllowsAllFileTypes(AllowedExtensions)) return false;

        return IsFileTypeNotAllowed(file, GetNormalizedExtensions(AllowedExtensions).ToArray());
    }

    internal string GetStatusMessage(BitFileInfo file)
    {
        return file.Status switch
        {
            // a file that was already on the server when the list was built did not just finish uploading,
            // and saying that it did would credit this visit with something that happened long before it.
            BitFileUploadStatus.Completed => file.IsPreloaded ? PreloadedFileMessage : SuccessfulUploadMessage,
            BitFileUploadStatus.Failed => FailedUploadMessage,
            BitFileUploadStatus.Canceled => CanceledUploadMessage,
            BitFileUploadStatus.RemoveFailed => FailedRemoveMessage,
            BitFileUploadStatus.NotAllowed => file.Message ?? NotAllowedExtensionErrorMessage,
            // a file waiting for a free slot of the concurrency limit says so, since it looks exactly
            // like a file nobody asked to upload while it is in fact already on its way.
            BitFileUploadStatus.Pending when file.IsQueued => QueuedUploadMessage,
            _ => string.Empty,
        };
    }



    // the public API methods can be called from outside a UI event - from a timer or a service callback -
    // where nothing would repaint the component on their behalf, so they ask for the render themselves.
    private void RequestRender()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }

    private void RequestFocus(BitFileInfo file, BitFileUploadFocusTarget target, BitFileInfo? fallback = null)
    {
        _pendingFocus = true;
        _pendingFocusTarget = target;
        _pendingFocusFileId = file.FileId;
        _pendingFocusFallbackFileId = fallback?.FileId;
    }

    private async Task RestorePendingFocus()
    {
        if (_pendingFocus is false) return;

        // a removal on its way to the server keeps its item in place, with a spinner where the button that
        // was pressed used to be, and renders again when it settles. moving the focus on already would take
        // it away from an item that is still there - and away from the failure message if the removal fails.
        if (IsRemoving) return;

        _pendingFocus = false;

        var target = _pendingFocusTarget;
        var fileId = _pendingFocusFileId;
        var fallbackId = _pendingFocusFallbackFileId;

        _pendingFocusFileId = null;
        _pendingFocusFallbackFileId = null;

        try
        {
            // the item the action was taken on comes first: it is still there whenever the action left it in
            // place, and the focus then belongs on the button that took over from the one that was pressed.
            if (fileId is not null && _itemRefs.TryGetValue(fileId, out var item) && await item.TryFocus(target)) return;

            if (fallbackId is not null && _itemRefs.TryGetValue(fallbackId, out var neighbor) && await neighbor.TryFocus(target)) return;

            if (_ShowLabelButton && IsEnabled)
            {
                await _labelRef.FocusAsync();
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        catch (JSException) { } // an element taken out of the DOM between the render and this call cannot be focused
    }

    // The file an uploader of the JavaScript side speaks for. The index is that uploader's own, which is the
    // position of the file among the ones picked here - the preloaded files sit in the list beside them
    // without an uploader of their own, so the list cannot simply be indexed into.
    private BitFileInfo? GetFileByUploaderIndex(int index)
    {
        if (index < 0) return null;

        return _files.FirstOrDefault(f => f.IsPreloaded is false && f.Index == index);
    }

    private static bool IsCountedInOverallProgress(BitFileInfo file)
    {
        // a file that is already on the server is not part of this transfer at all, and counting it would
        // report a batch nobody has started yet as complete and water down the progress of a real one.
        return file.IsPreloaded is false
            && file.Status is not BitFileUploadStatus.NotAllowed and not BitFileUploadStatus.Removed;
    }

    private string GetDragClass() => $"bit-upl-drg {Classes?.Dragging}".Trim();

    private async Task UpdateDropZone()
    {
        var dragClass = GetDragClass();
        var dragStyle = Styles?.Dragging;

        if (_allowDrop == AllowDrop && _allowPaste == AllowPaste && _expandDirectories == Directory &&
            _dropZoneSelector == DropZoneSelector && _dragClass == dragClass && _dragStyle == dragStyle) return;

        _allowDrop = AllowDrop;
        _allowPaste = AllowPaste;
        _expandDirectories = Directory;
        _dropZoneSelector = DropZoneSelector;
        _dragClass = dragClass;
        _dragStyle = dragStyle;

        try
        {
            await _dropZoneRef.InvokeVoidAsync("update", _allowDrop, _allowPaste, _expandDirectories,
                                               _dragClass, _dragStyle, _dropZoneSelector);
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

    private static bool IsFileTypeNotAllowed(BitFileInfo file, string[] allowedTypes)
    {
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

    private string? GetAcceptValue()
    {
        if (Accept.HasValue()) return Accept;

        if (AllowsAllFileTypes(AllowedExtensions)) return null;

        var accept = string.Join(",", GetNormalizedExtensions(AllowedExtensions));

        return accept.HasValue() ? accept : null;
    }

    private bool ValidateFile(BitFileInfo file)
    {
        if (MaxSize > 0 && file.Size > MaxSize)
        {
            file.Status = BitFileUploadStatus.NotAllowed;
            file.Message = MaxSizeErrorMessage;
            return false;
        }

        if (MinSize > 0 && file.Size < MinSize)
        {
            file.Status = BitFileUploadStatus.NotAllowed;
            file.Message = MinSizeErrorMessage;
            return false;
        }

        if (IsFileTypeNotAllowed(file))
        {
            file.Status = BitFileUploadStatus.NotAllowed;
            file.Message = NotAllowedExtensionErrorMessage;
            return false;
        }

        if (FileValidator is not null)
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
                file.Status = BitFileUploadStatus.NotAllowed;
                file.Message = message;
                return false;
            }
        }

        return true;
    }

    private async Task HandleOnChange()
    {
        // only the static configuration is baked into the requests at selection time. the providers are
        // the answer to a value that does not survive a batch - a token about to expire, a presigned URL -
        // so they are invoked right before each request instead, and are left out here on purpose.
        var url = UploadRequestQueryStrings is null ? UploadUrl : AddQueryString(UploadUrl, UploadRequestQueryStrings);

        if (Append is false)
        {
            // a selection replaces the selection, not the record: the files that are already on the server
            // are not this component's to drop from the view, and taking them out here would hide
            // attachments that are still very much there.
            _files.RemoveAll(f => f.IsPreloaded is false);
            _uploadQueue.Clear();
            UploadStatus = BitFileUploadStatus.Pending;
        }

        if (IsDisposed) return;

        var newFiles = await _js.BitFileUploadSetup(UniqueId, _dotnetObj, _inputRef, Append, url, UploadRequestHttpHeaders,
                                                    UploadRequestHttpMethod, WithCredentials,
                                                    (long)(UploadTimeout?.TotalMilliseconds ?? 0), UploadFormFieldName,
                                                    ShowPreview, ReadImageDimensions);

        if (IsDisposed) return;

        _files.AddRange(newFiles);

        if (_files.Any() is false) return;

        // a selection appended to a batch that is still uploading leaves the status of that batch alone:
        // only a batch known to be running is ever reported as complete, so taking it back to pending here
        // would keep the completion of the files already on the wire from ever being announced.
        if (UploadStatus is not BitFileUploadStatus.InProgress)
        {
            UploadStatus = BitFileUploadStatus.Pending;
        }

        // the built-in validations run right at selection time, so the user learns about a rejected file
        // immediately instead of at the moment the upload gets attempted.
        foreach (var file in newFiles)
        {
            ValidateFile(file);
        }

        ApplyListValidations();

        Announce();

        await OnChange.InvokeAsync([.. _files]);

        if (OnInvalid.HasDelegate)
        {
            var invalidFiles = newFiles.Where(f => f.Status == BitFileUploadStatus.NotAllowed).ToArray();

            if (invalidFiles.Length > 0)
            {
                await OnInvalid.InvokeAsync(invalidFiles);
            }
        }

        if (AutoUpload)
        {
            await Upload();
        }
    }

    // two selections of the same file are indistinguishable by their name, size and last modified time,
    // which is as close to an identity as the browser exposes for a picked file.
    private static string GetFileIdentity(BitFileInfo file)
    {
        return $"{file.Name}|{file.Size}|{file.LastModified}";
    }

    // a file the server reported has no last modified time of a file system behind it, so a file picked
    // here is the same one as an attachment already on the record when its name and size are - which is
    // as much as the two of them have in common to be compared on.
    private static string GetServerFileIdentity(BitFileInfo file)
    {
        return $"{file.Name}|{file.Size}";
    }

    // Whether the list has to be built from the preloaded files again. The collection arriving as another
    // instance is the obvious case, but a collection written inline in the markup is another instance on
    // every single render: rebuilding on that alone would put the files the user has removed back and
    // announce the whole list again each time, so what decides is which files are in it rather than which
    // array they came in.
    private bool ShouldApplyPreloadedFiles()
    {
        if (ReferenceEquals(_appliedPreloadedFiles, PreloadedFiles)) return false;

        _appliedPreloadedFiles = PreloadedFiles;

        var signature = GetPreloadedSignature();

        if (signature.SequenceEqual(_appliedPreloadedSignature, StringComparer.Ordinal)) return false;

        _appliedPreloadedSignature = signature;

        return true;
    }

    // What a preloaded file is recognized by from one render to the next: the id the app gave it, and
    // failing that - the component mints one for a file handed over without it - what it says about itself.
    private string[] GetPreloadedSignature()
    {
        if (PreloadedFiles is null) return [];

        return [.. PreloadedFiles.Where(f => f is not null)
                                 .Select(f => f.FileId.HasValue() ? f.FileId : GetServerFileIdentity(f))];
    }

    // The files that are already on the server take the head of the list, ahead of anything picked here, and
    // are rebuilt from scratch whenever the collection they come from is replaced - a form that has just
    // loaded the record it edits hands its attachments over exactly that way. They are adopted rather than
    // copied, so the very instances the app holds are the ones the list works on and their Status is what
    // tells it what became of each of them.
    private void ApplyPreloadedFiles()
    {
        _files.RemoveAll(f => f.IsPreloaded);

        if (PreloadedFiles is null) return;

        var position = 0;

        foreach (var file in PreloadedFiles)
        {
            if (file is null) continue;

            file.IsPreloaded = true;
            // there is no picked file and so no uploader behind this one, and the index is what would name
            // that uploader: it is left invalid on purpose, so that a stray call cannot reach another file's.
            file.Index = PRELOADED_FILE_INDEX;
            file.IsQueued = false;
            file.IsRequestInFlight = false;
            file.Status = BitFileUploadStatus.Completed;
            // the whole file is on the server already, which is what makes it count as fully uploaded
            // towards the progress of the batch instead of dragging it down to nothing.
            file.TotalUploadedSize = file.Size;
            file.LastChunkUploadedSize = 0;

            // the id travels to the server in the BIT_FILE_ID header of the remove request, so the one the
            // app gave it stands; only a file handed over without one needs something to be named by.
            if (file.FileId.HasNoValue())
            {
                file.FileId = Guid.NewGuid().ToString();
            }

            _files.Insert(position++, file);
        }
    }

    // the rules that judge a file against the rest of the list - being a duplicate, the maximum count and the
    // maximum total size - are re-evaluated from scratch every time the list changes, so that a file rejected
    // by one of them can be taken back as soon as a removal frees up room or drops the original it duplicated.
    private void ApplyListValidations()
    {
        foreach (var file in _files)
        {
            if (file.ListValidationFailed is false) continue;

            file.ListValidationFailed = false;
            file.Status = BitFileUploadStatus.Pending;
            file.Message = null;
        }

        if (AllowDuplicates is false)
        {
            var knownFiles = new HashSet<string>(StringComparer.Ordinal);
            // the attachments the record already has, which sit at the head of the list, so a file picked
            // here is judged against them by the time it is reached.
            var knownServerFiles = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in _files)
            {
                if (file.Status is BitFileUploadStatus.Removed) continue;

                // every file registers its identity, even a rejected one, so that a re-selection of a file
                // already in the list is caught no matter why that file was rejected.
                var isFirstOfItsKind = knownFiles.Add(GetFileIdentity(file));

                // what the server says is on the record is the truth about it, so one of its files is never
                // turned away as the copy of another - it only ever makes copies of itself out of the ones
                // picked here.
                if (file.IsPreloaded)
                {
                    knownServerFiles.Add(GetServerFileIdentity(file));
                    continue;
                }

                if (isFirstOfItsKind && knownServerFiles.Contains(GetServerFileIdentity(file)) is false) continue;

                // a file that already failed a validation of its own keeps that message, which would
                // otherwise be lost as soon as the duplication is resolved, and one that already started
                // uploading is committed and is not taken back by a later selection of the same file.
                if (file.Status is not BitFileUploadStatus.Pending) continue;

                Reject(file, DuplicateErrorMessage);
            }
        }

        if (MaxCount <= 0 && MaxTotalSize <= 0) return;

        var count = 0;
        var totalSize = 0L;

        foreach (var file in _files)
        {
            // only the files that made it through the other validations and are still around consume the
            // budget, so a file rejected for its size or type never pushes a good file over a limit.
            if (file.Status is BitFileUploadStatus.NotAllowed or BitFileUploadStatus.Removed) continue;

            // a file that already started or finished uploading is committed: it takes its share of the
            // budget, but a limit that a later selection pushed over never takes it back.
            if (file.Status is not BitFileUploadStatus.Pending)
            {
                count++;
                totalSize += file.Size;
                continue;
            }

            if (MaxCount > 0 && count >= MaxCount)
            {
                Reject(file, MaxCountErrorMessage);
            }
            else if (MaxTotalSize > 0 && totalSize + file.Size > MaxTotalSize)
            {
                Reject(file, MaxTotalSizeErrorMessage);
            }
            else
            {
                count++;
                totalSize += file.Size;
            }
        }

        static void Reject(BitFileInfo file, string message)
        {
            file.ListValidationFailed = true;
            file.Status = BitFileUploadStatus.NotAllowed;
            file.Message = message;
        }
    }

    // spending the retry budget on a failure that is going to come back identical helps nobody, so a
    // failure is only retried when a second attempt could plausibly go differently.
    private bool IsWorthRetrying(BitFileInfo file, int responseStatus)
    {
        if (ShouldAutoRetry is not null)
        {
            try
            {
                return ShouldAutoRetry(file, responseStatus);
            }
            catch
            {
                // a throwing predicate settles the file rather than taking the whole upload down with it.
                return false;
            }
        }

        // a status of 0 stands for a network error, a timeout or an aborted request, none of which say
        // anything about the request being wrong; 408 and 429 explicitly ask for another attempt later,
        // and a 5xx is the server having a bad moment rather than a verdict on what was sent.
        return responseStatus is 0 or 408 or 429 or (>= 500 and <= 599);
    }

    // whether the file still has bytes to send, which is what tells an upload call that is worth
    // starting apart from one landing on an already settled batch.
    // How long to wait before the attempt the file is about to make, its number being the number of
    // automatic retries already spent on it - the first retry is attempt 1.
    private TimeSpan? GetAutoRetryDelay(BitFileInfo file)
    {
        if (AutoRetryDelayProvider is null) return AutoRetryDelay;

        try
        {
            return AutoRetryDelayProvider(file, file.AutoRetryAttempts) ?? AutoRetryDelay;
        }
        catch
        {
            // a provider that throws decides nothing, and the retry it was asked about still happens.
            return AutoRetryDelay;
        }
    }

    private static bool HasPendingWork(BitFileInfo file)
    {
        return file.Status is not BitFileUploadStatus.Completed
                          and not BitFileUploadStatus.Removed
                          and not BitFileUploadStatus.NotAllowed
            && (file.Size == 0 || file.TotalUploadedSize < file.Size);
    }

    // starts as many queued files as the concurrency limit still has room for. it is called both when an
    // upload is requested and whenever a file settles, which is what keeps the queue draining on its own.
    private async Task PumpUploadQueue()
    {
        if (ConcurrentUploads <= 0) return;

        while (_uploadQueue.Count > 0)
        {
            if (_files.Count(f => f.Status is BitFileUploadStatus.InProgress) >= ConcurrentUploads) return;

            var next = _uploadQueue[0];
            _uploadQueue.RemoveAt(0);
            next.IsQueued = false;

            // a file paused, canceled, removed or reset while it was waiting has nothing left to start.
            if (HasPendingWork(next) is false) continue;

            await UploadOneFile(next, next.QueuedUploadUrl);
        }
    }

    private async Task UploadOneFile(BitFileInfo fileInfo, string? uploadUrl = null)
    {
        if (_files.Any() is false) return;
        // a file that is already on the server has nothing on this side to send.
        if (fileInfo.IsPreloaded) return;
        if (fileInfo.Status is BitFileUploadStatus.NotAllowed or BitFileUploadStatus.Removed) return;

        // a file whose request is already on the wire is busy: sending a second one would take the
        // connection away from the first and start the file over from its very first byte, which is
        // what a second press of an upload-everything button would otherwise do to every running file.
        if (fileInfo.IsRequestInFlight) return;

        var uploadedSize = fileInfo.TotalUploadedSize;
        if (fileInfo.Size != 0 && uploadedSize >= fileInfo.Size) return;

        if (ValidateFile(fileInfo) is false)
        {
            await UpdateStatus(BitFileUploadStatus.NotAllowed, fileInfo);
            return;
        }

        if (fileInfo.Status is BitFileUploadStatus.Failed)
        {
            // a manual retry of a failed file starts with a fresh automatic retry budget.
            fileInfo.AutoRetryAttempts = 0;
        }

        await UpdateStatus(BitFileUploadStatus.InProgress, fileInfo);

        long to;
        long from = 0;

        // a request that never finished left its partial byte count behind, which would otherwise be
        // added on top of the bytes of the new request and show a progress running ahead of the truth.
        fileInfo.LastChunkUploadedSize = 0;

        if (ChunkedUpload)
        {
            from = fileInfo.TotalUploadedSize;
            to = Math.Min(fileInfo.Size, from + _internalChunkSize);

            fileInfo.StartTimeUpload = DateTime.UtcNow;
        }
        else
        {
            to = fileInfo.Size;
        }

        // the span of the in-flight request is remembered as sent, since the chunk size can get
        // adjusted dynamically before the response of this request arrives.
        fileInfo.PendingChunkSize = to - from;

        if (from == 0)
        {
            await OnUploading.InvokeAsync(fileInfo);
        }

        // the providers get their say right before the request goes out, so a token they mint is as
        // fresh as it can be. they are awaited, and the file may well have been paused, canceled,
        // removed or reset in the meantime, in which case there is nothing left to send.
        var requestUrl = await GetRequestUploadUrl(uploadUrl);
        var requestHeaders = await GetUploadHeaders(fileInfo, from, to);

        if (IsDisposed) return;
        if (fileInfo.Status is not BitFileUploadStatus.InProgress || _files.Contains(fileInfo) is false) return;

        // the speed of the request is measured from the moment it is handed over to the browser.
        fileInfo.TransferStartTime = DateTime.UtcNow;
        fileInfo.TransferStartOffset = fileInfo.TotalUploadedSize;
        fileInfo.IsRequestInFlight = true;

        try
        {
            await _js.BitFileUploadUpload(UniqueId, from, to, fileInfo.Index, requestUrl, requestHeaders,
                                          GetUploadFormFields(fileInfo));
        }
        catch
        {
            // a request that never made it out of the interop call has no response coming back to declare
            // the file free again, and a file left marked as busy would turn down every later retry.
            fileInfo.IsRequestInFlight = false;
            throw;
        }
    }

    // the URL of a single request: an explicit one passed to Upload wins, then the providers get to mint
    // one for this very request, and otherwise the endpoint baked in at selection time keeps applying.
    private async Task<string?> GetRequestUploadUrl(string? uploadUrl)
    {
        if (uploadUrl.HasValue()) return uploadUrl;

        if (UploadUrlProvider is null && UploadRequestQueryStringsProvider is null) return null;

        var url = UploadUrlProvider is null ? UploadUrl : await UploadUrlProvider.Invoke();

        if (UploadRequestQueryStringsProvider is null)
        {
            // the static query strings are already part of the URL baked in at selection time, so they
            // only have to be put back when a provider replaced that URL with one of its own.
            return UploadRequestQueryStrings is null ? url : AddQueryString(url, UploadRequestQueryStrings);
        }

        var merged = MergeEntries(UploadRequestQueryStrings, await UploadRequestQueryStringsProvider.Invoke());

        return merged is null ? url : AddQueryString(url, merged);
    }

    // the extra multipart fields of a request: the ones of the whole component, with the ones of this
    // specific file - the OnUploading callback is where they are usually filled in - laid over them.
    private Dictionary<string, string>? GetUploadFormFields(BitFileInfo fileInfo)
    {
        return MergeEntries(UploadRequestFormFields, fileInfo.FormFields);
    }

    // combines two optional sets of entries into a new one, the second one winning over the first,
    // without ever handing back - let alone mutating - either of the dictionaries it was given.
    private static Dictionary<string, string>? MergeEntries(Dictionary<string, string>? first, Dictionary<string, string>? second)
    {
        if (first is null || first.Count == 0) return second;
        if (second is null || second.Count == 0) return first;

        var merged = new Dictionary<string, string>(first);

        foreach (var entry in second)
        {
            merged[entry.Key] = entry.Value;
        }

        return merged;
    }

    // the chunked mode leaves the server with the job of putting the pieces of a file back together, which
    // it can only do reliably when it is told where each piece belongs. a blind append breaks as soon as two
    // chunks overtake each other or a retry sends one of them twice, so every chunk carries its byte range -
    // as the standard Content-Range header and as plain numbers for the servers that would rather not parse it.
    private async Task<Dictionary<string, string>?> GetUploadHeaders(BitFileInfo fileInfo, long from, long to)
    {
        // the headers of the provider are per request, so they are never handed to the setup call and
        // are instead merged in here, under the ones of this specific file, right before each request.
        // an entry sent twice would be concatenated into one comma separated value by the browser,
        // which is exactly why the provider does not also go through the static setup headers.
        var providedHeaders = UploadRequestHttpHeadersProvider is null
                                ? null
                                : await UploadRequestHttpHeadersProvider.Invoke();

        var fileHeaders = MergeEntries(providedHeaders, fileInfo.HttpHeaders);

        if (ChunkedUpload is false) return fileHeaders;

        Dictionary<string, string> headers = fileHeaders is null
                                                ? []
                                                : new(fileHeaders);

        // an empty file has no byte to describe a range over, so it only carries the size headers.
        if (fileInfo.Size > 0)
        {
            headers["Content-Range"] = $"bytes {from}-{to - 1}/{fileInfo.Size}";
        }

        headers["BIT_CHUNK_FROM"] = from.ToString(CultureInfo.InvariantCulture);
        headers["BIT_CHUNK_TO"] = to.ToString(CultureInfo.InvariantCulture);
        headers["BIT_FILE_SIZE"] = fileInfo.Size.ToString(CultureInfo.InvariantCulture);

        return headers;
    }

    private async Task PauseOneFile(BitFileInfo file)
    {
        // a file waiting in the queue is already on its way, and taking it out of the queue is what
        // pausing means for it - there is no request of its own to abort yet.
        if (file.IsQueued)
        {
            _uploadQueue.Remove(file);
            file.IsQueued = false;

            await PauseUploadOneFile(file);
            return;
        }

        // there is nothing to pause about a file that was never asked to upload, or one that has already
        // settled: pausing them would only put them in a state their own buttons cannot get them out of.
        if (file.Status is not BitFileUploadStatus.InProgress) return;

        // aborting right away instead of waiting for the next chunk boundary, so that pausing
        // also works for non-chunked uploads whose only request is already in flight.
        await PauseUploadOneFile(file);
    }

    private async Task CancelOneFile(BitFileInfo file)
    {
        // a file that has already settled has nothing left to cancel: taking a completed upload back would
        // claim something about the server that canceling on this side cannot deliver, and calling off a
        // file that already failed would only replace the reason it failed with a less useful one.
        if (file.Status is BitFileUploadStatus.Completed
                        or BitFileUploadStatus.Failed
                        or BitFileUploadStatus.Canceled
                        or BitFileUploadStatus.Removed
                        or BitFileUploadStatus.NotAllowed) return;

        // an in-progress file aborts its in-flight request; a pending, queued, paused or failed file has
        // no request to abort, but its cancellation must still land right away rather than sit as an
        // intention nobody can see, waiting for an upload that may never be asked for.
        _uploadQueue.Remove(file);
        file.IsQueued = false;

        await CancelUploadOneFile(file);
    }

    private async Task PauseUploadOneFile(BitFileInfo file)
    {
        // the status changes before the abort, so that the abort callback coming back from JavaScript
        // finds the file already paused instead of mistaking the aborted request for a failed upload.
        await UpdateStatus(BitFileUploadStatus.Paused, file);
        file.IsRequestInFlight = false;

        await AbortRequestOf(file);
    }

    private async Task CancelUploadOneFile(BitFileInfo file)
    {
        // the status changes before the abort, so that the abort callback coming back from JavaScript
        // finds the file already canceled instead of mistaking the aborted request for a failed upload.
        await UpdateStatus(BitFileUploadStatus.Canceled, file);
        file.IsRequestInFlight = false;

        await AbortRequestOf(file);
    }

    // a file with no uploader of its own - a preloaded one - has no request to abort, and the index it
    // carries names no uploader, which the JavaScript side would read as "every file of this component".
    private async Task AbortRequestOf(BitFileInfo file)
    {
        if (file.IsPreloaded) return;

        await _js.BitFileUploadPause(UniqueId, file.Index);
    }

    // the speed of an upload is what turns a progress bar into an answer to "how long is this going to
    // take", so it is measured over the request currently in flight - the only window whose start is
    // known exactly - and the bytes still to send are divided by it to get the time left.
    private static void UpdateTransferRate(BitFileInfo file)
    {
        if (file.TransferStartTime is not { } startTime) return;

        var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;

        // the very first progress reports arrive too close to the start for their ratio to mean
        // anything, and a reading taken over no time at all would be an infinity.
        if (elapsed < 0.2) return;

        var uploaded = file.TotalUploadedSize + file.LastChunkUploadedSize - file.TransferStartOffset;

        if (uploaded <= 0) return;

        var speed = uploaded / elapsed;

        file.UploadSpeed = speed;

        // the progress events count the multipart overhead too, so the bytes reported as sent can run
        // slightly past the file itself, which must not turn into a negative time left.
        var remaining = Math.Max(0, file.Size - (file.TotalUploadedSize + file.LastChunkUploadedSize));

        file.RemainingTime = TimeSpan.FromSeconds(remaining / speed);
    }

    private void UpdateChunkSize(BitFileInfo file)
    {
        if (AutoChunkSize is false || ChunkedUpload is false) return;

        var dtNow = DateTime.UtcNow;
        var duration = (dtNow - file.StartTimeUpload.GetValueOrDefault(dtNow)).TotalMilliseconds;

        if (duration <= 0) return;
        if (duration is >= 1000 and <= 1500) return;

        // the new size is derived from the chunk that was actually measured rather than from the current
        // setting, which another file uploading in parallel may well have moved since this one was sent.
        if (file.PendingChunkSize <= 0) return;

        _internalChunkSize = Convert.ToInt64(file.PendingChunkSize / (duration / 1000));

        if (_internalChunkSize > MAX_CHUNK_SIZE)
        {
            _internalChunkSize = MAX_CHUNK_SIZE;
        }

        if (_internalChunkSize < MIN_CHUNK_SIZE)
        {
            _internalChunkSize = MIN_CHUNK_SIZE;
        }
    }

    private bool _HasDescription => DescriptionTemplate is not null || Description.HasValue();

    // the built-in browse button is what the label of the input turns into, so a template taking that label
    // over, a hidden label or no label at all each leave the input without a button to be named after.
    private bool _ShowLabelButton => LabelTemplate is null && HideLabel is false && Label.HasValue();

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

        var files = _files.Where(f => f.Status != BitFileUploadStatus.Removed).ToArray();

        if (files.Length == 0) return "No file selected.";

        // the files that were already on the server were never selected here and never uploaded here, so
        // they are counted apart rather than credited to a batch that may not have sent a single byte.
        var attached = files.Count(f => f.IsPreloaded);
        var selected = files.Where(f => f.IsPreloaded is false).ToArray();

        var completed = selected.Count(f => f.Status is BitFileUploadStatus.Completed);
        var failed = selected.Count(f => f.Status is BitFileUploadStatus.Failed);
        var notAllowed = selected.Count(f => f.Status is BitFileUploadStatus.NotAllowed);

        return (selected.Length > 0
                    ? $"{selected.Length} file{(selected.Length == 1 ? string.Empty : "s")} selected."
                    : "No file selected.") +
               (attached > 0 ? $" {attached} already attached." : string.Empty) +
               (completed > 0 ? $" {completed} uploaded." : string.Empty) +
               (failed > 0 ? $" {failed} failed." : string.Empty) +
               (notAllowed > 0 ? $" {notAllowed} not allowed." : string.Empty);
    }

    private async Task UpdateStatus(BitFileUploadStatus uploadStatus, BitFileInfo fileInfo)
    {
        if (uploadStatus is not BitFileUploadStatus.InProgress)
        {
            // a file that is not on the wire has no speed, and a time left measured over a transfer
            // that is over would keep counting down towards something that is never going to happen.
            fileInfo.UploadSpeed = null;
            fileInfo.RemainingTime = null;
            fileInfo.TransferStartTime = null;
        }

        if (fileInfo.Status != uploadStatus)
        {
            fileInfo.Status = uploadStatus;

            // upload outcomes get announced to screen readers; the ever-changing progress does not,
            // since announcing every tick would drown out everything else.
            if (uploadStatus is not BitFileUploadStatus.InProgress and not BitFileUploadStatus.Pending)
            {
                Announce();
            }

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

    // a file reaching a terminal state hands its slot over to the next file waiting in the queue,
    // and only once nothing is left running does the batch get reported as complete.
    private async Task SettleFile()
    {
        await PumpUploadQueue();

        await CheckAllUploadsComplete();
    }

    private async Task CheckAllUploadsComplete()
    {
        // a batch nobody ever asked to upload has not completed anything, however settled its files look:
        // a selection of files rejected by the validations is not an upload that finished.
        if (UploadStatus is not BitFileUploadStatus.InProgress) return;

        // paused files still count as in flight - the batch is only complete once every file has
        // reached a terminal state (completed, failed, canceled, removed or rejected by validation).
        if (_files.Any(f => f.Status is BitFileUploadStatus.Pending or BitFileUploadStatus.InProgress or BitFileUploadStatus.Paused)) return;

        UploadStatus = BitFileUploadStatus.Completed;
        await OnAllUploadsComplete.InvokeAsync([.. _files]);
    }

    private async Task RemoveOneFile(BitFileInfo fileInfo)
    {
        if (fileInfo.Status is BitFileUploadStatus.Removed) return;

        _uploadQueue.Remove(fileInfo);
        fileInfo.IsQueued = false;

        // a file on its way out has no reason to keep sending itself, so its in-flight request is dropped
        // before anything else - otherwise the bytes would keep flowing to an endpoint that is about to be
        // told to delete them.
        if (fileInfo.Status is BitFileUploadStatus.InProgress or BitFileUploadStatus.Paused)
        {
            await AbortRequestOf(fileInfo);
        }

        // a completed file counts as being on the server even when it carried no byte at all,
        // which is exactly the case of an empty file that uploaded successfully.
        var isOnServer = fileInfo.TotalUploadedSize > 0 || fileInfo.Status is BitFileUploadStatus.Completed;

        if (isOnServer && RemoveUrl.HasValue())
        {
            fileInfo.IsRemoving = true;
            StateHasChanged();

            await RemoveOneFileFromServer(fileInfo);

            fileInfo.IsRemoving = false;
        }
        else
        {
            await UpdateStatus(BitFileUploadStatus.Removed, fileInfo);
        }

        if (fileInfo.Status is not BitFileUploadStatus.Removed) return;

        // a removed file is never going to be sent again, so everything the browser was holding on to for
        // it is handed back: the picked file itself, which would otherwise stay in memory for the whole
        // life of the page, and the object URL of the thumbnail that is not rendered anymore.
        // a preloaded file's thumbnail is a URL of the server's own, which this side neither created nor
        // gets to hand back, and there is no uploader holding anything for it either.
        if (fileInfo.IsPreloaded) return;

        fileInfo.PreviewUrl = null;

        try
        {
            await _js.BitFileUploadRelease(UniqueId, fileInfo.Index);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task RemoveOneFileFromServer(BitFileInfo fileInfo)
    {
        try
        {
            var url = AddQueryString(RemoveUrl!, "fileName", fileInfo.Name);

            var qs = RemoveRequestQueryStringsProvider is null
                        ? RemoveRequestQueryStrings
                        : (await RemoveRequestQueryStringsProvider.Invoke());

            if (qs is not null)
            {
                url = AddQueryString(url, qs);
            }

            var method = RemoveRequestHttpMethod.HasValue()
                            ? new HttpMethod(RemoveRequestHttpMethod!)
                            : HttpMethod.Delete;

            using var request = new HttpRequestMessage(method, url);

            request.Headers.Add("BIT_FILE_ID", fileInfo.FileId);

            var httpHeaders = (RemoveRequestHttpHeadersProvider is null
                                ? RemoveRequestHttpHeaders
                                : (await RemoveRequestHttpHeadersProvider.Invoke())) ?? [];

            foreach (var header in httpHeaders)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            var response = await _httpClient.SendAsync(request);

            fileInfo.ResponseStatus = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                await UpdateStatus(BitFileUploadStatus.Removed, fileInfo);
            }
            else
            {
                fileInfo.Message = $"{(int)response.StatusCode} {response.ReasonPhrase}";
                await UpdateStatus(BitFileUploadStatus.RemoveFailed, fileInfo);
            }
        }
        catch (Exception ex)
        {
            // the request never came back with a status of its own, which is reported the same way the
            // upload side reports a request that never reached the server.
            fileInfo.ResponseStatus = 0;

            // only the message of the exception, since this text is rendered right in the file item and
            // a full stack trace there says nothing to the user while telling a stranger far too much.
            fileInfo.Message = ex.Message;
            await UpdateStatus(BitFileUploadStatus.RemoveFailed, fileInfo);
        }
    }

    private static string AddQueryString(string uri, string name, string value)
    {
        return AddQueryString(uri, new Dictionary<string, string> { { name, value } });
    }

    private static string AddQueryString(string? url, Dictionary<string, string> queryStrings)
    {
        if (url.HasNoValue()) return string.Empty;

        // this method is copied from:
        // https://github.com/aspnet/HttpAbstractions/blob/master/src/Microsoft.AspNetCore.WebUtilities/QueryHelpers.cs

        int anchorIndex = url!.IndexOf('#', StringComparison.InvariantCultureIgnoreCase);
        string uriToBeAppended = url;
        string? anchorText = null;

        // If there is an anchor, then the query string must be inserted before its first occurrence.
        if (anchorIndex != -1)
        {
            anchorText = url[anchorIndex..];
            uriToBeAppended = url[..anchorIndex];
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

    internal void OnSetChunkSize()
    {
        _internalChunkSize = ChunkSize.HasValue is false || AutoChunkSize
                                ? MIN_CHUNK_SIZE
                                : ChunkSize.Value;
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
                await _js.BitFileUploadClear(UniqueId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }
}
