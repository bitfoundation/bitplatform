using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Pages.Tenants;

public partial class AddOrEditTenantModal
{
    [AutoInject] ITenantController tenantController = default!;
    [AutoInject] ITenantManagementController tenantManagementController = default!;

    /// <summary>
    /// Passes the created/updated tenant.
    /// </summary>
    [Parameter] public EventCallback<TenantDto> OnSave { get; set; }

    private bool isOpen;
    private bool isSaving;
    private TenantDto tenant = new();
    private EditForm editForm = default!;
    private AppDataAnnotationsValidator validatorRef = default!;

    private bool isChanged => editForm?.EditContext?.IsModified() is true;

    public async Task ShowModal(TenantDto tenantToShow)
    {
        await InvokeAsync(() =>
        {
            isOpen = true;
            tenant = new();
            tenantToShow.Patch(tenant);
            StateHasChanged();
        });
    }

    private async Task Save()
    {
        if (isSaving) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        isSaving = true;

        try
        {
            TenantDto savedTenant;

            if (tenant.Id == default)
            {
                savedTenant = await tenantController.Create(tenant, CurrentCancellationToken);
            }
            else
            {
                savedTenant = await tenantManagementController.Update(tenant, CurrentCancellationToken);
            }

            await OnSave.InvokeAsync(savedTenant);
            isOpen = false;
        }
        catch (ResourceValidationException e)
        {
            validatorRef.DisplayErrors(e);
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

    private void OnNavigation(LocationChangingContext args)
    {
        args.PreventNavigation();
        if (isChanged) return;
        isOpen = false;
    }
}
