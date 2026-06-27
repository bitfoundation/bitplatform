using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.RichTextEditor;

public partial class BitRichTextEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Description = "Debounce window (ms) for content-change notifications while typing."
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
            Description = "Localized labels/tooltips provider. Null uses built-in English labels."
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
            Name = "OnImageUpload",
            Type = "Func<BitRichTextEditorImageUpload, Task<string?>>?",
            DefaultValue = "null",
            Description = "Invoked to persist an image binary, returning the URL to embed. When null, dropped or pasted images are embedded as inline data URLs.",
            LinkType = LinkType.Link,
            Href = "#image-upload"
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
            Name = "SanitizationPolicy",
            Type = "BitRichTextEditorSanitizationPolicy?",
            DefaultValue = "null",
            Description = "Allowlist policy applied to all content. When null a secure default allowlist is applied."
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
            Name = "ShowToolbar",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the formatting toolbar is shown."
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
            Name = "ExecuteCommandAsync",
            Type = "Task",
            Description = "Runs a raw editing command against the editor."
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
                new() { Name = "Order", Type = "IReadOnlyList<string>?", DefaultValue = "null", Description = "Explicit ordering of toolbar entry ids (built-in group ids and custom item ids)." },
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
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "Unique id used for ordering and lookup." },
                new() { Name = "Label", Type = "string?", DefaultValue = "null", Description = "Text label shown when no icon is provided." },
                new() { Name = "Icon", Type = "RenderFragment?", DefaultValue = "null", Description = "Optional icon content." },
                new() { Name = "AriaLabel", Type = "string", DefaultValue = "", Description = "Non-empty accessible label / tooltip." },
                new() { Name = "OnActivate", Type = "Func<BitRichTextEditor, Task>", DefaultValue = "", Description = "Action invoked when the item is activated; receives the editor instance." },
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
            Id = "editor-error",
            Title = "BitRichTextEditorError",
            Description = "An error surfaced by the editor (e.g. invalid URL, failed upload, invalid HTML).",
            Parameters =
            [
                new() { Name = "Code", Type = "string", DefaultValue = "", Description = "Stable error code, e.g. \"invalid-url\"." },
                new() { Name = "Message", Type = "string", DefaultValue = "", Description = "Human-readable description." },
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



    private string? boundHtml = "<p>Hello <strong>world</strong>.</p>";

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

    private string eventLog = "-";

    private readonly FormModel formModel = new();
    private bool formSubmitted;
    private void HandleValidSubmit()
    {
        formSubmitted = true;
    }
    public class FormModel
    {
        [Required(ErrorMessage = "The body is required.")]
        public string? Body { get; set; }
    }

    private BitRichTextEditor customEditor = default!;
    private readonly BitRichTextEditorToolbarConfig toolbarConfig = new()
    {
        CustomItems =
        [
            new()
            {
                Id = "signature",
                Label = "✍",
                AriaLabel = "Insert signature",
                OnActivate = async editor => await editor.ExecuteCommandAsync("insertText", " — Sent from BitRichTextEditor")
            }
        ]
    };



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";

    private readonly string example2RazorCode = @"
<BitRichTextEditor Placeholder=""Write something..."" />";

    private readonly string example3RazorCode = @"
<BitRichTextEditor ReadOnly Value=""<p>This editor is <strong>readonly</strong>.</p>"" />";

    private readonly string example4RazorCode = @"
<BitRichTextEditor @bind-Value=""boundHtml"" Placeholder=""Type here to update the bound value..."" />

<div>Bound HTML value:</div>
<pre>@boundHtml</pre>";
    private readonly string example4CsharpCode = @"
private string? boundHtml = ""<p>Hello <strong>world</strong>.</p>"";";

    private readonly string example5RazorCode = @"
<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.AllExtended""
                   Placeholder=""All of the available toolbar groups are enabled."" />";

    private readonly string example6RazorCode = @"
<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists | BitRichTextEditorToolbar.Link""
                   Placeholder=""Only the inline, lists and link groups are shown."" />";

    private readonly string example7RazorCode = @"
<BitRichTextEditor Height=""150px"" ShowCount MaxLength=""200""
                   Placeholder=""Up to 200 characters..."" />";

    private readonly string example8RazorCode = @"
<BitRichTextEditor @ref=""apiEditor"" />

<BitButton OnClick=""FocusEditor"">FocusAsync</BitButton>
<BitButton OnClick=""GetEditorHtml"">GetHtmlAsync</BitButton>
<BitButton OnClick=""@(() => apiEditor.ExecuteCommandAsync(""bold""))"">ExecuteCommand(""bold"")</BitButton>

<div>result:</div>
<pre>@apiResult</pre>";
    private readonly string example8CsharpCode = @"
private BitRichTextEditor apiEditor = default!;
private string? apiResult;
private async Task FocusEditor()
{
    await apiEditor.FocusAsync();
}
private async Task GetEditorHtml()
{
    apiResult = await apiEditor.GetHtmlAsync();
}";

    private readonly string example9RazorCode = @"
<BitRichTextEditor OnFocus=""() => eventLog = $""Focused at {DateTime.Now:HH:mm:ss}""""
                   OnBlur=""() => eventLog = $""Blurred at {DateTime.Now:HH:mm:ss}""""
                   OnError=""e => eventLog = $""Error ({e.Code}): {e.Message}""""
                   Toolbar=""BitRichTextEditorToolbar.AllExtended""
                   Placeholder=""Focus, blur, or trigger an error (e.g. an invalid link)."" />

<div>last event: @eventLog</div>";
    private readonly string example9CsharpCode = @"
private string eventLog = ""-"";";

    private readonly string example10RazorCode = @"
<EditForm Model=""formModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitRichTextEditor @bind-Value=""formModel.Body"" Placeholder=""The body is required..."" />
    <ValidationMessage For=""() => formModel.Body"" />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
private readonly FormModel formModel = new();
private bool formSubmitted;
private void HandleValidSubmit()
{
    formSubmitted = true;
}
public class FormModel
{
    [Required(ErrorMessage = ""The body is required."")]
    public string? Body { get; set; }
}";

    private readonly string example11RazorCode = @"
<BitRichTextEditor @ref=""customEditor"" ToolbarConfig=""toolbarConfig"" />";
    private readonly string example11CsharpCode = @"
private BitRichTextEditor customEditor = default!;
private BitRichTextEditorToolbarConfig toolbarConfig = new()
{
    CustomItems =
    [
        new()
        {
            Id = ""signature"",
            Label = ""✍"",
            AriaLabel = ""Insert signature"",
            OnActivate = async editor => await editor.ExecuteCommandAsync(""insertText"", "" — Sent from BitRichTextEditor"")
        }
    ]
};";

    private readonly string example12RazorCode = @"
<BitRichTextEditor Styles=""@(new() { Toolbar = ""border-bottom-color: red"", Editor = ""background-color: #fff8e1"" })""
                   Placeholder=""Custom styles applied to the toolbar and editor."" />";
}
