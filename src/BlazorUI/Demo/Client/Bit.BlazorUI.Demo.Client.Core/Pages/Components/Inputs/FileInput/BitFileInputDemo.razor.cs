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
            Description = "Whether a file that is already in the file list can be selected again. When disabled, a newly selected file matching an existing one by name, size and last modified time is marked as invalid with the DuplicateErrorMessage instead of being added as a second entry.",
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
            Name = "DuplicateErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "Custom error message displayed when a file is selected again while AllowDuplicates is disabled. Defaults to \"The file is already selected\"."
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
            Name = "FileSizeFormatter",
            Type = "Func<long, string>?",
            DefaultValue = "null",
            Description = "Custom formatter of the file size shown under the name of each file item. Receives the size of the file in bytes and returns the text to display, which is the place to localize the units or to switch between the binary and the decimal bases. When not set, a built-in humanizer is used.",
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
            Name = "ShowPreview",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to display a preview thumbnail for image files in the file list."
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
            Type = "(BitFileInputInfo? fileInfo = null) => Task",
            DefaultValue = "",
            Description = "Reads the content of the specified file from the browser and populates its Content property with the byte array, or reads every valid file of the file list when no file is specified. Only reads valid files and only while the component is enabled.",
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
                   Name = "FileName",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the file name of each file item of the BitFileInput."
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
        }
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


    private string? AnnounceAttachments(IReadOnlyList<BitFileInputInfo> files)
    {
        if (files.Count == 0) return "No attachment yet.";

        var rejected = files.Count(f => f.IsValid is false);

        return rejected == 0
            ? $"{files.Count} attachment(s) ready to send."
            : $"{files.Count - rejected} attachment(s) ready to send, {rejected} rejected as too large.";
    }
}
