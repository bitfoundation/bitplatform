namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.MarkdownEditor;

public partial class BitMarkdownEditorDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
    ];



    private BitMarkdownEditor editorRef = default!;
    private string? value;
    private async Task GetValue()
    {
        value = await editorRef.GetValue();
    }



    private readonly string example1RazorCode = @"
<BitMarkdownViewer Markdown=""@(""# Marked in the browser\n\nRendered by **marked**."")"" />";
}
