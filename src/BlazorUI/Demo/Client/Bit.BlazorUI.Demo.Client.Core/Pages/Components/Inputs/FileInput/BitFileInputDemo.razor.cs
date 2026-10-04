namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.FileInput;

public partial class BitFileInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accept",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accepted file types for the file browser using MIME types or file extensions (e.g., \"image/*\", \".pdf,.doc\"). Applied to the underlying HTML input element's accept attribute. When not set, the accept attribute is generated from AllowedExtensions.",
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
            Description = "Whether a file that is already in the file list can be selected again. When disabled, a newly selected file matching an existing one by folder, name, size and last modified time is marked as invalid with the DuplicateErrorMessage instead of being added as a second entry, becoming valid again once the file it duplicates is removed.",
        },
        new()
        {
            Name = "AllowedExtensions",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "[\"*\"]",
            Description = "Allowed file types for validation purposes, accepting both file extensions (e.g., [\".jpg\", \".png\", \".pdf\"]) and MIME types with an optional wildcard (e.g., [\"image/*\", \"application/pdf\"]). The leading dot of an extension is optional and the matching is case-insensitive. Use [\"*\"] to allow all file types. Files not matching any of these entries will be marked as invalid.",
        },
        new()
        {
            Name = "AllowPaste",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether files can be selected by pasting them from the clipboard onto the component. The paste is only captured while the focus is inside the component, so the browse button must be focused first.",
        },
        new()
        {
            Name = "AnnouncementProvider",
            Type = "Func<IReadOnlyList<BitFileInputInfo>, string?>?",
            DefaultValue = "null",
            Description = "Custom provider of the text announced by the screen reader through the live region of the component whenever the file list changes. Receives the current file list and returns the text to announce, or null to announce nothing. When not set, a built-in English announcement is used.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "Append",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to append newly selected files to the existing file list instead of replacing it."
        },
        new()
        {
            Name = "AutoReset",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the file input is automatically reset (cleared) before opening the file browser dialog, allowing the same file to be selected multiple times consecutively."
        },
        new()
        {
            Name = "Capture",
            Type = "string?",
            DefaultValue = "null",
            Description = "The capture behavior of the file input on devices with a camera or microphone, rendered as the capture attribute of the input element (e.g., \"user\" for the front camera, \"environment\" for the rear camera)."
        },
        new()
        {
            Name = "Classes",
            Type = "BitFileInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitFileInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the file input, applied to the browse button and the drag-and-drop indicator.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short hint rendered under the browse button and wired to it through aria-describedby, which is the place to spell out the accepted file types and the size limits so that both sighted and screen reader users learn the constraints before hitting them."
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Razor template of the hint rendered under the browse button, taking precedence over Description."
        },
        new()
        {
            Name = "Directory",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to select folders (directories) instead of files, rendered as the webkitdirectory attribute. All files inside the selected folder and its subfolders will be added to the file list. It also makes a dropped folder expand into its contents instead of being ignored."
        },
        new()
        {
            Name = "DropZoneIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The glyph of the drop zone panel, using custom CSS classes for external icon libraries. Takes precedence over DropZoneIconName when both are set, and is only rendered while ShowDropZone is enabled.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "DropZoneIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the drop zone panel's glyph from the built-in Fluent UI icons. Defaults to \"CloudUpload\", and an empty string leaves the panel without a glyph at all."
        },
        new()
        {
            Name = "DuplicateErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file is selected again while AllowDuplicates is disabled. Defaults to \"The file is already selected\"."
        },
        new()
        {
            Name = "FileIconSelector",
            Type = "Func<BitFileInputInfo, BitIconInfo?>?",
            DefaultValue = "null",
            Description = "Custom provider of the glyph shown in the thumbnail's place for a file that has no image preview, which is rendered while ShowPreview is enabled. Receives the file and returns the icon to draw, or null to leave that file without one. When not set, the icon is picked from the file's MIME type and extension.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info"
        },
        new()
        {
            Name = "FileListAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the file list, which tells a screen reader user walking the lists of the page what this one holds. Defaults to \"Selected files\"."
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
            Type = "Func<BitFileInputInfo, string?>?",
            DefaultValue = "null",
            Description = "Custom validation function called for each newly selected file after the built-in validations pass. Return an error message to mark the file as invalid, or null to accept it.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "FileViewTemplate",
            Type = "RenderFragment<BitFileInputInfo>?",
            DefaultValue = "null",
            Description = "Custom Razor template for rendering individual file items in the file list. Receives a BitFileInputInfo context for each file.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "HideFileList",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to hide the file list that displays the selected files in the UI."
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
            Description = "The text displayed on the browse button. Defaults to \"Browse\"."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom Razor template for the browse button area, allowing full customization of the file selection trigger UI."
        },
        new()
        {
            Name = "MaxCount",
            Type = "int",
            DefaultValue = "0",
            Description = "Maximum allowed number of files in the file list. Files selected beyond this count will be marked as invalid, becoming valid again once removals free up room. Set to 0 for no count limit."
        },
        new()
        {
            Name = "MaxCountErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when the number of files exceeds the maximum count limit. Defaults to \"The maximum number of files is exceeded\"."
        },
        new()
        {
            Name = "MaxSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Maximum allowed file size in bytes for validation. Files exceeding this size will be marked as invalid. Set to 0 for no size limit."
        },
        new()
        {
            Name = "MaxSizeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file exceeds the maximum size limit. Defaults to \"The file size is larger than the max size\"."
        },
        new()
        {
            Name = "MaxTotalSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Maximum allowed total size in bytes of all the files in the file list. Files pushing the accumulated size beyond this limit will be marked as invalid, becoming valid again once removals free up room. Set to 0 for no total size limit."
        },
        new()
        {
            Name = "MaxTotalSizeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file makes the total size of the file list exceed the maximum total size. Defaults to \"The total size of the files is larger than the max total size\"."
        },
        new()
        {
            Name = "MinSize",
            Type = "long",
            DefaultValue = "0",
            Description = "Minimum allowed file size in bytes for validation. Files smaller than this size will be marked as invalid. Set to 0 for no size limit."
        },
        new()
        {
            Name = "MinSizeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file is smaller than the minimum size limit. Defaults to \"The file size is smaller than the min size\"."
        },
        new()
        {
            Name = "Multiple",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to allow selecting multiple files simultaneously through the file browser dialog."
        },
        new()
        {
            Name = "NotAllowedExtensionErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file's extension is not in the allowed extensions list. Defaults to \"The file type is not allowed\"."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback invoked when the file selection changes, providing an array of BitFileInputInfo representing all selected files. It is also invoked after removing a file through the remove button or the RemoveFile method.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "OnInvalid",
            Type = "EventCallback<BitFileInputInfo[]>",
            Description = "Callback invoked right after OnChange whenever the file list holds at least one invalid file, providing an array of only the invalid files along with their validation messages.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "OnRemove",
            Type = "EventCallback<BitFileInputInfo>",
            Description = "Callback invoked for each file that gets removed from the file list, either through the remove button or the RemoveFile method.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "ReadImageDimensions",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to decode every selected image file to fill the Width and Height properties of its file info with the pixel dimensions, which makes it possible to enforce resolution rules from a FileValidator. Decoding costs time and memory proportional to the images, so it is disabled by default.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "RemoveButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the remove button icon using custom CSS classes for external icon libraries. Takes precedence over RemoveButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "RemoveButtonIconName",
            Type = "string?",
            DefaultValue = "Delete",
            Description = "Gets or sets the name of the remove button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "RemoveButtonTitle",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the remove button, which is also used as the prefix of its accessible label (e.g., \"Remove report.pdf\"). Defaults to \"Remove\"."
        },
        new()
        {
            Name = "ShowDropZone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to render the browse area as a full width drop zone panel - a dashed rule around a glyph and the label - instead of an ordinary button. It is the same button underneath, so it is still reached with Tab and activated with Enter or Space, and it carries the drag indicator exactly as the button does. It also makes Outline the default variant; set Variant to take that back."
        },
        new()
        {
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to display a preview thumbnail for image files in the file list, and a file type glyph in the same place for every file that has no preview."
        },
        new()
        {
            Name = "ShowRemoveButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to display a remove button next to each file in the file list, allowing individual file removal."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the file input, applied to the browse button and the file list items.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitFileInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitFileInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip of the browse button, rendered as its title attribute."
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
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Files",
            Type = "IReadOnlyList<BitFileInputInfo>",
            DefaultValue = "[]",
            Description = "A read-only list of all currently selected files with their metadata, validation status, and content.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "InputId",
            Type = "string?",
            DefaultValue = "",
            Description = "The unique identifier of the underlying HTML file input element.",
        },
        new()
        {
            Name = "Browse",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Opens the file browser dialog programmatically, allowing users to select files. If AutoReset is enabled, the input is reset before opening.",
        },
        new()
        {
            Name = "ReadContentAsync",
            Type = "(BitFileInputInfo? fileInfo = null, CancellationToken cancellationToken = default) => Task",
            DefaultValue = "",
            Description = "Reads the content of the specified file from the browser and populates its Content property with the byte array, or reads every valid file of the file list when no file is specified. Only reads valid files and only while the component is enabled. The whole file crosses the interop boundary as one message, which on Blazor Server the circuit caps (SignalR's MaximumReceiveMessageSize, 32 KB by default), so anything larger is read with OpenReadStreamAsync instead.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "OpenReadStreamAsync",
            Type = "(BitFileInputInfo fileInfo, long? maxAllowedSize = null, CancellationToken cancellationToken = default) => Task<Stream>",
            DefaultValue = "",
            Description = "Opens a forward-only stream over the content of the specified file, which the runtime reads from the browser in chunks instead of materializing the whole file in memory the way ReadContentAsync does - which is also what gets a file past a Blazor Server circuit's message size cap. Unlike ReadContentAsync it also reads a file the validations rejected. maxAllowedSize defaults to the size the browser reported for the file, and the stream must be disposed by the caller.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "RemoveFile",
            Type = "(BitFileInputInfo? fileInfo = null) => Task",
            DefaultValue = "",
            Description = "Removes a specific file from the selected files list, or clears all files when no file is specified, invoking the OnRemove callback for each removed file and the OnChange callback afterwards.",
            LinkType = LinkType.Link,
            Href = "#file-input-info"
        },
        new()
        {
            Name = "Reset",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Clears all selected files and resets the file input to its initial state without invoking any callback.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "file-input-info",
            Title = "BitFileInputInfo",
            Description = "Represents metadata, validation state, and content of a file selected through BitFileInput.",
            Parameters =
            [
               new()
               {
                   Name = "ContentType",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The MIME content type of the file (e.g., \"image/png\", \"application/pdf\")."
               },
               new()
               {
                   Name = "Name",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The name of the file including its extension (e.g., \"document.pdf\")."
               },
               new()
               {
                   Name = "RelativePath",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "The path of the file relative to the selected folder, including the folder's own name (e.g., \"photos/2024/summer.jpg\"). It is only reported by the browser for a folder selection or a dropped folder, and is an empty string for a file picked or dropped on its own."
               },
               new()
               {
                   Name = "Size",
                   Type = "long",
                   Description = "The size of the file in bytes."
               },
               new()
               {
                   Name = "FileId",
                   Type = "string",
                   DefaultValue = "string.Empty",
                   Description = "A unique identifier (GUID) assigned to the file upon selection, used to reference the file in JavaScript interop."
               },
               new()
               {
                   Name = "Index",
                   Type = "int",
                   Description = "The zero-based index of the file in the current selection list."
               },
               new()
               {
                   Name = "LastModified",
                   Type = "long",
                   Description = "The last modified time of the file reported by the browser, in milliseconds since the Unix epoch."
               },
               new()
               {
                   Name = "Extension",
                   Type = "string",
                   Description = "The extension of the file including its leading dot, lowercased (e.g. \".pdf\"), or an empty string for a file whose name carries none, a dotfile (\".gitignore\") included."
               },
               new()
               {
                   Name = "LastModifiedDate",
                   Type = "DateTimeOffset",
                   Description = "The last modified time of the file reported by the browser, as a DateTimeOffset."
               },
               new()
               {
                   Name = "PreviewUrl",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "An object URL of the file content that can be used as the source of an img element to preview image files. This is only populated for image files when the ShowPreview parameter of the BitFileInput is enabled."
               },
               new()
               {
                   Name = "Width",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "The width of the image in pixels, only populated for decodable image files when the ReadImageDimensions parameter of the BitFileInput is enabled. It is null for anything else."
               },
               new()
               {
                   Name = "Height",
                   Type = "int?",
                   DefaultValue = "null",
                   Description = "The height of the image in pixels, only populated for decodable image files when the ReadImageDimensions parameter of the BitFileInput is enabled. It is null for anything else."
               },
               new()
               {
                   Name = "IsValid",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether the file has passed all validation checks including size constraints and allowed extensions."
               },
               new()
               {
                   Name = "Message",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "The validation error message when the file has failed a validation check (e.g., size or extension). This is null when the file is valid."
               },
               new()
               {
                   Name = "Content",
                   Type = "byte[]?",
                   DefaultValue = "null",
                   Description = "The file content as a byte array, populated by calling ReadContentAsync. This is null by default and only loaded on demand."
               }
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitFileInputClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitFileInput."
               },
               new()
               {
                   Name = "Dragging",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element while files are being dragged over the BitFileInput."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the browse button (label) of the BitFileInput."
               },
               new()
               {
                   Name = "DropZoneIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the glyph of the drop zone panel, which is only rendered while ShowDropZone is enabled."
               },
               new()
               {
                   Name = "Description",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the description (hint) of the BitFileInput."
               },
               new()
               {
                   Name = "FileList",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file list container of the BitFileInput."
               },
               new()
               {
                   Name = "FileItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for each file item of the BitFileInput."
               },
               new()
               {
                   Name = "Preview",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the image preview thumbnail of each file item of the BitFileInput."
               },
               new()
               {
                   Name = "FileIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file type glyph shown in the thumbnail's place of each file item that has no image preview."
               },
               new()
               {
                   Name = "FileName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file name of each file item of the BitFileInput."
               },
               new()
               {
                   Name = "FilePath",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the folder of each file item that came from a folder selection."
               },
               new()
               {
                   Name = "FileSize",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file size of each file item of the BitFileInput."
               },
               new()
               {
                   Name = "ErrorMessage",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the validation error message of each invalid file item of the BitFileInput."
               },
               new()
               {
                   Name = "RemoveButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the remove button of each file item of the BitFileInput."
               },
               new()
               {
                   Name = "RemoveIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the remove button icon of each file item of the BitFileInput."
               }
            ]
        },
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
                new() { Name = "Small", Description = "The small size file input.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size file input.", Value = "1" },
                new() { Name = "Large", Description = "The large size file input.", Value = "2" }
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
        }
    ];



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-FileInput-max-width",
            DefaultValue = "21.875rem",
            Description = "The widest the whole component gets. Set it to 100% for a file input that fills its column.",
        },
        new()
        {
            Name = "--bit-FileInput-color",
            DefaultValue = "The Color role's main color",
            Description = "The role color: the fill of a Fill browse button, the rule and the text of an Outline or Text one.",
        },
        new()
        {
            Name = "--bit-FileInput-text-color",
            DefaultValue = "The Color role's on-color",
            Description = "Text drawn on top of the role color.",
        },
        new()
        {
            Name = "--bit-FileInput-hover-color",
            DefaultValue = "The Color role's hover color",
            Description = "Role color while the browse button is hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-FileInput-active-color",
            DefaultValue = "The Color role's active color",
            Description = "Role color while the browse button is pressed.",
        },
        new()
        {
            Name = "--bit-FileInput-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring of the browse button and of each remove button.",
        },
        new()
        {
            Name = "--bit-FileInput-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Foreground when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-FileInput-disabled-background",
            DefaultValue = "--bit-clr-bg-dis",
            Description = "Background of the browse button and of the remove buttons when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-FileInput-disabled-border-color",
            DefaultValue = "--bit-clr-brd-dis",
            Description = "Border color of the browse button when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-FileInput-label-height",
            DefaultValue = "--bit-siz-ctrl-sm / -md / -lg per Size",
            Description = "Smallest height of the browse button, which is what lines it up with the other controls of its size.",
        },
        new()
        {
            Name = "--bit-FileInput-label-padding",
            DefaultValue = "--bit-siz-ctrl-pad-y-* --bit-siz-ctrl-pad-x-* per Size",
            Description = "Padding of the browse button.",
        },
        new()
        {
            Name = "--bit-FileInput-label-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the browse button.",
        },
        new()
        {
            Name = "--bit-FileInput-label-font-weight",
            DefaultValue = "--bit-tpg-font-weight",
            Description = "Text weight of the browse button, which defaults to the weight the theme gives every control label.",
        },
        new()
        {
            Name = "--bit-FileInput-label-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of the browse button, which its focus ring follows.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-height",
            DefaultValue = "Per Size, 4.5rem / 5.5rem / 6.5rem",
            Description = "The smallest height of the drop zone panel rendered by ShowDropZone.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-padding",
            DefaultValue = "Per Size, from the spacing rhythm",
            Description = "Padding of the drop zone panel.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner radius of the drop zone panel, which its focus ring follows.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-border-width",
            DefaultValue = "--bit-shp-brd-width-thick",
            Description = "Rule thickness of the drop zone panel.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-border-style",
            DefaultValue = "dashed",
            Description = "Rule style of the drop zone panel at rest. Set it to solid for a panel that reads as a surface rather than as a target.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-zone-icon-size",
            DefaultValue = "Per Size, 1.5rem / 2rem / 2.5rem",
            Description = "Glyph size inside the drop zone panel.",
        },
        new()
        {
            Name = "--bit-FileInput-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the hint under the browse button.",
        },
        new()
        {
            Name = "--bit-FileInput-description-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of that hint, of the file size and of the folder.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-color",
            DefaultValue = "--bit-FileInput-text-color",
            Description = "Rule and text of the browse button while files are dragged over the component.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-background",
            DefaultValue = "--bit-FileInput-hover-color",
            Description = "Fill of the browse button while files are dragged over the component.",
        },
        new()
        {
            Name = "--bit-FileInput-drop-border-style",
            DefaultValue = "dashed",
            Description = "Border style of the drop indicator. Set it to solid for a drop state that does not change the shape of the button.",
        },
        new()
        {
            Name = "--bit-FileInput-file-list-max-height",
            DefaultValue = "none",
            Description = "The tallest the file list gets before it scrolls, which is what keeps a folder selection of thousands of files from pushing the rest of the page away.",
        },
        new()
        {
            Name = "--bit-FileInput-item-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of a file item, valid or not.",
        },
        new()
        {
            Name = "--bit-FileInput-item-hover-background",
            DefaultValue = "--bit-FileInput-item-background",
            Description = "Background of a hovered valid file item (pointer devices only).",
        },
        new()
        {
            Name = "--bit-FileInput-item-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Border of a file item at rest.",
        },
        new()
        {
            Name = "--bit-FileInput-item-hover-border-color",
            DefaultValue = "--bit-FileInput-item-border-color, then --bit-clr-brd-pri-hover",
            Description = "Border of a hovered file item (pointer devices only). It falls back to the resting border color first, so repainting an item's rule covers its hover without a second variable.",
        },
        new()
        {
            Name = "--bit-FileInput-item-radius",
            DefaultValue = "--bit-shp-radius-surface",
            Description = "Corner radius of a file item.",
        },
        new()
        {
            Name = "--bit-FileInput-item-padding",
            DefaultValue = "0.5rem",
            Description = "Padding around the name, the size and the error message of a file item.",
        },
        new()
        {
            Name = "--bit-FileInput-item-gap",
            DefaultValue = "0.1875rem",
            Description = "Room between two file items, and above the first one.",
        },
        new()
        {
            Name = "--bit-FileInput-item-font-size",
            DefaultValue = "Per Size, from the type ramp",
            Description = "Text size of the file name.",
        },
        new()
        {
            Name = "--bit-FileInput-file-name-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the file name.",
        },
        new()
        {
            Name = "--bit-FileInput-file-size-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the file size and of the folder beside it.",
        },
        new()
        {
            Name = "--bit-FileInput-error-color",
            DefaultValue = "--bit-clr-err",
            Description = "Message and border of an invalid file item.",
        },
        new()
        {
            Name = "--bit-FileInput-invalid-background",
            DefaultValue = "--bit-FileInput-item-background",
            Description = "Background of an invalid file item. It matches a valid one by default so a rejected file still reads as a row of the list; a tint of the error color is the other reasonable choice.",
        },
        new()
        {
            Name = "--bit-FileInput-preview-size",
            DefaultValue = "2rem / 2.5rem / 3.25rem per Size",
            Description = "Side of the image thumbnail and of the file type glyph standing in for it.",
        },
        new()
        {
            Name = "--bit-FileInput-preview-radius",
            DefaultValue = "--bit-shp-radius-sm",
            Description = "Corner radius of the image thumbnail.",
        },
        new()
        {
            Name = "--bit-FileInput-file-icon-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the file type glyph.",
        },
        new()
        {
            Name = "--bit-FileInput-remove-button-size",
            DefaultValue = "--bit-siz-ctrl-sm / -md / -lg per Size",
            Description = "Side of the square remove button.",
        },
        new()
        {
            Name = "--bit-FileInput-remove-button-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Glyph color of the remove button.",
        },
        new()
        {
            Name = "--bit-FileInput-remove-button-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of the remove button at rest.",
        },
        new()
        {
            Name = "--bit-FileInput-remove-button-hover-background",
            DefaultValue = "--bit-clr-bg-sec-hover",
            Description = "Background of a hovered remove button (pointer devices only).",
        },
        new()
        {
            Name = "--bit-FileInput-remove-button-active-background",
            DefaultValue = "--bit-clr-bg-sec-active",
            Description = "Background of a pressed remove button.",
        },
        new()
        {
            Name = "--bit-FileInput-remove-icon-size",
            DefaultValue = "--bit-siz-icon-sm / -md / -lg per Size",
            Description = "Glyph size of the remove button.",
        },
    ];



    private BitFileInput bitFileInput = default!;


    private bool allowDrop = true;
    private bool allowPaste = true;


    private string? ValidateEmptyFile(BitFileInputInfo file)
    {
        return file.Size == 0 ? "Empty files are not allowed" : null;
    }


    private string? ValidateImageDimensions(BitFileInputInfo file)
    {
        // dropped and pasted files bypass the accept filter, so non-image files reach the validator as well.
        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) is false) return null;

        if (file.Width is null || file.Height is null) return "This image could not be decoded";

        return (file.Width < 300 || file.Height < 300)
            ? $"The image is {file.Width}×{file.Height}, smaller than the required 300×300"
            : null;
    }


    private BitIconInfo? SelectFileIcon(BitFileInputInfo file)
    {
        // anything the app knows nothing about is left without a glyph rather than given a generic one.
        if (file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)) return BitIconInfo.Bit("MyMoviesTV");
        if (file.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)) return BitIconInfo.Bit("Volume3");

        return Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".pdf" => BitIconInfo.Bit("PDF"),
            ".zip" or ".rar" or ".7z" => BitIconInfo.Bit("ZipFolder"),
            _ => null
        };
    }


    private static readonly string[] farsiUnits = ["بایت", "کیلوبایت", "مگابایت", "گیگابایت"];

    private string FormatFileSizeInFarsi(long size)
    {
        double value = size;
        var unit = 0;

        while (value >= 1024 && unit < farsiUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{Math.Round(value, 1)} {farsiUnits[unit]}";
    }


    private BitFileInputInfo[] hiddenListFiles = [];

    private void HandleOnHiddenListChange(BitFileInputInfo[] files)
    {
        hiddenListFiles = files;
    }


    private BitFileInput eventsFileInput = default!;
    private BitFileInputInfo[] eventsFiles = [];
    private readonly List<string> eventsLog = [];

    private async Task HandleOnChange(BitFileInputInfo[] files)
    {
        eventsFiles = files;

        AddEventLog($"OnChange: {files.Length} file(s) selected");

        // reads the content of every valid file of the list.
        await eventsFileInput.ReadContentAsync();
    }

    private void HandleOnInvalid(BitFileInputInfo[] files)
    {
        AddEventLog($"OnInvalid: {string.Join(", ", files.Select(f => $"{f.Name} ({f.Message})"))}");
    }

    private void HandleOnRemove(BitFileInputInfo file)
    {
        AddEventLog($"OnRemove: {file.Name}");
    }

    private void AddEventLog(string log)
    {
        eventsLog.Insert(0, log);

        if (eventsLog.Count > 5)
        {
            eventsLog.RemoveAt(eventsLog.Count - 1);
        }
    }


    private BitFileInput publicApiFileInput = default!;
    private string? streamHash;

    private async Task HashTheFirstFile()
    {
        streamHash = null;

        var file = publicApiFileInput.Files.FirstOrDefault(f => f.IsValid);

        if (file is null) return;

        // nothing of the file is ever held whole: the stream is read in chunks and folded into the hash.
        await using var stream = await publicApiFileInput.OpenReadStreamAsync(file);

        streamHash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream));
    }


    private string? AnnounceInFarsi(IReadOnlyList<BitFileInputInfo> files)
    {
        if (files.Count == 0) return "فایلی انتخاب نشده است.";

        var rejected = files.Count(f => f.IsValid is false);

        return rejected == 0
            ? $"{files.Count} فایل انتخاب شد."
            : $"{files.Count} فایل انتخاب شد، {rejected} مورد نامعتبر است.";
    }


    private readonly BitFileInputParams[] fileInputParams =
    [
        new()
        {
            Multiple = true,
            ShowPreview = true,
            ShowRemoveButton = true,
            Label = "Attach a file",
            MaxSize = 1024 * 1024 * 1,
            MaxSizeErrorMessage = "Attachments are limited to 1 MB.",
            Description = "Anything up to 1 MB. Drop it here or browse.",
            FileSizeFormatter = size => $"{size:N0} bytes"
        }
    ];
}
