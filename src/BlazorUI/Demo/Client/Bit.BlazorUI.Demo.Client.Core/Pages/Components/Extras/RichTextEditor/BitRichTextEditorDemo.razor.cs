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
                new() { Name = "AriaLabel", Type = "string?", DefaultValue = "null", Description = "Optional accessible label / tooltip. When omitted, Label is used as the accessible name." },
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



    private readonly string readOnlyHtml = "<p>This instance is <strong>read-only</strong> with the toolbar hidden - useful for displaying stored content.</p>";

    private string? bindingHtml = "<p>The bound value is just a <strong>string</strong> you own.</p>";

    private string? debounceHtml = "<p>Type and watch the value update after the debounce window.</p>";

    private string focusState = "blurred";

    private string? formattingHtml = "<h2>Headings</h2><p>Mix <strong>bold</strong>, <em>italic</em>, <u>underline</u> and <s>strikethrough</s>.</p><blockquote>A short quote.</blockquote><ol><li>First</li><li>Second</li></ol>";

    private string? scriptHtml = "<p>Water is H<sub>2</sub>O. Einstein wrote E = mc<sup>2</sup>.</p>";

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

    private string? colorHtml = "<p>Make words <span style=\"color:#5b3df5\">colorful</span> or <span style=\"background:#fff3a3\">highlighted</span>.</p>";

    private string? fontHtml = "<p>Choose a typeface for this paragraph.</p>";
    private readonly string[] fonts = ["Segoe UI", "Georgia", "Courier New", "Comic Sans MS"];
    private readonly string[] sizes = ["12px", "16px", "20px", "28px"];

    private string? tableHtml = "<table><thead><tr><th>Feature</th><th>Status</th></tr></thead><tbody><tr><td>Tables</td><td>Ready</td></tr><tr><td>Merge cells</td><td>Ready</td></tr></tbody></table>";

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

    private string? findHtml = "<p>The quick brown fox jumps over the lazy dog. The fox is quick.</p>";

    private string? fullScreenHtml = "<p>Expand me to full screen and write without distractions.</p>";

    private string? slashHtml = "<p>Place the cursor on a new line and type a slash.</p>";

    private string? shortcutHtml = "<p>Press your custom Ctrl/Cmd+Shift+S or Ctrl/Cmd+Shift+L.</p>";
    private readonly Dictionary<string, string> shortcuts = new()
    {
        ["ctrl+shift+s"] = "strikeThrough",
        ["ctrl+shift+l"] = "insertUnorderedList",
    };

    private string? emojiHtml = "<p>Add a little ✨ to your text.</p>";

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
                OnActivate = editor => editor.ExecuteCommandAsync("insertText", DateTime.Now.ToString("yyyy-MM-dd"))
            }
        ]
    };

    private string? reorderHtml = "<p>The inline, lists and link groups are pulled to the front.</p>";
    private readonly BitRichTextEditorToolbarConfig reorderConfig = new()
    {
        Order = ["inline", "lists", "link"]
    };

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



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";

    private readonly string example2RazorCode = @"
<BitRichTextEditor Placeholder=""Write something..."" Height=""10rem"" />";

    private readonly string example3RazorCode = @"
<BitRichTextEditor Value=""<p>This is <strong>read-only</strong>.</p>""
                   ReadOnly ShowToolbar=""false"" Height=""auto"" />";

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
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Indent | BitRichTextEditorToolbar.Script"" />";

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
                   Toolbar=""BitRichTextEditorToolbar.Media | BitRichTextEditorToolbar.Rule | BitRichTextEditorToolbar.BlockFormat"" />";

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
};";

    private readonly string example19RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" PasteAsPlainText />";

    private readonly string example20RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example21RazorCode = @"
<BitRichTextEditor @bind-Value=""html""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.FullScreen"" />";

    private readonly string example22RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" Toolbar=""BitRichTextEditorToolbar.AllExtended"" />";

    private readonly string example23RazorCode = @"
<BitRichTextEditor @bind-Value=""html"" KeyboardShortcuts=""shortcuts"" />";
    private readonly string example23CsharpCode = @"
private readonly Dictionary<string, string> shortcuts = new()
{
    [""ctrl+shift+s""] = ""strikeThrough"",
    [""ctrl+shift+l""] = ""insertUnorderedList"",
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
            OnActivate = editor => editor.ExecuteCommandAsync(""insertText"", DateTime.Now.ToString(""yyyy-MM-dd""))
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
    Order = [""inline"", ""lists"", ""link""]
};";

    private readonly string example29RazorCode = @"
<BitRichTextEditor @ref=""apiEditor"" @bind-Value=""html"" Toolbar=""BitRichTextEditorToolbar.All"" />

<BitButton OnClick=""FocusEditor"">FocusAsync</BitButton>
<BitButton OnClick='@(() => apiEditor.ExecuteCommandAsync(""bold""))'>ExecuteCommand(""bold"")</BitButton>
<BitButton OnClick=""GetEditorHtml"">GetHtmlAsync</BitButton>

<pre>@apiResult</pre>";
    private readonly string example29CsharpCode = @"
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

    private readonly string example30RazorCode = @"
<BitRichTextEditor Styles=""@(new() { Toolbar = ""border-bottom-color: red"", Editor = ""background-color: #fff8e1"" })""
                   Classes=""@(new() { Toolbar = ""custom-rte-toolbar"", Editor = ""custom-rte-editor"" })""
                   Placeholder=""Custom styles and classes applied to the toolbar and editor."" />";
}
