namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownEditor;

public partial class BitMarkdownEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AcceptedImageTypes",
            Type = "string?",
            DefaultValue = "null",
            Description = "The image types the paste/drop upload accepts, as a comma separated list of MIME types or extensions (image/png,image/jpeg or .png,.jpg). Null accepts every image type.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Moves the keyboard focus into the editor as soon as it is initialized.",
        },
        new()
        {
            Name = "AutoPair",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables wrapping the current selection when a pairing character (for example *, `, [) is typed.",
        },
        new()
        {
            Name = "AutoSaveId",
            Type = "string?",
            DefaultValue = "null",
            Description = "A stable key under which the editor content is autosaved to the browser's localStorage. When set, a draft is written as the user types and restored on initialization if no Value/DefaultValue is supplied.",
        },
        new()
        {
            Name = "ChangeDebounceTime",
            Type = "int",
            DefaultValue = "0",
            Description = "The debounce window (in milliseconds) before the typed value is pushed to .NET. Increase it to reduce interop traffic on Blazor Server.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMarkdownEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DebounceTime",
            Type = "int",
            DefaultValue = "150",
            Description = "The debounce window (in milliseconds) before the preview re-renders while typing.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default text value of the editor to use at initialization.",
        },
        new()
        {
            Name = "FullScreen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the editor is rendered in full-screen mode (two-way bindable).",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the editor (any CSS length). Ignored in full-screen mode.",
        },
        new()
        {
            Name = "IndentUnit",
            Type = "string",
            DefaultValue = "\"  \"",
            Description = "The string inserted per indent level (default: two spaces).",
        },
        new()
        {
            Name = "MaxImageSize",
            Type = "long?",
            DefaultValue = "null",
            Description = "The largest pasted or dropped image (in bytes) the editor uploads. A bigger file is refused before its bytes are read and reported through OnImageRejected.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int?",
            DefaultValue = "null",
            Description = "The maximum number of characters the editor accepts. When set, the status bar counter shows the limit alongside the count.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitMarkdownEditorMode",
            DefaultValue = "BitMarkdownEditorMode.Split",
            Description = "Determines which panes of the editor are visible (edit / split / preview). Two-way bindable.",
            LinkType = LinkType.Link,
            Href = "#editor-mode-enum",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the editor loses the keyboard focus.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Callback for when the editor value changes.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the editor receives the keyboard focus.",
        },
        new()
        {
            Name = "OnImageRejected",
            Type = "EventCallback<BitMarkdownEditorImageRejection>",
            DefaultValue = "",
            Description = "Callback for a pasted or dropped image the editor refused to upload because of MaxImageSize or AcceptedImageTypes.",
            LinkType = LinkType.Link,
            Href = "#image-rejection",
        },
        new()
        {
            Name = "OnImageUpload",
            Type = "Func<BitMarkdownEditorImageUploadInfo, Task<string?>>?",
            DefaultValue = "null",
            Description = "A handler that uploads a pasted or dropped image and returns the URL to reference it by. When set, the editor enables clipboard-paste and drag-and-drop image upload; returning null cancels the insertion.",
            LinkType = LinkType.Link,
            Href = "#image-upload-info",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder text shown when the editor is empty.",
        },
        new()
        {
            Name = "PreviewPipeline",
            Type = "BitMarkdownPipeline?",
            DefaultValue = "null",
            Description = "The markdown processing pipeline used by the preview pane. Defaults to BitMarkdownPipelines.GitHub.",
        },
        new()
        {
            Name = "PreviewTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "A custom template to render the preview pane. Receives the current markdown value and replaces the built-in BitMarkdownViewer based preview.",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the editor read-only.",
        },
        new()
        {
            Name = "Resizable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the user drag the bottom edge of the editor to change its height.",
        },
        new()
        {
            Name = "ShowReadingTime",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the estimated reading time is shown in the status bar.",
        },
        new()
        {
            Name = "ShowStatusBar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the word/character status bar is shown.",
        },
        new()
        {
            Name = "ShowToolbar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the formatting toolbar is shown.",
        },
        new()
        {
            Name = "SpellCheck",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the native browser spell checking in the textarea.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMarkdownEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "SyncScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "Synchronizes scrolling between the editor and preview panes in split mode.",
        },
        new()
        {
            Name = "TabIndents",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the Tab key indents the selection instead of moving the focus to the next control. Pressing Escape first always lets a single Tab out, either way.",
        },
        new()
        {
            Name = "TableColumns",
            Type = "int",
            DefaultValue = "2",
            Description = "How many columns the toolbar's table command inserts.",
        },
        new()
        {
            Name = "TableRows",
            Type = "int",
            DefaultValue = "1",
            Description = "How many body rows the toolbar's table command inserts, beside its header row.",
        },
        new()
        {
            Name = "Texts",
            Type = "BitMarkdownEditorTexts?",
            DefaultValue = "null",
            Description = "The localized strings of the editor UI (toolbar titles, status bar, find panel, help panel, aria labels). Defaults to English.",
            LinkType = LinkType.Link,
            Href = "#texts",
        },
        new()
        {
            Name = "Toolbar",
            Type = "IReadOnlyList<BitMarkdownEditorToolbarItem>?",
            DefaultValue = "null",
            Description = "A custom toolbar layout. Defaults to BitMarkdownEditorToolbar.Default when null.",
            LinkType = LinkType.Link,
            Href = "#toolbar-item",
        },
        new()
        {
            Name = "Value",
            Type = "string?",
            DefaultValue = "null",
            Description = "The two-way bound text value of the editor.",
        },
        new()
        {
            Name = "WordsPerMinute",
            Type = "int",
            DefaultValue = "200",
            Description = "Words-per-minute used to estimate the reading time shown in the status bar.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CanUndo",
            Type = "bool",
            DefaultValue = "false",
            Description = "True when there is at least one change that can be undone.",
        },
        new()
        {
            Name = "CanRedo",
            Type = "bool",
            DefaultValue = "false",
            Description = "True when there is at least one undone change that can be redone.",
        },
        new()
        {
            Name = "GetValue",
            Type = "Func<ValueTask<string>>",
            DefaultValue = "",
            Description = "Returns the current value of the editor directly from the textarea.",
        },
        new()
        {
            Name = "Run",
            Type = "Func<BitMarkdownEditorCommand, ValueTask>",
            DefaultValue = "",
            Description = "Runs a specific command on the current selection of the editor.",
            LinkType = LinkType.Link,
            Href = "#command-enum",
        },
        new()
        {
            Name = "Insert",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Inserts the given markdown text at the current selection (replacing it) as a single undo step.",
        },
        new()
        {
            Name = "Replace",
            Type = "Func<string, string, bool, bool, ValueTask<int>>",
            DefaultValue = "",
            Description = "Replaces occurrences of a search term and returns the replacement count. With all off, the first occurrence at or after the caret is replaced (wrapping to the top).",
        },
        new()
        {
            Name = "FindNext",
            Type = "Func<string, bool, ValueTask<BitMarkdownEditorFindResult>>",
            DefaultValue = "",
            Description = "Selects the next occurrence of the search term after the caret, wrapping around the end of the document.",
            LinkType = LinkType.Link,
            Href = "#find-result",
        },
        new()
        {
            Name = "FindPrevious",
            Type = "Func<string, bool, ValueTask<BitMarkdownEditorFindResult>>",
            DefaultValue = "",
            Description = "Selects the occurrence of the search term before the caret, wrapping around the start of the document.",
            LinkType = LinkType.Link,
            Href = "#find-result",
        },
        new()
        {
            Name = "GetSelection",
            Type = "Func<ValueTask<BitMarkdownEditorSelection>>",
            DefaultValue = "",
            Description = "Returns the current selection range of the editor along with the selected text.",
            LinkType = LinkType.Link,
            Href = "#selection",
        },
        new()
        {
            Name = "SetSelection",
            Type = "Func<int, int, ValueTask>",
            DefaultValue = "",
            Description = "Selects the given range in the editor and moves the focus into it. The range is clamped to the current content length.",
        },
        new()
        {
            Name = "ClearDraft",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Clears the autosaved draft (if AutoSaveId is set), e.g. after the content has been persisted server-side.",
        },
        new()
        {
            Name = "Undo",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Reverts the editor to the previous state in the undo history.",
        },
        new()
        {
            Name = "Redo",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Re-applies the most recently undone change.",
        },
        new()
        {
            Name = "Focus",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Moves the keyboard focus into the editor textarea.",
        },
        new()
        {
            Name = "Blur",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Moves the keyboard focus out of the editor textarea.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "toolbar-item",
            Title = "BitMarkdownEditorToolbarItem",
            Description = "Describes a single button (or separator) in the editor toolbar. The toolbar is fully data-driven, so consumers can reorder, remove, or add items by supplying their own list to the Toolbar parameter.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "Stable identifier, handy for tests and custom styling. The names of the default toolbar are also the keys the Texts parameter localizes by.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "Tooltip / accessible label shown to the user.",
                },
                new()
                {
                    Name = "Icon",
                    Type = "string",
                    DefaultValue = "string.Empty",
                    Description = "Raw inline SVG markup rendered inside the button.",
                },
                new()
                {
                    Name = "Type",
                    Type = "BitMarkdownEditorToolbarItemType",
                    DefaultValue = "BitMarkdownEditorToolbarItemType.Command",
                    Description = "How the item behaves when activated.",
                    LinkType = LinkType.Link,
                    Href = "#toolbar-item-type-enum",
                },
                new()
                {
                    Name = "Command",
                    Type = "BitMarkdownEditorCommand?",
                    DefaultValue = "null",
                    Description = "The text command to run when the Type is Command.",
                    LinkType = LinkType.Link,
                    Href = "#command-enum",
                },
                new()
                {
                    Name = "Shortcut",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Optional human readable shortcut hint, e.g. \"Ctrl+B\". Shown in the tooltip and reported as aria-keyshortcuts.",
                },
                new()
                {
                    Name = "OnClick",
                    Type = "Func<BitMarkdownEditor, Task>?",
                    DefaultValue = "null",
                    Description = "Callback used when the Type is Custom. Receives the editor instance so the handler can read or rewrite the content.",
                },
                new()
                {
                    Name = "Children",
                    Type = "IReadOnlyList<BitMarkdownEditorToolbarItem>?",
                    DefaultValue = "null",
                    Description = "Child items shown in the menu when the Type is Dropdown.",
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Optional short text label rendered inside the button (used by dropdown menu items).",
                },
                new()
                {
                    Name = "AlwaysEnabled",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Keeps the item enabled while the editor is read-only, for custom items that only read the content (export, copy, save).",
                },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitMarkdownEditorClassStyles",
            Description = "Custom CSS classes/styles for different parts of the BitMarkdownEditor.",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitMarkdownEditor.",
                },
                new()
                {
                    Name = "Toolbar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar of the BitMarkdownEditor.",
                },
                new()
                {
                    Name = "ToolbarButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar buttons of the BitMarkdownEditor.",
                },
                new()
                {
                    Name = "TextArea",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text area of the BitMarkdownEditor.",
                },
                new()
                {
                    Name = "Preview",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the preview pane of the BitMarkdownEditor.",
                },
                new()
                {
                    Name = "StatusBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the status bar of the BitMarkdownEditor.",
                },
            ]
        },
        new()
        {
            Id = "image-upload-info",
            Title = "BitMarkdownEditorImageUploadInfo",
            Description = "Describes an image pasted into or dropped onto the editor, passed to the OnImageUpload handler.",
            Parameters =
            [
                new()
                {
                    Name = "FileName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The original file name (may be a generic name for clipboard images).",
                },
                new()
                {
                    Name = "ContentType",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The MIME type of the image, for example image/png.",
                },
                new()
                {
                    Name = "Data",
                    Type = "byte[]",
                    DefaultValue = "",
                    Description = "The raw image bytes.",
                },
            ]
        },
        new()
        {
            Id = "image-rejection",
            Title = "BitMarkdownEditorImageRejection",
            Description = "Describes an image the editor refused to upload, passed to the OnImageRejected callback so the app can tell the user why nothing was inserted.",
            Parameters =
            [
                new()
                {
                    Name = "FileName",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The original file name (may be a generic name for clipboard images).",
                },
                new()
                {
                    Name = "ContentType",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The MIME type of the image, for example image/png.",
                },
                new()
                {
                    Name = "Size",
                    Type = "long",
                    DefaultValue = "0",
                    Description = "The size of the file in bytes.",
                },
                new()
                {
                    Name = "Reason",
                    Type = "BitMarkdownEditorImageRejectionReason",
                    DefaultValue = "BitMarkdownEditorImageRejectionReason.Type",
                    Description = "Why the file was refused.",
                    LinkType = LinkType.Link,
                    Href = "#image-rejection-reason-enum",
                },
            ]
        },
        new()
        {
            Id = "find-result",
            Title = "BitMarkdownEditorFindResult",
            Description = "The outcome of a find (or find & replace) round trip.",
            Parameters =
            [
                new()
                {
                    Name = "Count",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "How many occurrences of the search term the document contains.",
                },
                new()
                {
                    Name = "Index",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The 1-based position of the currently selected occurrence, or 0 when none is selected.",
                },
                new()
                {
                    Name = "HasMatches",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True when at least one occurrence was found.",
                },
            ]
        },
        new()
        {
            Id = "selection",
            Title = "BitMarkdownEditorSelection",
            Description = "A selection range inside the editor textarea.",
            Parameters =
            [
                new()
                {
                    Name = "Start",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The selection start (char index).",
                },
                new()
                {
                    Name = "End",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The selection end (char index).",
                },
                new()
                {
                    Name = "Text",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The selected text, empty when the selection is a caret.",
                },
                new()
                {
                    Name = "IsEmpty",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "True when nothing is selected and the range is a plain caret position.",
                },
            ]
        },
        new()
        {
            Id = "texts",
            Title = "BitMarkdownEditorTexts",
            Description = "The texts of the editor UI. All strings default to English; override individual properties to localize the editor. The counting strings are format templates so the translation controls the word order.",
            Parameters =
            [
                new()
                {
                    Name = "ToolbarAriaLabel, EditorAriaLabel, PreviewAriaLabel",
                    Type = "string",
                    DefaultValue = "\"Markdown formatting\", \"Markdown editor\", \"Markdown preview\"",
                    Description = "The accessible names of the toolbar, the textarea and the preview pane.",
                },
                new()
                {
                    Name = "ToolbarUndo … ToolbarHelp",
                    Type = "string",
                    DefaultValue = "\"Undo\" … \"Keyboard shortcuts\"",
                    Description = "The tooltip and aria-label of every button of the default toolbar, keyed by the item Name so a custom toolbar reusing those names is localized automatically.",
                },
                new()
                {
                    Name = "WordsFormat, CharsFormat, CharsWithMaxFormat, ReadingTimeFormat",
                    Type = "string",
                    DefaultValue = "\"{0} words\", \"{0} chars\", \"{0} / {1} chars\", \"{0} min read\"",
                    Description = "The status bar templates. CharsWithMaxFormat replaces CharsFormat while a MaxLength is set.",
                },
                new()
                {
                    Name = "ModeEdit, ModeSplit, ModePreview",
                    Type = "string",
                    DefaultValue = "\"Edit\", \"Split\", \"Preview\"",
                    Description = "The mode label of the status bar.",
                },
                new()
                {
                    Name = "FindReplaceTitle, FindPlaceholder, ReplacePlaceholder, ReplaceButton, ReplaceAllButton, FindNextAriaLabel, FindPreviousAriaLabel, MatchCaseAriaLabel, MatchesFormat, NoMatchesText",
                    Type = "string",
                    DefaultValue = "\"Find and replace\" …",
                    Description = "The find & replace panel. MatchesFormat is a template of the current match and the total ({0} of {1}).",
                },
                new()
                {
                    Name = "KeyboardShortcutsTitle, CloseAriaLabel, Shortcut*",
                    Type = "string",
                    DefaultValue = "\"Keyboard shortcuts\" …",
                    Description = "The shortcut help dialog: its title, its close button and the label of every listed shortcut.",
                },
                new()
                {
                    Name = "PreviewEmptyText",
                    Type = "string",
                    DefaultValue = "\"Nothing to preview yet.\"",
                    Description = "The text shown in the preview pane while the editor is empty.",
                },
                new()
                {
                    Name = "UploadingText",
                    Type = "string",
                    DefaultValue = "\"uploading\"",
                    Description = "The word shown inside the placeholder that stands in for an image while it uploads.",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "command-enum",
            Name = "BitMarkdownEditorCommand",
            Description = "The set of built-in editing commands the toolbar, the keyboard shortcuts and the Run method can invoke. These are pure text transformations executed in C#.",
            Items =
            [
                new() { Name = "Bold", Value = "0", Description = "Toggles bold formatting on the current selection." },
                new() { Name = "Italic", Value = "1", Description = "Toggles italic formatting on the current selection." },
                new() { Name = "Strikethrough", Value = "2", Description = "Toggles strikethrough formatting on the current selection." },
                new() { Name = "InlineCode", Value = "3", Description = "Toggles inline code formatting on the current selection." },
                new() { Name = "Heading1", Value = "4", Description = "Toggles a level 1 heading on the selected lines." },
                new() { Name = "Heading2", Value = "5", Description = "Toggles a level 2 heading on the selected lines." },
                new() { Name = "Heading3", Value = "6", Description = "Toggles a level 3 heading on the selected lines." },
                new() { Name = "Heading4", Value = "7", Description = "Toggles a level 4 heading on the selected lines." },
                new() { Name = "Heading5", Value = "8", Description = "Toggles a level 5 heading on the selected lines." },
                new() { Name = "Heading6", Value = "9", Description = "Toggles a level 6 heading on the selected lines." },
                new() { Name = "Quote", Value = "10", Description = "Toggles a blockquote on the selected lines." },
                new() { Name = "CodeBlock", Value = "11", Description = "Wraps the current selection in a fenced code block." },
                new() { Name = "Link", Value = "12", Description = "Inserts a link, turns the current selection into a link's label, or uses a selected url as its target." },
                new() { Name = "Image", Value = "13", Description = "Inserts an image, turns the current selection into an image's alt text, or uses a selected url as its source." },
                new() { Name = "UnorderedList", Value = "14", Description = "Toggles an unordered (bullet) list on the selected lines." },
                new() { Name = "OrderedList", Value = "15", Description = "Toggles an ordered (numbered) list on the selected lines." },
                new() { Name = "TaskList", Value = "16", Description = "Toggles a task (checkbox) list on the selected lines." },
                new() { Name = "Table", Value = "17", Description = "Inserts a table template at the caret position." },
                new() { Name = "HorizontalRule", Value = "18", Description = "Inserts a horizontal rule at the caret position." },
                new() { Name = "Indent", Value = "19", Description = "Increases the indentation of the selected lines (Tab)." },
                new() { Name = "Outdent", Value = "20", Description = "Decreases the indentation of the selected lines (Shift+Tab)." },
                new() { Name = "NewLine", Value = "21", Description = "Smart newline that continues lists and quotes (Enter)." },
                new() { Name = "Superscript", Value = "22", Description = "Toggles superscript (^text^) on the current selection. None of the BitMarkdownPipelines renders that extension, so the built-in preview shows it verbatim." },
                new() { Name = "Subscript", Value = "23", Description = "Toggles subscript (~text~) on the current selection. None of the BitMarkdownPipelines renders that extension, so the built-in preview shows it verbatim." },
                new() { Name = "ClearFormatting", Value = "24", Description = "Removes inline and block markdown formatting (including links) from the selected lines." },
                new() { Name = "MoveLineUp", Value = "25", Description = "Swaps the selected lines with the line above them (Alt+Up)." },
                new() { Name = "MoveLineDown", Value = "26", Description = "Swaps the selected lines with the line below them (Alt+Down)." },
                new() { Name = "DuplicateLine", Value = "27", Description = "Duplicates the selected lines right below themselves (Ctrl+D)." },
                new() { Name = "DeleteLine", Value = "28", Description = "Deletes the selected lines entirely (Ctrl+Shift+D)." },
            ]
        },
        new()
        {
            Id = "editor-mode-enum",
            Name = "BitMarkdownEditorMode",
            Description = "Controls which panes the BitMarkdownEditor displays.",
            Items =
            [
                new() { Name = "Edit", Value = "0", Description = "Only the markdown text area is shown." },
                new() { Name = "Split", Value = "1", Description = "Editor and rendered preview are shown side by side." },
                new() { Name = "Preview", Value = "2", Description = "Only the rendered preview is shown." },
            ]
        },
        new()
        {
            Id = "image-rejection-reason-enum",
            Name = "BitMarkdownEditorImageRejectionReason",
            Description = "Why the editor refused to upload a pasted or dropped image.",
            Items =
            [
                new() { Name = "Type", Value = "0", Description = "The file's type is not one of the accepted image types." },
                new() { Name = "Size", Value = "1", Description = "The file is larger than the allowed maximum size." },
            ]
        },
        new()
        {
            Id = "toolbar-item-type-enum",
            Name = "BitMarkdownEditorToolbarItemType",
            Description = "Describes how a BitMarkdownEditorToolbarItem behaves when clicked.",
            Items =
            [
                new() { Name = "Command", Value = "0", Description = "Runs the associated BitMarkdownEditorCommand against the text." },
                new() { Name = "Undo", Value = "1", Description = "Reverts the editor to the previous state in the undo history." },
                new() { Name = "Redo", Value = "2", Description = "Re-applies the most recently undone change." },
                new() { Name = "Separator", Value = "3", Description = "A non-interactive vertical divider in the toolbar." },
                new() { Name = "TogglePreview", Value = "4", Description = "Cycles the editor display mode (edit / split / preview)." },
                new() { Name = "ToggleFullScreen", Value = "5", Description = "Toggles the full-screen mode of the editor." },
                new() { Name = "Help", Value = "6", Description = "Toggles the keyboard-shortcut help panel." },
                new() { Name = "Find", Value = "7", Description = "Toggles the find & replace panel." },
                new() { Name = "Custom", Value = "8", Description = "Invokes a user-supplied callback." },
                new() { Name = "Dropdown", Value = "9", Description = "A button that reveals a menu of child items (e.g. a heading picker)." },
            ]
        },
    ];



    private string? introValue =
@"# BitMarkdownEditor in action

A **native** Blazor markdown editor with:

- [x] Data-driven toolbar
- [x] Keyboard shortcuts (Ctrl+B, Ctrl+I, Ctrl+K, ...)
- [x] Smart list continuation & Tab indentation
- [x] Undo/Redo history (Ctrl+Z / Ctrl+Y)
- [x] Find & replace (Ctrl+F)
- [x] Live *GitHub flavored* preview via `BitMarkdownViewer`

| Mode | Description |
| ---- | ----------- |
| Edit | Only the text area |
| Split | Side-by-side |
| Preview | Only the preview |

Start typing here...";

    private string? bindingValue = "# Two-way binding";

    private string? onChangeValue;

    private BitMarkdownEditorMode mode = BitMarkdownEditorMode.Split;
    private string modeDefaultValue =
@"# Mode
Switch between **Edit**, **Split** and **Preview** using the choice group above,
the eye button of the toolbar, the F9 key, or the `@bind-Mode` parameter.";

    private string? customToolbarValue = "The toolbar of this editor only offers **basic** formatting and a custom *clear* button.";
    private IReadOnlyList<BitMarkdownEditorToolbarItem> customToolbar = [];

    private BitMarkdownEditor commandsRef = default!;
    private string? getValueResult;

    private string previewDefaultValue =
@"GitHub flavored extras like ~~strikethrough~~, https://bitplatform.dev autolinks,

- [ ] task
- [x] lists

| and | tables |
| --- | ------ |
| are | here   |";

    private bool fullScreen;

    private string readOnlyDefaultValue =
@"# Read-only
The content of this editor **cannot** be edited, but can still be *selected* and the preview stays live.";

    private BitMarkdownEditor findRef = default!;
    private string? findText = "markdown";
    private string? replaceText = "Markdown";
    private string? findStatus;
    private string findDefaultValue =
@"# Find and replace

The markdown editor finds every markdown word in this markdown document.
Press Ctrl+F to open the built-in panel, or use the buttons above.";

    private string? imageStatus;

    private string imageDefaultValue =
@"# Image upload
Paste an image from the clipboard, or drop an image file anywhere on this editor.";

    private BitMarkdownEditor autoSaveRef = default!;

    private string statusDefaultValue =
@"The status bar counts words and characters 👋 and estimates the reading time.";

    private string localizationDefaultValue =
@"# Un éditeur **localisé**

Chaque libellé de l'éditeur vient du paramètre `Texts` :
survolez la barre d'outils, ouvrez la recherche ou lisez la barre d'état.";

    private BitMarkdownEditorTexts frenchTexts = new()
    {
        ToolbarAriaLabel = "Mise en forme Markdown",
        EditorAriaLabel = "Éditeur Markdown",
        PreviewAriaLabel = "Aperçu Markdown",
        ToolbarBold = "Gras",
        ToolbarItalic = "Italique",
        ToolbarHeading = "Titre",
        ToolbarQuote = "Citation",
        ToolbarLink = "Lien",
        ToolbarImage = "Image",
        ToolbarTable = "Tableau",
        ToolbarFind = "Rechercher et remplacer",
        ToolbarHelp = "Raccourcis clavier",
        WordsFormat = "{0} mots",
        CharsFormat = "{0} caractères",
        ReadingTimeFormat = "{0} min de lecture",
        ModeEdit = "Édition",
        ModeSplit = "Partagé",
        ModePreview = "Aperçu",
        FindPlaceholder = "Rechercher",
        ReplacePlaceholder = "Remplacer par",
        ReplaceButton = "Remplacer",
        ReplaceAllButton = "Tout",
        MatchesFormat = "{0} sur {1}",
        NoMatchesText = "Aucun résultat",
        UploadingText = "envoi",
        KeyboardShortcutsTitle = "Raccourcis clavier",
        PreviewEmptyText = "Rien à prévisualiser pour l'instant.",
    };

    private string rtlDefaultValue =
@"# ویرایشگر مارک‌داون
این یک متن **راست به چپ** برای نمایش قابلیت RTL است.

- پشتیبانی کامل از لیست‌ها
- میانبرهای صفحه‌کلید";

    protected override void OnInitialized()
    {
        customToolbar =
        [
            new() { Name = "bold", Title = "Bold", Command = BitMarkdownEditorCommand.Bold, Icon = BitMarkdownEditorToolbar.Icons.Bold, Shortcut = "Ctrl+B" },
            new() { Name = "italic", Title = "Italic", Command = BitMarkdownEditorCommand.Italic, Icon = BitMarkdownEditorToolbar.Icons.Italic, Shortcut = "Ctrl+I" },
            new()
            {
                Name = "heading",
                Title = "Heading",
                Icon = BitMarkdownEditorToolbar.Icons.Heading,
                Type = BitMarkdownEditorToolbarItemType.Dropdown,
                Children =
                [
                    new() { Name = "h1", Title = "Heading 1", Text = "Heading 1", Command = BitMarkdownEditorCommand.Heading1, Icon = BitMarkdownEditorToolbar.Icons.H1 },
                    new() { Name = "h2", Title = "Heading 2", Text = "Heading 2", Command = BitMarkdownEditorCommand.Heading2, Icon = BitMarkdownEditorToolbar.Icons.H2 },
                    new() { Name = "h3", Title = "Heading 3", Text = "Heading 3", Command = BitMarkdownEditorCommand.Heading3, Icon = BitMarkdownEditorToolbar.Icons.H3 },
                ]
            },
            BitMarkdownEditorToolbarItem.Separator,
            new() { Name = "link", Title = "Link", Command = BitMarkdownEditorCommand.Link, Icon = BitMarkdownEditorToolbar.Icons.Link, Shortcut = "Ctrl+K" },
            new() { Name = "image", Title = "Image", Command = BitMarkdownEditorCommand.Image, Icon = BitMarkdownEditorToolbar.Icons.Image },
            BitMarkdownEditorToolbarItem.Separator,
            new()
            {
                Name = "clear",
                Title = "Clear content",
                Icon = "🗑️",
                Type = BitMarkdownEditorToolbarItemType.Custom,
                OnClick = _ =>
                {
                    customToolbarValue = string.Empty;
                    return Task.CompletedTask;
                }
            },
        ];
    }

    private async Task RunCommand(BitMarkdownEditorCommand command)
    {
        await commandsRef.Run(command);
    }

    private async Task InsertSignature()
    {
        await commandsRef.Insert("\n\n---\n_Written with **BitMarkdownEditor**._\n");
    }

    private async Task Undo()
    {
        await commandsRef.Undo();
    }

    private async Task Redo()
    {
        await commandsRef.Redo();
    }

    private async Task GetValue()
    {
        getValueResult = await commandsRef.GetValue();
    }

    private async Task ShowSelection()
    {
        var selection = await commandsRef.GetSelection();
        getValueResult = $"[{selection.Start}..{selection.End}] {selection.Text}";
    }

    private async Task SelectFirstLine()
    {
        var value = await commandsRef.GetValue();
        var end = value.IndexOf('\n');
        await commandsRef.SetSelection(0, end < 0 ? value.Length : end);
    }

    private async Task FindNext()
    {
        var result = await findRef.FindNext(findText ?? string.Empty);
        findStatus = result.HasMatches ? $"{result.Index} of {result.Count}" : "No results";
    }

    private async Task FindPrevious()
    {
        var result = await findRef.FindPrevious(findText ?? string.Empty);
        findStatus = result.HasMatches ? $"{result.Index} of {result.Count}" : "No results";
    }

    private async Task ReplaceAll()
    {
        var count = await findRef.Replace(findText ?? string.Empty, replaceText ?? string.Empty);
        findStatus = $"{count} replaced";
    }

    private async Task<string?> UploadImage(BitMarkdownEditorImageUploadInfo info)
    {
        // A real handler would post the bytes to a storage service and return the url.
        await Task.Delay(500);
        return $"data:{info.ContentType};base64,{Convert.ToBase64String(info.Data)}";
    }

    private void ImageRejected(BitMarkdownEditorImageRejection rejection)
    {
        imageStatus = rejection.Reason switch
        {
            BitMarkdownEditorImageRejectionReason.Size => $"{rejection.FileName} is too large ({rejection.Size} bytes).",
            _ => $"{rejection.FileName} is not an accepted image type ({rejection.ContentType})."
        };
    }

    private async Task ClearDraft()
    {
        await autoSaveRef.ClearDraft();
    }



    private readonly string example1RazorCode = @"
<BitMarkdownEditor />";

    private readonly string example2RazorCode = @"
<BitMarkdownEditor @bind-Value=""bindingValue"" Mode=""BitMarkdownEditorMode.Edit"" />

<BitTextField Multiline Rows=""4"" Label=""Bound value (editable)"" @bind-Value=""@bindingValue"" Immediate />";
    private readonly string example2CsharpCode = @"
private string? bindingValue = ""# Two-way binding"";";

    private readonly string example3RazorCode = @"
<BitMarkdownEditor DefaultValue=""# This is the default value""
                   Mode=""BitMarkdownEditorMode.Edit""
                   OnChange=""v => onChangeValue = v"" />

<div>Current value:</div>
<pre class=""code-box"">@onChangeValue</pre>";
    private readonly string example3CsharpCode = @"
private string? onChangeValue;";

    private readonly string example4RazorCode = @"
<BitChoiceGroup Horizontal @bind-Value=""@mode"" TItem=""BitChoiceGroupOption<BitMarkdownEditorMode>"" TValue=""BitMarkdownEditorMode"">
    <BitChoiceGroupOption Text=""Edit"" Value=""BitMarkdownEditorMode.Edit"" />
    <BitChoiceGroupOption Text=""Split"" Value=""BitMarkdownEditorMode.Split"" />
    <BitChoiceGroupOption Text=""Preview"" Value=""BitMarkdownEditorMode.Preview"" />
</BitChoiceGroup>

<BitMarkdownEditor @bind-Mode=""mode"" DefaultValue=""@modeDefaultValue"" />";
    private readonly string example4CsharpCode = @"
private BitMarkdownEditorMode mode = BitMarkdownEditorMode.Split;
private string modeDefaultValue =
@""# Mode
Switch between **Edit**, **Split** and **Preview** using the choice group above,
the eye button of the toolbar, the F9 key, or the `@bind-Mode` parameter."";";

    private readonly string example5RazorCode = @"
<BitMarkdownEditor @bind-Value=""customToolbarValue"" Toolbar=""customToolbar"" />

<BitMarkdownEditor ShowToolbar=""false"" ShowStatusBar=""false"" />";
    private readonly string example5CsharpCode = @"
private string? customToolbarValue = ""The toolbar of this editor only offers **basic** formatting and a custom *clear* button."";
private IReadOnlyList<BitMarkdownEditorToolbarItem> customToolbar = [];

protected override void OnInitialized()
{
    customToolbar =
    [
        new() { Name = ""bold"", Title = ""Bold"", Command = BitMarkdownEditorCommand.Bold, Icon = BitMarkdownEditorToolbar.Icons.Bold, Shortcut = ""Ctrl+B"" },
        new() { Name = ""italic"", Title = ""Italic"", Command = BitMarkdownEditorCommand.Italic, Icon = BitMarkdownEditorToolbar.Icons.Italic, Shortcut = ""Ctrl+I"" },
        new()
        {
            Name = ""heading"",
            Title = ""Heading"",
            Icon = BitMarkdownEditorToolbar.Icons.Heading,
            Type = BitMarkdownEditorToolbarItemType.Dropdown,
            Children =
            [
                new() { Name = ""h1"", Title = ""Heading 1"", Text = ""Heading 1"", Command = BitMarkdownEditorCommand.Heading1, Icon = BitMarkdownEditorToolbar.Icons.H1 },
                new() { Name = ""h2"", Title = ""Heading 2"", Text = ""Heading 2"", Command = BitMarkdownEditorCommand.Heading2, Icon = BitMarkdownEditorToolbar.Icons.H2 },
                new() { Name = ""h3"", Title = ""Heading 3"", Text = ""Heading 3"", Command = BitMarkdownEditorCommand.Heading3, Icon = BitMarkdownEditorToolbar.Icons.H3 },
            ]
        },
        BitMarkdownEditorToolbarItem.Separator,
        new() { Name = ""link"", Title = ""Link"", Command = BitMarkdownEditorCommand.Link, Icon = BitMarkdownEditorToolbar.Icons.Link, Shortcut = ""Ctrl+K"" },
        new() { Name = ""image"", Title = ""Image"", Command = BitMarkdownEditorCommand.Image, Icon = BitMarkdownEditorToolbar.Icons.Image },
        BitMarkdownEditorToolbarItem.Separator,
        new()
        {
            Name = ""clear"",
            Title = ""Clear content"",
            Icon = ""🗑️"",
            Type = BitMarkdownEditorToolbarItemType.Custom,
            OnClick = _ =>
            {
                customToolbarValue = string.Empty;
                return Task.CompletedTask;
            }
        },
    ];
}";

    private readonly string example6RazorCode = @"
<div class=""commands-bar"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Heading1)"">H1</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Bold)""><b>B</b></BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Italic)""><i>I</i></BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Strikethrough)""><s>S</s></BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Link)"">Link</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Image)"">Image</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.UnorderedList)"">List</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.TaskList)"">Tasks</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Table)"">Table</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.CodeBlock)"">Code</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.DuplicateLine)"">Duplicate line</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.MoveLineUp)"">Move line up</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""InsertSignature"">Insert</BitButton>
    <BitButton Variant=""BitVariant.Outline"" IsEnabled=""commandsRef?.CanUndo ?? false"" OnClick=""Undo"">Undo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" IsEnabled=""commandsRef?.CanRedo ?? false"" OnClick=""Redo"">Redo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""GetValue"">GetValue</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ShowSelection"">GetSelection</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""SelectFirstLine"">SetSelection</BitButton>
</div>

<BitMarkdownEditor @ref=""commandsRef"" ShowToolbar=""false"" OnChange=""_ => InvokeAsync(StateHasChanged)"" />

<div>Result:</div>
<pre class=""code-box"">@getValueResult</pre>";
    private readonly string example6CsharpCode = @"
private BitMarkdownEditor commandsRef = default!;
private string? getValueResult;

private async Task RunCommand(BitMarkdownEditorCommand command)
{
    await commandsRef.Run(command);
}

private async Task InsertSignature()
{
    await commandsRef.Insert(""\n\n---\n_Written with **BitMarkdownEditor**._\n"");
}

private async Task Undo() => await commandsRef.Undo();

private async Task Redo() => await commandsRef.Redo();

private async Task GetValue()
{
    getValueResult = await commandsRef.GetValue();
}

private async Task ShowSelection()
{
    var selection = await commandsRef.GetSelection();
    getValueResult = $""[{selection.Start}..{selection.End}] {selection.Text}"";
}

private async Task SelectFirstLine()
{
    var value = await commandsRef.GetValue();
    var end = value.IndexOf('\n');
    await commandsRef.SetSelection(0, end < 0 ? value.Length : end);
}";

    private readonly string example7RazorCode = @"
<BitMarkdownEditor DefaultValue=""@previewDefaultValue"" PreviewPipeline=""BitMarkdownPipelines.Basic"" />

<BitMarkdownEditor DefaultValue=""@previewDefaultValue"">
    <PreviewTemplate>
        <pre style=""margin:0;white-space:pre-wrap"">@context</pre>
    </PreviewTemplate>
</BitMarkdownEditor>";
    private readonly string example7CsharpCode = @"
private string previewDefaultValue =
@""GitHub flavored extras like ~~strikethrough~~, https://bitplatform.dev autolinks,

- [ ] task
- [x] lists

| and | tables |
| --- | ------ |
| are | here   |"";";

    private readonly string example8RazorCode = @"
<BitToggleButton @bind-IsChecked=""fullScreen"" OnText=""Exit full-screen"" OffText=""Go full-screen"" />

<BitMarkdownEditor @bind-FullScreen=""fullScreen"" Height=""10rem"" Resizable />";
    private readonly string example8CsharpCode = @"
private bool fullScreen;";

    private readonly string example9RazorCode = @"
<BitMarkdownEditor Placeholder=""Write your story here...""
                   SpellCheck=""false""
                   AutoPair=""false""
                   SyncScroll=""false""
                   ChangeDebounceTime=""300""
                   TabIndents=""false""
                   IndentUnit=""@(""    "")"" />

<BitMarkdownEditor TableColumns=""4"" TableRows=""3"" Mode=""BitMarkdownEditorMode.Edit"" />

<BitMarkdownEditor ReadOnly DefaultValue=""@readOnlyDefaultValue"" />";

    private readonly string example10RazorCode = @"
<div class=""commands-bar"">
    <BitTextField @bind-Value=""findText"" Placeholder=""Find"" Immediate />
    <BitTextField @bind-Value=""replaceText"" Placeholder=""Replace with"" Immediate />
    <BitButton Variant=""BitVariant.Outline"" OnClick=""FindNext"">Find next</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""FindPrevious"">Find previous</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ReplaceAll"">Replace all</BitButton>
</div>

<div>@findStatus</div>

<BitMarkdownEditor @ref=""findRef"" DefaultValue=""@findDefaultValue"" Mode=""BitMarkdownEditorMode.Edit"" />";
    private readonly string example10CsharpCode = @"
private BitMarkdownEditor findRef = default!;
private string? findText = ""markdown"";
private string? replaceText = ""Markdown"";
private string? findStatus;

private async Task FindNext()
{
    var result = await findRef.FindNext(findText ?? string.Empty);
    findStatus = result.HasMatches ? $""{result.Index} of {result.Count}"" : ""No results"";
}

private async Task FindPrevious()
{
    var result = await findRef.FindPrevious(findText ?? string.Empty);
    findStatus = result.HasMatches ? $""{result.Index} of {result.Count}"" : ""No results"";
}

private async Task ReplaceAll()
{
    var count = await findRef.Replace(findText ?? string.Empty, replaceText ?? string.Empty);
    findStatus = $""{count} replaced"";
}";

    private readonly string example11RazorCode = @"
<div>@imageStatus</div>

<BitMarkdownEditor OnImageUpload=""UploadImage""
                   OnImageRejected=""ImageRejected""
                   AcceptedImageTypes=""image/png,image/jpeg,image/gif""
                   MaxImageSize=""1048576""
                   DefaultValue=""@imageDefaultValue"" />";
    private readonly string example11CsharpCode = @"
private string? imageStatus;

private async Task<string?> UploadImage(BitMarkdownEditorImageUploadInfo info)
{
    // A real handler would post the bytes to a storage service and return the url.
    await Task.Delay(500);
    return $""data:{info.ContentType};base64,{Convert.ToBase64String(info.Data)}"";
}

private void ImageRejected(BitMarkdownEditorImageRejection rejection)
{
    imageStatus = rejection.Reason switch
    {
        BitMarkdownEditorImageRejectionReason.Size => $""{rejection.FileName} is too large ({rejection.Size} bytes)."",
        _ => $""{rejection.FileName} is not an accepted image type ({rejection.ContentType}).""
    };
}";

    private readonly string example12RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""ClearDraft"">Clear the draft</BitButton>

<BitMarkdownEditor @ref=""autoSaveRef"" AutoSaveId=""bit-mde-demo-draft"" Placeholder=""This draft survives a page reload..."" />";
    private readonly string example12CsharpCode = @"
private BitMarkdownEditor autoSaveRef = default!;

private async Task ClearDraft()
{
    await autoSaveRef.ClearDraft();
}";

    private readonly string example13RazorCode = @"
<BitMarkdownEditor ShowReadingTime
                   WordsPerMinute=""120""
                   MaxLength=""280""
                   Mode=""BitMarkdownEditorMode.Edit""
                   DefaultValue=""@statusDefaultValue"" />";
    private readonly string example13CsharpCode = @"
private string statusDefaultValue =
@""The status bar counts words and characters 👋 and estimates the reading time."";";

    private readonly string example14RazorCode = @"
<BitMarkdownEditor Texts=""frenchTexts"" DefaultValue=""@localizationDefaultValue"" ShowReadingTime />";
    private readonly string example14CsharpCode = @"
private BitMarkdownEditorTexts frenchTexts = new()
{
    ToolbarAriaLabel = ""Mise en forme Markdown"",
    EditorAriaLabel = ""Éditeur Markdown"",
    PreviewAriaLabel = ""Aperçu Markdown"",
    ToolbarBold = ""Gras"",
    ToolbarItalic = ""Italique"",
    ToolbarHeading = ""Titre"",
    ToolbarQuote = ""Citation"",
    ToolbarLink = ""Lien"",
    ToolbarImage = ""Image"",
    ToolbarTable = ""Tableau"",
    ToolbarFind = ""Rechercher et remplacer"",
    ToolbarHelp = ""Raccourcis clavier"",
    WordsFormat = ""{0} mots"",
    CharsFormat = ""{0} caractères"",
    ReadingTimeFormat = ""{0} min de lecture"",
    ModeEdit = ""Édition"",
    ModeSplit = ""Partagé"",
    ModePreview = ""Aperçu"",
    FindPlaceholder = ""Rechercher"",
    ReplacePlaceholder = ""Remplacer par"",
    ReplaceButton = ""Remplacer"",
    ReplaceAllButton = ""Tout"",
    MatchesFormat = ""{0} sur {1}"",
    NoMatchesText = ""Aucun résultat"",
    UploadingText = ""envoi"",
    KeyboardShortcutsTitle = ""Raccourcis clavier"",
    PreviewEmptyText = ""Rien à prévisualiser pour l'instant."",
};";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        box-shadow: aqua 0 0 1rem 0.5rem;
    }

    .custom-toolbar {
        background: linear-gradient(90deg, #ff7e5f, #feb47b);
    }

    .custom-textarea {
        font-family: 'Courier New', monospace;
        color: mediumseagreen;
    }
</style>

<BitMarkdownEditor Style=""border-color:brown;border-radius:1rem;overflow:hidden"" Class=""custom-class"" />

<BitMarkdownEditor Classes=""@(new() { Toolbar = ""custom-toolbar"", TextArea = ""custom-textarea"" })""
                   Styles=""@(new() { StatusBar = ""color:tomato;font-weight:bold"" })"" />";

    private readonly string example16RazorCode = @"
<BitMarkdownEditor Dir=""BitDir.Rtl"" DefaultValue=""@rtlDefaultValue"" />";
}
