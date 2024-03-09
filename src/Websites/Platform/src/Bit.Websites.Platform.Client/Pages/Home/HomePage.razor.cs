using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class HomePage
{
    private const string QUICK_START_SECTION_ID = "quick-start";

    private ElementReference quickStartRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(QUICK_START_SECTION_ID))
        {
            await quickStartRef.ScrollIntoView();
        }
    }
}
