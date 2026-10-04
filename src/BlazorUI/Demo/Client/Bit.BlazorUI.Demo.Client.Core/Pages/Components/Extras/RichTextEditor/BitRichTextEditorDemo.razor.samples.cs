namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.RichTextEditor;

public partial class BitRichTextEditorDemo
{
    private readonly string example1RazorCode = @"
<BitRichTextEditor />";

    private readonly string example2RazorCode = @"
<BitRichTextEditor Placeholder=""Write something..."" Height=""8rem"" MaxHeight=""14rem"" SpellCheck=""false"" Resizable />";

    private readonly string example3RazorCode = @"
<BitButton OnClick='() => bindingHtml = ""<h3>Set from code</h3><p>The editor just updated.</p>""'>Set content</BitButton>
<BitButton OnClick=""() => bindingHtml = string.Empty"">Clear</BitButton>
<BitButton OnClick='() => bindingHtml += ""<p>Appended paragraph.</p>""'>Append</BitButton>

<BitRichTextEditor @bind-Value=""bindingHtml"" DebounceMs=""400"" OnChange=""_ => changeCount++""
                   Height=""8rem"" Placeholder=""Edit me and watch the value update..."" />

<div>Changes: <b>@changeCount</b></div>
<pre>@bindingHtml</pre>";
    private readonly string example3CsharpCode = @"
private string? bindingHtml = ""<p>The bound value is just a <strong>string</strong> you own.</p>"";
private int changeCount;";

    private readonly string example4RazorCode = @"
<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists | BitRichTextEditorToolbar.Link""
                   Height=""6rem"" Placeholder=""Only the inline, lists and link groups."" />

<BitRichTextEditor Toolbar=""BitRichTextEditorToolbar.AllExtended"" StickyToolbar Height=""28rem"" />";

    private readonly string example5RazorCode = @"
<BitRichTextEditor @bind-Value=""customHtml"" ToolbarConfig=""customConfig"" Height=""8rem"" />";
    private readonly string example5CsharpCode = @"
private string? customHtml = ""<p>The inline group comes first, 'Today' inserts the date and 'Callout' toggles a quote.</p>"";

private readonly BitRichTextEditorToolbarConfig customConfig = new()
{
    Order = [BitRichTextEditorToolbarConfig.GroupIds.Inline, ""insert-date"", ""callout""],
    CustomItems =
    [
        new()
        {
            Id = ""insert-date"",
            Label = ""Today"",
            AriaLabel = ""Insert today's date"",
            OnActivate = editor => editor.InsertTextAsync(DateTime.Now.ToString(""yyyy-MM-dd""))
        },
        new()
        {
            Id = ""callout"",
            Label = ""Callout"",
            IsActive = state => state.Block == ""blockquote"",
            OnActivate = editor => editor.ExecuteCommandAsync(""formatBlock"", editor.SelectionState.Block == ""blockquote"" ? ""p"" : ""blockquote"")
        }
    ]
};";

    private readonly string example6RazorCode = @"
<BitRichTextEditor Value=""<p>This instance is <strong>read-only</strong>.</p>""
                   ReadOnly ShowToolbar=""false"" Height=""auto"" />

<BitRichTextEditor Value=""<p>This instance is <strong>disabled</strong>.</p>""
                   IsEnabled=""false"" Height=""auto""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists"" />";

    private readonly string example7RazorCode = @"
<BitRichTextEditor Value=""@linkHtml"" ValueChanged=""HandleLinkHtmlChanged"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Link""
                   OnError='e => linkError = $""{e.Code}: {e.Message}""'
                   Placeholder=""Select text, then add a link..."" />

@if (linkError is not null)
{
    <BitText Color=""BitColor.Error"">@linkError</BitText>
}";
    private readonly string example7CsharpCode = @"
private string? linkHtml = ""<p>Read the <a href=\""https://learn.microsoft.com/aspnet/core/blazor\"">Blazor docs</a> to learn more.</p>"";
private string? linkError;

private void HandleLinkHtmlChanged(string? value)
{
    linkHtml = value;
    // A successful content update means the previous error no longer applies.
    linkError = null;
}";

    private readonly string example8RazorCode = @"
<BitRichTextEditor @bind-Value=""imageHtml"" Height=""12rem""
                   Toolbar=""BitRichTextEditorToolbar.Image | BitRichTextEditorToolbar.Inline""
                   OnImageUpload=""HandleImageUpload""
                   MaxImageSize=""2 * 1024 * 1024""
                   Placeholder=""Drop or paste an image, or use the image button..."" />

@if (lastUpload is not null)
{
    <div>Last upload: <b>@lastUpload</b></div>
}";
    private readonly string example8CsharpCode = @"
private string? imageHtml = ""<p>Images can sit inline with text.</p>"";
private string? lastUpload;

private async Task<string?> HandleImageUpload(BitRichTextEditorImageUpload image)
{
    lastUpload = $""{image.FileName} ({image.ContentType}, {image.Content.Length:N0} bytes)"";
    // Upload the bytes to your storage here and return the public URL (or null to cancel);
    // the delay stands in for that round trip, while the editor shows it is uploading.
    await Task.Delay(1500);
    return $""data:{image.ContentType};base64,{Convert.ToBase64String(image.Content)}"";
}";

    private readonly string example9RazorCode = @"
<BitRichTextEditor @bind-Value=""colorHtml"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.Color | BitRichTextEditorToolbar.Font""
                   ColorPalette=""palette""
                   FontFamilies=""fonts"" FontSizes=""sizes"" />";
    private readonly string example9CsharpCode = @"
private string? colorHtml = ""<p>Make words <span style=\""color:#5b3df5\"">colorful</span> or <span style=\""background-color:#fff3a3\"">highlighted</span>, then pick a typeface.</p>"";
private readonly string[] fonts = [""Segoe UI"", ""Georgia"", ""Courier New"", ""Comic Sans MS""];
private readonly string[] sizes = [""12px"", ""16px"", ""20px"", ""28px""];
private readonly BitRichTextEditorColor[] palette =
[
    new(""#1f2937"", ""Ink""),
    new(""#5b3df5"", ""Brand violet""),
    new(""#2563eb"", ""Blue""),
    new(""#059669"", ""Green""),
    new(""#d97706"", ""Amber""),
    new(""#dc2626"", ""Red""),
    new(""#fff3a3"", ""Soft yellow""),
    new(""#dbeafe"", ""Soft blue""),
];";

    private readonly string example10RazorCode = @"
<BitRichTextEditor @bind-Value=""tableHtml"" Height=""12rem""
                   Toolbar=""BitRichTextEditorToolbar.Table | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example11RazorCode = @"
<BitRichTextEditor @bind-Value=""mediaHtml"" Height=""12rem""
                   Toolbar=""BitRichTextEditorToolbar.Media | BitRichTextEditorToolbar.Rule | BitRichTextEditorToolbar.BlockFormat"" />";

    private readonly string example12RazorCode = @"
<BitRichTextEditor @bind-Value=""findHtml"" Height=""10rem""
                   Toolbar=""BitRichTextEditorToolbar.Find | BitRichTextEditorToolbar.Inline"" />";

    private readonly string example13RazorCode = @"
<BitRichTextEditor @bind-Value=""sanitizationHtml"" Height=""10rem""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Source""
                   SanitizationPolicy=""sanitizationPolicy""
                   Placeholder=""Paste rich content; only allowlisted tags survive..."" />

<BitRichTextEditor @bind-Value=""plainPasteHtml"" PasteAsPlainText Height=""6rem""
                   Placeholder=""Pasted content arrives as plain text..."" />";
    private readonly string example13CsharpCode = @"
private string? sanitizationHtml = ""<p>Only allowlisted tags, attributes and URI schemes survive.</p>"";
private string? plainPasteHtml;

private readonly BitRichTextEditorSanitizationPolicy sanitizationPolicy = new()
{
    AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""p"", ""br"", ""strong"", ""em"", ""a"", ""ul"", ""ol"", ""li"" },
    AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        [""*""] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""class"" },
        [""a""] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""href"", ""title"" },
    },
    AllowedUriSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ""http"", ""https"", ""mailto"" },
    AllowDataImageUris = false,
};";

    private readonly string example14RazorCode = @"
<BitRichTextEditor @bind-Value=""typingHtml"" SmartTypography Height=""10rem""
                   Placeholder=""Type / on a new line, start a line with # or [], or type -- and (c)..."" />";

    private readonly string example15RazorCode = @"
<BitRichTextEditor @bind-Value=""shortcutHtml"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Link | BitRichTextEditorToolbar.Help""
                   KeyboardShortcuts=""shortcuts"" />";
    private readonly string example15CsharpCode = @"
private string? shortcutHtml = ""<p>Press Ctrl/Cmd+Shift+S, Ctrl/Cmd+Shift+L or Ctrl/Cmd+Shift+1, and Alt+0 to list them all.</p>"";

private readonly Dictionary<string, string> shortcuts = new()
{
    [""ctrl+shift+s""] = ""strikeThrough"",
    [""ctrl+shift+l""] = ""insertUnorderedList"",
    [""ctrl+shift+1""] = ""h1"",
};";

    private readonly string example16RazorCode = @"
<BitRichTextEditor @bind-Value=""quickHtml"" ShowQuickToolbar ShowToolbar=""false"" Height=""8rem"" />";

    private readonly string example17RazorCode = @"
<BitRichTextEditor @bind-Value=""mentionHtml"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.Inline""
                   OnMentionSearch=""SearchMentions""
                   OnMentionSelected=""m => lastMention = m.Display""
                   Placeholder=""Type @@ to mention someone..."" />

@if (lastMention is not null)
{
    <div>Last mention: <b>@lastMention</b></div>
}";
    private readonly string example17CsharpCode = @"
private string? mentionHtml = ""<p>Type @ to bring up the people picker.</p>"";
private string? lastMention;

private static readonly BitRichTextEditorMention[] people =
[
    new(""1"", ""Ada Lovelace"", ""Analytical engine""),
    new(""2"", ""Alan Turing"", ""Computing machinery""),
    new(""3"", ""Grace Hopper"", ""Compilers""),
    new(""4"", ""Katherine Johnson"", ""Orbital mechanics""),
    new(""5"", ""Margaret Hamilton"", ""Flight software""),
];

private Task<IReadOnlyList<BitRichTextEditorMention>> SearchMentions(string term)
{
    IReadOnlyList<BitRichTextEditorMention> matches = string.IsNullOrWhiteSpace(term)
        ? people
        : people.Where(p => p.Display.Contains(term, StringComparison.OrdinalIgnoreCase)).ToArray();
    return Task.FromResult(matches);
}";

    private readonly string example18RazorCode = @"
<EditForm Model=""formModel"" OnValidSubmit=""HandleValidSubmit"" novalidate>
    <DataAnnotationsValidator />
    <BitRichTextEditor @bind-Value=""formModel.Body""
                       Label=""Description""
                       Required
                       Description=""Plain words work best: what changed and why.""
                       ShowCount
                       MaxLength=""500""
                       Height=""8rem""
                       Placeholder=""Write at least 20 characters..."" />
    <ValidationMessage For=""() => formModel.Body"" />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>

@if (formSubmitted)
{
    <BitText Color=""BitColor.Success"">Submitted successfully!</BitText>
}

<BitRichTextEditor @bind-Value=""commentHtml"" @bind-Value:after=""() => commentError = null""
                   Label=""Comment""
                   Toolbar=""BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Link""
                   ErrorMessage=""@commentError""
                   Height=""6rem"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""SaveComment"">Save comment</BitButton>";
    private readonly string example18CsharpCode = @"
private readonly FormModel formModel = new();
private bool formSubmitted;

private void HandleValidSubmit()
{
    formSubmitted = true;
}

public class FormModel : IValidatableObject
{
    [Required(ErrorMessage = ""The description is required."")]
    public string? Body { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // The value is HTML, so measure its visible text: markup with no text passes [Required].
        var stripped = System.Text.RegularExpressions.Regex.Replace(Body ?? """", ""<[^>]+>"", """");
        var text = System.Net.WebUtility.HtmlDecode(stripped).Trim();
        if (string.IsNullOrWhiteSpace(Body) is false && text.Length == 0)
        {
            yield return new ValidationResult(""The description is required."", [nameof(Body)]);
        }
        else if (text.Length > 0 && text.Length < 20)
        {
            yield return new ValidationResult(""Add a bit more detail (min 20 characters)."", [nameof(Body)]);
        }
    }
}

private string? commentHtml = ""<p>Save this to see the server reject it, then edit it to clear the error.</p>"";
private string? commentError;
// Stands in for a server round trip that refuses the content.
private void SaveComment() => commentError = ""The server refused the comment: it reads like spam."";";

    private readonly string example19RazorCode = @"
<BitRichTextEditor @bind-Value=""eventsHtml"" Height=""8rem""
                   OnFocus='() => focusState = ""focused""'
                   OnBlur='() => focusState = ""blurred""'
                   OnSelectionChange=""HandleSelectionChange"" />

<div>Editor is <b>@focusState</b>; the caret is in: <b>@selectionSummary</b></div>";
    private readonly string example19CsharpCode = @"
private string? eventsHtml = ""<h2>A heading</h2><p>Some <strong>bold</strong> text with a <a href=\""https://example.com\"">link</a> in it.</p>"";
private string focusState = ""blurred"";
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

    private readonly string example20RazorCode = @"
<BitRichTextEditor @bind-Value=""localizedHtml"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Find""
                   Localizer=""localizer"" />";
    private readonly string example20CsharpCode = @"
private string? localizedHtml = ""<p>Pase el cursor sobre los botones para ver las descripciones traducidas.</p>"";
private readonly SpanishEditorLocalizer localizer = new();

// Only the keys present here are translated; every other key keeps the built-in English text.
public class SpanishEditorLocalizer : IBitRichTextEditorLocalizer
{
    private static readonly Dictionary<string, string> Labels = new()
    {
        [""toolbar""] = ""Formato"",
        [""editor""] = ""Editor de texto enriquecido"",
        [""undo""] = ""Deshacer (Ctrl+Z)"",
        [""undo-label""] = ""Deshacer"",
        [""redo""] = ""Rehacer (Ctrl+Y)"",
        [""redo-label""] = ""Rehacer"",
        [""bold""] = ""Negrita (Ctrl+B)"",
        [""bold-label""] = ""Negrita"",
        [""italic""] = ""Cursiva (Ctrl+I)"",
        [""italic-label""] = ""Cursiva"",
        [""underline""] = ""Subrayado (Ctrl+U)"",
        [""underline-label""] = ""Subrayado"",
        [""strikethrough""] = ""Tachado"",
        [""paragraph-format""] = ""Formato de párrafo"",
        [""block-normal""] = ""Normal"",
        [""heading-1""] = ""Título 1"",
        [""heading-2""] = ""Título 2"",
        [""heading-3""] = ""Título 3"",
        [""bullet-list""] = ""Lista con viñetas"",
        [""numbered-list""] = ""Lista numerada"",
        [""task-list""] = ""Lista de tareas"",
        [""quote""] = ""Cita"",
        [""code-block""] = ""Bloque de código"",
        [""link""] = ""Insertar o editar enlace"",
        [""link-label""] = ""Insertar o editar enlace"",
        [""find-replace""] = ""Buscar y reemplazar"",
        [""find-replace-label""] = ""Buscar y reemplazar"",
        [""find""] = ""Buscar"",
        [""replace""] = ""Reemplazar"",
        [""no-matches""] = ""Sin coincidencias"",
        [""clear-formatting""] = ""Borrar formato"",
    };

    public string? this[string key] => Labels.TryGetValue(key, out var value) ? value : null;
}";

    private readonly string example21RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""FocusEditor"">FocusAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => apiEditor.ExecuteCommandAsync(""bold""))"">ExecuteCommand(""bold"")</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='@(() => apiEditor.InsertTextAsync("" inserted""))'>InsertTextAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick='@(() => apiEditor.InsertHtmlAsync(""<b>bold</b>""))'>InsertHtmlAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(async () => await apiEditor.SelectAllAsync())"">SelectAllAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => apiEditor.UndoAsync())"">UndoAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""GetEditorHtml"">GetHtmlAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""GetEditorText"">GetTextAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""GetEditorSelection"">GetSelectedTextAsync</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowEditorFacts"">Counts</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => apiEditor.ClearAsync())"">ClearAsync</BitButton>

<BitRichTextEditor @ref=""apiEditor"" @bind-Value=""apiHtml"" Height=""8rem"" />

<div>Result:</div>
<pre>@apiResult</pre>";
    private readonly string example21CsharpCode = @"
private BitRichTextEditor apiEditor = default!;
private string? apiHtml = ""<p>Drive me from the buttons above.</p>"";
private string? apiResult;

private async Task FocusEditor() => await apiEditor.FocusAsync();
private async Task GetEditorHtml() => apiResult = await apiEditor.GetHtmlAsync();
private async Task GetEditorText() => apiResult = await apiEditor.GetTextAsync();
private async Task GetEditorSelection() => apiResult = await apiEditor.GetSelectedTextAsync();
private void ShowEditorFacts() => apiResult = $""{apiEditor.WordCount} words, {apiEditor.CharacterCount} chars, empty: {apiEditor.IsEmpty}"";";

    private readonly string example22RazorCode = @"
<BitParams Parameters=""richTextEditorParams"">
    <BitRichTextEditor />
    <BitRichTextEditor Placeholder=""My own placeholder"" />
</BitParams>";
    private readonly string example22CsharpCode = @"
private readonly BitRichTextEditorParams[] richTextEditorParams =
[
    new()
    {
        Height = ""6rem"",
        ShowCount = true,
        StickyToolbar = true,
        Toolbar = BitRichTextEditorToolbar.Inline | BitRichTextEditorToolbar.Lists | BitRichTextEditorToolbar.Link,
        Placeholder = ""Defaults come from BitParams..."",
    }
];";

    private readonly string example23RazorCode = @"
<style>
    .custom-class {
        box-shadow: aqua 0 0 1rem 0.5rem;
    }

    .custom-toolbar {
        background: linear-gradient(90deg, #ff7e5f, #feb47b);
    }

    .custom-editor {
        color: mediumseagreen;
    }
</style>

<BitRichTextEditor Style=""border-color:brown"" Class=""custom-class"" Height=""6rem"" />

<BitRichTextEditor Styles=""@(new() { Toolbar = ""border-bottom-color:tomato"", Editor = ""font-family:Georgia, serif"" })""
                   Classes=""@(new() { Toolbar = ""custom-toolbar"", Editor = ""custom-editor"" })""
                   Height=""6rem"" />

<BitRichTextEditor @bind-Value=""cssVariablesHtml"" Style=""@cssVariablesStyle"" Height=""8rem"" />";
    private readonly string example23CsharpCode = @"
private string? cssVariablesHtml = ""<p>Restyled with the <a href=\""#example23\"">public CSS variables</a>: <code>no class needed</code>.</p><blockquote>Even the quote rule.</blockquote>"";

private const string cssVariablesStyle = ""--bit-RichTextEditor-border-radius:1rem;"" +
                                         ""--bit-RichTextEditor-border-color:var(--bit-clr-pri);"" +
                                         ""--bit-RichTextEditor-toolbar-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);"" +
                                         ""--bit-RichTextEditor-footer-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);"" +
                                         ""--bit-RichTextEditor-button-radius:999px;"" +
                                         ""--bit-RichTextEditor-quote-border-color:var(--bit-clr-pri);"" +
                                         ""--bit-RichTextEditor-font-family:Georgia, serif;"" +
                                         ""--bit-RichTextEditor-line-height:1.8;"";";

    private readonly string example24RazorCode = @"
<BitRichTextEditor @bind-Value=""rtlHtml"" Dir=""BitDir.Rtl"" Height=""8rem""
                   Toolbar=""BitRichTextEditorToolbar.All | BitRichTextEditorToolbar.Direction""
                   Placeholder=""متنی بنویسید..."" />";
    private readonly string example24CsharpCode = @"
private string? rtlHtml = ""<p>این ویرایشگر از راست به چپ چیده شده است.</p><p dir=\""ltr\"">And this block is left-to-right.</p>"";";
}
