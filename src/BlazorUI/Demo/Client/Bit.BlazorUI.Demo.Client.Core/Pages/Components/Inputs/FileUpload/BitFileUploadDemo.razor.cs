namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileUpload;

public partial class BitFileUploadDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accept",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accepted file types for the file browser using MIME types or file extensions (e.g., \"image/*\", \".pdf,.doc\"), applied to the accept attribute of the underlying input element. When not set, the accept attribute is generated from AllowedExtensions.",
        },
        new()
        {
            Name = "AllowDrop",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether files can be selected by dragging them from the operating system and dropping them on the component.",
        },
        new()
        {
            Name = "AllowDuplicates",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether a file that is already in the file list can be selected again. When disabled, a newly selected file matching an existing one by name, size and last modified time is rejected with the DuplicateErrorMessage instead of being uploaded a second time, becoming eligible again once the file it duplicates is removed.",
        },
        new()
        {
            Name = "AllowedExtensions",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "[\"*\"]",
            Description = "Allowed file types for validation purposes, accepting both file extensions (with an optional leading dot, case-insensitive) and MIME types with an optional wildcard (e.g., \"image/*\"). Use [\"*\"] to allow all file types. Files not matching any of these entries will not be uploaded.",
        },
        new()
        {
            Name = "AllowPaste",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether files can be selected by pasting them from the clipboard onto the component. The paste is only captured while the focus is inside the component.",
        },
        new()
        {
            Name = "AnnouncementProvider",
            Type = "Func<IReadOnlyList<BitFileInfo>, string?>?",
            DefaultValue = "null",
            Description = "Custom provider of the text announced by the screen reader through the live region of the component whenever the file list or an upload outcome changes. Receives the current file list and returns the text to announce, or null to announce nothing. When not set, a built-in English announcement is used.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "Append",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a new selection is added to the end of the current file list instead of replacing it, which is what lets the user build a batch up over several rounds of browsing, dropping or pasting. The files already in the list keep their upload state."
        },
        new()
        {
            Name = "AutoChunkSize",
            Type = "bool",
            DefaultValue = "false",
            Description = "Calculate the chunk size dynamically based on the user's Internet speed between 512 KB and 10 MB."
        },
        new()
        {
            Name = "AutoReset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the file list and the upload state are cleared right before the file dialog opens, so that every browse starts from a clean slate - the list empties even if the dialog is then cancelled."
        },
        new()
        {
            Name = "AutoRetries",
            Type = "int",
            DefaultValue = "0",
            Description = "The number of times a failed upload of a file gets retried automatically before it is reported as failed. In the chunked mode each retry resumes from the last successfully uploaded chunk. Set to 0 (the default) to disable the automatic retries."
        },
        new()
        {
            Name = "AutoRetryDelay",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The delay before each automatic retry of a failed upload. Set to null (the default) to retry immediately."
        },
        new()
        {
            Name = "AutoUpload",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the selected files start uploading the moment they are selected, skipping the per-file upload button entirely, for the cases where the selection itself expresses the intent to upload."
        },
        new()
        {
            Name = "CancelButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the cancel upload button, which is also used as the prefix of its accessible label (e.g., \"Cancel report.pdf\"). Defaults to \"Cancel\".",
        },
        new()
        {
            Name = "CancelIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to use for the cancel upload button using custom CSS classes for external icon libraries. Takes precedence over CancelIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "CancelIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to use for the cancel upload button from the built-in Fluent UI icons. Defaults to Cancel when not set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "CanceledUploadMessage",
            Type = "string",
            DefaultValue = "File upload canceled",
            Description = "The message shown for canceled file uploads."
        },
        new()
        {
            Name = "Capture",
            Type = "string?",
            DefaultValue = "null",
            Description = "The capture behavior of the file input on devices with a camera or microphone, rendered as the capture attribute of the input element (e.g., \"user\" for the front camera, \"environment\" for the rear camera).",
        },
        new()
        {
            Name = "ChunkedUpload",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether each file is sliced and sent as a series of sequential requests instead of one monolithic one, which is what makes a paused or failed file resume from the last chunk that made it through rather than starting over, so a dropped connection costs one chunk instead of the whole transfer."
        },
        new()
        {
            Name = "ChunkSize",
            Type = "long?",
            DefaultValue = "null",
            Description = "The size in bytes of each chunk of a chunked upload. When not set - and whenever AutoChunkSize is enabled, which takes the decision over - it starts at 512 KB."
        },
        new()
        {
            Name = "Classes",
            Type = "BitFileUploadClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitFileUpload.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the file upload, applied to the browse button, the drag-and-drop indicator, the progress bars and the hovered action buttons.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "ConcurrentUploads",
            Type = "int",
            DefaultValue = "0",
            Description = "The maximum number of files uploading at the same time, the remaining ones waiting in a queue in selection order and starting as soon as a slot frees up. Set to 0 (the default) to start every file at once."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short hint rendered under the browse button and wired to it through aria-describedby, which is the place to spell out the accepted file types and the size limits so that both sighted and screen reader users learn the constraints before hitting them.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Razor template of the hint rendered under the browse button, taking precedence over Description.",
        },
        new()
        {
            Name = "Directory",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to select folders (directories) instead of files, rendered as the webkitdirectory attribute. All files inside the selected folder and its subfolders will be added to the file list. It also makes a dropped folder expand into its contents instead of being ignored.",
        },
        new()
        {
            Name = "DuplicateErrorMessage",
            Type = "string",
            DefaultValue = "The file is already selected",
            Description = "The message shown for the files rejected for being already in the file list while AllowDuplicates is disabled."
        },
        new()
        {
            Name = "FailedRemoveMessage",
            Type = "string",
            DefaultValue = "File remove failed",
            Description = "The message shown for failed file removes."
        },
        new()
        {
            Name = "FailedUploadMessage",
            Type = "string",
            DefaultValue = "File upload failed",
            Description = "The message shown for failed file uploads."
        },
        new()
        {
            Name = "FileSizeFormatter",
            Type = "Func<long, string>?",
            DefaultValue = "null",
            Description = "Custom formatter of the file size shown under the name of each file item. Receives the size of the file in bytes and returns the text to display, which is the place to localize the units or to switch between the binary and the decimal bases. When not set, a built-in humanizer is used.",
        },
        new()
        {
            Name = "FileValidator",
            Type = "Func<BitFileInfo, string?>?",
            DefaultValue = "null",
            Description = "Custom validation function called for each newly selected file after the built-in validations pass. Return an error message to reject the file so it will not be uploaded, or null to accept it.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "FileViewTemplate",
            Type = "RenderFragment<BitFileInfo>?",
            DefaultValue = "null",
            Description = "Custom Razor template rendering each item of the file list in place of the built-in one, receiving the file as its context with its name, size, progress, speed and status all available. It is only asked for the files that are actually in the list, so a removed file leaves no empty item behind.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "HideFileView",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the built-in file list is left unrendered. The files are still selected, validated, uploaded and reported through the Files property and the callbacks - they are simply not drawn, which is what the surrounding page needs when it shows the attachments in a layout of its own."
        },
        new()
        {
            Name = "HideLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the default browse button label from the UI."
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "Browse",
            Description = "The text of the browse button. Setting it to an empty string hides the button altogether."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Razor template rendered in place of the browse button, which also replaces the built-in dashed drop indicator living on that button - a custom label should bring its own drag feedback through the Dragging entry of Classes or Styles."
        },
        new()
        {
            Name = "MaxCount",
            Type = "int",
            DefaultValue = "0",
            Description = "Maximum allowed number of files in the file list (0 for unlimited). Files selected beyond this count are rejected at selection time and will not be uploaded. Only files that pass the other validations consume a slot."
        },
        new()
        {
            Name = "MaxCountErrorMessage",
            Type = "string",
            DefaultValue = "The maximum number of files is exceeded",
            Description = "Specifies the message shown for the files rejected due to exceeding the maximum number of files."
        },
        new()
        {
            Name = "MaxSize",
            Type = "long",
            DefaultValue = "0",
            Description = "The maximum allowed size in bytes of each file (0 for unlimited). A larger file is rejected at selection time with the MaxSizeErrorMessage and will not be uploaded."
        },
        new()
        {
            Name = "MaxSizeErrorMessage",
            Type = "string",
            DefaultValue = "The file size is larger than the max size",
            Description = "The message shown for the files rejected for being larger than the MaxSize."
        },
        new()
        {
            Name = "MaxTotalSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Maximum allowed total size in bytes of all the files of the file list (0 for unlimited). Files pushing the accumulated size beyond this limit are rejected at selection time and will not be uploaded, becoming eligible again once removals free up room. Only files that pass the other validations consume the budget."
        },
        new()
        {
            Name = "MaxTotalSizeErrorMessage",
            Type = "string",
            DefaultValue = "The total size of the files is larger than the max total size",
            Description = "Specifies the message shown for the files rejected for making the total size of the file list exceed the maximum total size."
        },
        new()
        {
            Name = "MinSize",
            Type = "long",
            DefaultValue = "0",
            Description = "The minimum allowed size in bytes of each file (0 for no limit). A smaller file is rejected at selection time with the MinSizeErrorMessage and will not be uploaded."
        },
        new()
        {
            Name = "MinSizeErrorMessage",
            Type = "string",
            DefaultValue = "The file size is smaller than the min size",
            Description = "The message shown for the files rejected for being smaller than the MinSize."
        },
        new()
        {
            Name = "Multiple",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether several files can be handed over at once, both through the file dialog and through a single drop or paste. Without it a multi-file drop or paste is trimmed down to its first file."
        },
        new()
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string",
            DefaultValue = "The file type is not allowed",
            Description = "The message shown for the files rejected for not matching any entry of AllowedExtensions."
        },
        new()
        {
            Name = "OnAllUploadsComplete",
            Type = "EventCallback<BitFileInfo[]>",
            Description = "Callback for when every file of a batch that actually started uploading has reached a terminal state - completed, failed, canceled, removed or rejected by the validations. A selection that was never asked to upload never settles, so it never reports itself as complete.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInfo[]>",
            Description = "Callback for when file or files status change. It is invoked with the whole file list right after a selection, and with only the file that changed whenever a single status changes afterwards, so the current state of the batch is better read back from the Files property than from the argument.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnInvalid",
            Type = "EventCallback<BitFileInfo[]>",
            Description = "Callback invoked right after OnChange whenever a selection carries at least one file rejected by the validations, providing an array of only the rejected files along with their messages.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnProgress",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when the upload of a file makes progress, invoked on every progress report of the browser with the file whose TotalUploadedSize, UploadSpeed and RemainingTime have just moved.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnRemoveComplete",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a file has been removed, whether it was dropped from the list on this side or deleted from the server through the RemoveUrl.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnRemoveFailed",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when the removal of a file from the server failed, leaving the file in the list with the FailedRemoveMessage rather than pretending it is gone.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnUploading",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a file upload is about to start, invoked before the request that carries its first byte and therefore once per run of the file rather than once per chunk. It is the place to attach the HttpHeaders and the FormFields that belong to this one file, both of which are read again for every request it makes.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnUploadComplete",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when a file has been uploaded successfully, with the body of the server response of its last request on its Message.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "OnUploadFailed",
            Type = "EventCallback<BitFileInfo>",
            Description = "Callback for when the upload of a file failed for good - after the automatic retries, if any, have all been spent - with the body of the failed response on its Message.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "PauseButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the pause upload button, which is also used as the prefix of its accessible label (e.g., \"Pause report.pdf\"). Defaults to \"Pause\".",
        },
        new()
        {
            Name = "PauseIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to use for the pause upload button using custom CSS classes for external icon libraries. Takes precedence over PauseIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "PauseIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to use for the pause upload button from the built-in Fluent UI icons. Defaults to Pause when not set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "QueuedUploadMessage",
            Type = "string",
            DefaultValue = "Waiting to upload",
            Description = "The message shown for the files waiting in the queue for a free slot of the ConcurrentUploads limit, which is what tells a file that is about to start apart from one that was never asked to upload."
        },
        new()
        {
            Name = "ReadImageDimensions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to read the pixel dimensions of the selected image files, filling the Width and Height of each of them before the validations run, so that a FileValidator can reject an image by its dimensions. Reading them means decoding every image in the browser, which costs time and memory on a large selection, so it is off by default.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "RemoveButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the remove file button, which is also used as the prefix of its accessible label (e.g., \"Remove report.pdf\"). Defaults to \"Remove\".",
        },
        new()
        {
            Name = "RemoveIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to use for the remove file button using custom CSS classes for external icon libraries. Takes precedence over RemoveIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "RemoveIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to use for the remove file button from the built-in Fluent UI icons. Defaults to Delete when not set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "RemoveRequestHttpHeaders",
            Type = "Dictionary<string, string>?",
            DefaultValue = "null",
            Description = "Custom HTTP headers attached to the remove request."
        },
        new()
        {
            Name = "RemoveRequestHttpHeadersProvider",
            Type = "Func<Task<Dictionary<string, string>>>?",
            DefaultValue = "null",
            Description = "The provider function creating the HTTP headers of the remove request, invoked right before the request goes out and taking precedence over RemoveRequestHttpHeaders."
        },
        new()
        {
            Name = "RemoveRequestHttpMethod",
            Type = "string?",
            DefaultValue = "null",
            Description = "The HTTP method of the remove request (e.g., \"POST\"). Defaults to \"DELETE\".",
        },
        new()
        {
            Name = "RemoveRequestQueryStrings",
            Type = "Dictionary<string, string>?",
            DefaultValue = "null",
            Description = "Custom query strings appended to the URL of the remove request."
        },
        new()
        {
            Name = "RemoveRequestQueryStringsProvider",
            Type = "Func<Task<Dictionary<string, string>>>?",
            DefaultValue = "null",
            Description = "The provider function creating the query strings of the remove request, invoked right before the request goes out and taking precedence over RemoveRequestQueryStrings."
        },
        new()
        {
            Name = "RemoveUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL of the server endpoint removing the files. A file whose bytes already reached the server is deleted from it through a request to this URL carrying its name as a query string and its id in the BIT_FILE_ID header; a file that never uploaded is simply dropped from the list without one."
        },
        new()
        {
            Name = "RetryButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the retry button of a failed or canceled file, which is also used as the prefix of its accessible label (e.g., \"Retry report.pdf\"). Falls back to UploadButtonTitle and then to \"Retry\".",
        },
        new()
        {
            Name = "RetryIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to use for the retry button of a failed or canceled file using custom CSS classes for external icon libraries. Takes precedence over RetryIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "RetryIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to use for the retry button of a failed or canceled file from the built-in Fluent UI icons. Falls back to UploadIconName and then to Refresh.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "ShouldAutoRetry",
            Type = "Func<BitFileInfo, int, bool>?",
            DefaultValue = "null",
            Description = "Decides whether a failed upload is worth retrying automatically, receiving the file and the HTTP status code of the failed request (0 for a network error, a timeout or an aborted request) and returning true to spend one of the AutoRetries attempts on it. When not set, a built-in rule retries network errors, timeouts, 408, 429 and the 5xx server errors, and gives up right away on the other 4xx.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a thumbnail of every selected image is shown at the head of its file item, produced entirely in the browser from an object URL that is handed back as soon as the file is removed or the component is reset. The same URL is on the PreviewUrl of each file."
        },
        new()
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether each settled file item offers a remove button, which drops a file that never uploaded from the list and deletes an uploaded one from the server through the RemoveUrl."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the file upload, applied to the browse button and the file list items.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitFileUploadClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitFileUpload.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "SuccessfulUploadMessage",
            Type = "string",
            DefaultValue = "File upload succeeded",
            Description = "The message shown for successful file uploads."
        },
        new()
        {
            Name = "UploadButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the upload button, which is also used as the prefix of its accessible label (e.g., \"Upload report.pdf\"). Defaults to \"Upload\".",
        },
        new()
        {
            Name = "UploadFormFieldName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the form field carrying the file content in the upload request. Defaults to \"file\".",
        },
        new()
        {
            Name = "UploadIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to use for the upload button using custom CSS classes for external icon libraries. Takes precedence over UploadIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "UploadIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon to use for the upload button from the built-in Fluent UI icons. Defaults to Play when not set.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography"
        },
        new()
        {
            Name = "UploadRequestFormFields",
            Type = "Dictionary<string, string>?",
            DefaultValue = "null",
            Description = "Additional multipart form fields sent alongside the content of every file in its upload requests, for the endpoints that read their metadata from the form rather than from the query string. The FormFields of a file is merged over these for that file.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "UploadRequestHttpHeaders",
            Type = "Dictionary<string, string>?",
            DefaultValue = "null",
            Description = "Custom HTTP headers attached to the upload requests, fixed at selection time."
        },
        new()
        {
            Name = "UploadRequestHttpHeadersProvider",
            Type = "Func<Task<Dictionary<string, string>>>?",
            DefaultValue = "null",
            Description = "The provider function to create the http headers for upload request. Unlike UploadRequestHttpHeaders, it is invoked right before every single request - each file and each chunk - which is what lets it hand over a freshly minted access token."
        },
        new()
        {
            Name = "UploadRequestHttpMethod",
            Type = "string?",
            DefaultValue = "null",
            Description = "The HTTP method of the upload request (e.g., \"PUT\"). Defaults to \"POST\".",
        },
        new()
        {
            Name = "UploadRequestQueryStrings",
            Type = "Dictionary<string, string>?",
            DefaultValue = "null",
            Description = "Custom query strings appended to the URL of the upload requests, fixed at selection time."
        },
        new()
        {
            Name = "UploadRequestQueryStringsProvider",
            Type = "Func<Task<Dictionary<string, string>>>?",
            DefaultValue = "null",
            Description = "The provider function to create the query strings for upload request. Unlike UploadRequestQueryStrings, it is invoked right before every single request - each file and each chunk - which is what lets it hand over a value that does not survive a batch."
        },
        new()
        {
            Name = "UploadTimeout",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The timeout of the upload request for each file or chunk. When it elapses the upload of the file fails. Set to null (the default) for no timeout.",
        },
        new()
        {
            Name = "UploadUrl",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL of the server endpoint receiving the files, fixed at selection time. Use UploadUrlProvider instead for an endpoint that has to be minted per request."
        },
        new()
        {
            Name = "UploadUrlProvider",
            Type = "Func<Task<string?>>?",
            DefaultValue = "null",
            Description = "The provider function to create the URL of the server endpoint receiving the files. Unlike UploadUrl, it is invoked right before every single request - each file and each chunk - which is what lets it hand over a presigned URL that expires."
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the browse button, which decides how much of the Color it carries: a full fill, only an outline, or neither.",
            LinkType = LinkType.Link,
            Href = "#variant-enum"
        },
        new()
        {
            Name = "WithCredentials",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the upload request is sent with credentials such as cookies and authorization headers for cross-origin requests (the withCredentials flag of the underlying XMLHttpRequest).",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
        new()
        {
            Id = "file-info",
            Title = "BitFileInfo",
            Parameters =
            [
               new()
               {
                   Name = "ContentType",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The Content-Type of the selected file."
               },
               new()
               {
                   Name = "Name",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The name of the selected file."
               },
               new()
               {
                   Name = "Size",
                   Type = "long",
                   Description = "The size of the selected file."
               },
               new()
               {
                   Name = "FileId",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The file ID of the selected file, this is a GUID."
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   Description = "The index of the selected file."
               },
               new()
               {
                   Name = "LastModified",
                   Type = "long",
                   Description = "The last modified time of the file reported by the browser, in milliseconds since the Unix epoch."
               },
               new()
               {
                   Name = "LastModifiedDate",
                   Type = "DateTimeOffset",
                   Description = "The last modified time of the file reported by the browser, as a DateTimeOffset."
               },
               new()
               {
                   Name = "LastChunkUploadedSize",
                   Type = "long",
                   Description = "The size of the last uploaded chunk of the file."
               },
               new()
               {
                   Name = "TotalUploadedSize",
                   Type = "long",
                   Description = "The total uploaded size of the file."
               },
               new()
               {
                   Name = "PreviewUrl",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "An object URL of the file content that can be used as the source of an img element to preview image files. This is only populated for image files when the ShowPreview parameter of the BitFileUpload is enabled."
               },
               new()
               {
                   Name = "Width",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "The width of the image in pixels, only populated for decodable image files when the ReadImageDimensions parameter of the BitFileUpload is enabled. It is null for anything else."
               },
               new()
               {
                   Name = "Height",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "The height of the image in pixels, only populated for decodable image files when the ReadImageDimensions parameter of the BitFileUpload is enabled. It is null for anything else."
               },
               new()
               {
                   Name = "UploadSpeed",
                   Type = "double?",
                   DefaultValue = "null",
                   Description = "The observed speed of the upload of this file in bytes per second, measured over the request currently in flight. It is null while the file is not uploading and until the first progress report arrives."
               },
               new()
               {
                   Name = "RemainingTime",
                   Type = "TimeSpan?",
                   DefaultValue = "null",
                   Description = "The estimated time left before the upload of this file completes, derived from the UploadSpeed and the bytes still to be sent. It is null whenever the speed is unknown."
               },
               new()
               {
                   Name = "IsQueued",
                   Type = "bool",
                   DefaultValue = "false",
                   Description = "Whether the file is waiting in the upload queue for a free slot of the ConcurrentUploads limit, which is what tells a file that is about to start apart from one that was never asked to upload."
               },
               new()
               {
                   Name = "Message",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The message attached to the current Status of the file: the reason it was rejected by the validations before the upload, or the body of the server response of its upload or removal."
               },
               new()
               {
                   Name = "Status",
                   Type = "BitFileUploadStatus",
                   DefaultValue = "Pending",
                   Description = "The status of the file in the BitFileUpload.",
                   LinkType = LinkType.Link,
                   Href = "#upload-status-enum"
               },
               new()
               {
                   Name = "HttpHeaders",
                   Type = "Dictionary<string, string>?",
                   DefaultValue = "null",
                   Description = "Additional custom HTTP headers attached to the upload requests of this specific file (e.g., set from the OnUploading callback)."
               },
               new()
               {
                   Name = "FormFields",
                   Type = "Dictionary<string, string>?",
                   DefaultValue = "null",
                   Description = "Additional multipart form fields sent alongside the content of this specific file in its upload requests, merged over the ones of the UploadRequestFormFields parameter of the BitFileUpload. The natural place to fill it in is the OnUploading callback."
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitFileUploadClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitFileUpload."
               },
               new()
               {
                   Name = "Dragging",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element while files are being dragged over the BitFileUpload."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the browse button (label) of the BitFileUpload."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description (hint) of the BitFileUpload."
               },
               new()
               {
                   Name = "FileList",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file list container of the BitFileUpload."
               },
               new()
               {
                   Name = "FileItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "Preview",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image preview thumbnail of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "FileName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file name of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "FileSize",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file size of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "Percentage",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the upload percent indicator of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "ProgressBarContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the progress bar container of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "ProgressBar",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the progress bar of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "StatusMessage",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the status message of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "UploadButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the upload button of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "UploadIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the upload button icon of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "PauseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the pause button of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "PauseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the pause button icon of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "CancelButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the cancel button of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "CancelIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the cancel button icon of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "RemoveButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the remove button of each file item of the BitFileUpload."
               },
               new()
               {
                   Name = "RemoveIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the remove button icon of each file item of the BitFileUpload."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the general sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size file upload.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size file upload.", Value = "1" },
                new() { Name = "Large", Description = "The large size file upload.", Value = "2" }
            ]
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new() { Name = "Fill", Description = "Fill styled variant.", Value = "0" },
                new() { Name = "Outline", Description = "Outline styled variant.", Value = "1" },
                new() { Name = "Text", Description = "Text styled variant.", Value = "2" }
            ]
        },
        new()
        {
            Id = "upload-status-enum",
            Name = "BitFileUploadStatus",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Pending",
                    Description = "The file is selected and queued, and its uploading has not started yet.",
                    Value = "0",
                },
                new()
                {
                    Name = "InProgress",
                    Description = "File uploading is in progress.",
                    Value = "1",
                },
                new()
                {
                    Name = "Paused",
                    Description = "File uploading progress is paused by the user.",
                    Value = "2",
                },
                new()
                {
                    Name = "Canceled",
                    Description = "File uploading progress is canceled by the user.",
                    Value = "3",
                },
                new()
                {
                    Name = "Completed",
                    Description = "The file is successfully uploaded.",
                    Value = "4",
                },
                new()
                {
                    Name = "Failed",
                    Description = "The file has a problem and progress is failed.",
                    Value = "5",
                },
                new()
                {
                    Name = "Removed",
                    Description = "The uploaded file removed by the user.",
                    Value = "6",
                },
                new()
                {
                    Name = "RemoveFailed",
                    Description = "The file removal failed.",
                    Value = "7",
                },
                new()
                {
                    Name = "NotAllowed",
                    Description = "The file is rejected by the validations (size, count, type or a custom rule) and will not be uploaded.",
                    Value = "8",
                }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Files",
            Type = "IReadOnlyList<BitFileInfo>",
            DefaultValue = "[]",
            Description = "A list of all of the selected files to upload.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "UploadStatus",
            Type = "BitFileUploadStatus",
            DefaultValue = "Pending",
            Description = "The current status of the file uploader.",
            LinkType = LinkType.Link,
            Href = "#upload-status-enum"
        },
        new()
        {
            Name = "InputId",
            Type = "string?",
            DefaultValue = "",
            Description = "The id of the file input element.",
        },
        new()
        {
            Name = "IsRemoving",
            Type = "bool",
            DefaultValue = "false",
            Description = "Indicates that the file upload is in the middle of removing a file.",
        },
        new()
        {
            Name = "TotalSize",
            Type = "long",
            DefaultValue = "0",
            Description = "The total size in bytes of all the files of the batch, excluding the removed ones and the ones rejected by the validations.",
        },
        new()
        {
            Name = "TotalUploadedSize",
            Type = "long",
            DefaultValue = "0",
            Description = "The total uploaded size in bytes across all the files of the batch, excluding the removed ones and the ones rejected by the validations.",
        },
        new()
        {
            Name = "OverallUploadProgress",
            Type = "int",
            DefaultValue = "0",
            Description = "The overall upload progress of the batch as a percentage (0 to 100), combining the progress of all the files weighted by their size.",
        },
        new()
        {
            Name = "TotalUploadSpeed",
            Type = "double?",
            DefaultValue = "null",
            Description = "The combined speed in bytes per second of every file of the batch that is uploading right now, which is what the connection as a whole is carrying. It is null while nothing is on the wire.",
        },
        new()
        {
            Name = "OverallRemainingTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The estimated time left before the whole batch is uploaded, derived from the TotalUploadSpeed and the bytes of the batch that are still to be sent. It is null whenever nothing is uploading and the speed is therefore unknown.",
        },
        new()
        {
            Name = "Upload",
            Type = "(BitFileInfo? fileInfo = null, string? uploadUrl = null) => Task",
            DefaultValue = "",
            Description = "Starts uploading a specific file, or all files when no file is specified, resuming a paused or chunked file from the last chunk that made it through and retrying a failed or canceled one with a fresh budget of automatic retries. A file whose request is already on the wire is left running rather than being started over.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "PauseUpload",
            Type = "(BitFileInfo? fileInfo = null) => Task",
            DefaultValue = "",
            Description = "Pauses the upload of a specific file, or all files when no file is specified, applying to the files that are on their way: an in-progress file aborts its in-flight request and keeps the bytes that made it, and a file waiting in the concurrency queue is taken out of it. Both can be resumed later through the Upload method. A file that was never asked to upload, and one that has already settled, are left as they are.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "CancelUpload",
            Type = "(BitFileInfo? fileInfo = null) => Task",
            DefaultValue = "",
            Description = "Cancels the upload of a specific file, or all files when no file is specified, settling every file that is still in play - running, queued, paused or merely selected - as canceled right away and aborting the in-flight request of a running one. A file that has already settled is left alone, and a canceled file can be started again later.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "RemoveFile",
            Type = "(BitFileInfo? fileInfo = null) => Task",
            DefaultValue = "",
            Description = "Removes a specific file, or all files when no file is specified, deleting the (partially) uploaded ones from the server through the RemoveUrl.",
            LinkType = LinkType.Link,
            Href = "#file-info"
        },
        new()
        {
            Name = "Browse",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Opens a file selection dialog.",
        },
        new()
        {
            Name = "Reset",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Resets the file upload, clearing the file list and the upload state.",
        }
    ];



    [Inject] private IConfiguration _configuration { get; set; } = default!;

    private bool allowDrop = true;
    private bool allowPaste = true;
    private int tokenRequestCount;
    private BitVariant variant = BitVariant.Fill;
    private string onInvalidText = string.Empty;
    private string onAllUploadsCompleteText = "No File";
    private string UploadUrl => $"{_configuration.GetApiServerAddress()}FileUpload/UploadNonChunkedFile";
    private string ChunkedUploadUrl => $"{_configuration.GetApiServerAddress()}FileUpload/UploadChunkedFile";
    private string NonExistingUploadUrl => $"{_configuration.GetApiServerAddress()}FileUpload/MissingUploadEndpoint";
    private string RemoveUrl => $"{_configuration.GetApiServerAddress()}FileUpload/RemoveFile";

    private BitFileUpload bitFileUpload = default!;
    private BitFileUpload? overallFileUpload;
    private BitFileUpload? speedFileUpload;
    private BitFileUpload? hiddenViewFileUpload;

    // OnChange reports a single file when its status changes and the whole selection when files are picked,
    // so the summary is read back from the Files property instead of from the argument.
    private IEnumerable<BitFileInfo> HiddenViewFiles =>
        hiddenViewFileUpload?.Files.Where(f => f.Status != BitFileUploadStatus.Removed) ?? [];

    private IEnumerable<BitFileInfo> SpeedFiles =>
        speedFileUpload?.Files.Where(f => f.Status != BitFileUploadStatus.Removed) ?? [];

    private BitFileUpload bitFileUploadWithBrowseFile = default!;

    private bool FileUploadIsEmpty() => !bitFileUpload.Files.Any(f => f.Status != BitFileUploadStatus.Removed);

    private static string? ValidateEmptyFile(BitFileInfo file)
    {
        return file.Size == 0 ? "Empty files cannot be uploaded." : null;
    }

    private static string? ValidateImageDimensions(BitFileInfo file)
    {
        // an image the browser could not decode has no dimensions to judge, which is not the same
        // as failing the rule, so it is let through for the other validations to deal with.
        if (file.Width is null || file.Height is null) return null;

        if (file.Width < 200 || file.Height < 200) return "The image is smaller than 200x200 pixels.";

        if (file.Width > 4000 || file.Height > 4000) return "The image is larger than 4000x4000 pixels.";

        return null;
    }

    private Task<Dictionary<string, string>> GetFreshAuthHeaders()
    {
        // a provider is called once per request - per chunk in the chunked mode - which is what makes it
        // the right place for a token that would have gone stale by the time a long upload reaches its end.
        tokenRequestCount++;

        return Task.FromResult(new Dictionary<string, string> { { "Authorization", $"Bearer token-{tokenRequestCount}" } });
    }

    private static string? AnnounceUploads(IReadOnlyList<BitFileInfo> files)
    {
        var completed = files.Count(f => f.Status == BitFileUploadStatus.Completed);

        return $"{files.Count} attachment(s), {completed} uploaded so far.";
    }

    private async Task HandleUploadOnClick()
    {
        await bitFileUpload.Upload();
    }

    private static int GetFileUploadPercent(BitFileInfo file)
    {
        // an empty file has no byte whose progress could be measured, so it is either done or not started.
        if (file.Size == 0) return file.Status is BitFileUploadStatus.Completed ? 100 : 0;

        if (file.TotalUploadedSize >= file.Size) return 100;

        // the progress events count the bytes of the whole request body, multipart overhead included,
        // so the raw ratio can slightly overshoot and has to be capped.
        return Math.Min(100, (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100));
    }

    private static string GetFileUploadSize(BitFileInfo file)
    {
        long totalSize = file.Size / 1024;
        long uploadSize;
        if (file.TotalUploadedSize >= file.Size)
        {
            uploadSize = totalSize;
        }
        else
        {
            uploadSize = (file.TotalUploadedSize + file.LastChunkUploadedSize) / 1024;
        }

        return $"{uploadSize}KB / {totalSize}KB";
    }

    private string GetUploadMessageStr(BitFileInfo file) => file.Status switch
    {
        BitFileUploadStatus.Completed => bitFileUpload.SuccessfulUploadMessage,
        BitFileUploadStatus.Failed => bitFileUpload.FailedUploadMessage,
        BitFileUploadStatus.Canceled => bitFileUpload.CanceledUploadMessage,
        BitFileUploadStatus.RemoveFailed => bitFileUpload.FailedRemoveMessage,
        BitFileUploadStatus.NotAllowed => file.Message ?? bitFileUpload.NotAllowedExtensionErrorMessage,
        _ => string.Empty,
    };

    private async Task HandleBrowseFileOnClick()
    {
        await bitFileUploadWithBrowseFile.Browse();
    }



    private readonly string example1RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" />";
    private readonly string example1CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example2RazorCode = @"
<BitCheckbox @bind-Value=""allowDrop"" Label=""AllowDrop"" />
<BitCheckbox @bind-Value=""allowPaste"" Label=""AllowPaste"" />

<BitFileUpload Label=""Select, drop or paste files"" UploadUrl=""@UploadUrl"" AllowDrop=""allowDrop"" AllowPaste=""allowPaste"" />";
    private readonly string example2CsharpCode = @"
private bool allowDrop = true;
private bool allowPaste = true;
private string UploadUrl = ""/Upload"";";

    private readonly string example3RazorCode = @"
<BitFileUpload Label=""Browse for a document"" UploadUrl=""@UploadUrl"" Accept="".pdf,.docx"" MaxSize=""1024 * 1024 * 5""
               Description=""PDF or DOCX, up to 5 MB."" />

<BitFileUpload Label=""Browse for an image"" UploadUrl=""@UploadUrl"" Accept=""image/*"" MaxSize=""1024 * 1024 * 2"">
    <DescriptionTemplate>
        <i class=""bit-icon bit-icon--Info"" />
        <span>Images only. Up to <b>2 MB</b>.</span>
    </DescriptionTemplate>
</BitFileUpload>";
    private readonly string example3CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example4RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple />";
    private readonly string example4CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example5RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" AutoUpload />";
    private readonly string example5CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example6RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" AutoReset />";
    private readonly string example6CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example7RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Append />";
    private readonly string example7CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example8RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple Append
               AllowDuplicates=""false"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" />";
    private readonly string example8CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example9RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" MaxSize=""1024 * 1024 * 1"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" MinSize=""1024"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple Append
               MaxTotalSize=""1024 * 1024 * 2"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" MaxSize=""1024 * 1024 * 1""
               MaxSizeErrorMessage=""The file is too big! Please select a file smaller than 1 MB."" />";
    private readonly string example9CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example10RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Accept="".png,.jpg"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               AllowedExtensions=""@(new List<string> { "".gif"","".jpg"","".mp4"" })"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               AllowedExtensions=""@(new List<string> { ""image/*"", ""application/pdf"" })"" />";
    private readonly string example10CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example11RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple Append MaxCount=""3""
               ShowRemoveButton RemoveUrl=""@RemoveUrl"" />";
    private readonly string example11CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example12RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple FileValidator=""@ValidateEmptyFile"" />";
    private readonly string example12CsharpCode = @"
private string UploadUrl = ""/Upload"";

private static string? ValidateEmptyFile(BitFileInfo file)
{
    return file.Size == 0 ? ""Empty files cannot be uploaded."" : null;
}";

    private readonly string example13RazorCode = @"
<BitFileUpload Label=""Select or drag and drop a folder"" UploadUrl=""@UploadUrl"" Directory Multiple
               ConcurrentUploads=""3"" MaxCount=""20"" />";
    private readonly string example13CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example14RazorCode = @"
<BitFileUpload Label=""Take a photo"" UploadUrl=""@UploadUrl"" Accept=""image/*"" Capture=""environment"" AutoUpload
               Description=""Opens the rear camera on a mobile device."" />

<BitFileUpload Label=""Record a video"" UploadUrl=""@UploadUrl"" Accept=""video/*"" Capture=""user""
               Description=""Opens the front camera on a mobile device."" />";
    private readonly string example14CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example15RazorCode = @"
<BitFileUpload Label=""Select or drag and drop images"" UploadUrl=""@UploadUrl"" Multiple ShowPreview
               Accept=""image/*"" ShowRemoveButton RemoveUrl=""@RemoveUrl""
               Description=""The selected images are previewed right in the list."" />";
    private readonly string example15CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example16RazorCode = @"
<style>
    .dimensions-item {
        display: flex;
        gap: 1rem;
        justify-content: space-between;
        padding: 0.25rem 0;
    }
</style>


<BitFileUpload Label=""Select or drag and drop images"" UploadUrl=""@UploadUrl"" Multiple ShowPreview
               Accept=""image/*"" ReadImageDimensions FileValidator=""@ValidateImageDimensions""
               Description=""Images between 200x200 and 4000x4000 pixels."" />

<BitFileUpload Label=""Select or drag and drop images"" UploadUrl=""@UploadUrl"" Multiple
               Accept=""image/*"" ReadImageDimensions>
    <FileViewTemplate Context=""file"">
        <div class=""dimensions-item"">
            <span>@file.Name</span>
            <span>@(file.Width is null ? ""unknown size"" : $""{file.Width} x {file.Height}"")</span>
        </div>
    </FileViewTemplate>
</BitFileUpload>";
    private readonly string example16CsharpCode = @"
private string UploadUrl = ""/Upload"";

private static string? ValidateImageDimensions(BitFileInfo file)
{
    // an image the browser could not decode has no dimensions to judge, which is not the same
    // as failing the rule, so it is let through for the other validations to deal with.
    if (file.Width is null || file.Height is null) return null;

    if (file.Width < 200 || file.Height < 200) return ""The image is smaller than 200x200 pixels."";

    if (file.Width > 4000 || file.Height > 4000) return ""The image is larger than 4000x4000 pixels."";

    return null;
}";

    private readonly string example17RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               ShowRemoveButton RemoveUrl=""@RemoveUrl"" />";
    private readonly string example17CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example18RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple MaxSize=""1024 * 1024 * 1""
               OnAllUploadsComplete=""@(() => onAllUploadsCompleteText = ""All files are uploaded"")""
               OnInvalid=""@(files => onInvalidText = $""{files.Length} file(s) rejected: {string.Join("", "", files.Select(f => f.Name))}"")""
               OnUploading=""@(info => info.HttpHeaders = new Dictionary<string, string> { {""key1"", ""value1""} })"" />

<div>@onAllUploadsCompleteText</div>
<div>@onInvalidText</div>";
    private readonly string example18CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string onInvalidText = string.Empty;
private string onAllUploadsCompleteText = ""No File"";";

    private readonly string example19RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" RemoveUrl=""@RemoveUrl""
               UploadRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs1"", ""qsValue1"" } })""
               UploadRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header1"", ""value1"" } })""
               UploadRequestFormFields=""@(new Dictionary<string, string>{ {""folder"", ""invoices"" } })""
               RemoveRequestQueryStrings=""@(new Dictionary<string, string>{ {""qs2"", ""qsValue2"" } })""
               RemoveRequestHttpHeaders=""@(new Dictionary<string, string>{ {""header2"", ""value2"" } })"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple ChunkedUpload
               UploadRequestHttpHeadersProvider=""@GetFreshAuthHeaders"" />

<div>Requests so far: @tokenRequestCount</div>

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" RemoveUrl=""@RemoveUrl""
               UploadRequestHttpMethod=""POST""
               UploadFormFieldName=""file""
               RemoveRequestHttpMethod=""DELETE""
               ShowRemoveButton
               WithCredentials
               UploadTimeout=""TimeSpan.FromMinutes(10)"" />";
    private readonly string example19CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";
private int tokenRequestCount;

private Task<Dictionary<string, string>> GetFreshAuthHeaders()
{
    // a provider is called once per request - per chunk in the chunked mode - which is what makes it
    // the right place for a token that would have gone stale by the time a long upload reaches its end.
    tokenRequestCount++;

    return Task.FromResult(new Dictionary<string, string> { { ""Authorization"", $""Bearer token-{tokenRequestCount}"" } });
}";

    private readonly string example20RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@ChunkedUploadUrl"" ChunkedUpload />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@ChunkedUploadUrl"" ChunkedUpload AutoChunkSize />";
    private readonly string example20CsharpCode = @"
private string ChunkedUploadUrl = ""/ChunkedUpload"";";

    private readonly string example21RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               AutoRetries=""2"" AutoRetryDelay=""TimeSpan.FromSeconds(1)"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@NonExistingUploadUrl""
               AutoRetries=""2"" AutoRetryDelay=""TimeSpan.FromSeconds(1)""
               RetryButtonTitle=""Try again"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@NonExistingUploadUrl""
               AutoRetries=""2"" AutoRetryDelay=""TimeSpan.FromSeconds(1)""
               ShouldAutoRetry=""@((file, status) => true)""
               RetryButtonTitle=""Try again"" />";
    private readonly string example21CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string NonExistingUploadUrl = ""/MissingUploadEndpoint"";";

    private readonly string example22RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple ConcurrentUploads=""2"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple AutoUpload
               ConcurrentUploads=""1"" QueuedUploadMessage=""In the queue…"" />";
    private readonly string example22CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example23RazorCode = @"
<BitFileUpload @ref=""overallFileUpload"" Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple
               OnChange=""@(_ => StateHasChanged())"" OnProgress=""@(_ => StateHasChanged())"" />

<BitProgress Percent=""@(overallFileUpload?.OverallUploadProgress ?? 0)"" ShowPercentNumber />";
    private readonly string example23CsharpCode = @"
private string UploadUrl = ""/Upload"";
private BitFileUpload? overallFileUpload;";

    private readonly string example24RazorCode = @"
<BitFileUpload @ref=""speedFileUpload"" Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple
               OnChange=""@(_ => StateHasChanged())"" OnProgress=""@(_ => StateHasChanged())"" />

@foreach (var file in SpeedFiles)
{
    <div>
        @file.Name -
        @(file.UploadSpeed is null ? ""-"" : $""{file.UploadSpeed / 1024:N0} KB/s"")
        (@(file.RemainingTime is null ? ""-"" : $""{file.RemainingTime:mm\\:ss} left""))
    </div>
}

<div>
    <b>
        Batch: @(speedFileUpload?.TotalUploadSpeed is null ? ""idle"" : $""{speedFileUpload.TotalUploadSpeed / 1024:N0} KB/s"")
        (@(speedFileUpload?.OverallRemainingTime is null ? ""-"" : $""{speedFileUpload.OverallRemainingTime:mm\\:ss} left""))
    </b>
</div>";
    private readonly string example24CsharpCode = @"
private string UploadUrl = ""/Upload"";
private BitFileUpload? speedFileUpload;

private IEnumerable<BitFileInfo> SpeedFiles =>
    speedFileUpload?.Files.Where(f => f.Status != BitFileUploadStatus.Removed) ?? [];";

    private readonly string example25RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple
               FileSizeFormatter=""@(size => $""{size:N0} bytes"")"" />";
    private readonly string example25CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example26RazorCode = @"
<BitFileUpload @ref=""hiddenViewFileUpload"" Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               Multiple AutoUpload HideFileView
               OnChange=""@(_ => StateHasChanged())"" OnProgress=""@(_ => StateHasChanged())"" />

@if (HiddenViewFiles.Any())
{
    <ul>
        @foreach (var file in HiddenViewFiles)
        {
            <li>@file.Name - @file.Status</li>
        }
    </ul>
    <div>Overall progress: @(hiddenViewFileUpload?.OverallUploadProgress ?? 0)%</div>
}
else
{
    <div>No file selected yet.</div>
}";
    private readonly string example26CsharpCode = @"
private string UploadUrl = ""/Upload"";
private BitFileUpload? hiddenViewFileUpload;

// OnChange reports a single file when its status changes and the whole selection when files are picked,
// so the summary is read back from the Files property instead of from the argument.
private IEnumerable<BitFileInfo> HiddenViewFiles =>
    hiddenViewFileUpload?.Files.Where(f => f.Status != BitFileUploadStatus.Removed) ?? [];";

    private readonly string example27RazorCode = @"
<style>
    .browse-file {
        border: 1px solid #D2D2D7;
        border-radius: 2px;
        padding: 24px;
        width: 420px;
        height: 200px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 50px;
        cursor: pointer;
        color: inherit;
        font: inherit;
        background-color: transparent;
    }

    .browse-file-header {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        font-size: 16px;
    }

    .browse-file-header i {
        font-size: 24px;
        font-weight: 700;
        color: #0072CE;
    }

    .browse-file-header strong {
        color: #0072CE;
    }

    .browse-file-footer {
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        font-size: 12px;
        color: #78787D;
    }

    .custom-drop-zone .browse-file {
        border-style: dashed;
        border-color: #0072CE;
        background-color: #eaf4fd;
    }

    .file-list {
        border: 1px solid #D2D2D7;
        border-radius: 2px;
        padding: 24px;
        width: 420px;
        height: 200px;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
    }

    .file-info {
        display: flex;
        justify-content: space-between;
    }

    .file-info-name {
        overflow: hidden;
        margin-right: 10px;
    }

    .file-info-title {
        color: #5A5A5F;
        line-height: 22px;
        display: flex;
        justify-content: space-between;
    }

    .file-info-subtitle {
        color: #909096;
    }

    .file-info-ico {
        border: 1px solid #F3F3F8;
        border-radius: 2px;
        background-color: #F3F3F8;
        width: 80px;
        height: 80px;
        display: flex;
        justify-content: center;
        align-items: center;
    }

    .file-info-ico i {
        font-size: 24px;
    }

    .file-info-data {
        width: 275px;
    }

    .file-info-btns {
        display: flex;
        justify-content: space-between;
        gap: 8px;
    }

    .file-info-btns button {
        border: 0;
        padding: 0;
        display: flex;
        cursor: pointer;
        background-color: transparent;
    }

    .file-info-btns i {
        display: block;
        cursor: pointer;
    }

    .file-info-btns .upload-ico {
        color: #0072CE;
    }

    .file-info-btns .remove-ico {
        color: #F9423A;
    }

    .file-info-progressbar-container {
        width: 100%;
        overflow: hidden;
        height: 2px;
        margin-top: 24px;
        background-color: #D9D9D9;
    }

    .file-info-progressbar {
        height: 2px;
        transition: width 0.15s linear 0s;
        background-color: #0072CE;
    }

    .file-info-s-msg {
        margin-top: 12px;
        color: #5EB227;
    }

    .file-info-e-msg {
        margin-top: 12px;
        color: #F9423A;
    }

    .file-list-footer {
        font-size: 12px;
        color: #78787D;
    }
</style>


<BitFileUpload @ref=""bitFileUpload"" UploadUrl=""@UploadUrl"" RemoveUrl=""@RemoveUrl""
               Classes=""@(new() { Dragging = ""custom-drop-zone"" })"">
    <LabelTemplate>
        @if (FileUploadIsEmpty())
        {
            <button type=""button"" class=""browse-file"" @onclick=""() => bitFileUpload.Browse()"">
                <div class=""browse-file-header"">
                    <i class=""bit-icon bit-icon--CloudUpload"" />
                    <div>
                        Drag and drop or
                    </div>
                    <div>
                        <strong>
                            Browse file
                        </strong>
                    </div>
                </div>

                <div class=""browse-file-footer"">
                    <div>
                        Max file size: 2 MB
                    </div>
                    <div>
                        Supported file types: jpg, jpeg, png, bmp
                    </div>
                </div>
            </button>
        }
    </LabelTemplate>
    <FileViewTemplate Context=""file"">
        @if (file.Status != BitFileUploadStatus.Removed)
        {
            <div class=""file-list"">
                <div class=""file-info"">
                    <div class=""file-info-ico"">
                        <i class=""bit-icon bit-icon--FileImage"" />
                    </div>
                    <div class=""file-info-data"">
                        <div class=""file-info-title"">
                            <div class=""file-info-name"">@file.Name</div>
                            <div class=""file-info-btns"">
                                <button type=""button"" aria-label=""@($""Upload {file.Name}"")"" @onclick=""() => bitFileUpload.Upload(file)"">
                                    <i class=""bit-icon bit-icon--CloudUpload upload-ico"" />
                                </button>
                                <button type=""button"" aria-label=""@($""Remove {file.Name}"")"" @onclick=""() => bitFileUpload.RemoveFile(file)"">
                                    <i class=""bit-icon bit-icon--ChromeClose remove-ico"" />
                                </button>
                            </div>
                        </div>
                        @if (file.Status is BitFileUploadStatus.InProgress or BitFileUploadStatus.Pending)
                        {
                            var fileUploadPercent = GetFileUploadPercent(file);
                            <div class=""file-info-subtitle"">@GetFileUploadSize(file) - @fileUploadPercent%</div>
                            <div class=""file-info-progressbar-container"">
                                <div class=""file-info-progressbar"" role=""progressbar"" style=""width:@fileUploadPercent%;"" aria-valuemin=""0"" aria-valuemax=""100"" aria-valuenow=""@fileUploadPercent""></div>
                            </div>
                        }
                        else
                        {
                            <div class=""@(file.Status == BitFileUploadStatus.Completed ? ""file-info-s-msg"" : ""file-info-e-msg"")"">@GetUploadMessageStr(file)</div>
                        }
                    </div>
                </div>

                <div class=""file-list-footer"">
                    <div>
                        Max file size: 2 MB
                    </div>
                    <div>
                        Supported file types: jpg, jpeg, png, bmp
                    </div>
                </div>
            </div>
        }
    </FileViewTemplate>
</BitFileUpload>

<BitButton OnClick=""HandleUploadOnClick"">Upload</BitButton>";
    private readonly string example27CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";

private BitFileUpload bitFileUpload = default!;

private bool FileUploadIsEmpty() => !bitFileUpload.Files.Any(f => f.Status != BitFileUploadStatus.Removed);

private async Task HandleUploadOnClick()
{
    await bitFileUpload.Upload();
}

private static int GetFileUploadPercent(BitFileInfo file)
{
    // an empty file has no byte whose progress could be measured, so it is either done or not started.
    if (file.Size == 0) return file.Status is BitFileUploadStatus.Completed ? 100 : 0;

    if (file.TotalUploadedSize >= file.Size) return 100;

    // the progress events count the bytes of the whole request body, multipart overhead included,
    // so the raw ratio can slightly overshoot and has to be capped.
    return Math.Min(100, (int)((file.TotalUploadedSize + file.LastChunkUploadedSize) / (float)file.Size * 100));
}

private static string GetFileUploadSize(BitFileInfo file)
{
    long totalSize = file.Size / 1024;
    long uploadSize;
    if (file.TotalUploadedSize >= file.Size)
    {
        uploadSize = totalSize;
    }
    else
    {
        uploadSize = (file.TotalUploadedSize + file.LastChunkUploadedSize) / 1024;
    }

    return $""{uploadSize}KB / {totalSize}KB"";
}

private string GetUploadMessageStr(BitFileInfo file) => file.Status switch
{
    BitFileUploadStatus.Completed => bitFileUpload.SuccessfulUploadMessage,
    BitFileUploadStatus.Failed => bitFileUpload.FailedUploadMessage,
    BitFileUploadStatus.Canceled => bitFileUpload.CanceledUploadMessage,
    BitFileUploadStatus.RemoveFailed => bitFileUpload.FailedRemoveMessage,
    BitFileUploadStatus.NotAllowed => file.Message ?? bitFileUpload.NotAllowedExtensionErrorMessage,
    _ => string.Empty,
};";

    private readonly string example28RazorCode = @"
<BitFileUpload @ref=""bitFileUploadWithBrowseFile"" HideLabel Multiple
               UploadUrl=""@UploadUrl""
               RemoveUrl=""@RemoveUrl"" />

<BitButton OnClick=""HandleBrowseFileOnClick"">Browse files</BitButton>
<BitButton OnClick=""() => bitFileUploadWithBrowseFile.Upload()"">Upload all</BitButton>
<BitButton OnClick=""() => bitFileUploadWithBrowseFile.PauseUpload()"">Pause all</BitButton>
<BitButton OnClick=""() => bitFileUploadWithBrowseFile.CancelUpload()"">Cancel all</BitButton>
<BitButton OnClick=""() => bitFileUploadWithBrowseFile.Reset()"">Reset</BitButton>";
    private readonly string example28CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";
private BitFileUpload bitFileUploadWithBrowseFile = default!;

private async Task HandleBrowseFileOnClick()
{
    await bitFileUploadWithBrowseFile.Browse();
}";

    private readonly string example29RazorCode = @"
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl""
               AriaLabel=""Select a document to upload"" />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Multiple
               AnnouncementProvider=""@AnnounceUploads"" />";
    private readonly string example29CsharpCode = @"
private string UploadUrl = ""/Upload"";

private static string? AnnounceUploads(IReadOnlyList<BitFileInfo> files)
{
    var completed = files.Count(f => f.Status == BitFileUploadStatus.Completed);

    return $""{files.Count} attachment(s), {completed} uploaded so far."";
}";

    private readonly string example30RazorCode = @"
<BitChoiceGroup @bind-Value=""variant"" Horizontal TItem=""BitChoiceGroupOption<BitVariant>"" TValue=""BitVariant"">
    <BitChoiceGroupOption Text=""Fill"" Value=""BitVariant.Fill"" />
    <BitChoiceGroupOption Text=""Outline"" Value=""BitVariant.Outline"" />
    <BitChoiceGroupOption Text=""Text"" Value=""BitVariant.Text"" />
</BitChoiceGroup>

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" Variant=""variant"" />

<BitFileUpload Label=""Disabled"" UploadUrl=""@UploadUrl"" Variant=""variant"" IsEnabled=""false"" />";
    private readonly string example30CsharpCode = @"
private string UploadUrl = ""/Upload"";
private BitVariant variant = BitVariant.Fill;";

    private readonly string example31RazorCode = @"
<BitFileUpload Label=""Primary"" UploadUrl=""@UploadUrl"" Color=""BitColor.Primary"" />
<BitFileUpload Label=""Secondary"" UploadUrl=""@UploadUrl"" Color=""BitColor.Secondary"" />
<BitFileUpload Label=""Tertiary"" UploadUrl=""@UploadUrl"" Color=""BitColor.Tertiary"" />
<BitFileUpload Label=""Info"" UploadUrl=""@UploadUrl"" Color=""BitColor.Info"" />
<BitFileUpload Label=""Success"" UploadUrl=""@UploadUrl"" Color=""BitColor.Success"" />
<BitFileUpload Label=""Warning"" UploadUrl=""@UploadUrl"" Color=""BitColor.Warning"" />
<BitFileUpload Label=""SevereWarning"" UploadUrl=""@UploadUrl"" Color=""BitColor.SevereWarning"" />
<BitFileUpload Label=""Error"" UploadUrl=""@UploadUrl"" Color=""BitColor.Error"" />

<BitFileUpload Label=""PrimaryBackground"" UploadUrl=""@UploadUrl"" Color=""BitColor.PrimaryBackground"" />
<BitFileUpload Label=""SecondaryBackground"" UploadUrl=""@UploadUrl"" Color=""BitColor.SecondaryBackground"" />
<BitFileUpload Label=""TertiaryBackground"" UploadUrl=""@UploadUrl"" Color=""BitColor.TertiaryBackground"" />
<BitFileUpload Label=""PrimaryForeground"" UploadUrl=""@UploadUrl"" Color=""BitColor.PrimaryForeground"" />
<BitFileUpload Label=""SecondaryForeground"" UploadUrl=""@UploadUrl"" Color=""BitColor.SecondaryForeground"" />
<BitFileUpload Label=""TertiaryForeground"" UploadUrl=""@UploadUrl"" Color=""BitColor.TertiaryForeground"" />
<BitFileUpload Label=""PrimaryBorder"" UploadUrl=""@UploadUrl"" Color=""BitColor.PrimaryBorder"" />
<BitFileUpload Label=""SecondaryBorder"" UploadUrl=""@UploadUrl"" Color=""BitColor.SecondaryBorder"" />
<BitFileUpload Label=""TertiaryBorder"" UploadUrl=""@UploadUrl"" Color=""BitColor.TertiaryBorder"" />";
    private readonly string example31CsharpCode = @"
private string UploadUrl = ""/Upload"";";

    private readonly string example32RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<div>FontAwesome:</div>
<br />
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl""
               UploadIcon=""@(""fa-solid fa-upload"")""
               PauseIcon=""@(""fa-solid fa-pause"")""
               RetryIcon=""@(""fa-solid fa-rotate-right"")""
               CancelIcon=""@(""fa-solid fa-xmark"")""
               RemoveIcon=""@(""fa-solid fa-trash"")"" />

<br /><br />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl""
               UploadIcon=""@BitIconInfo.Fa(""solid cloud-arrow-up"")""
               PauseIcon=""@BitIconInfo.Fa(""solid circle-pause"")""
               RetryIcon=""@BitIconInfo.Fa(""solid arrow-rotate-right"")""
               CancelIcon=""@BitIconInfo.Fa(""solid circle-xmark"")""
               RemoveIcon=""@BitIconInfo.Fa(""solid trash-can"")"" />

<br /><br /><br />

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<div>Bootstrap:</div>
<br />
<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl""
               UploadIcon=""@(""bi bi-cloud-upload"")""
               PauseIcon=""@(""bi bi-pause-circle"")""
               RetryIcon=""@(""bi bi-arrow-clockwise"")""
               CancelIcon=""@(""bi bi-x-circle"")""
               RemoveIcon=""@(""bi bi-trash"")"" />

<br /><br />

<BitFileUpload Label=""Select or drag and drop files"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl""
               UploadIcon=""@BitIconInfo.Bi(""cloud-arrow-up"")""
               PauseIcon=""@BitIconInfo.Bi(""pause-circle"")""
               RetryIcon=""@BitIconInfo.Bi(""arrow-repeat"")""
               CancelIcon=""@BitIconInfo.Bi(""x-circle"")""
               RemoveIcon=""@BitIconInfo.Bi(""trash"")"" />";
    private readonly string example32CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example33RazorCode = @"
<BitFileUpload Label=""Small"" UploadUrl=""@UploadUrl"" Size=""BitSize.Small"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" />

<BitFileUpload Label=""Medium"" UploadUrl=""@UploadUrl"" Size=""BitSize.Medium"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" />

<BitFileUpload Label=""Large"" UploadUrl=""@UploadUrl"" Size=""BitSize.Large"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" />";
    private readonly string example33CsharpCode = @"
private string UploadUrl = ""/Upload"";
private string RemoveUrl = ""/Remove"";";

    private readonly string example34RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.25rem;
        border: 2px dashed mediumseagreen;
    }

    .custom-label {
        color: white;
        border-color: mediumseagreen;
        background-color: mediumseagreen;
    }

    .custom-dragging {
        background-color: honeydew;
    }

    .custom-item {
        border-color: mediumseagreen;
    }

    .custom-remove {
        color: white;
        background-color: mediumseagreen;
    }
</style>


<BitFileUpload Label=""Styled file upload"" UploadUrl=""@UploadUrl""
               Style=""box-shadow: dodgerblue 0 0 1rem; border-radius: 1rem; padding: 0.5rem;"" />

<BitFileUpload Label=""Classed file upload"" UploadUrl=""@UploadUrl"" Class=""custom-class"" />


<BitFileUpload Label=""Styles"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" Styles=""@(new()
               {
                   Label = ""border-color: deeppink; background-color: deeppink; color: white;"",
                   Dragging = ""background-color: lavenderblush;"",
                   FileName = ""color: deeppink;"",
                   ProgressBar = ""background-color: deeppink;""
               })"" />

<BitFileUpload Label=""Classes"" UploadUrl=""@UploadUrl"" ShowRemoveButton RemoveUrl=""@RemoveUrl"" Classes=""@(new()
               {
                   Label = ""custom-label"",
                   Dragging = ""custom-dragging"",
                   FileItem = ""custom-item"",
                   RemoveButton = ""custom-remove""
               })"" />";

    private readonly string example35RazorCode = @"
<div dir=""rtl"">
    <BitFileUpload Dir=""BitDir.Rtl""
                   Label=""انتخاب یا رها کردن فایل""
                   UploadUrl=""@UploadUrl""
                   ShowRemoveButton RemoveUrl=""@RemoveUrl""
                   UploadButtonTitle=""بارگذاری""
                   PauseButtonTitle=""توقف""
                   CancelButtonTitle=""لغو""
                   RemoveButtonTitle=""حذف""
                   SuccessfulUploadMessage=""بارگذاری فایل موفق بود""
                   FailedUploadMessage=""بارگذاری فایل شکست خورد"" />
</div>";
}
