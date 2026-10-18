namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class TrimmingPage
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
