using Boilerplate.Shared.Dtos.Identity;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components;

public partial class SignalRHandler : AppComponentBase
{
    [AutoInject] private MessageBoxService messageBoxService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;

    private HubConnection? hubConnection;

    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        authManager.AuthenticationStateChanged += AuthenticationStateChanged;

        await ConnectSignalR();
    }

    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            await ConnectSignalR();
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
    }

    private async Task ConnectSignalR()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        var access_token = await AuthTokenProvider.GetAccessTokenAsync();

        hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Configuration.GetServerAddress()}/app-hub?access_token={access_token}")
            .Build();

        hubConnection.On<string>("TwoFactorToken", async (token) =>
        {
            await messageBoxService.Show(Localizer[nameof(AppStrings.TwoFactorTokenPushText), token]);

            // The following code block is not required for Bit.BlazorUI components to perform UI changes. However, it may be necessary in other scenarios.
            /*await InvokeAsync(async () =>
            {
                StateHasChanged();
            });*/

            // You can also leverage IPubSubService to notify other components in the application.
        });

        await hubConnection.StartAsync(CurrentCancellationToken);
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        await base.DisposeAsync(disposing);
    }
}
