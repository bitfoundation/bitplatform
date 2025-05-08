//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class RolesPage
{
    private bool isLoadingRoles;
    private bool isLoadingUsers;
    private string? newRoleName;
    private string? editRoleName;
    private string? loadingRoleKey;
    private BitNavItem? selectedRole;
    private int? maxPrivilegedSessions;
    private string? notificationMessage;
    private List<UserDto> allUsers = [];
    private List<UserDto> selectedRoleUsers = [];
    private List<BitNavItem> allPermissions = [];
    private List<BitNavItem> allRoleNavItems = [];
    private CancellationTokenSource? loadRoleDataCts;
    private List<string?> selectedRolePermissions = [];


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

            allRoleNavItems = [.. (await roleController.GetAllRoles(CurrentCancellationToken)).Select(r => new BitNavItem
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

            allUsers = [.. (await roleController.GetAllUsers(CurrentCancellationToken))];
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
            var id = Guid.Parse(item.Key!);

            await Task.WhenAll(LoadRoleUsers(id, loadRoleDataCts.Token),
                               LoadRolePermissions(id, loadRoleDataCts.Token));
        }
        finally
        {
            if (loadingRoleKey == item.Key)
            {
                loadingRoleKey = null;
            }
        }
    }

    private async Task LoadRoleUsers(Guid roleId, CancellationToken cancellationToken)
    {
        selectedRoleUsers = [.. (await roleController.GetUsers(roleId, cancellationToken))];
    }

    private async Task LoadRolePermissions(Guid roleId, CancellationToken cancellationToken)
    {
        var claims = await roleController.GetClaims(roleId, cancellationToken);
        selectedRolePermissions = [.. claims.Select(c => c.ClaimValue)];
    }

    private bool IsPermissionAssigned(BitNavItem item)
    {
        return selectedRolePermissions.Any(p => item.Key == p);
    }

    private bool IsUserAssigned(UserDto user)
    {
        return selectedRoleUsers.Any(u => user.Id == u.Id);
    }

    private async Task AddRole()
    {
        if (string.IsNullOrWhiteSpace(newRoleName)) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleController.Create(new RoleDto { Name = newRoleName }, CurrentCancellationToken);

        newRoleName = null;

        await LoadAllRoles();
    }

    private async Task EditRole()
    {
        if (string.IsNullOrWhiteSpace(editRoleName) || selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleController.Update(new RoleDto { Id = Guid.Parse(selectedRole.Key!), Name = editRoleName }, CurrentCancellationToken);

        editRoleName = null;

        await LoadAllRoles();
    }

    private async Task TogglePermission(BitNavItem item)
    {
        if (selectedRole is null) return;

        ToggleRolePermissionDto dto = new()
        {
            Value = item.Key,
            RoleId = Guid.Parse(selectedRole.Key!),
            IsAdd = IsPermissionAssigned(item) is false
        };

        await roleController.TogglePermission(dto, CurrentCancellationToken);
    }

    private async Task ToggleUser(UserDto user)
    {
        if (selectedRole is null) return;

        ToggleRoleUserDto dto = new()
        {
            UserId = user.Id,
            RoleId = Guid.Parse(selectedRole.Key!),
            IsAdd = IsUserAssigned(user) is false
        };

        await roleController.ToggleUser(dto, CurrentCancellationToken);
    }

    private async Task SaveMaxPrivilegedSessions()
    {
        if (maxPrivilegedSessions.HasValue is false) return;

        await roleController.SaveMaxPrivilegedSessions(maxPrivilegedSessions.Value, CurrentCancellationToken);
    }

    private async Task SendNotification()
    {
        if (selectedRole is null) return;

        await roleController.SendNotification(new() { RoleId = Guid.Parse(selectedRole.Key!), Message = notificationMessage }, CurrentCancellationToken);
    }
}




