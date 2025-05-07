//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Security;

public partial class RolesPage
{
    private bool isLoadingRoles;
    private bool isLoadingUsers;
    private string? newRoleName;
    private string? editRoleName;
    private string? loadingRoleKey;
    private BitNavItem? selectedRole;
    private int? maxPrivilegedSessions;
    private List<BitNavItem> allRoles = [];
    private List<BitNavItem> allUsers = [];
    private List<UserDto> selectedRoleUsers = [];
    private List<BitNavItem> allPermissions = [];
    private List<string?> selectedRolePermissions = [];
    private CancellationTokenSource? loadRoleDataCts;


    [AutoInject] IRoleController roleController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();


        allPermissions = [.. AppPermissions.GetAll().GroupBy(p => p.Group).Select(g => new BitNavItem
        {
            Text = g.Key.Name,
            ChildItems = [.. g.Select(p => new BitNavItem
            {
                Key = p.Value,
                Text = p.Key
            })]
        })];

        await Task.WhenAll(LoadAllRoles(), LoadAllUsers());
    }


    private async Task LoadAllRoles()
    {
        try
        {
            isLoadingRoles = true;

            allRoles = [.. (await roleController.GetAllRoles(CurrentCancellationToken)).Select(r => new BitNavItem
            {
                Key = r.Id.ToString(),
                Text = r.NormalizedName ?? r.Name ?? string.Empty,
                Data = r
            })];
        }
        finally
        {
            isLoadingRoles = false;
        }
    }

    private async Task LoadAllUsers()
    {
        try
        {
            isLoadingUsers = true;

            allUsers = [.. (await roleController.GetAllUsers(CurrentCancellationToken)).Select(u => new BitNavItem
            {
                Key = u.Id.ToString(),
                Text = u.DisplayName ?? string.Empty,
                Data = u
            })];
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private async Task HandleOnSelectRole(BitNavItem item)
    {
        if (item is null) return;

        try
        {
            if (loadRoleDataCts is not null)
            {
                await loadRoleDataCts.CancelAsync();
                loadRoleDataCts.Dispose();
            }

            loadRoleDataCts = new();

            selectedRole = item;
            loadingRoleKey = item.Key;
            editRoleName = selectedRole.Text;

            await Task.WhenAll(LoadRoleUsers((RoleDto)item.Data!, loadRoleDataCts.Token),
                               LoadRoleClaims(Guid.Parse(item.Key!), loadRoleDataCts.Token));
        }
        finally
        {
            if (loadingRoleKey == item.Key)
            {
                loadingRoleKey = null;
            }
        }
    }

    private async Task LoadRoleUsers(RoleDto roleDto, CancellationToken cancellationToken)
    {
        selectedRoleUsers = await roleController.GetUsers(roleDto, cancellationToken);
    }

    private async Task LoadRoleClaims(Guid roleId, CancellationToken cancellationToken)
    {
        var claims = await roleController.GetClaims(roleId, cancellationToken);
        selectedRolePermissions = [.. claims.Select(c => c.ClaimValue)];
    }

    private bool IsPermissionAssigned(BitNavItem item)
    {
        return selectedRolePermissions.Any(p => item.Key == p);
    }

    private async Task AddRole()
    {
        if (string.IsNullOrWhiteSpace(newRoleName)) return;

        await roleController.Create(new RoleDto { Name = newRoleName }, CurrentCancellationToken);

        newRoleName = null;

        await LoadAllRoles();
    }

    private async Task EditRole()
    {
        if (string.IsNullOrWhiteSpace(editRoleName) || selectedRole is null) return;

        await roleController.Update(new RoleDto { Id = Guid.Parse(selectedRole.Key!), Name = editRoleName }, CurrentCancellationToken);

        editRoleName = null;

        await LoadAllRoles();
    }

    private async Task SaveMaxPrivilegedSessions()
    {

    }
}




