using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class Header : AppComponentBase
{
    private string? pageTitle;
    private string? pageSubtitle;
    private bool showGoBackButton;
    private Action unsubscribePageTitleChanged = default!;

    [CascadingParameter] private BitDir? currentDir { get; set; }

    [AutoInject] private History history = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        unsubscribePageTitleChanged = PubSubService.Subscribe(ClientPubSubMessages.PAGE_DATA_CHANGED, async payload =>
        {
            (pageTitle, pageSubtitle, showGoBackButton) = ((string?, string?,bool))payload!;

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

    private async Task GoBack()
    {
        await history.GoBack();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribePageTitleChanged?.Invoke();
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }
}
