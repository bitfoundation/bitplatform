
namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class GettingStartedPage
{
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
