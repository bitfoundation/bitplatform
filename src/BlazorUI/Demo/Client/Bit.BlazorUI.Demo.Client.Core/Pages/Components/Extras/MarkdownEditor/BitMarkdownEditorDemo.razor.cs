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
                }
            ]
        },
    ];



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
    private BitMarkdownEditor advancedRef;



    private readonly string example1RazorCode = @"
<BitMarkdownEditor />";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""GetValue"">Get Value</BitButton>
<div style=""margin-top:1rem;display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditor @ref=""editorRef"" />
    <pre style=""width:100%"">
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
    <pre style=""padding:1rem;width:100%"">
        @bindingValue
    </pre>
</div>";
    private readonly string example4CsharpCode = @"
private string? bindingValue;";

    private readonly string example5RazorCode = @"
<div style=""display:flex;gap:1rem;height:300px"">
    <BitMarkdownEditor @bind-Value=""bindingValue"" />
    <pre style=""padding:1rem;width:100%"">
        @bindingValue
    </pre>
</div>";
    private readonly string example5CsharpCode = @"
private string? bindingValue;";
}
