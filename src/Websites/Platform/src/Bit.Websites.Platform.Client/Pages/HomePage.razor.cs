namespace Bit.Websites.Platform.Client.Pages;

public partial class HomePage
{
    private const string QuickStartSectionId = "quick-start";

    //private ElementReference quickStartRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(QuickStartSectionId))
        {
            //await JSRuntime.ScrollElementIntoView(quickStartRef);
        }
    }
}
