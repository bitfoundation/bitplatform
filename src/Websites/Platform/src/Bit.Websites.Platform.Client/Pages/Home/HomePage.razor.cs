using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Home;

public partial class HomePage
{
    private const string OUR_PRODUCTS_SECTION_ID = "our-products";

    private ElementReference ourProductsRef = default!;

    protected override async Task OnAfterFirstRenderAsync()
    {
        if (NavigationManager.Uri.Contains(OUR_PRODUCTS_SECTION_ID))
        {
            await ourProductsRef.ScrollIntoView();
        }
    }
}
