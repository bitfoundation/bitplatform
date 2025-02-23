using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class Header : AppComponentBase
{
    private string? pageTitle;
    private string? pageSubtitle;
    private Action unsubscribePageTitleChanged = default!;


    [CascadingParameter(Name = Parameters.IsAuthenticated)] public bool? IsAuthenticated { get; set; }


    protected override async Task OnInitAsync()
    {
        unsubscribePageTitleChanged = PubSubService.Subscribe(ClientPubSubMessages.PAGE_TITLE_CHANGED, async payload =>
        {
            (pageTitle, pageSubtitle) = ((string, string))payload!;

            StateHasChanged();
        });

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up button hrefs are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
    }


    private void OpenNavPanel()
    {
        PubSubService.Publish(ClientPubSubMessages.OPEN_NAV_PANEL);
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribePageTitleChanged?.Invoke();
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;

        await base.DisposeAsync(disposing);
    }
}
