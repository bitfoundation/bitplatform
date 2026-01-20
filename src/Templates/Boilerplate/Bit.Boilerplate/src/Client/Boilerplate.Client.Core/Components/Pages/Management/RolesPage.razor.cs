//+:cnd:noEmit
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Client.Core.Components.Pages.Management;

public partial class RolesPage
{
    private string? newRoleName;
    private string? editRoleName;
    private string? roleSearchText;
    private string? loadingRoleKey;
    private string? userSearchText;
    private bool isDeleteDialogOpen;
    private bool isLoadingRoles = true;
    private bool isLoadingUsers = true;
    private int? maxPrivilegedSessions;
    private List<RoleDto> allRoles = [];
    private List<UserDto> allUsers = [];
    private BitNavItem? selectedRoleItem;
    private List<UserDto> filteredUsers = [];
    private List<BitNavItem> roleNavItems = [];
    private List<UserDto> selectedRoleUsers = [];
    private List<BitNavItem> featureNavItems = [];
    private List<ClaimDto> selectedRoleClaims = [];
    private bool isRemoveRoleFromAllUsersDialogOpen;
    private CancellationTokenSource? loadRoleDataCts;


    [AutoInject] IRoleManagementController roleManagementController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        featureNavItems = [.. AppFeatures.GetAll().GroupBy(p => p.Group).Select(g => new BitNavItem
        {
            Text = g.Key.Name,
            ChildItems = [.. g.Select(p => new BitNavItem
            {
                Key = p.Value,
                Text = p.Name
            })]
        })];

        await RefreshData();
    }


    private async Task RefreshData()
    {
        await Task.WhenAll(LoadAllRoles(), LoadAllUsers());
    }

    private async Task LoadAllRoles()
    {
        try
        {
            allRoles = await roleManagementController.GetAllRoles(CurrentCancellationToken);

            SearchRoles();

            editRoleName = null;
            selectedRoleItem = null;
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

            allUsers = await roleManagementController.GetAllUsers(CurrentCancellationToken);

            SearchUsers();
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private async Task HandleOnSelectRole(BitNavItem? item)
    {
        if (item is null) return;

        try
        {
            if (loadRoleDataCts is not null)
            {
                using var currentCts = loadRoleDataCts;
                loadRoleDataCts = new();

                await currentCts.TryCancel();
            }

            loadRoleDataCts = new();

            selectedRoleItem = item;
            loadingRoleKey = item.Key;
            editRoleName = selectedRoleItem.Text;

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
        selectedRoleUsers = await roleManagementController.GetUsers(roleId, cancellationToken);
    }

    private async Task LoadRoleClaims(Guid roleId, CancellationToken cancellationToken)
    {
        selectedRoleClaims = await roleManagementController.GetClaims(roleId, cancellationToken);

        var maxPrivilegedSessionsValue = selectedRoleClaims.FirstOrDefault(c => c.ClaimType == AppClaimTypes.MAX_PRIVILEGED_SESSIONS)?.ClaimValue;
        maxPrivilegedSessions = maxPrivilegedSessionsValue is null
                                ? null
                                : int.Parse(maxPrivilegedSessionsValue, CultureInfo.InvariantCulture);

        SetClaimsToFeatureNavItems();
    }

    private async Task AddRole()
    {
        if (string.IsNullOrWhiteSpace(newRoleName)) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleManagementController.Create(new RoleDto { Name = newRoleName }, CurrentCancellationToken);

        newRoleName = null;

        await LoadAllRoles();
    }

    private async Task EditRole()
    {
        if (string.IsNullOrWhiteSpace(editRoleName) || selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var role = ((RoleDto)selectedRoleItem.Data!);
        role.Name = editRoleName;
        await roleManagementController.Update(role, CurrentCancellationToken);

        await LoadAllRoles();
    }

    private async Task DeleteRole()
    {
        if (selectedRoleItem is null) return;
        if (AppRoles.IsBuiltInRole(selectedRoleItem.Text)) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;
        var roleId = Guid.Parse(selectedRoleItem.Key!);
        var role = ((RoleDto)selectedRoleItem.Data!);
        await roleManagementController.Delete(roleId, CurrentCancellationToken);

        await LoadAllRoles();
    }

    private async Task AddFeatures(BitNavItem parent)
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var itemsToAdd = parent.ChildItems.Where(ci => IsFeatureAssigned(ci) is false);

        var dtos = itemsToAdd.Select(i => new ClaimDto
        {
            ClaimValue = i.Key,
            ClaimType = AppClaimTypes.FEATURES
        }).ToList();

        if (dtos.Count == 0) return;

        await roleManagementController.AddClaims(Guid.Parse(selectedRoleItem.Key!), dtos, CurrentCancellationToken);

        selectedRoleClaims.AddRange(dtos);

        SetClaimsToFeatureNavItems();
    }

    private async Task DeleteFeatures(BitNavItem parent)
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var itemsToDelete = parent.ChildItems.Where(IsFeatureAssigned)
                                  .Select(i => i.Data)
                                  .Where(d => d is ClaimDto)
                                  .Select(d => (d as ClaimDto)!)
                                  .ToList();

        if (itemsToDelete.Count == 0) return;

        await roleManagementController.DeleteClaims(Guid.Parse(selectedRoleItem.Key!), itemsToDelete, CurrentCancellationToken);

        _ = itemsToDelete.Select(selectedRoleClaims.Remove).ToList();

        SetClaimsToFeatureNavItems();
    }

    private async Task ToggleFeature(BitNavItem item)
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var roleId = Guid.Parse(selectedRoleItem.Key!);

        if (IsFeatureAssigned(item))
        {
            if (item.Data is not ClaimDto claim) return;

            await roleManagementController.DeleteClaims(roleId, [new ClaimDto { ClaimType = claim.ClaimType, ClaimValue = claim.ClaimValue }], CurrentCancellationToken);

            selectedRoleClaims.Remove(claim);
        }
        else
        {
            ClaimDto dto = new()
            {
                ClaimValue = item.Key,
                ClaimType = AppClaimTypes.FEATURES,
            };

            await roleManagementController.AddClaims(roleId, [dto], CurrentCancellationToken);

            selectedRoleClaims.Add(dto);
        }

        SetClaimsToFeatureNavItems();
    }

    private async Task ToggleUser(UserDto user)
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        UserRoleDto dto = new()
        {
            UserId = user.Id,
            RoleId = Guid.Parse(selectedRoleItem.Key!)
        };

        await roleManagementController.ToggleUserRole(dto, CurrentCancellationToken);

        if (IsUserAssigned(user) is false)
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
        if (selectedRoleItem is null || maxPrivilegedSessions.HasValue is false) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        var claim = selectedRoleClaims.SingleOrDefault(c => c.ClaimType == AppClaimTypes.MAX_PRIVILEGED_SESSIONS);

        var roleId = Guid.Parse(selectedRoleItem.Key!);

        if (claim is not null)
        {
            ClaimDto dto = new()
            {
                ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
                ClaimValue = maxPrivilegedSessions.Value.ToString(),
            };

            await roleManagementController.UpdateClaims(roleId, [dto], CurrentCancellationToken);
        }
        else
        {
            ClaimDto dto = new()
            {
                ClaimType = AppClaimTypes.MAX_PRIVILEGED_SESSIONS,
                ClaimValue = maxPrivilegedSessions.Value.ToString(),
            };

            await roleManagementController.AddClaims(roleId, [dto], CurrentCancellationToken);

            selectedRoleClaims.Add(dto);
        }
    }

    //#if (notification == true || signalR == true)
    private string? notificationMessage;
    private string? notificationPageUrl;
    private async Task SendNotification()
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleManagementController.SendNotification(new()
        {
            RoleId = Guid.Parse(selectedRoleItem.Key!),
            Message = notificationMessage,
            PageUrl = notificationPageUrl
        }, CurrentCancellationToken);

        notificationMessage = "";
        notificationPageUrl = "";
    }
    //#endif

    private bool IsFeatureAssigned(BitNavItem item)
    {
        if (loadingRoleKey is not null || selectedRoleItem is null) return false;

        if (item.ChildItems.Count == 0)
            return selectedRoleClaims.Any(p => p.ClaimValue == item.Key);

        return item.ChildItems.Any(IsFeatureAssigned);
    }

    private bool IsUserAssigned(UserDto user)
    {
        return loadingRoleKey is null && selectedRoleItem is not null && selectedRoleUsers.Any(u => user.Id == u.Id);
    }

    private void SetClaimsToFeatureNavItems()
    {
        foreach (var item in featureNavItems.SelectMany(i => i.ChildItems))
        {
            item.Data = selectedRoleClaims.FirstOrDefault(p => p.ClaimValue == item.Key);
        }
    }

    private void SearchRoles()
    {
        var filteredRoles = allRoles;

        if (string.IsNullOrWhiteSpace(roleSearchText) is false)
        {
            var t = roleSearchText.Trim();
            filteredRoles = [.. allRoles.Where(u => ((u.Name + u.NormalizedName) ?? string.Empty).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }

        roleNavItems = [.. filteredRoles.Select(r => new BitNavItem
        {
            Key = r.Id.ToString(),
            Text = r.Name ?? string.Empty,
            Data = r
        })];
    }

    private void SearchUsers()
    {
        filteredUsers = allUsers;

        if (string.IsNullOrWhiteSpace(userSearchText) is false)
        {
            var t = userSearchText.Trim();
            filteredUsers = [.. allUsers.Where(u => ((u.FullName + u.Email + u.PhoneNumber + u.UserName) ?? string.Empty).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }
    }

    private async Task RemoveAllUsersFromRole()
    {
        if (selectedRoleItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await roleManagementController.RemoveAllUsersFromRole(Guid.Parse(selectedRoleItem.Key!), CurrentCancellationToken);

        selectedRoleUsers.Clear();
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (loadRoleDataCts is not null)
        {
            await loadRoleDataCts.TryCancel();
            loadRoleDataCts.Dispose();
        }

        await base.DisposeAsync(disposing);
    }
}
