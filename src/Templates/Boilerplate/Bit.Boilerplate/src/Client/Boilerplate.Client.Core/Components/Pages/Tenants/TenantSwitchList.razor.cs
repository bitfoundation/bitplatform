using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Tenants;

/// <summary>
/// The tenants a user can get into right where she is, such as on the not-authorized page a user without a selected
/// tenant gets: her pending invitations first, each with its Accept, then the tenants she can switch into. Accepting an
/// invitation is switching into its tenant (See IdentityController.Refresh), and what switching does is up to
/// <see cref="OnSwitch"/>.
/// </summary>
public partial class TenantSwitchList
{
    private Guid? switchingTenantId;
    private TenantDto[] invitations = [];
    private TenantDto[] switchTargets = [];


    /// <summary>
    /// The tenants UserController.GetTenants returns, in its order.
    /// </summary>
    [Parameter] public IReadOnlyList<TenantDto> Tenants { get; set; } = [];

    /// <summary>
    /// A global admin is listed every active tenant and always just switches, so nothing is an invitation to her (See
    /// ManageMyTenantsPage.IsPendingInvitation).
    /// </summary>
    [Parameter] public bool IsGlobalAdmin { get; set; }

    /// <summary>
    /// Switches into the tenant. Its button shows it's busy, and the others wait disabled, until this completes.
    /// </summary>
    [Parameter] public EventCallback<TenantDto> OnSwitch { get; set; }


    protected override async Task OnParamsSetAsync()
    {
        await base.OnParamsSetAsync();

        invitations = [.. Tenants.Where(IsPendingInvitation)];
        switchTargets = [.. Tenants.Where(tenant => IsPendingInvitation(tenant) is false)];
    }

    private bool IsPendingInvitation(TenantDto tenant) => IsGlobalAdmin is false && tenant.CurrentUserMembershipStatus is TenantMembershipStatus.Invited;

    private bool IsSwitching(TenantDto tenant) => switchingTenantId == tenant.Id;

    private bool IsWaiting(TenantDto tenant) => switchingTenantId is not null && switchingTenantId != tenant.Id;

    private static string? GetDisplayName(TenantDto tenant) => string.IsNullOrWhiteSpace(tenant.Title) ? tenant.Name : tenant.Title;

    private async Task SwitchTo(TenantDto tenant)
    {
        if (switchingTenantId is not null) return;

        switchingTenantId = tenant.Id;

        try
        {
            await OnSwitch.InvokeAsync(tenant);
        }
        finally
        {
            switchingTenantId = null;
        }
    }
}
