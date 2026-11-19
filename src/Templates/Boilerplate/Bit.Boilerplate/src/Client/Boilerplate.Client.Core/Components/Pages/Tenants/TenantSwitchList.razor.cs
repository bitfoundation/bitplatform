using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Tenants;

public partial class TenantSwitchList
{
    private Guid? switchingTenantId;
    private TenantDto[] invitations = [];
    private TenantDto[] switchTargets = [];


    [Parameter] public IReadOnlyList<TenantDto> Tenants { get; set; } = [];

    /// <summary>A global admin is listed every active tenant, so none of them is an invitation to her.</summary>
    [Parameter] public bool IsGlobalAdmin { get; set; }

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
