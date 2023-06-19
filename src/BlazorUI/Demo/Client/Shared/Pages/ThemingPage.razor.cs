namespace Bit.BlazorUI.Demo.Client.Shared.Pages;

public partial class ThemingPage
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        await JSRuntime.InvokeVoidAsync("highlightSnippet");
    }
}
