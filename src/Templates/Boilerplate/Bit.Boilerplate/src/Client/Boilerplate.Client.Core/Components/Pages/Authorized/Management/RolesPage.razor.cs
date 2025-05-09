//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class RolesPage
{
    private bool isLoadingRoles = true;
    private bool isLoadingUsers = true;
    private string? newRoleName;
    private string? editRoleName;
    private string? loadingRoleKey;
    private BitNavItem? selectedRole;
    private int? maxPrivilegedSessions;
    private string? notificationMessage;
    private List<UserDto> allUsers = [];
    private List<BitNavItem> roleNavItems = [];
    private List<UserDto> selectedRoleUsers = [];
    private List<BitNavItem> permissionNavItems = [];
    private CancellationTokenSource? loadRoleDataCts;
    private List<RoleClaimDto> selectedRoleClaims = [];


    [AutoInject] IRoleController roleController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        permissionNavItems = [.. AppPermissions.GetAll().GroupBy(p => p.Group).Select(g => new BitNavItem
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
            roleNavItems = [.. (await roleController.GetAllRoles(CurrentCancellationToken)).Select(r => new BitNavItem
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
                using var currentCts = loadRoleDataCts;
                loadRoleDataCts = new();

                if (currentCts.IsCancellationRequested is false)
                {
                    await currentCts.CancelAsync();
                }
            }

            loadRoleDataCts = new();

            selectedRole = item;
            loadingRoleKey = item.Key;
            editRoleName = selectedRole.Text;
            var id = Guid.Parse(item.Key!);

            await Task.WhenAll(LoadRoleUsers(id, loadRoleDataCts.Token),
                               LoadRoleClaims(id, loadRoleDataCts.Token));
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
        selectedRoleUsers = await roleController.GetUsers(roleId, cancellationToken);
    }

    private async Task LoadRoleClaims(Guid roleId, CancellationToken cancellationToken)
    {
        selectedRoleClaims = await roleController.GetClaims(roleId, cancellationToken);

        var maxPrivilegedSessionsValue = selectedRoleClaims.FirstOrDefault(c => c.ClaimType == AppClaimTypes.MAX_PRIVILEGED_SESSIONS)?.ClaimValue;
        maxPrivilegedSessions = maxPrivilegedSessionsValue is null
                                ? null
                                : int.Parse(maxPrivilegedSessionsValue, CultureInfo.InvariantCulture);

        SetClaimsToPermissionNavItems();
    }

    private bool IsPermissionAssigned(BitNavItem item)
    {
        if (item.ChildItems.Count == 0)
            return selectedRoleClaims.Any(p => p.ClaimValue == item.Key);

        return item.ChildItems.Any(IsPermissionAssigned);
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

    private async Task AddPermissions(BitNavItem parent)
    {
        if (selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var itemsToAdd = parent.ChildItems.Where(ci => IsPermissionAssigned(ci) is false);

        var dtos = itemsToAdd.Select(i => new RoleClaimRequestDto
        {
            ClaimValue = i.Key,
            ClaimType = AppClaimTypes.PERMISSIONS,
            RoleId = Guid.Parse(selectedRole.Key!)
        }).ToList();

        if (dtos.Count == 0) return;

        var claims = await roleController.AddClaims(dtos, CurrentCancellationToken);

        selectedRoleClaims.AddRange(claims);

        SetClaimsToPermissionNavItems();
    }

    private async Task DeletePermissions(BitNavItem parent)
    {
        if (selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var itemsToDelete = parent.ChildItems.Where(IsPermissionAssigned)
                                  .Select(i => i.Data)
                                  .Where(d => d is RoleClaimDto)
                                  .Select(d => (d as RoleClaimDto)!);

        var dtos = itemsToDelete.Select(i => new RoleClaimRequestDto { Id = i.Id }).ToList();

        if (dtos.Count == 0) return;

        await roleController.DeleteClaims(dtos, CurrentCancellationToken);

        _ = itemsToDelete.Select(selectedRoleClaims.Remove).ToList();

        SetClaimsToPermissionNavItems();
    }

    private async Task TogglePermission(BitNavItem item)
    {
        if (selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        if (IsPermissionAssigned(item))
        {
            if (item.Data is not RoleClaimDto claim) return;

            await roleController.DeleteClaims([new RoleClaimRequestDto { Id = claim.Id }], CurrentCancellationToken);

            selectedRoleClaims.Remove(claim);
        }
        else
        {
            RoleClaimRequestDto dto = new()
            {
                ClaimValue = item.Key,
                ClaimType = AppClaimTypes.PERMISSIONS,
                RoleId = Guid.Parse(selectedRole.Key!)
            };

            var claims = await roleController.AddClaims([dto], CurrentCancellationToken);

            selectedRoleClaims.AddRange(claims);
        }

        SetClaimsToPermissionNavItems();
    }

    private async Task ToggleUser(UserDto user)
    {
        if (selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        ToggleRoleUserDto dto = new()
        {
            UserId = user.Id,
            RoleId = Guid.Parse(selectedRole.Key!),
            IsAdd = IsUserAssigned(user) is false
        };

        await roleController.ToggleUser(dto, CurrentCancellationToken);

        if (dto.IsAdd)
        {
            selectedRoleUsers.Add(user);
        }
        else
        {
            selectedRoleUsers.RemoveAll(u => u.Id == user.Id);
        }
    }

    private async Task SaveMaxPrivilegedSessions()
    {
        if (selectedRole is null || maxPrivilegedSessions.HasValue is false) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var claim = selectedRoleClaims.SingleOrDefault(c => c.ClaimType == AppClaimTypes.MAX_PRIVILEGED_SESSIONS);

        if (claim is not null)
        {
            RoleClaimRequestDto dto = new()
            {
                Id = claim.Id,
                ClaimValue = maxPrivilegedSessions.Value.ToString(),
            };

            await roleController.UpdateClaims([dto], CurrentCancellationToken);
        }
        else
        {
            RoleClaimRequestDto dto = new()
            {
                RoleId = Guid.Parse(selectedRole.Key!),
                ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
                ClaimValue = maxPrivilegedSessions.Value.ToString(),
            };

            var claims = await roleController.AddClaims([dto], CurrentCancellationToken);

            selectedRoleClaims.AddRange(claims);
        }
    }

    private async Task SendNotification()
    {
        if (selectedRole is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleController.SendNotification(new() { RoleId = Guid.Parse(selectedRole.Key!), Message = notificationMessage }, CurrentCancellationToken);
    }

    private void SetClaimsToPermissionNavItems()
    {
        foreach (var item in permissionNavItems.SelectMany(i => i.ChildItems))
        {
            item.Data = selectedRoleClaims.FirstOrDefault(p => p.ClaimValue == item.Key);
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (loadRoleDataCts is not null && loadRoleDataCts.IsCancellationRequested is false)
        {
            await loadRoleDataCts.CancelAsync();
            loadRoleDataCts.Dispose();
        }

        await base.DisposeAsync(disposing);
    }
}
