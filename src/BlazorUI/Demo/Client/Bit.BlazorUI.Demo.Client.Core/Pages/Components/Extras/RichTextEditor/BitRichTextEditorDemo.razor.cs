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
            Name = "ColorPalette",
            Type = "IReadOnlyList<BitRichTextEditorColor>?",
            DefaultValue = "null",
            Description = "The colors the text and highlight color buttons offer as swatches (plus a custom field). Null or empty keeps the browser's color picker.",
            LinkType = LinkType.Link,
            Href = "#color"
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
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The helper text shown in the footer of the editor; the editing surface is described by it."
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message shown under the editor when its content was rejected. It marks the editor invalid, describes the surface and is announced as it appears."
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
            Type = "string?",
            DefaultValue = "null",
            Description = "The height the editing surface starts at (any CSS length); it grows with the content up to MaxHeight. Null leaves it to --bit-RichTextEditor-height (300px)."
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the content as invalid (error border and aria-invalid) for a rejection that does not come from the cascading EditContext."
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
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The visible label of the editor. It names the editing surface for assistive technologies, and clicking it focuses the text."
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
            Description = "Maximum height of the editing surface (any CSS length); content beyond it scrolls inside the editor. Null leaves it to --bit-RichTextEditor-max-height (unbounded)."
        },
        new()
        {
            Name = "MaxImageSize",
            Type = "long",
            DefaultValue = "10485760",
            Description = "The largest image, in bytes, that a drop or a paste may insert (10 MB). Checked in the browser and again before OnImageUpload; zero or less restores the default."
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
            Description = "Invoked to persist an image binary, returning the URL to embed. While it runs the editor shows and announces the upload (aria-busy on the surface). When null, dropped or pasted images are embedded as inline data URLs.",
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
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the editor as required: aria-required on the surface and an asterisk on the Label."
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
            Name = "StickyToolbar",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the toolbar in view while the page scrolls past a tall editor, --bit-RichTextEditor-toolbar-sticky-offset below the top."
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
            Name = "SelectionState",
            Type = "BitRichTextEditorSelectionState",
            Description = "The formatting under the current selection, the snapshot the toolbar highlights itself from.",
            LinkType = LinkType.Link,
            Href = "#selection-state"
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-RichTextEditor-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text color of the editor." },
        new() { Name = "--bit-RichTextEditor-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Background of the editor and of its floating toolbar and menus." },
        new() { Name = "--bit-RichTextEditor-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Color of the outer border." },
        new() { Name = "--bit-RichTextEditor-border-width", DefaultValue = "var(--bit-shp-brd-width)", Description = "Width of the outer border." },
        new() { Name = "--bit-RichTextEditor-border-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the editor." },
        new() { Name = "--bit-RichTextEditor-divider-color", DefaultValue = "var(--bit-clr-brd-sec)", Description = "Lines between the toolbar, its panels, the surface and the footer, and between the toolbar groups." },
        new() { Name = "--bit-RichTextEditor-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Every focus indicator of the editor. While it is unset, every focus ring held off its element is the library's own --bit-shd-focus-ring; the ones drawn flush against a cell or a field keep this color." },
        new() { Name = "--bit-RichTextEditor-error-color", DefaultValue = "var(--bit-clr-err)", Description = "Border of an invalid editor, the required asterisk, the error message, the inline error and the limit reached." },
        new() { Name = "--bit-RichTextEditor-height", DefaultValue = "spacing(37.5)", Description = "Height the editing surface starts at (the Height parameter sets it on the instance)." },
        new() { Name = "--bit-RichTextEditor-max-height", DefaultValue = "none", Description = "Height past which the surface scrolls (the MaxHeight parameter sets it on the instance)." },
        new() { Name = "--bit-RichTextEditor-padding", DefaultValue = "spacing(1.75) spacing(2)", Description = "Padding of the editing surface and the source view." },
        new() { Name = "--bit-RichTextEditor-font-family", DefaultValue = "inherit", Description = "Font of the content." },
        new() { Name = "--bit-RichTextEditor-font-size", DefaultValue = "inherit", Description = "Font size of the content." },
        new() { Name = "--bit-RichTextEditor-line-height", DefaultValue = "1.6", Description = "Line height of the content and the source view." },
        new() { Name = "--bit-RichTextEditor-placeholder-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Placeholder text of the editing surface." },
        new() { Name = "--bit-RichTextEditor-link-color", DefaultValue = "var(--bit-clr-pri)", Description = "Links in the content." },
        new() { Name = "--bit-RichTextEditor-code-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of code spans, code blocks and the source view." },
        new() { Name = "--bit-RichTextEditor-quote-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Leading rule of a block quote." },
        new() { Name = "--bit-RichTextEditor-table-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Table cell borders and horizontal rules in the content." },
        new() { Name = "--bit-RichTextEditor-mention-color", DefaultValue = "var(--bit-clr-pri-dark)", Description = "Text of a mention chip." },
        new() { Name = "--bit-RichTextEditor-mention-background", DefaultValue = "var(--bit-clr-pri-light)", Description = "Background of a mention chip." },
        new() { Name = "--bit-RichTextEditor-find-background", DefaultValue = "var(--bit-clr-wrn-light)", Description = "Highlight of every find match (the current one takes the primary color)." },
        new() { Name = "--bit-RichTextEditor-toolbar-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the toolbar and its panels." },
        new() { Name = "--bit-RichTextEditor-toolbar-sticky-offset", DefaultValue = "0", Description = "Gap between a StickyToolbar and the top of the scrolling area once it is pinned (the height of a fixed header)." },
        new() { Name = "--bit-RichTextEditor-button-size", DefaultValue = "var(--bit-siz-ctrl-md)", Description = "Height (and minimum width) of a toolbar button and selector." },
        new() { Name = "--bit-RichTextEditor-button-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Glyph of a toolbar button." },
        new() { Name = "--bit-RichTextEditor-button-radius", DefaultValue = "var(--bit-shp-radius-button)", Description = "Corner radius of a toolbar button." },
        new() { Name = "--bit-RichTextEditor-button-hover-background", DefaultValue = "var(--bit-clr-bg-sec-hover)", Description = "Background of a toolbar button, an emoji or a menu item under the pointer." },
        new() { Name = "--bit-RichTextEditor-button-active-color", DefaultValue = "var(--bit-clr-pri)", Description = "Glyph and border of a pressed button (the format at the caret, an open panel) and of the current menu item." },
        new() { Name = "--bit-RichTextEditor-button-active-background", DefaultValue = "var(--bit-clr-bg-pri-active)", Description = "Background of a pressed button and of the current menu item." },
        new() { Name = "--bit-RichTextEditor-description-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Helper text (Description) in the footer." },
        new() { Name = "--bit-RichTextEditor-footer-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Text of the counts in the footer." },
        new() { Name = "--bit-RichTextEditor-footer-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of the footer that holds the description and the counts." },
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
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the label of the BitRichTextEditor." },
                new() { Name = "Toolbar", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar of the BitRichTextEditor." },
                new() { Name = "Group", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar groups of the BitRichTextEditor." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar buttons of the BitRichTextEditor." },
                new() { Name = "Panel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the tool panels (link, image, media, table, find, emoji, color and keyboard help) of the BitRichTextEditor." },
                new() { Name = "QuickToolbar", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the floating selection toolbar of the BitRichTextEditor." },
                new() { Name = "Menu", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the slash command and mention menus of the BitRichTextEditor." },
                new() { Name = "Error", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the inline error banner (a failed command, link or upload) of the BitRichTextEditor." },
                new() { Name = "Editor", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the editor (content) area of the BitRichTextEditor." },
                new() { Name = "Source", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the HTML source view textarea of the BitRichTextEditor." },
                new() { Name = "ErrorMessage", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the error message (the ErrorMessage parameter) of the BitRichTextEditor." },
                new() { Name = "Footer", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the footer row (holding the description and the counts) of the BitRichTextEditor." },
                new() { Name = "Description", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the description (helper text) in the footer of the BitRichTextEditor." },
                new() { Name = "Count", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the character/word counts in the footer of the BitRichTextEditor." },
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
                new() { Name = "IsActive", Type = "Func<BitRichTextEditorSelectionState, bool>?", DefaultValue = "null", Description = "Whether the item shows as pressed (and reports aria-pressed) for the formatting at the caret. Null makes a plain action button." },
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
                new() { Name = "ImageWidth", Type = "int?", DefaultValue = "null", Description = "Width in pixels the selected image was given, or null when it keeps its natural size or no image is selected." },
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
            Id = "color",
            Title = "BitRichTextEditorColor",
            Description = "A color offered as a swatch by the text and highlight color panels.",
            Parameters =
            [
                new() { Name = "Value", Type = "string", DefaultValue = "", Description = "The color applied, as a hex value (#2563eb); a hex value also lets the swatch show it is the color under the caret." },
                new() { Name = "Name", Type = "string?", DefaultValue = "null", Description = "The accessible name and tooltip of the swatch (\"Brand blue\"). Without it the value is read out." },
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
                new() { Name = "Help", Value = "2097152" },
                new() { Name = "All", Value = "255" },
                new() { Name = "AllExtended", Value = "4194303" },
            ]
        }
    ];



    private readonly string readOnlyHtml = "<p>This instance is <strong>read-only</strong>.</p>";

    private readonly string disabledHtml = "<p>This instance is <strong>disabled</strong>.</p>";

    private string? bindingHtml = "<p>The bound value is just a <strong>string</strong> you own.</p>";
    private int changeCount;

    private string? toolbarHtml = "<h2>Every group</h2><p>Scroll the page while this editor is in view: the toolbar stays pinned to the top.</p><ul class=\"bit-rte-tasks\"><li data-checked=\"true\">Try the table, emoji and find buttons</li><li data-checked=\"false\">Toggle full screen</li></ul>";

    private string? customHtml = "<p>The inline group comes first, 'Today' inserts the date and 'Callout' toggles a quote.</p>";
    private readonly BitRichTextEditorToolbarConfig customConfig = new()
    {
        Order = [BitRichTextEditorToolbarConfig.GroupIds.Inline, "insert-date", "callout"],
        CustomItems =
        [
            new()
            {
                Id = "insert-date",
                Label = "Today",
                AriaLabel = "Insert today's date",
                OnActivate = editor => editor.InsertTextAsync(DateTime.Now.ToString("yyyy-MM-dd"))
            },
            new()
            {
                Id = "callout",
                Label = "Callout",
                IsActive = state => state.Block == "blockquote",
                OnActivate = editor => editor.ExecuteCommandAsync("formatBlock", editor.SelectionState.Block == "blockquote" ? "p" : "blockquote")
            }
        ]
    };

    private string? linkHtml = "<p>Read the <a href=\"https://learn.microsoft.com/aspnet/core/blazor\">Blazor docs</a> to learn more.</p>";
    private string? linkError;
    private void HandleLinkHtmlChanged(string? value)
    {
        linkHtml = value;
        // A successful content update means the previous error no longer applies.
        linkError = null;
    }

    private string? imageHtml = "<p>Images can sit inline with text.</p>";
    private string? lastUpload;
    private async Task<string?> HandleImageUpload(BitRichTextEditorImageUpload image)
    {
        lastUpload = $"{image.FileName} ({image.ContentType}, {image.Content.Length:N0} bytes)";
        // Stands in for the round trip to a storage service, while the editor shows it is uploading.
        await Task.Delay(1500);
        return $"data:{image.ContentType};base64,{Convert.ToBase64String(image.Content)}";
    }

    private string? colorHtml = "<p>Make words <span style=\"color:#5b3df5\">colorful</span> or <span style=\"background-color:#fff3a3\">highlighted</span>, then pick a typeface.</p>";
    private readonly string[] fonts = ["Segoe UI", "Georgia", "Courier New", "Comic Sans MS"];
    private readonly string[] sizes = ["12px", "16px", "20px", "28px"];
    private readonly BitRichTextEditorColor[] palette =
    [
        new("#1f2937", "Ink"),
        new("#5b3df5", "Brand violet"),
        new("#2563eb", "Blue"),
        new("#059669", "Green"),
        new("#d97706", "Amber"),
        new("#dc2626", "Red"),
        new("#fff3a3", "Soft yellow"),
        new("#dbeafe", "Soft blue"),
    ];

    private string? tableHtml = "<table><thead><tr><th>Feature</th><th>Status</th></tr></thead><tbody><tr><td>Tables</td><td>Ready</td></tr><tr><td>Merge &amp; split cells</td><td>Ready</td></tr></tbody></table>";

    private string? mediaHtml = "<p>Add a divider below, then embed a video.</p><hr><p>Next section.</p>";

    private string? findHtml = "<p>The quick brown fox jumps over the lazy dog. The fox is quick, and the foxes are quicker.</p>";

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
    private string? plainPasteHtml;

    private string? typingHtml = "<p>Type \"quotes\" -- an ellipsis... or (c) 2026, and start a new line with / or [] .</p>";

    private string? shortcutHtml = "<p>Press Ctrl/Cmd+Shift+S, Ctrl/Cmd+Shift+L or Ctrl/Cmd+Shift+1, and Alt+0 to list them all.</p>";
    private readonly Dictionary<string, string> shortcuts = new()
    {
        ["ctrl+shift+s"] = "strikeThrough",
        ["ctrl+shift+l"] = "insertUnorderedList",
        ["ctrl+shift+1"] = "h1",
    };

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

    private readonly FormModel formModel = new();
    private bool formSubmitted;
    private void HandleValidSubmit()
    {
        formSubmitted = true;
    }
    public class FormModel : IValidatableObject
    {
        [Required(ErrorMessage = "The description is required.")]
        public string? Body { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // The value is HTML, so measure its visible text: markup with no text passes [Required].
            var stripped = System.Text.RegularExpressions.Regex.Replace(Body ?? "", "<[^>]+>", "");
            var text = System.Net.WebUtility.HtmlDecode(stripped).Trim();
            if (string.IsNullOrWhiteSpace(Body) is false && text.Length == 0)
            {
                yield return new ValidationResult("The description is required.", [nameof(Body)]);
            }
            else if (text.Length > 0 && text.Length < 20)
            {
                yield return new ValidationResult("Add a bit more detail (min 20 characters).", [nameof(Body)]);
            }
        }
    }

    private string? commentHtml = "<p>Save this to see the server reject it, then edit it to clear the error.</p>";
    private string? commentError;
    // Stands in for a server round trip that refuses the content.
    private void SaveComment() => commentError = "The server refused the comment: it reads like spam.";

    private string? eventsHtml ="<h2>A heading</h2><p>Some <strong>bold</strong> text with a <a href=\"https://example.com\">link</a> in it.</p>";
    private string focusState = "blurred";
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

    private string? localizedHtml = "<p>Pase el cursor sobre los botones para ver las descripciones traducidas.</p>";
    private readonly SpanishEditorLocalizer localizer = new();
    // Only the keys present here are translated; every other key keeps the built-in English text.
    public class SpanishEditorLocalizer : IBitRichTextEditorLocalizer
    {
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["toolbar"] = "Formato",
            ["editor"] = "Editor de texto enriquecido",
            ["undo"] = "Deshacer (Ctrl+Z)",
            ["undo-label"] = "Deshacer",
            ["redo"] = "Rehacer (Ctrl+Y)",
            ["redo-label"] = "Rehacer",
            ["bold"] = "Negrita (Ctrl+B)",
            ["bold-label"] = "Negrita",
            ["italic"] = "Cursiva (Ctrl+I)",
            ["italic-label"] = "Cursiva",
            ["underline"] = "Subrayado (Ctrl+U)",
            ["underline-label"] = "Subrayado",
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
            ["link-label"] = "Insertar o editar enlace",
            ["find-replace"] = "Buscar y reemplazar",
            ["find-replace-label"] = "Buscar y reemplazar",
            ["find"] = "Buscar",
            ["replace"] = "Reemplazar",
            ["no-matches"] = "Sin coincidencias",
            ["clear-formatting"] = "Borrar formato",
        };

        public string? this[string key] => Labels.TryGetValue(key, out var value) ? value : null;
    }

    private BitRichTextEditor apiEditor = default!;
    private string? apiHtml = "<p>Drive me from the buttons above.</p>";
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
        apiResult = $"{apiEditor.WordCount} words, {apiEditor.CharacterCount} chars, empty: {apiEditor.IsEmpty}";
    }

    private readonly BitRichTextEditorParams[] richTextEditorParams =
    [
        new()
        {
            Height = "6rem",
            ShowCount = true,
            StickyToolbar = true,
            Toolbar = BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists | BitRichTextEditorToolbar.Link,
            Placeholder = "Defaults come from BitParams...",
        }
    ];

    private string? cssVariablesHtml = "<p>Restyled with the <a href=\"#example23\">public CSS variables</a>: <code>no class needed</code>.</p><blockquote>Even the quote rule.</blockquote>";
    private const string cssVariablesStyle = "--bit-RichTextEditor-border-radius:1rem;" +
                                             "--bit-RichTextEditor-border-color:var(--bit-clr-pri);" +
                                             "--bit-RichTextEditor-toolbar-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);" +
                                             "--bit-RichTextEditor-footer-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);" +
                                             "--bit-RichTextEditor-button-radius:999px;" +
                                             "--bit-RichTextEditor-quote-border-color:var(--bit-clr-pri);" +
                                             "--bit-RichTextEditor-font-family:Georgia, serif;" +
                                             "--bit-RichTextEditor-line-height:1.8;";

    private string? rtlHtml = "<p>این ویرایشگر از راست به چپ چیده شده است.</p><p dir=\"ltr\">And this block is left-to-right.</p>";
}
