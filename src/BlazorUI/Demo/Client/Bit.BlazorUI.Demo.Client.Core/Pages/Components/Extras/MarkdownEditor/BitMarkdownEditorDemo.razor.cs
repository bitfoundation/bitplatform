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
<BitButton OnClick=""GetValue"">Get Value</BitButton>
<div style=""margin-top:1rem;display:flex;flex-direction:row;gap:1rem;height:300px"">
    <BitMarkdownEditor @ref=""editorRef"" />
    <pre style=""width:100%"">
        @value
    </pre>
</div>";
    private readonly string example1CsharpCode = @"
private BitMarkdownEditor editorRef = default!;
private string? value;
private async Task GetValue()
{
    value = await editorRef.GetValue();
}";
}
