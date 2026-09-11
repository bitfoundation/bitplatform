using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.RichTextEditor;

public partial class BitRichTextEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Automatically moves keyboard focus into the editor after the first render."
        },
        new()
        {
            Name = "AutoLink",
            Type = "bool",
            DefaultValue = "true",
            Description = "Turns a URL typed into the editor into a link as soon as the word is finished."
        },
        new()
        {
            Name = "Classes",
            Type = "BitRichTextEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the rich text editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "DebounceMs",
            Type = "int",
            DefaultValue = "200",
            Description = "Debounce window (ms) for content-change notifications while typing. Negative values are treated as 0."
        },
        new()
        {
            Name = "FontFamilies",
            Type = "IReadOnlyList<string>?",
            DefaultValue = "null",
            Description = "Font families offered in the font-family selector. Null/empty uses defaults."
        },
        new()
        {
            Name = "FontSizes",
            Type = "IReadOnlyList<string>?",
            DefaultValue = "null",
            Description = "Font sizes offered in the font-size selector. Null/empty uses defaults."
        },
        new()
        {
            Name = "Height",
            Type = "string",
            DefaultValue = "300px",
            Description = "Minimum height of the editing surface (any CSS length)."
        },
        new()
        {
            Name = "KeyboardShortcuts",
            Type = "IReadOnlyDictionary<string, string>?",
            DefaultValue = "null",
            Description = "Custom key-combo to command map, merged over the built-in defaults."
        },
        new()
        {
            Name = "Localizer",
            Type = "IBitRichTextEditorLocalizer?",
            DefaultValue = "null",
            Description = "Localized labels/tooltips provider. Null uses built-in English labels.",
            LinkType = LinkType.Link,
            Href = "#localizer"
        },
        new()
        {
            Name = "MaxHeight",
            Type = "string?",
            DefaultValue = "null",
            Description = "Maximum height of the editing surface (any CSS length). Content beyond it scrolls inside the editor."
        },
        new()
        {
            Name = "MaxLength",
            Type = "int?",
            DefaultValue = "null",
            Description = "Maximum plain-text character count. Null means unlimited."
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the editor loses focus."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Callback for when the editor content changes."
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback<BitRichTextEditorError>",
            DefaultValue = "",
            Description = "Callback for when the editor encounters a recoverable error.",
            LinkType = LinkType.Link,
            Href = "#editor-error"
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when the editor gains focus."
        },
        new()
        {
            Name = "OnMentionSearch",
            Type = "Func<string, Task<IReadOnlyList<BitRichTextEditorMention>>>?",
            DefaultValue = "null",
            Description = "Supplies the suggestions shown after the user types \"@\". Leaving it null disables mentions.",
            LinkType = LinkType.Link,
            Href = "#mention"
        },
        new()
        {
            Name = "OnMentionSelected",
            Type = "EventCallback<BitRichTextEditorMention>",
            DefaultValue = "",
            Description = "Callback for when a mention is picked from the menu.",
            LinkType = LinkType.Link,
            Href = "#mention"
        },
        new()
        {
            Name = "OnImageUpload",
            Type = "Func<BitRichTextEditorImageUpload, Task<string?>>?",
            DefaultValue = "null",
            Description = "Invoked to persist an image binary, returning the URL to embed. When null, dropped or pasted images are embedded as inline data URLs.",
            LinkType = LinkType.Link,
            Href = "#image-upload"
        },
        new()
        {
            Name = "OnSelectionChange",
            Type = "EventCallback<BitRichTextEditorSelectionState>",
            DefaultValue = "",
            Description = "Callback for when the selection - or the formatting under it - changes; receives the same snapshot the toolbar highlights itself from.",
            LinkType = LinkType.Link,
            Href = "#selection-state"
        },
        new()
        {
            Name = "PasteAsPlainText",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, pasted content is inserted as plain text."
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The placeholder value of the editor shown while it is empty."
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the editor readonly."
        },
        new()
        {
            Name = "Resizable",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the reader drag the bottom edge of the editing surface to make it taller or shorter."
        },
        new()
        {
            Name = "SanitizationPolicy",
            Type = "BitRichTextEditorSanitizationPolicy?",
            DefaultValue = "null",
            Description = "Allowlist policy applied to all content. When null a secure default allowlist is applied.",
            LinkType = LinkType.Link,
            Href = "#sanitization-policy"
        },
        new()
        {
            Name = "ShowCount",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show the character/word count footer."
        },
        new()
        {
            Name = "ShowQuickToolbar",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether a small formatting toolbar floats next to the current text selection."
        },
        new()
        {
            Name = "ShowToolbar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the formatting toolbar is shown."
        },
        new()
        {
            Name = "SmartTypography",
            Type = "bool",
            DefaultValue = "false",
            Description = "Replaces common text patterns with their typographic characters as they are typed: curly quotes, em dash, ellipsis, copyright/registered/trademark signs, fractions, arrows and comparison symbols."
        },
        new()
        {
            Name = "SpellCheck",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the browser's native spell checking runs over the editor content."
        },
        new()
        {
            Name = "Styles",
            Type = "BitRichTextEditorClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the rich text editor.",
            LinkType = LinkType.Link,
            Href = "#class-styles"
        },
        new()
        {
            Name = "Toolbar",
            Type = "BitRichTextEditorToolbar",
            DefaultValue = "BitRichTextEditorToolbar.All",
            Description = "Which toolbar groups to display.",
            LinkType = LinkType.Link,
            Href = "#toolbar-enum"
        },
        new()
        {
            Name = "ToolbarConfig",
            Type = "BitRichTextEditorToolbarConfig?",
            DefaultValue = "null",
            Description = "Custom toolbar items and ordering. Null uses the default group order.",
            LinkType = LinkType.Link,
            Href = "#toolbar-config"
        },
        new()
        {
            Name = "Value",
            Type = "string?",
            DefaultValue = "null",
            Description = "The two-way bound HTML content of the editor."
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "CharacterCount",
            Type = "int",
            Description = "The plain-text character count of the current content, as the count footer and MaxLength count it."
        },
        new()
        {
            Name = "ClearAsync",
            Type = "Task",
            Description = "Clears the editor content."
        },
        new()
        {
            Name = "ExecuteCommandAsync",
            Type = "Task",
            Description = "Runs a raw editing command against the editor."
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Moves keyboard focus into the editor."
        },
        new()
        {
            Name = "GetHtmlAsync",
            Type = "ValueTask<string>",
            Description = "Returns the current HTML content of the editor."
        },
        new()
        {
            Name = "GetSelectedTextAsync",
            Type = "ValueTask<string>",
            Description = "Returns the plain text of the current selection, or an empty string when nothing inside the editor is selected."
        },
        new()
        {
            Name = "GetTextAsync",
            Type = "ValueTask<string>",
            Description = "Returns the current content of the editor as plain text, with block boundaries rendered as newlines and all markup removed."
        },
        new()
        {
            Name = "InsertHtmlAsync",
            Type = "Task",
            Description = "Inserts HTML at the current caret position, after running it through the active sanitization policy."
        },
        new()
        {
            Name = "InsertTextAsync",
            Type = "Task",
            Description = "Inserts plain text at the current caret position, honoring MaxLength."
        },
        new()
        {
            Name = "IsEmpty",
            Type = "bool",
            Description = "Whether the editor holds nothing a reader would see - no text and none of the elements that are content without carrying text (images, tables, rules, media)."
        },
        new()
        {
            Name = "RedoAsync",
            Type = "Task",
            Description = "Redoes the last undone edit."
        },
        new()
        {
            Name = "SelectAllAsync",
            Type = "ValueTask",
            Description = "Selects the whole editor content."
        },
        new()
        {
            Name = "SetHtmlAsync",
            Type = "Task",
            Description = "Replaces the whole content with the given HTML (sanitized), or clears it when null/empty."
        },
        new()
        {
            Name = "UndoAsync",
            Type = "Task",
            Description = "Undoes the last edit."
        },
        new()
        {
            Name = "WordCount",
            Type = "int",
            Description = "The word count of the current content."
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitRichTextEditorClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root of the BitRichTextEditor." },
                new() { Name = "Toolbar", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar of the BitRichTextEditor." },
                new() { Name = "Group", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar groups of the BitRichTextEditor." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar buttons of the BitRichTextEditor." },
                new() { Name = "Editor", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the editor (content) area of the BitRichTextEditor." },
                new() { Name = "Source", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the HTML source view textarea of the BitRichTextEditor." },
                new() { Name = "Count", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the character/word count footer of the BitRichTextEditor." },
            ]
        },
        new()
        {
            Id = "toolbar-config",
            Title = "BitRichTextEditorToolbarConfig",
            Description = "Configures toolbar ordering and custom items.",
            Parameters =
            [
                new() { Name = "Order", Type = "IReadOnlyList<string>?", DefaultValue = "null", Description = "Explicit ordering of toolbar entry ids (built-in group ids and custom item ids). Use BitRichTextEditorToolbarConfig.GroupIds for the built-in ids." },
                new() { Name = "CustomItems", Type = "IReadOnlyList<BitRichTextEditorToolbarItem>?", DefaultValue = "null", Description = "Custom toolbar items (max 50 are rendered)." },
            ]
        },
        new()
        {
            Id = "toolbar-item",
            Title = "BitRichTextEditorToolbarItem",
            Description = "A custom toolbar button supplied by the host.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "Unique id used for ordering and lookup. It must not collide with a built-in group id." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Text label shown when no icon is provided." },
                new() { Name = "Icon", Type = "RenderFragment?", DefaultValue = "null", Description = "Optional icon content." },
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "Optional accessible label / tooltip. When omitted, Label is used as the accessible name." },
                new() { Name = "OnActivate", Type = "Func<BitRichTextEditor, Task>", DefaultValue = "", Description = "Action invoked when the item is activated; receives the editor instance." },
            ]
        },
        new()
        {
            Id = "sanitization-policy",
            Title = "BitRichTextEditorSanitizationPolicy",
            Description = "An allowlist sanitization policy. Only the listed tags, attributes, and URI schemes are retained; everything else is removed. The static Default property returns a fresh copy of the built-in policy.",
            Parameters =
            [
                new() { Name = "AllowedTags", Type = "ISet<string>", DefaultValue = "", Description = "Permitted (lowercase) element/tag names." },
                new() { Name = "AllowedAttributes", Type = "IDictionary<string, ISet<string>>", DefaultValue = "", Description = "Permitted attributes per tag name. Use the key \"*\" for attributes allowed on any tag. A permitted style attribute is filtered further, down to presentational CSS properties." },
                new() { Name = "AllowedUriSchemes", Type = "ISet<string>", DefaultValue = "", Description = "Permitted URI schemes for href/src attributes (e.g. http, https, mailto)." },
                new() { Name = "AllowDataImageUris", Type = "bool", DefaultValue = "true", Description = "Whether data: image URIs are permitted in image sources." },
                new() { Name = "AllowedIframeHosts", Type = "IReadOnlyCollection<string>?", DefaultValue = "null", Description = "Hosts an iframe may point at, over https. Null applies the built-in approved embed hosts (YouTube, YouTube-nocookie, Vimeo); an empty collection permits no iframe at all; a single \"*\" entry lifts the restriction and lets any https source through." },
            ]
        },
        new()
        {
            Id = "image-upload",
            Title = "BitRichTextEditorImageUpload",
            Description = "An image to be persisted by the host's OnImageUpload delegate.",
            Parameters =
            [
                new() { Name = "FileName", Type = "string", DefaultValue = "", Description = "Original file name, when available." },
                new() { Name = "ContentType", Type = "string", DefaultValue = "", Description = "MIME type, e.g. \"image/png\"." },
                new() { Name = "Content", Type = "byte[]", DefaultValue = "", Description = "Raw image bytes." },
            ]
        },
        new()
        {
            Id = "selection-state",
            Title = "BitRichTextEditorSelectionState",
            Description = "Snapshot of the formatting under the selection, reported to OnSelectionChange and used to highlight the toolbar.",
            Parameters =
            [
                new() { Name = "Bold", Type = "bool", DefaultValue = "false", Description = "Whether the selection is bold." },
                new() { Name = "Italic", Type = "bool", DefaultValue = "false", Description = "Whether the selection is italic." },
                new() { Name = "Underline", Type = "bool", DefaultValue = "false", Description = "Whether the selection is underlined." },
                new() { Name = "StrikeThrough", Type = "bool", DefaultValue = "false", Description = "Whether the selection is struck through." },
                new() { Name = "InlineCode", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits inside an inline code span (not a code block)." },
                new() { Name = "Subscript", Type = "bool", DefaultValue = "false", Description = "Whether the selection is subscript." },
                new() { Name = "Superscript", Type = "bool", DefaultValue = "false", Description = "Whether the selection is superscript." },
                new() { Name = "OrderedList", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits in a numbered list." },
                new() { Name = "UnorderedList", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits in a bulleted list." },
                new() { Name = "TaskList", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits in a checklist item." },
                new() { Name = "JustifyLeft", Type = "bool", DefaultValue = "false", Description = "Whether the block is aligned left." },
                new() { Name = "JustifyCenter", Type = "bool", DefaultValue = "false", Description = "Whether the block is centered." },
                new() { Name = "JustifyRight", Type = "bool", DefaultValue = "false", Description = "Whether the block is aligned right." },
                new() { Name = "JustifyFull", Type = "bool", DefaultValue = "false", Description = "Whether the block is justified." },
                new() { Name = "Block", Type = "string", DefaultValue = "\"\"", Description = "The current block tag (\"p\", \"h1\", \"blockquote\", \"pre\", ...), lowercase." },
                new() { Name = "Direction", Type = "string?", DefaultValue = "null", Description = "Text direction of the selected block (\"ltr\"/\"rtl\"), or null." },
                new() { Name = "ForeColor", Type = "string?", DefaultValue = "null", Description = "Active text color of the selection, or null when mixed/none." },
                new() { Name = "BackColor", Type = "string?", DefaultValue = "null", Description = "Active highlight color of the selection, or null when mixed/none." },
                new() { Name = "FontName", Type = "string?", DefaultValue = "null", Description = "Active font family, or null when the selection spans several." },
                new() { Name = "FontSize", Type = "string?", DefaultValue = "null", Description = "Active font size as a CSS length, or null when the selection spans several." },
                new() { Name = "InLink", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits inside a hyperlink." },
                new() { Name = "LinkHref", Type = "string?", DefaultValue = "null", Description = "The href of the link under the selection, or null when none/multiple." },
                new() { Name = "LinkNewTab", Type = "bool", DefaultValue = "false", Description = "Whether that link opens in a new tab (target=\"_blank\")." },
                new() { Name = "InTable", Type = "bool", DefaultValue = "false", Description = "Whether the selection sits inside a table cell, which is what enables the table operations." },
                new() { Name = "ImageSelected", Type = "bool", DefaultValue = "false", Description = "Whether an image inside the editor is selected." },
                new() { Name = "ImageAlign", Type = "string?", DefaultValue = "null", Description = "Alignment of the selected image (\"left\", \"center\", \"right\"), or null when it flows inline." },
                new() { Name = "ImageSrc", Type = "string?", DefaultValue = "null", Description = "Source of the selected image, or null when no image is selected." },
                new() { Name = "ImageAlt", Type = "string?", DefaultValue = "null", Description = "Alternative text of the selected image (empty when it has none), or null when no image is selected." },
                new() { Name = "HasSelection", Type = "bool", DefaultValue = "false", Description = "Whether a non-empty range inside the editor is selected." },
                new() { Name = "SelectionTop", Type = "double", DefaultValue = "0", Description = "Top of the selection rectangle, in pixels relative to the component root." },
                new() { Name = "SelectionLeft", Type = "double", DefaultValue = "0", Description = "Left edge of the selection rectangle, in pixels relative to the component root." },
                new() { Name = "SelectionWidth", Type = "double", DefaultValue = "0", Description = "Width of the selection rectangle in pixels." },
                new() { Name = "SelectionHeight", Type = "double", DefaultValue = "0", Description = "Height of the selection rectangle in pixels." },
            ]
        },
        new()
        {
            Id = "mention",
            Title = "BitRichTextEditorMention",
            Description = "A single suggestion offered by the mention menu.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "Stable identifier written into the inserted markup as data-mention-id." },
                new() { Name = "Display", Type = "string", DefaultValue = "", Description = "The text shown in the menu and inserted after the trigger character." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Optional secondary line shown under the display text in the menu." },
            ]
        },
        new()
        {
            Id = "editor-error",
            Title = "BitRichTextEditorError",
            Description = "An error surfaced by the editor (e.g. invalid URL, failed upload, invalid HTML).",
            Parameters =
            [
                new() { Name = "Code", Type = "string", DefaultValue = "", Description = "Stable error code, e.g. \"invalid-url\"." },
                new() { Name = "Message", Type = "string", DefaultValue = "", Description = "Human-readable description." },
            ]
        },
        new()
        {
            Id = "localizer",
            Title = "IBitRichTextEditorLocalizer",
            Description = "Provides localized labels and tooltips for the editor's controls.",
            Parameters =
            [
                new() { Name = "this[string key]", Type = "string?", DefaultValue = "", Description = "Returns the localized string for the given key, or null to use the built-in English default." },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "toolbar-enum",
            Name = "BitRichTextEditorToolbar",
            Description = "Toolbar button groups. Combine with bitwise OR, or use All / AllExtended.",
            Items =
            [
                new() { Name = "None", Value = "0" },
                new() { Name = "History", Value = "1" },
                new() { Name = "BlockFormat", Value = "2" },
                new() { Name = "Inline", Value = "4" },
                new() { Name = "Lists", Value = "8" },
                new() { Name = "Blocks", Value = "16" },
                new() { Name = "Link", Value = "32" },
                new() { Name = "Alignment", Value = "64" },
                new() { Name = "Clear", Value = "128" },
                new() { Name = "Image", Value = "256" },
                new() { Name = "Color", Value = "512" },
                new() { Name = "Font", Value = "1024" },
                new() { Name = "Indent", Value = "2048" },
                new() { Name = "Script", Value = "4096" },
                new() { Name = "Source", Value = "8192" },
                new() { Name = "Table", Value = "16384" },
                new() { Name = "Media", Value = "32768" },
                new() { Name = "Rule", Value = "65536" },
                new() { Name = "Emoji", Value = "131072" },
                new() { Name = "Find", Value = "262144" },
                new() { Name = "FullScreen", Value = "524288" },
                new() { Name = "Direction", Value = "1048576" },
                new() { Name = "All", Value = "255" },
                new() { Name = "AllExtended", Value = "2097151" },
            ]
        }
    ];



    private readonly string readOnlyHtml = "<p>This instance is <strong>read-only</strong> with the toolbar hidden - useful for displaying stored content.</p>";

    private readonly string disabledHtml = "<p>This instance is <strong>disabled</strong>: the toolbar and the surface both refuse input.</p>";

    private string? bindingHtml = "<p>The bound value is just a <strong>string</strong> you own.</p>";

    private string? debounceHtml = "<p>Type and watch the value update after the debounce window.</p>";

    private string focusState = "blurred";

    private string? formattingHtml = "<h2>Headings</h2><p>Mix <strong>bold</strong>, <em>italic</em>, <u>underline</u>, <s>strikethrough</s> and <code>inline code</code>.</p><blockquote>A short quote.</blockquote><ol><li>First</li><li>Second</li></ol><ul class=\"bit-rte-tasks\"><li data-checked=\"true\">A finished task</li><li data-checked=\"false\">Something still to do</li></ul>";

    private string? scriptHtml = "<p>Water is H<sub>2</sub>O. Einstein wrote E = mc<sup>2</sup>.</p><ul><li>Put the caret here and press Tab to nest this item.</li></ul>";

    private string? linkHtml = "<p>Read the <a href=\"https://learn.microsoft.com/aspnet/core/blazor\">Blazor docs</a> to learn more.</p>";
    private string? linkError;

    private void HandleLinkHtmlChanged(string? value)
    {
        linkHtml = value;
        // A successful content update means the previous error no longer applies, so clear the
        // stale message that OnError left behind.
        linkError = null;
    }

    private string? imageHtml = "<p>Images can sit inline with text.</p>";
    private string? lastUpload;
    private Task<string?> HandleImageUpload(BitRichTextEditorImageUpload image)
    {
        lastUpload = $"{image.FileName} ({image.ContentType}, {image.Content.Length:N0} bytes)";
        var dataUrl = $"data:{image.ContentType};base64,{Convert.ToBase64String(image.Content)}";
        return Task.FromResult<string?>(dataUrl);
    }

    private string? colorHtml = "<p>Make words <span style=\"color:#5b3df5\">colorful</span> or <span style=\"background-color:#fff3a3\">highlighted</span>.</p>";

    private string? fontHtml = "<p>Choose a typeface for this paragraph.</p>";
    private readonly string[] fonts = ["Segoe UI", "Georgia", "Courier New", "Comic Sans MS"];
    private readonly string[] sizes = ["12px", "16px", "20px", "28px"];

    private string? tableHtml = "<table><thead><tr><th>Feature</th><th>Status</th></tr></thead><tbody><tr><td>Tables</td><td>Ready</td></tr><tr><td>Merge &amp; split cells</td><td>Ready</td></tr></tbody></table>";

    private string? mediaHtml = "<p>Add a divider below, then embed a video.</p><hr><p>Next section.</p>";

    private string? sourceHtml = "<p>Switch to <strong>source view</strong> to see and edit the raw HTML.</p>";

    private string? sanitizationHtml = "<p>Only allowlisted tags, attributes and URI schemes survive.</p>";
    private readonly BitRichTextEditorSanitizationPolicy sanitizationPolicy = new()
    {
        AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p", "br", "strong", "em", "a", "ul", "ol", "li" },
        AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["*"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class" },
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href", "title" },
        },
        AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" },
        AllowDataImageUris = false,
    };

    private string? plainPasteHtml = "<p>Try pasting formatted content here.</p>";

    private string? findHtml = "<p>The quick brown fox jumps over the lazy dog. The fox is quick, and the foxes are quicker.</p>";

    private string? fullScreenHtml = "<p>Expand me to full screen and write without distractions.</p>";

    private string? slashHtml = "<p>Place the cursor on a new line and type a slash, or start a line with # or -.</p>";

    private string? shortcutHtml = "<p>Press your custom Ctrl/Cmd+Shift+S, Ctrl/Cmd+Shift+L, or Ctrl/Cmd+Shift+1.</p>";
    private readonly Dictionary<string, string> shortcuts = new()
    {
        ["ctrl+shift+s"] = "strikeThrough",
        ["ctrl+shift+l"] = "insertUnorderedList",
        ["ctrl+shift+1"] = "h1",
    };

    private string? emojiHtml = "<p>Add a little ✨ to your text — or a → arrow, a ½ fraction, or π.</p>";

    private string? countHtml = "<p>Counting characters and words.</p>";

    private readonly FormModel formModel = new();
    private bool formSubmitted;
    private void HandleValidSubmit()
    {
        formSubmitted = true;
    }
    // Clear the submitted banner whenever the bound content changes so the "submitted" state does
    // not linger after the user edits the body again.
    private void HandleFormBodyChanged(string? value)
    {
        formModel.Body = value;
        formSubmitted = false;
    }
    public class FormModel : IValidatableObject
    {
        [Required(ErrorMessage = "The body is required.")]
        public string? Body { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // The bound value is HTML, so measure normalized visible text: strip tags, decode
            // entities, and trim. Markup with no visible text (e.g. "<p></p>") passes [Required]
            // because the raw HTML is non-empty, so require non-whitespace visible text here and
            // base the min-length check on that same normalized text.
            var stripped = System.Text.RegularExpressions.Regex.Replace(Body ?? "", "<[^>]+>", "");
            var text = System.Net.WebUtility.HtmlDecode(stripped).Trim();
            if (string.IsNullOrWhiteSpace(Body) is false && text.Length == 0)
            {
                yield return new ValidationResult("The body is required.", [nameof(Body)]);
            }
            else if (text.Length > 0 && text.Length < 20)
            {
                yield return new ValidationResult("Add a bit more detail (min 20 characters).", [nameof(Body)]);
            }
        }
    }

    private string? customHtml = "<p>A custom toolbar button can run any command.</p>";
    private readonly BitRichTextEditorToolbarConfig customConfig = new()
    {
        CustomItems =
        [
            new()
            {
                Id = "insert-date",
                Label = "Today",
                AriaLabel = "Insert today's date",
                OnActivate = editor => editor.InsertTextAsync(DateTime.Now.ToString("yyyy-MM-dd"))
            }
        ]
    };

    private string? reorderHtml = "<p>The inline, lists and link groups are pulled to the front.</p>";
    private readonly BitRichTextEditorToolbarConfig reorderConfig = new()
    {
        Order =
        [
            BitRichTextEditorToolbarConfig.GroupIds.Inline,
            BitRichTextEditorToolbarConfig.GroupIds.Lists,
            BitRichTextEditorToolbarConfig.GroupIds.Link
        ]
    };

    private string? localizedHtml = "<p>Pase el cursor sobre los botones para ver las descripciones traducidas.</p>";
    private readonly SpanishEditorLocalizer localizer = new();
    // Only the keys present here are translated; every other key falls back to the built-in
    // English text, so a partial dictionary is a valid localizer.
    public class SpanishEditorLocalizer : IBitRichTextEditorLocalizer
    {
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["toolbar"] = "Formato",
            ["editor"] = "Editor de texto enriquecido",
            ["undo"] = "Deshacer (Ctrl+Z)",
            ["redo"] = "Rehacer (Ctrl+Y)",
            ["bold"] = "Negrita (Ctrl+B)",
            ["italic"] = "Cursiva (Ctrl+I)",
            ["underline"] = "Subrayado (Ctrl+U)",
            ["strikethrough"] = "Tachado",
            ["paragraph-format"] = "Formato de párrafo",
            ["block-normal"] = "Normal",
            ["heading-1"] = "Título 1",
            ["heading-2"] = "Título 2",
            ["heading-3"] = "Título 3",
            ["bullet-list"] = "Lista con viñetas",
            ["numbered-list"] = "Lista numerada",
            ["task-list"] = "Lista de tareas",
            ["quote"] = "Cita",
            ["code-block"] = "Bloque de código",
            ["link"] = "Insertar o editar enlace",
            ["find-replace"] = "Buscar y reemplazar",
            ["find"] = "Buscar",
            ["replace"] = "Reemplazar",
            ["no-matches"] = "Sin coincidencias",
            ["clear-formatting"] = "Borrar formato",
        };

        public string? this[string key] => Labels.TryGetValue(key, out var value) ? value : null;
    }

    private BitRichTextEditor apiEditor = default!;
    private string? apiHtml = "<p>Drive me from the buttons below.</p>";
    private string? apiResult;
    private async Task FocusEditor()
    {
        await apiEditor.FocusAsync();
    }
    private async Task GetEditorHtml()
    {
        apiResult = await apiEditor.GetHtmlAsync();
    }
    private async Task GetEditorText()
    {
        apiResult = await apiEditor.GetTextAsync();
    }
    private void ShowEditorFacts()
    {
        apiResult = $"{apiEditor.WordCount} words, {apiEditor.CharacterCount} chars, empty: {apiEditor.IsEmpty}";
    }

    private async Task GetEditorSelection()
    {
        apiResult = await apiEditor.GetSelectedTextAsync();
    }

    private string? quickHtml = "<p>Select any part of this sentence and the formatting bar appears right above it.</p>";

    private string? mentionHtml = "<p>Type @ to bring up the people picker.</p>";
    private string? lastMention;
    private static readonly BitRichTextEditorMention[] people =
    [
        new("1", "Ada Lovelace", "Analytical engine"),
        new("2", "Alan Turing", "Computing machinery"),
        new("3", "Grace Hopper", "Compilers"),
        new("4", "Katherine Johnson", "Orbital mechanics"),
        new("5", "Margaret Hamilton", "Flight software"),
    ];
    private Task<IReadOnlyList<BitRichTextEditorMention>> SearchMentions(string term)
    {
        IReadOnlyList<BitRichTextEditorMention> matches = string.IsNullOrWhiteSpace(term)
            ? people
            : people.Where(p => p.Display.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
        return Task.FromResult(matches);
    }

    private string? selectionHtml = "<h2>A heading</h2><p>Some <strong>bold</strong> text with a <a href=\"https://example.com\">link</a> in it.</p>";
    private string selectionSummary = "nothing yet";

    private void HandleSelectionChange(BitRichTextEditorSelectionState state)
    {
        var parts = new List<string>();
        if (string.IsNullOrEmpty(state.Block) is false) parts.Add(state.Block);
        if (state.Bold) parts.Add("bold");
        if (state.Italic) parts.Add("italic");
        if (state.InLink) parts.Add("a link");
        if (state.InTable) parts.Add("a table cell");
        if (state.ImageSelected) parts.Add("an image");
        selectionSummary = parts.Count > 0 ? string.Join(", ", parts) : "nothing yet";
    }

    private string? smartTypographyHtml ="<p>Type: \"quoted words\", an em dash -- like this, an ellipsis ... , (c) 2026, 1/2 a cup, -> and >= .</p>";

    private string? rtlHtml = "<p>این ویرایشگر از راست به چپ چیده شده است.</p><p dir=\"ltr\">And this block is left-to-right.</p>";



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";

    private readonly string example2RazorCode = @"
<BitRichTextEditor Placeholder=""Write something...""
                   Height=""10rem"" MaxHeight=""14rem"" SpellCheck=""false"" Resizable />";

    private readonly string example3RazorCode = @"
<BitRichTextEditor Value=""<p>This is <strong>read-only</strong>.</p>""
                   ReadOnly ShowToolbar=""false"" Height=""auto"" />

<BitRichTextEditor Value=""<p>This is <strong>disabled</strong>.</p>""
                   IsEnabled=""false"" Height=""auto""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists"" />";

    private readonly string example4RazorCode = @"
<BitButton OnClick='() => bindingHtml = ""<h3>Set from code</h3>""'>Set content</BitButton>
<BitButton OnClick=""() => bindingHtml = string.Empty"">Clear</BitButton>
<BitButton OnClick='() => bindingHtml += ""<p>Appended.</p>""'>Append</BitButton>

<BitRichTextEditor @bind-Value=""bindingHtml"" Placeholder=""Edit me..."" />

<pre>@bindingHtml</pre>";
    private readonly string example4CsharpCode = @"
private string? bindingHtml = ""<p>The bound value is just a <strong>string</strong> you own.</p>"";";

    private readonly string example5RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" DebounceMs=""600""
                   Placeholder=""Change notifications are debounced by 600ms..."" />";

    private readonly string example6RazorCode = @"
<BitRichTextEditor OnFocus='() => focusState = ""focused""'
                   OnBlur='() => focusState = ""blurred""' />

<div>Editor is currently: <b>@focusState</b></div>";
    private readonly string example6CsharpCode = @"
private string focusState = ""blurred"";";

    private readonly string example7RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" Toolbar=""BitRichTextEditorToolbar.All"" />";

    private readonly string example8RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists |
                            BitRichTextEditorToolbar.Indent | BitRichTextEditorToolbar.Script"" />";

    private readonly string example9RazorCode = @"
<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists"" />";

    private readonly string example10RazorCode = @"
<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.AllExtended"" />";

    private readonly string example11RazorCode = @"
<BitRichTextEditor Value=""@linkHtml"" ValueChanged=""HandleLinkHtmlChanged""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Link""
                   OnError='e => linkError = $""{e.Code}: {e.Message}""' />";
    private readonly string example11CsharpCode = @"
private string? linkHtml = ""<p>Read the <a href=\""https://...\"">docs</a>.</p>"";
private string? linkError;

private void HandleLinkHtmlChanged(string? value)
{
    linkHtml = value;
    linkError = null; // a successful update clears the stale error
}";

    private readonly string example12RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Image | BitRichTextEditorToolbar.Inline""
                   OnImageUpload=""HandleImageUpload"" />";
    private readonly string example12CsharpCode = @"
private async Task<string?> HandleImageUpload(BitRichTextEditorImageUpload image)
{
    // image.FileName, image.ContentType, image.Content (byte[])
    var url = await storage.SaveAsync(image.FileName, image.Content);
    return url; // return null to cancel the insert
}";

    private readonly string example13RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Color | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example14RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Font""
                   FontFamilies=""fonts""
                   FontSizes=""sizes"" />";
    private readonly string example14CsharpCode = @"
private readonly string[] fonts = [""Segoe UI"", ""Georgia"", ""Courier New"", ""Comic Sans MS""];
private readonly string[] sizes = [""12px"", ""16px"", ""20px"", ""28px""];";

    private readonly string example15RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Table | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example16RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Media | BitRichTextEditorToolbar.Rule |
                            BitRichTextEditorToolbar.BlockFormat"" />";

    private readonly string example17RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Source"" />";

    private readonly string example18RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Source""
                   SanitizationPolicy=""sanitizationPolicy"" />";
    private readonly string example18CsharpCode = @"
private readonly BitRichTextEditorSanitizationPolicy sanitizationPolicy = new()
{
    AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ""p"", ""br"", ""strong"", ""em"", ""a"", ""ul"", ""ol"", ""li"" },
    AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        [""*""] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""class"" },
        [""a""] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""href"", ""title"" },
    },
    AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { ""http"", ""https"", ""mailto"" },
    AllowDataImageUris = false,
};

// BitRichTextEditorSanitizationPolicy.Default returns a fresh copy of the built-in policy,
// so a custom policy can start from it and only add or remove what it needs.";

    private readonly string example19RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" PasteAsPlainText />";

    private readonly string example20RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example21RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.FullScreen"" />";

    private readonly string example22RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" Toolbar=""BitRichTextEditorToolbar.AllExtended"" />

<!-- set AutoLink=""false"" to stop a typed URL from becoming a link -->";

    private readonly string example23RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" KeyboardShortcuts=""shortcuts"" />";
    private readonly string example23CsharpCode = @"
private readonly Dictionary<string, string> shortcuts = new()
{
    [""ctrl+shift+s""] = ""strikeThrough"",
    [""ctrl+shift+l""] = ""insertUnorderedList"",
    // A paragraph format works as a shortcut command too, and toggles like the toolbar button.
    [""ctrl+shift+1""] = ""h1"",
};";

    private readonly string example24RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Emoji | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example25RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" ShowCount MaxLength=""120"" />";

    private readonly string example26RazorCode = @"
<EditForm Model=""formModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitRichTextEditor @bind-Value=""formModel.Body""
                       Toolbar=""BitRichTextEditorToolbar.All""
                       ShowCount MaxLength=""500"" />
    <ValidationMessage For=""() => formModel.Body"" />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example26CsharpCode = @"
private readonly FormModel formModel = new();
private bool formSubmitted;
private void HandleValidSubmit() => formSubmitted = true;

public class FormModel : System.ComponentModel.DataAnnotations.IValidatableObject
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = ""The body is required."")]
    public string? Body { get; set; }

    public System.Collections.Generic.IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(System.ComponentModel.DataAnnotations.ValidationContext validationContext)
    {
        // Body is HTML, so validate the normalized visible text: strip tags, decode entities,
        // and trim. Markup with no visible text passes [Required] (the raw HTML is non-empty),
        // so require non-whitespace visible text and base the min-length check on it too.
        var stripped = System.Text.RegularExpressions.Regex.Replace(Body ?? """", ""<[^>]+>"", """");
        var text = System.Net.WebUtility.HtmlDecode(stripped).Trim();
        if (string.IsNullOrWhiteSpace(Body) is false && text.Length == 0)
            yield return new System.ComponentModel.DataAnnotations.ValidationResult(""The body is required."", [nameof(Body)]);
        else if (text.Length > 0 && text.Length < 20)
            yield return new System.ComponentModel.DataAnnotations.ValidationResult(""Add a bit more detail (min 20 characters)."", [nameof(Body)]);
    }
}";

    private readonly string example27RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All""
                   ToolbarConfig=""customConfig"" />";
    private readonly string example27CsharpCode = @"
private readonly BitRichTextEditorToolbarConfig customConfig = new()
{
    CustomItems =
    [
        new()
        {
            Id = ""insert-date"",
            Label = ""Today"",
            AriaLabel = ""Insert today's date"",
            OnActivate = editor => editor.InsertTextAsync(DateTime.Now.ToString(""yyyy-MM-dd""))
        }
    ]
};";

    private readonly string example28RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All""
                   ToolbarConfig=""reorderConfig"" />";
    private readonly string example28CsharpCode = @"
// listed ids appear first; enabled-but-omitted groups follow in default order
private readonly BitRichTextEditorToolbarConfig reorderConfig = new()
{
    Order =
    [
        BitRichTextEditorToolbarConfig.GroupIds.Inline,
        BitRichTextEditorToolbarConfig.GroupIds.Lists,
        BitRichTextEditorToolbarConfig.GroupIds.Link
    ]
};";

    private readonly string example29RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Find""
                   Localizer=""localizer"" />";
    private readonly string example29CsharpCode = @"
private readonly SpanishEditorLocalizer localizer = new();

// Only the keys present here are translated; every other key falls back to the built-in
// English text, so a partial dictionary is a valid localizer.
public class SpanishEditorLocalizer : IBitRichTextEditorLocalizer
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        [""toolbar""] = ""Formato"",
        [""bold""] = ""Negrita (Ctrl+B)"",
        [""italic""] = ""Cursiva (Ctrl+I)"",
        [""bullet-list""] = ""Lista con viñetas"",
        [""find-replace""] = ""Buscar y reemplazar"",
        [""no-matches""] = ""Sin coincidencias"",
        // ...
    };

    public string? this[string key] => Labels.TryGetValue(key, out var value) ? value : null;
}";

    private readonly string example30RazorCode = @"
<BitRichTextEditor @ref=""apiEditor"" @bind-Value=""html"" Toolbar=""BitRichTextEditorToolbar.All"" />

<BitButton OnClick=""FocusEditor"">FocusAsync</BitButton>
<BitButton OnClick='@(() => apiEditor.ExecuteCommandAsync(""bold""))'>ExecuteCommand(""bold"")</BitButton>
<BitButton OnClick='@(() => apiEditor.InsertTextAsync("" inserted""))'>InsertTextAsync</BitButton>
<BitButton OnClick='@(() => apiEditor.InsertHtmlAsync(""<b>bold</b>""))'>InsertHtmlAsync</BitButton>
<BitButton OnClick=""@(() => apiEditor.SelectAllAsync())"">SelectAllAsync</BitButton>
<BitButton OnClick=""@(() => apiEditor.UndoAsync())"">UndoAsync</BitButton>
<BitButton OnClick=""GetEditorHtml"">GetHtmlAsync</BitButton>
<BitButton OnClick=""GetEditorText"">GetTextAsync</BitButton>
<BitButton OnClick=""GetEditorSelection"">GetSelectedTextAsync</BitButton>
<BitButton OnClick=""ShowEditorFacts"">Counts</BitButton>
<BitButton OnClick=""@(() => apiEditor.ClearAsync())"">ClearAsync</BitButton>

<pre>@apiResult</pre>";
    private readonly string example30CsharpCode = @"
private BitRichTextEditor apiEditor = default!;
private string? apiResult;
private async Task FocusEditor()
{
    await apiEditor.FocusAsync();
}
private async Task GetEditorHtml()
{
    apiResult = await apiEditor.GetHtmlAsync();
}
private async Task GetEditorText()
{
    apiResult = await apiEditor.GetTextAsync();
}
private async Task GetEditorSelection()
{
    apiResult = await apiEditor.GetSelectedTextAsync();
}
private void ShowEditorFacts()
{
    // Kept current by the editor itself - no interop call needed.
    apiResult = $""{apiEditor.WordCount} words, {apiEditor.CharacterCount} chars, empty: {apiEditor.IsEmpty}"";
}";

    private readonly string example31RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" ShowQuickToolbar ShowToolbar=""false"" />";

    private readonly string example32RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   OnMentionSearch=""SearchMentions""
                   OnMentionSelected=""m => lastMention = m.Display"" />";
    private readonly string example32CsharpCode = @"
private static readonly BitRichTextEditorMention[] people =
[
    new(""1"", ""Ada Lovelace"", ""Analytical engine""),
    new(""2"", ""Alan Turing"", ""Computing machinery""),
    new(""3"", ""Grace Hopper"", ""Compilers""),
];

private Task<IReadOnlyList<BitRichTextEditorMention>> SearchMentions(string term)
{
    IReadOnlyList<BitRichTextEditorMention> matches = string.IsNullOrWhiteSpace(term)
        ? people
        : people.Where(p => p.Display.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
    return Task.FromResult(matches);
}";

    private readonly string example33RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" SmartTypography />";

    private readonly string example34RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All""
                   OnSelectionChange=""HandleSelectionChange"" />

<div>Caret is in: <b>@selectionSummary</b></div>";
    private readonly string example34CsharpCode = @"
private string selectionSummary = ""nothing yet"";

private void HandleSelectionChange(BitRichTextEditorSelectionState state)
{
    var parts = new List<string>();
    if (string.IsNullOrEmpty(state.Block) is false) parts.Add(state.Block);
    if (state.Bold) parts.Add(""bold"");
    if (state.Italic) parts.Add(""italic"");
    if (state.InLink) parts.Add(""a link"");
    if (state.InTable) parts.Add(""a table cell"");
    if (state.ImageSelected) parts.Add(""an image"");
    selectionSummary = parts.Count > 0 ? string.Join("", "", parts) : ""nothing yet"";
}";

    private readonly string example35RazorCode = @"
<BitRichTextEditor Styles=""@(new() { Toolbar = ""border-bottom-color: red"", Editor = ""background-color: #fff8e1"" })""
                   Classes=""@(new() { Toolbar = ""custom-rte-toolbar"", Editor = ""custom-rte-editor"" })""
                   Placeholder=""Custom styles and classes applied to the toolbar and editor."" />";

    private readonly string example36RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" Dir=""BitDir.Rtl""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Direction"" />";
}
