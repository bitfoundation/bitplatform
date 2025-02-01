namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class ThemingPage
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
