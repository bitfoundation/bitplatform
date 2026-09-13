
namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class GettingStartedPage
{
    // Bound by the live "Rendered result" panel of step 5, which renders the same markup the code
    // box above it shows - so the reader can compare what they pasted with what it produces.
    private string? _name;



    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoid("highlightSnippet");
    }
}
