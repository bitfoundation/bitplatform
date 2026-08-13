namespace Bit.BlazorUI.Demo.Client.Core.Pages.Theming;

public partial class ThemingPage
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
