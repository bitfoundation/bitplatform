using Bit.BlazorUI.Legacy;
namespace Bit.BlazorUI.Legacy.Demo.MarkdownEditor;

public partial class BitMarkdownEditorLegacyDemo
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
            Type = "Func<BitMarkdownEditorLegacyCommand, ValueTask>",
            DefaultValue = "",
            Description = "Runs a specific command on the editor.",
            LinkType = LinkType.Link,
            Href = "#command-enum"
        },
        new()
        {
            Name = "Add",
            Type = "Func<string, BitMarkdownEditorLegacyContentType, ValueTask>",
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
            Name = "BitMarkdownEditorLegacyCommand",
            Description = "Available commands to run by a BitMarkdownEditorLegacy on its current value.",
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
            Name = "BitMarkdownEditorLegacyContentType",
            Description = "The type of the content to add to the BitMarkdownEditorLegacy.",
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


    private string? introValue = "# BitMarkdownEditorLegacy in action\n\n- Ctrl+H  =>  Heading\n- Ctrl+B  =>  Bold\n- Ctrl+I  =>  Italic\n- Ctrl+L  =>  Link\n- Ctrl+P  =>  Picture/Image\n- Ctrl+Q  =>  Quote\n- auto handling ordered/unordered lists\n- auto handling Ctrl+X and Ctrl+C\n- Ctrl+Z  =>  Undo\n- Ctrl+Y / Ctrl+Shift+Z  =>  Redo\n\n### Missing features:\n1. Multi-level unordered lists\n2. Tab to go deeper list level\n\nStart typing here...";

    private BitMarkdownEditorLegacy editorRef = default!;
    private string? value;
    private async Task GetValue()
    {
        value = await editorRef.GetValue();
    }

    private string? onChangeValue;

    private string? bindingValue;

    private bool showPreview;
    private string? advancedValue;
    private BitMarkdownEditorLegacy advancedRef = default!;



    private readonly string example1RazorCode = @"
<div style=""height:300px"">
    <BitMarkdownEditorLegacy />
</div>";

    private readonly string example2RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditorLegacy @ref=""editorRef"" />
    <BitButton OnClick=""GetValue"">=></BitButton>
    <pre style=""padding:1rem;width:100%"">
        @value
    </pre>
</div>";
    private readonly string example2CsharpCode = @"
private BitMarkdownEditorLegacy editorRef = default!;
private string? value;
private async Task GetValue()
{
    value = await editorRef.GetValue();
}";

    private readonly string example3RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditorLegacy DefaultValue=""# This is the default value"" OnChange=""v => onChangeValue = v"" />
    <pre style=""padding:1rem;width:100%"">
        @onChangeValue
    </pre>
</div>";
    private readonly string example3CsharpCode = @"
private string? onChangeValue;";

    private readonly string example4RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditorLegacy @bind-Value=""bindingValue"" />
    <textarea @bind-value=""@bindingValue"" @bind-value:event=""oninput"" style=""width:100%;background:transparent""/>
</div>";
    private readonly string example4CsharpCode = @"
private string? bindingValue;";

    private readonly string example5RazorCode = @"
<div style=""height:300px"">
    <BitMarkdownEditorLegacy DefaultValue=""# Undo/Redo Demo"" />
</div>";

    private readonly string example6RazorCode = @"
<div style=""display:flex;gap:1rem;margin-bottom:1rem"">
    <BitToggleButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" OnText=""Write"" OffText=""Preview"" @bind-IsChecked=""showPreview"" />
            
    <div style=""flex-grow:1""></div>

    <div style=""display:@(showPreview ? ""none"" : ""flex"");gap:0.5rem;align-items:center"">
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Heading"" 
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Heading)"">H</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Bold""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Bold)"">B</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Italic""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Italic)"">I</BitButton>
        |
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Link""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Link)"">L</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Picture""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Picture)"">P</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Quote""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Quote)"">Q</BitButton>
        |
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Code""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.Code)"">C</BitButton>
        <BitButton Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" FixedColor Title=""Code block""
                   OnClick=""async () => await advancedRef.Run(BitMarkdownEditorLegacyCommand.CodeBlock)"">CB</BitButton>
    </div>
</div>

<div style=""height:300px"">
    <BitMarkdownEditorLegacy @ref=""advancedRef"" @bind-Value=""advancedValue"" 
                       Style=""@($""display:{(showPreview ? ""none"" : ""block"")}"")"" />
    <BitMarkdownViewerLegacy Markdown=""@advancedValue"" 
                       Style=""@($""display:{(showPreview ? ""block"" : ""none"")}"")"" />
</div>";
    private readonly string example6CsharpCode = @"
private bool showPreview;
private string? advancedValue;
private BitMarkdownEditorLegacy advancedRef = default!;";
}
