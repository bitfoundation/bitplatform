using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Tenants;

public partial class ManageAllTenantsPage
{
    private bool isLoading;
    private AddOrEditTenantModal? modal;

    private BitDataGrid<TenantDto>? dataGrid;

    [AutoInject] ITenantManagementController tenantManagementController = default!;


    private async Task<BitDataGridReadResult<TenantDto>> LoadTenants(BitDataGridReadRequest req)
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var query = new ODataQuery
            {
                Top = req.Take, // null when exporting (CSV/Excel) so the server returns every matching row.
                Skip = req.Skip,
                OrderBy = req.Sorts.Count > 0
                    ? string.Join(", ", req.Sorts.Select(s => $"{s.ColumnId} {(s.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                    : $"{nameof(TenantDto.Name)} asc"
            };

            var filter = string.Join(" and ", req.Filters
                .Where(f => string.IsNullOrEmpty(f.Value?.ToString()) is false)
                .Select(f => $"contains(tolower({f.ColumnId}),'{f.Value!.ToString()!.ToLower().Replace("'", "''")}')"));
            if (string.IsNullOrEmpty(filter) is false)
            {
                query.Filter = filter;
            }

            var data = await tenantManagementController.WithQuery(query.ToString())
                                                       .GetTenants(req.CancellationToken);

            return new BitDataGridReadResult<TenantDto>(data!.Items!, (int)data!.TotalCount);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
            return new BitDataGridReadResult<TenantDto>([], 0);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RefreshData()
    {
        await dataGrid!.RefreshAsync();
    }

    private async Task CreateTenant()
    {
        await modal!.ShowModal(new TenantDto());
    }

    private async Task EditTenant(TenantDto tenant)
    {
        await modal!.ShowModal(tenant);
    }

    /// <summary>
    /// Deactivating a tenant kicks out all users that have this tenant in the list of their accepted tenants immediately.
    /// </summary>
    private async Task ToggleIsActive(TenantDto tenant)
    {
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        try
        {
            tenant.IsActive = tenant.IsActive is false;

            await tenantManagementController.Update(tenant, CurrentCancellationToken);
        }
        catch (KnownException exp)
        {
            SnackBarService.Error(exp.Message);
        }
        finally
        {
            await RefreshData();
        }
    }
}
