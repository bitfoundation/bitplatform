namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownEditor;

public partial class BitMarkdownEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "The default text value of the editor to use at initialization.",
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
            Name = "GetValue",
            Type = "Func<ValueTask<string>>",
            DefaultValue = "",
            Description = "Returns the current value of the editor."
        },
        new()
        {
            Name = "Run",
            Type = "Func<BitMarkdownEditorCommand, ValueTask>",
            DefaultValue = "",
            Description = "Runs a specific command on the editor.",
            LinkType = LinkType.Link,
            Href = "#command-enum"
        },
        new()
        {
            Name = "Add",
            Type = "Func<string, BitMarkdownEditorContentType, ValueTask>",
            DefaultValue = "",
            Description = "Adds a specific content to the editor.",
            LinkType = LinkType.Link,
            Href = "#content-type-enum"
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "command-enum",
            Name = "BitMarkdownEditorCommand",
            Description = "Available commands to run by a BitMarkdownEditor on its current value.",
            Items =
            [
                new()
                {
                    Name= "Heading",
                    Description="Makes the current line a heading.",
                    Value="0",
                },
                new()
                {
                    Name= "Bold",
                    Description="Makes the current selection text bold.",
                    Value="1",
                },
                new()
                {
                    Name= "Italic",
                    Description="Makes the current selection text italic.",
                    Value="2",
                },
                new()
                {
                    Name= "Link",
                    Description="Makes the current selection text a link.",
                    Value="3",
                },
                new()
                {
                    Name= "Picture",
                    Description="Makes the current selection text an image.",
                    Value="4",
                },
                new()
                {
                    Name= "Quote",
                    Description="Makes the current selection text a quote message.",
                    Value="5",
                },
                new()
                {
                    Name= "Code",
                    Description="Makes the current selection text a code phrase.",
                    Value="6",
                },
                new()
                {
                    Name= "CodeBlock",
                    Description="Makes the current selection text a code block.",
                    Value="7",
                }
            ]
        },
        new()
        {
            Id = "content-type-enum",
            Name = "BitMarkdownEditorContentType",
            Description = "The type of the content to add to the BitMarkdownEditor.",
            Items =
            [
                new()
                {
                    Name= "Inline",
                    Description="Inline content type.",
                    Value="0",
                },
                new()
                {
                    Name= "Block",
                    Description="Block content type.",
                    Value="1",
                },
                new()
                {
                    Name= "Wrap",
                    Description="Wrap content type.",
                    Value="2",
                }
            ]
        },
    ];


    private string? introValue = "# BitMarkdownEditor in action\n\n- Ctrl+H  =>  Heading\n- Ctrl+B  =>  Bold\n- Ctrl+I  =>  Italic\n- Ctrl+L  =>  Link\n- Ctrl+P  =>  Picture/Image\n- Ctrl+Q  =>  Quote\n- auto handling ordered/unordered lists\n- auto handling Ctrl+X and Ctrl+C\n\n### Missing features:\n1. Undo/Redo\n2. Multi-level unordered lists\n3. Tab to go deeper list level\n\nStart typing here...";

    private BitMarkdownEditor editorRef = default!;
    private string? value;
    private async Task GetValue()
    {
        value = await editorRef.GetValue();
    }

    private string? onChangeValue;

    private string? bindingValue;

    private bool showPreview;
    private string? advancedValue;
    private BitMarkdownEditor advancedRef = default!;



    private readonly string example1RazorCode = @"
<div style=""height:300px"">
    <BitMarkdownEditor />
</div>";

    private readonly string example2RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditor @ref=""editorRef"" />
    <BitButton OnClick=""GetValue"">=></BitButton>
    <pre style=""padding:1rem;width:100%"">
        @value
    </pre>
</div>";
    private readonly string example2CsharpCode = @"
private BitMarkdownEditor editorRef = default!;
private string? value;
private async Task GetValue()
{
    value = await editorRef.GetValue();
}";

    private readonly string example3RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditor DefaultValue=""# This is the default value"" OnChange=""v => onChangeValue = v"" />
    <pre style=""padding:1rem;width:100%"">
        @onChangeValue
    </pre>
</div>";
    private readonly string example3CsharpCode = @"
private string? onChangeValue;";

    private readonly string example4RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditor @bind-Value=""bindingValue"" />
    <textarea @bind-value=""@bindingValue"" @bind-value:event=""oninput"" style=""width:100%;background:transparent""/>
</div>";
    private readonly string example4CsharpCode = @"
private string? bindingValue;";

    private readonly string example5RazorCode = @"
<div style=""display:flex;gap:1rem;margin-bottom:1rem"">
    <BitToggleButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" OnText=""Write"" OffText=""Preview"" @bind-IsChecked=""showPreview"" />
            
    <div style=""flex-grow:1""></div>

    <div style=""display:@(showPreview ? ""none"" : ""flex"");gap:0.5rem;align-items:center"">
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Heading"" 
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Heading)"">H</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Bold""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Bold)"">B</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Italic""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Italic)"">I</BitButton>
        |
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Link""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Link)"">L</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Picture""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Picture)"">P</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Quote""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Quote)"">Q</BitButton>
        |
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Code""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.Code)"">C</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Code block""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorCommand.CodeBlock)"">CB</BitButton>
    </div>
</div>

<div style=""height:300px"">
    <BitMarkdownEditor @ref=""advancedRef"" @bind-Value=""advancedValue"" 
                       Style=""@($""display:{(showPreview ? ""none"" : ""block"")}"")"" />
    <BitMarkdownViewer Markdown=""@advancedValue"" 
                       Style=""@($""display:{(showPreview ? ""block"" : ""none"")}"")"" />
</div>";
    private readonly string example5CsharpCode = @"
private bool showPreview;
private string? advancedValue;
private BitMarkdownEditor advancedRef = default!;";
}
