//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Management;

public partial class OAuthClientsPage
{
    [AutoInject] private IOAuthClientManagementController oauthClientManagementController = default!;

    private enum Busy { None, Revoking }

    private bool isLoading = true;
    private Busy busyAction;
    private string? busyClientId;
    private List<OAuthClientDto> clients = [];

    protected override async Task OnInitAsync()
    {
        await LoadClients();

        await base.OnInitAsync();
    }

    private async Task LoadClients()
    {
        isLoading = true;

        try
        {
            clients = await oauthClientManagementController.GetAllClients(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }

    private OAuthClientDto? pendingClient;
    private bool isRevokeDialogOpen;

    // Confirmed, because it signs other people's tools out rather than the operator's own.
    private void AskToRevokeGrants(OAuthClientDto client) => (pendingClient, isRevokeDialogOpen) = (client, true);

    /// <summary>Ends access without removing the client; it may be authorized again.</summary>
    private async Task RevokeGrants()
    {
        if (pendingClient is not { } client) return;

        await Run(client, Busy.Revoking, async () =>
        {
            var revoked = await oauthClientManagementController.RevokeGrants(new() { ClientId = client.ClientId }, CurrentCancellationToken);
            SnackBarService.Success(Localizer[nameof(AppStrings.OAuthClientRevokedMessage), revoked.ToString("N0", CultureInfo.CurrentUICulture)]);
        });
    }

    /// <summary>Reloads rather than patching the row: a stale grant count is the one thing not to act on here.</summary>
    private async Task Run(OAuthClientDto client, Busy action, Func<Task> operation)
    {
        (busyClientId, busyAction) = (client.ClientId, action);

        try
        {
            await operation();
            await LoadClients();
        }
        finally
        {
            (busyClientId, busyAction, pendingClient) = (null, Busy.None, null);
        }
    }

    private string KindLabel(OAuthClientKind kind) => kind switch
    {
        OAuthClientKind.Configured => Localizer[nameof(AppStrings.OAuthClientKindConfigured)],
        _ => Localizer[nameof(AppStrings.OAuthClientKindSelfDescribed)]
    };

    private static BitColor KindColor(OAuthClientKind kind) => kind switch
    {
        // Only a client an operator put in configuration was vouched for by anybody here.
        OAuthClientKind.Configured => BitColor.Success,
        _ => BitColor.Info
    };
}
