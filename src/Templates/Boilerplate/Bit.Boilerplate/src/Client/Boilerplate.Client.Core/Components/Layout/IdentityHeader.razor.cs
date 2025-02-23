﻿using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class IdentityHeader : AppComponentBase
{
    private string? backLinkPayload;
    private Action unsubscribeUpdateBackLink = default!;


    [CascadingParameter] private BitDir? currentDir { get; set; }
    [CascadingParameter(Name = Parameters.IsCrossLayoutPage)] private bool? isCrossLayoutPage { get; set; }


    protected override async Task OnInitAsync()
    {
        unsubscribeUpdateBackLink = PubSubService.Subscribe(ClientPubSubMessages.UPDATE_IDENTITY_HEADER_BACK_LINK, async payload =>
        {
            backLinkPayload = (string?)payload;

            await InvokeAsync(StateHasChanged);
        });

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;

        await base.OnInitAsync();
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up button hrefs are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
    }

    private async Task HandleBackLinkClick()
    {
        PubSubService.Publish(ClientPubSubMessages.IDENTITY_HEADER_BACK_LINK_CLICKED, backLinkPayload);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribeUpdateBackLink?.Invoke();
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;

        await base.DisposeAsync(disposing);
    }
}
