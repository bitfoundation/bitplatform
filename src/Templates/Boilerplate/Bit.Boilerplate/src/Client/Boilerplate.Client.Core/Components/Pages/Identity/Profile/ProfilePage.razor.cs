//+:cnd:noEmit
//#if (signalr == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

[Authorize]
public partial class ProfilePage
{
    private UserDto? user;
    private bool isLoading;
    //#if (signalr == true)
    private HubConnection? hubConnection;
    private BitSnackBar snackBar = default!;
    //#endif

    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            user = await userController.GetCurrentUser(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }

    //#if (signalr == true)
    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        var access_token = await AuthTokenProvider.GetAccessTokenAsync();

        hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(Configuration.GetServerAddress(), $"identity-hub?access_token={access_token}"))
            .Build();

        hubConnection.On<UserSessionDto>("NewUserSession", async (userSession) =>
        {
            await InvokeAsync(async () =>
            {
                await snackBar.Info("New user session", userSession.Device);

                StateHasChanged();
            });
        });

        await hubConnection.StartAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        await base.DisposeAsync(disposing);
    }
    //#endif
}
