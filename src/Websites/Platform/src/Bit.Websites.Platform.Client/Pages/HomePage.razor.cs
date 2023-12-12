namespace Bit.Websites.Platform.Client.Pages;

public partial class HomePage
{
    private const string QUICK_START_SECTION_ID = "quick-start";

    private ElementReference quickStartRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(QUICK_START_SECTION_ID))
        {
            await JSRuntime.ScrollElementIntoView(quickStartRef);
        }
    }
}
