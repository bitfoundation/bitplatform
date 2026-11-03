namespace Boilerplate.LandingPage.Client.Components.Common;

public partial class StoreRating
{
    [AutoInject] private IStoreListingClient storeListingClient = default!;

    [Parameter, EditorRequired] public string AppleAppId { get; set; } = default!;

    [Parameter] public int MinimumRatings { get; set; } = 25;

    [Parameter] public string? Class { get; set; }

    [PersistentState] public StoreListing? Listing { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Listing ??= await storeListingClient.GetAppStoreListingAsync(AppleAppId, CancellationToken.None);

        await base.OnInitializedAsync();
    }
}
