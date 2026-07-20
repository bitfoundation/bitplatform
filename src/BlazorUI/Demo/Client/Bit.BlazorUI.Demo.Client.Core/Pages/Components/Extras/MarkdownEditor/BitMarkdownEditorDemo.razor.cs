namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownEditor;

public partial class BitMarkdownEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Mode",
            Type = "BitMarkdownEditorMode",
            DefaultValue = "BitMarkdownEditorMode.Split",
            Description = "Determines which panes of the editor are visible (edit / split / preview). Two-way bindable.",
            LinkType = LinkType.Link,
            Href = "#editor-mode-enum",
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
            Name = "Texts",
            Type = "BitMarkdownEditorTexts?",
            DefaultValue = "null",
            Description = "The localized strings of the editor UI (status bar, help panel, aria labels). Defaults to English.",
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
                    Description = "Stable identifier, handy for tests and custom styling.",
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
                    Description = "Optional human readable shortcut hint, e.g. \"Ctrl+B\".",
                },
                new()
                {
                    Name = "OnClick",
                    Type = "Func<BitMarkdownEditor, Task>?",
                    DefaultValue = "null",
                    Description = "Callback used when the Type is Custom.",
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
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "command-enum",
            Name = "BitMarkdownEditorCommand",
            Description = "The set of built-in editing commands the toolbar and keyboard shortcuts can invoke. These are pure text transformations executed in C#.",
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
                new() { Name = "Link", Value = "12", Description = "Inserts a link or turns the current selection into a link." },
                new() { Name = "Image", Value = "13", Description = "Inserts an image or turns the current selection into an image." },
                new() { Name = "UnorderedList", Value = "14", Description = "Toggles an unordered (bullet) list on the selected lines." },
                new() { Name = "OrderedList", Value = "15", Description = "Toggles an ordered (numbered) list on the selected lines." },
                new() { Name = "TaskList", Value = "16", Description = "Toggles a task (checkbox) list on the selected lines." },
                new() { Name = "Table", Value = "17", Description = "Inserts a table template at the caret position." },
                new() { Name = "HorizontalRule", Value = "18", Description = "Inserts a horizontal rule at the caret position." },
                new() { Name = "Indent", Value = "19", Description = "Increases the indentation of the selected lines (Tab)." },
                new() { Name = "Outdent", Value = "20", Description = "Decreases the indentation of the selected lines (Shift+Tab)." },
                new() { Name = "NewLine", Value = "21", Description = "Smart newline that continues lists and quotes (Enter)." },
                new() { Name = "Superscript", Value = "22", Description = "Toggles superscript on the current selection." },
                new() { Name = "Subscript", Value = "23", Description = "Toggles subscript on the current selection." },
                new() { Name = "ClearFormatting", Value = "24", Description = "Removes inline and block markdown formatting from the selected lines." },
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
                new() { Name = "Custom", Value = "7", Description = "Invokes a user-supplied callback." },
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
the eye button of the toolbar, or the `@bind-Mode` parameter.";

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
the eye button of the toolbar, or the `@bind-Mode` parameter."";";

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
    <BitButton Variant=""BitVariant.Outline"" IsEnabled=""commandsRef?.CanUndo ?? false"" OnClick=""Undo"">Undo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" IsEnabled=""commandsRef?.CanRedo ?? false"" OnClick=""Redo"">Redo</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""GetValue"">GetValue</BitButton>
</div>

<BitMarkdownEditor @ref=""commandsRef"" ShowToolbar=""false"" OnChange=""_ => InvokeAsync(StateHasChanged)"" />

<div>GetValue result:</div>
<pre class=""code-box"">@getValueResult</pre>";
    private readonly string example6CsharpCode = @"
private BitMarkdownEditor commandsRef = default!;
private string? getValueResult;

private async Task RunCommand(BitMarkdownEditorCommand command)
{
    await commandsRef.Run(command);
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

<BitMarkdownEditor @bind-FullScreen=""fullScreen"" Height=""10rem"" />";
    private readonly string example8CsharpCode = @"
private bool fullScreen;";

    private readonly string example9RazorCode = @"
<BitMarkdownEditor Placeholder=""Write your story here...""
                   SpellCheck=""false""
                   DebounceTime=""500""
                   IndentUnit=""@(""    "")"" />

<BitMarkdownEditor ReadOnly DefaultValue=""@readOnlyDefaultValue"" />";

    private readonly string example10RazorCode = @"
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

    private readonly string example11RazorCode = @"
<BitMarkdownEditor Dir=""BitDir.Rtl"" DefaultValue=""@rtlDefaultValue"" />";
}
