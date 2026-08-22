using Bit.BlazorUI;
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Tenants;

public partial class ManageMyTenantsPage
{
    private bool isLoading;
    private bool isSaving;
    private bool isInviting;
    private bool isGlobalAdmin;
    private Guid? leavingTenantId;
    private TenantDto? tenantToLeave;
    private bool isLeaveTenantDialogOpen;
    private TenantDto? currentTenant;
    private TenantDto newTenant = new();
    private TenantDto editingTenant = new();
    private List<TenantDto> tenants = [];
    private InviteUserToTenantRequestDto inviteRequest = new();
    private AppDataAnnotationsValidator createValidatorRef = default!;
    private AppDataAnnotationsValidator editValidatorRef = default!;
    private AppDataAnnotationsValidator inviteValidatorRef = default!;


    [AutoInject] IUserController userController = default!;
    [AutoInject] ITenantController tenantController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadTenants();
    }

    private async Task LoadTenants()
    {
        isLoading = true;

        try
        {
            var user = (await AuthenticationStateTask).User;
            // The tenant currently selected in the user's claims (if any) is highlighted in the list.
            // Global admins see every active tenant and can freely switch into any of them, so they never see the Accept (invitation) action.
            isGlobalAdmin = await AuthorizationService.IsAuthorized(user, AppFeatures.Management.Tenants_Manage_Global);
            tenants = await userController.GetTenants(CurrentCancellationToken);
            currentTenant = tenants.FirstOrDefault(t => t.Id == user.GetTenantId());
            ResetEditingTenant();
        }
        finally
        {
            isLoading = false;
        }
    }

    private bool IsSelected(TenantDto tenant) => tenant.Id == currentTenant?.Id;

    // A pending invitation (Accept action) is only relevant for regular users; global admins always just switch.
    private bool IsPendingInvitation(TenantDto tenant) => tenant.CurrentUserHasAcceptedThisTenantInvitation is false && isGlobalAdmin is false;

    // Whether the current user has a membership row for this tenant at all. Null means she has none, which only happens
    // for a global admin (she is listed every active tenant) - and leaving a tenant she never joined can only 404.
    private bool IsMemberOf(TenantDto tenant) => tenant.CurrentUserHasAcceptedThisTenantInvitation is not null;

    // Keyed off AcceptedOn exactly like the server: leaving a membership that was never accepted DELETES it (declining
    // the invitation) rather than resetting it, and that is irreversible without a fresh invite. Deliberately not
    // IsPendingInvitation, which also ands in isGlobalAdmin and so would promise the recoverable outcome to a global
    // admin whose row the server is about to delete.
    private bool IsDecliningInvitation(TenantDto tenant) => tenant.CurrentUserHasAcceptedThisTenantInvitation is false;

    // Reopening a section starts its form fresh, discarding any edits left over from a previous (un-saved) visit.
    private void OnSectionExpand(BitAccordionListOption section)
    {
        switch (section.Key)
        {
            case "create":
                newTenant = new();
                break;
            case "update":
                ResetEditingTenant();
                break;
        }
    }

    // Seeds the rename form with a fresh copy of the current tenant (empty when none is selected).
    private void ResetEditingTenant()
    {
        editingTenant = new();
        currentTenant?.Patch(editingTenant);
    }

    /// <summary>
    /// Switches into (or accepts the invitation of) the given tenant. Accepting and switching share the exact same code path,
    /// since the server accepts a pending invitation the first time the user switches into the tenant (See IdentityController.Refresh).
    /// </summary>
    private async Task SwitchTo(TenantDto tenant)
    {
        if (IsSelected(tenant)) return;

        if (await AuthManager.SwitchTenant(tenant.Id, CurrentCancellationToken))
        {
            await Refresh();
        }
    }

    private async Task CreateTenant()
    {
        if (isSaving) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isSaving = true;

        try
        {
            // Creating a tenant makes the current user its t-admin, so she gets switched into it right away.
            var createdTenant = await tenantController.Create(newTenant, CurrentCancellationToken);
            newTenant = new();

            if (await AuthManager.SwitchTenant(createdTenant.Id, CurrentCancellationToken))
            {
                await Refresh();
                return;
            }

            await LoadTenants();
        }
        catch (ResourceValidationException e)
        {
            createValidatorRef.DisplayErrors(e);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task UpdateTenant()
    {
        if (isSaving || currentTenant is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isSaving = true;

        try
        {
            // Update only ever targets the current tenant (See ITenantController.Update / AppFeatures.Management.Tenant_Manage).
            await tenantController.Update(editingTenant, CurrentCancellationToken);

            await LoadTenants();
        }
        catch (ResourceValidationException e)
        {
            editValidatorRef.DisplayErrors(e);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    /// <summary>
    /// Leaves the given tenant (whether it's the current one or not) without having to pick another tenant first.
    /// The server moves the user's affected sessions to her next accepted tenant or none, then the token gets refreshed
    /// so the client picks up the new (or empty) tenant claim.
    /// </summary>
    private void ConfirmLeaveTenant(TenantDto tenant)
    {
        tenantToLeave = tenant;
        isLeaveTenantDialogOpen = true;
    }

    private async Task LeaveTenant()
    {
        if (leavingTenantId is not null) return;

        if (tenantToLeave is null) return;

        var tenant = tenantToLeave;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        leavingTenantId = tenant.Id;

        try
        {
            var wasDecliningInvitation = IsDecliningInvitation(tenant);

            await userController.LeaveTenant(tenant.Id, CurrentCancellationToken);

            // Refreshing the token makes the server re-evaluate the tenant claim (falling back to another accepted tenant or none).
            await AuthManager.RefreshToken(requestedBy: "LeaveTenant");

            SnackBarService.Success(wasDecliningInvitation
                                    ? Localizer[nameof(AppStrings.DeclinedInvitationSuccessfullyMessage)]
                                    : Localizer[nameof(AppStrings.LeftTenantSuccessfullyMessage)]);

            await Refresh();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            leavingTenantId = null;
            tenantToLeave = null;
        }
    }

    private async Task InviteUser()
    {
        if (isInviting) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isInviting = true;

        try
        {
            await tenantController.InviteUser(inviteRequest, CurrentCancellationToken);

            inviteRequest = new();

            SnackBarService.Success(Localizer[nameof(AppStrings.UserInvitedSuccessfullyMessage)]);
        }
        catch (ResourceValidationException e)
        {
            inviteValidatorRef.DisplayErrors(e);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isInviting = false;
        }
    }

    private async Task Refresh()
    {
        await LoadTenants();
        PubSubService.Publish(ClientAppMessages.CURRENT_TENANT_CHANGED, currentTenant);
    }
}
