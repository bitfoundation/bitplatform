namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class HomePage
{
    /// <summary>
    /// The page's code boxes hold constant strings, so one pass once is all Prism ever needs here.
    /// The showcase section re-highlights its own box when the reader picks a different component
    /// (see PopularComponents), and nothing else on the page changes.
    /// </summary>
    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
