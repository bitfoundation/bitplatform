namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownEditor;

public partial class BitMarkdownEditorDemo
{
    private readonly string example1RazorCode = @"
<BitMarkdownEditor />";

    private readonly string example2RazorCode = @"
<BitMarkdownEditor @bind-Value=""bindingValue"" />

<BitTextField Multiline Rows=""4"" Label=""Bound value (editable)"" @bind-Value=""@bindingValue"" Immediate />

<BitMarkdownEditor DefaultValue=""# This is the default value"" OnChange=""v => onChangeValue = v"" />

<div>OnChange value:</div>
<pre>@onChangeValue</pre>";
    private readonly string example2CsharpCode = @"
private string? bindingValue = ""# Two-way binding"";
private string? onChangeValue;";

    private readonly string example3RazorCode = @"
<BitChoiceGroup Horizontal @bind-Value=""@mode"" TItem=""BitChoiceGroupOption<BitMarkdownEditorMode>"" TValue=""BitMarkdownEditorMode"">
    <BitChoiceGroupOption Text=""Edit"" Value=""BitMarkdownEditorMode.Edit"" />
    <BitChoiceGroupOption Text=""Split"" Value=""BitMarkdownEditorMode.Split"" />
    <BitChoiceGroupOption Text=""Preview"" Value=""BitMarkdownEditorMode.Preview"" />
</BitChoiceGroup>

<BitMarkdownEditor @bind-Mode=""mode"" DefaultValue=""@modeDefaultValue"" />";
    private readonly string example3CsharpCode = @"
private BitMarkdownEditorMode mode = BitMarkdownEditorMode.Split;
private string modeDefaultValue =
@""# Mode
Switch between **Edit**, **Split** and **Preview** using the choice group above,
the eye button of the toolbar, the F9 key, or the `@bind-Mode` parameter."";";

    private readonly string example4RazorCode = @"
<BitToggleButton @bind-IsChecked=""fullScreen"" OnText=""Exit full-screen"" OffText=""Go full-screen"" />

<BitMarkdownEditor @bind-FullScreen=""fullScreen"" Height=""10rem"" MinHeight=""6rem"" MaxHeight=""20rem"" Resizable />

<BitMarkdownEditor AutoHeight StickyToolbar MinHeight=""5rem"" Mode=""BitMarkdownEditorMode.Edit"" />";
    private readonly string example4CsharpCode = @"
private bool fullScreen;";

    private readonly string example5RazorCode = @"
<BitMarkdownEditor @bind-Value=""customToolbarValue"" Toolbar=""customToolbar"" Height=""8rem"" />
<div role=""status"">@toolbarStatus</div>

<BitMarkdownEditor ShowToolbar=""false"" ShowStatusBar=""false"" Height=""8rem"" />";
    private readonly string example5CsharpCode = @"
private string? customToolbarValue = ""The toolbar of this editor only offers **basic** formatting, a custom *clear* button and a *Save* button bound to Ctrl+S."";
private IReadOnlyList<BitMarkdownEditorToolbarItem> customToolbar = [];
private string? toolbarStatus;

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
            IconName = BitIconName.Delete,
            Type = BitMarkdownEditorToolbarItemType.Custom,
            OnClick = _ =>
            {
                customToolbarValue = string.Empty;
                return Task.CompletedTask;
            }
        },
        new()
        {
            Name = ""save"",
            Text = ""Save"",
            IconName = BitIconName.Save,
            Shortcut = ""Ctrl+S"",
            Type = BitMarkdownEditorToolbarItemType.Custom,
            OnClick = _ =>
            {
                toolbarStatus = $""Saved {customToolbarValue?.Length ?? 0} characters at {DateTime.Now:T}."";
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
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Link)"">Link</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.TaskList)"">Tasks</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.Table)"">Table</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunCommand(BitMarkdownEditorCommand.DuplicateLine)"">Duplicate line</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""InsertSignature"">Insert</BitButton>
    <BitButton Variant=""BitVariant.Outline"" Disabled=""commandsRef?.CanUndo is not true"" OnClick=""Undo"">Undo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" Disabled=""commandsRef?.CanRedo is not true"" OnClick=""Redo"">Redo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""GetValue"">GetValue</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ShowSelection"">GetSelection</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""SelectFirstLine"">SetSelection</BitButton>
</div>

<BitMarkdownEditor @ref=""commandsRef"" ShowToolbar=""false"" OnChange=""_ => InvokeAsync(StateHasChanged)"" />

<div>Result:</div>
<pre>@getValueResult</pre>";
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

<BitMarkdownEditor DefaultValue=""@previewDefaultValue"" DebounceTime=""500"">
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
<BitMarkdownEditor Placeholder=""Type a ( or a ` here, or select a word and type *...""
                   Mode=""BitMarkdownEditorMode.Edit""
                   Height=""8rem""
                   SpellCheck=""false""
                   AutoClosePairs
                   TabIndents=""false""
                   ChangeDebounceTime=""300""
                   IndentUnit=""@(""    "")"" />

<BitMarkdownEditor BoldStyle=""BitMarkdownEditorEmphasisStyle.Underscore""
                   ItalicStyle=""BitMarkdownEditorEmphasisStyle.Underscore""
                   BulletStyle=""BitMarkdownEditorBulletStyle.Asterisk""
                   TableColumns=""4""
                   TableRows=""3""
                   Mode=""BitMarkdownEditorMode.Edit""
                   Height=""8rem""
                   DefaultValue=""@markdownStyleDefaultValue"" />

<BitMarkdownEditor ReadOnly DefaultValue=""@readOnlyDefaultValue"" Height=""8rem"" />

<BitMarkdownEditor Disabled DefaultValue=""# Disabled"" Height=""6rem"" />";
    private readonly string example8CsharpCode = @"
private string markdownStyleDefaultValue =
@""Select some text and hit Ctrl+B or Ctrl+I, or use the list buttons."";

private string readOnlyDefaultValue =
@""# Read-only
The content of this editor **cannot** be edited, but can still be *selected* and the preview stays live."";";

    private readonly string example9RazorCode = @"
<div class=""commands-bar"">
    <BitTextField Label=""Find"" @bind-Value=""findText"" Placeholder=""Find"" Immediate />
    <BitTextField Label=""Replace with"" @bind-Value=""replaceText"" Placeholder=""Replace with"" Immediate />
    <BitButton Variant=""BitVariant.Outline"" OnClick=""FindNext"">Find next</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""FindPrevious"">Find previous</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""ReplaceAll"">Replace all</BitButton>
</div>

<div role=""status"">@findStatus</div>

<BitMarkdownEditor @ref=""findRef"" DefaultValue=""@findDefaultValue"" Mode=""BitMarkdownEditorMode.Edit"" Height=""8rem"" />";
    private readonly string example9CsharpCode = @"
private BitMarkdownEditor findRef = default!;
private string? findText = ""markdown"";
private string? replaceText = ""Markdown"";
private string? findStatus;
private string findDefaultValue =
@""# Find and replace

The markdown editor finds every markdown word in this markdown document.
Press Ctrl+F to open the built-in panel, or use the buttons above."";

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

    private readonly string example10RazorCode = @"
<div class=""commands-bar"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunTableCommand(BitMarkdownEditorCommand.TableInsertRowBelow)"">Row below</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunTableCommand(BitMarkdownEditorCommand.TableDeleteRow)"">Delete row</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunTableCommand(BitMarkdownEditorCommand.TableInsertColumnAfter)"">Column after</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunTableCommand(BitMarkdownEditorCommand.TableDeleteColumn)"">Delete column</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => RunTableCommand(BitMarkdownEditorCommand.TableAlignCenter)"">Center column</BitButton>
</div>

<BitMarkdownEditor @ref=""tableRef"" DefaultValue=""@tableDefaultValue"" />";
    private readonly string example10CsharpCode = @"
private BitMarkdownEditor tableRef = default!;
private string tableDefaultValue =
@""| Package | Downloads | Notes |
| ------- | --------- | ----- |
| Core    | 1.2M      | ships the components |
| Extras  | 480K      | ships this editor |"";

private async Task RunTableCommand(BitMarkdownEditorCommand command)
{
    await tableRef.Run(command);
}";

    private readonly string example11RazorCode = @"
<div role=""status"">@imageStatus</div>

<BitMarkdownEditor OnImageUpload=""UploadImage""
                   OnImageRejected=""ImageRejected""
                   AcceptedImageTypes=""image/png,image/jpeg,image/gif""
                   MaxImageSize=""1048576""
                   DefaultValue=""@imageDefaultValue"" />";
    private readonly string example11CsharpCode = @"
private string? imageStatus;
private string imageDefaultValue =
@""# Image upload
Paste an image, drop one anywhere on this editor, or pick one with the upload button of the toolbar."";

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
        BitMarkdownEditorImageRejectionReason.Length => $""{rejection.FileName} does not fit in the remaining characters."",
        _ => $""{rejection.FileName} is not an accepted image type ({rejection.ContentType}).""
    };
}";

    private readonly string example12RazorCode = @"
<BitButton Variant=""BitVariant.Outline"" OnClick=""ClearDraft"">Clear the draft</BitButton>

<div role=""status"">@draftStatus</div>

<BitMarkdownEditor @ref=""autoSaveRef""
                   AutoSaveId=""bit-mde-demo-draft""
                   OnDraftRestored=""DraftRestored""
                   Placeholder=""This draft survives a page reload..."" />";
    private readonly string example12CsharpCode = @"
private string? draftStatus;
private BitMarkdownEditor autoSaveRef = default!;

private void DraftRestored(string? value)
{
    draftStatus = $""{value?.Length ?? 0} characters restored from the last session."";
}

private async Task ClearDraft()
{
    await autoSaveRef.ClearDraft();
    draftStatus = ""Draft cleared."";
}";

    private readonly string example13RazorCode = @"
<BitMarkdownEditor ShowReadingTime
                   ShowCursorPosition
                   WordsPerMinute=""120""
                   MaxLength=""280""
                   Mode=""BitMarkdownEditorMode.Edit""
                   Height=""8rem""
                   DefaultValue=""@statusDefaultValue"" />";
    private readonly string example13CsharpCode = @"
private string statusDefaultValue =
@""The status bar counts words and characters 👋 and estimates the reading time."";";

    private readonly string example14RazorCode = @"
<BitMarkdownEditor Texts=""frenchTexts"" DefaultValue=""@localizationDefaultValue"" ShowReadingTime />";
    private readonly string example14CsharpCode = @"
private string localizationDefaultValue =
@""# Un éditeur **localisé**

Chaque libellé de l'éditeur vient du paramètre `Texts` :
survolez la barre d'outils, ouvrez la recherche ou lisez la barre d'état."";

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
    ToolbarTogglePreview = ""Changer de mode"",
    ToolbarHelp = ""Raccourcis clavier"",
    WordsFormat = ""{0} mots"",
    CharsFormat = ""{0} caractères"",
    ReadingTimeFormat = ""{0} min de lecture"",
    ModeEdit = ""Édition"",
    ModeSplit = ""Partagé"",
    ModePreview = ""Aperçu"",
    ModeAnnouncementFormat = ""Mode {0}"",
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
<EditForm EditContext=""formEditContext"" OnValidSubmit=""HandleValidSubmit"" novalidate>
    <DataAnnotationsValidator />
    <BitMarkdownEditor @bind-Value=""formModel.ReleaseNotes""
                       Label=""Release notes""
                       Required
                       Description=""Markdown is supported. Press Ctrl+Enter to publish.""
                       Name=""releaseNotes""
                       Height=""8rem""
                       Placeholder=""At least 20 characters...""
                       OnFocus=""EditorFocused""
                       OnBlur=""EditorBlurred""
                       OnSubmit=""SubmitFromEditor"" />
    <ValidationMessage For=""() => formModel.ReleaseNotes"" />
    <BitButton ButtonType=""BitButtonType.Submit"">Publish</BitButton>
</EditForm>

<div role=""status"">@focusStatus @submittedStatus</div>";
    private readonly string example15CsharpCode = @"
public class ReleaseNotesModel
{
    [Required(ErrorMessage = ""Release notes are required."")]
    [MinLength(20, ErrorMessage = ""Write at least 20 characters."")]
    public string? ReleaseNotes { get; set; }
}

private string? focusStatus;
private string? submittedStatus;
private readonly ReleaseNotesModel formModel = new();
private EditContext formEditContext = default!;

protected override void OnInitialized()
{
    formEditContext = new(formModel);
}

private void EditorFocused() => focusStatus = ""The editor has the keyboard focus."";

private void EditorBlurred() => focusStatus = ""The editor lost the keyboard focus."";

private void SubmitFromEditor(string? value)
{
    formModel.ReleaseNotes = value;

    if (formEditContext.Validate()) HandleValidSubmit();
}

private void HandleValidSubmit() => submittedStatus = $""Published {formModel.ReleaseNotes?.Length ?? 0} characters."";";

    private readonly string example16RazorCode = @"
<BitParams Parameters=""markdownEditorParams"">
    <BitMarkdownEditor />
    <BitMarkdownEditor Placeholder=""My own placeholder"" />
</BitParams>";
    private readonly string example16CsharpCode = @"
private readonly BitMarkdownEditorParams[] markdownEditorParams =
[
    new()
    {
        Height = ""8rem"",
        Resizable = true,
        ShowReadingTime = true,
        Placeholder = ""Defaults come from BitParams..."",
    }
];";

    private readonly string example17RazorCode = @"
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

<BitMarkdownEditor Style=""border-color:brown"" Class=""custom-class"" Height=""8rem"" />

<BitMarkdownEditor Classes=""@(new() { Toolbar = ""custom-toolbar"", TextArea = ""custom-textarea"" })""
                   Styles=""@(new() { StatusBar = ""color:tomato;font-weight:bold"" })""
                   Height=""8rem"" />

<BitMarkdownEditor Style=""@cssVariablesStyle"" DefaultValue=""@cssVariablesDefaultValue"" />";
    private readonly string example17CsharpCode = @"
private const string cssVariablesStyle = ""--bit-MarkdownEditor-border-radius:1rem;"" +
                                         ""--bit-MarkdownEditor-border-color:var(--bit-clr-pri);"" +
                                         ""--bit-MarkdownEditor-toolbar-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);"" +
                                         ""--bit-MarkdownEditor-statusbar-background:color-mix(in srgb, var(--bit-clr-pri) 12%, transparent);"" +
                                         ""--bit-MarkdownEditor-button-radius:999px;"" +
                                         ""--bit-MarkdownEditor-button-active-background:var(--bit-clr-sec);"" +
                                         ""--bit-MarkdownEditor-preview-background:var(--bit-clr-bg-sec);"" +
                                         ""--bit-MarkdownEditor-font-family:Georgia,serif;"" +
                                         ""--bit-MarkdownEditor-font-size:1rem;"" +
                                         ""--bit-MarkdownEditor-pane-min-width:0"";

private string cssVariablesDefaultValue =
@""# Restyled with **CSS variables**

Rounded corners, a tinted toolbar, pill buttons, a serif writing font,
and split panes that never stack (`--bit-MarkdownEditor-pane-min-width:0`)."";";

    private readonly string example18RazorCode = @"
<BitMarkdownEditor Dir=""BitDir.Rtl"" DefaultValue=""@rtlDefaultValue"" />";
    private readonly string example18CsharpCode = @"
private string rtlDefaultValue =
@""# ویرایشگر مارک‌داون
این یک متن **راست به چپ** برای نمایش قابلیت RTL است.

- پشتیبانی کامل از لیست‌ها
- میانبرهای صفحه‌کلید"";";
}
