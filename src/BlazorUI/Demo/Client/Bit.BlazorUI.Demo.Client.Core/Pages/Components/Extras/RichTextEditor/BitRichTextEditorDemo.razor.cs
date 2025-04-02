namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.RichTextEditor;

public partial class BitRichTextEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Theme",
            Type = "BitRichTextEditorTheme?",
            DefaultValue = "null",
            Description = "The theme of the editor.",
            Href = "#rich-text-editor-theme",
            LinkType = LinkType.Link,
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "GetText",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current text content of the editor."
        },
        new()
        {
            Name = "GetHtml",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current html content of the editor."
        },
        new()
        {
            Name = "GetContent",
            Type = "Func<ValueTask<string>>",
            Description = "Gets the current content of the editor in JSON format."
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "rich-text-editor-theme",
            Name = "BitRichTextEditorTheme",
            Description = "",
            Items =
            [
                new() { Name = "Snow", Value = "0" },
                new() { Name = "Bubble", Value = "1" },
            ]
        }
    ];


    private BitRichTextEditor editorRef = default!;
    private string? result;
    private async Task GetText()
    {
        result = await editorRef.GetText();
    }
    private async Task GetHtml()
    {
        result = await editorRef.GetHtml();
    }
    private async Task GetContent()
    {
        result = await editorRef.GetContent();
    }



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";
    private readonly string example1CsharpCode = @"
private bool isBasicProPanelOpen;";
}
