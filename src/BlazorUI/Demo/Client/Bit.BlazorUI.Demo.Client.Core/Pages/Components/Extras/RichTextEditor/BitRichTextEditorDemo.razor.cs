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



    private readonly string example1RazorCode = @"
<BitRichTextEditor />";
    private readonly string example1CsharpCode = @"
private bool isBasicProPanelOpen;";
}
