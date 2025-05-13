//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class UsersPage
{
    private UserDto selectedUserDto = new();


    private bool isLoadingUsers;
    private int? onlineUsersCount;
    private string? loadingUserKey;
    private string? userSearchText;
    private List<UserDto> allUsers = [];
    private bool isDeleteUserDialogOpen;
    private BitNavItem? selectedUserItem;
    private bool isLoadingOnlineUsersCount;
    private List<BitNavItem> userNavItems = [];
    private bool isRevokeAllUserSessionsDialogOpen;
    private CancellationTokenSource? loadRoleDataCts;
    private List<UserSessionDto> allUserSessions = [];


    [AutoInject] IUserManagementController userManagementController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await RefreshData();
    }


    private async Task RefreshData()
    {
        await Task.WhenAll(
            LoadAllUsers(),
            LoadOnlineUsersCount()
        );
    }

    private async Task LoadAllUsers()
    {
        if (isLoadingUsers) return;

        try
        {
            isLoadingUsers = true;

            allUsers = await userManagementController.GetAllUsers(CurrentCancellationToken);

            SearchNavItems();

            allUserSessions = [];
            selectedUserDto = new();
            selectedUserItem = null;
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private async Task LoadOnlineUsersCount()
    {
        if (isLoadingOnlineUsersCount) return;

        try
        {
            isLoadingOnlineUsersCount = true;
            onlineUsersCount = await userManagementController.GetOnlineUsersCount(CurrentCancellationToken);
        }
        finally
        {
            isLoadingOnlineUsersCount = false;
        }
    }

    private async Task DeleteUser()
    {
        if (selectedUserItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await userManagementController.Delete(Guid.Parse(selectedUserItem.Key!), CurrentCancellationToken);

        await LoadAllUsers();
    }

    private async Task HandleOnSelectUser(BitNavItem item)
    {
        if (item is null) return;

        try
        {
            if (loadRoleDataCts is not null)
            {
                using var currentCts = loadRoleDataCts;
                loadRoleDataCts = new();

                await currentCts.CancelAsync();
            }

            loadRoleDataCts = new();

            loadingUserKey = item.Key;
            selectedUserItem = item;
            var user = (item.Data as UserDto)!;

            user.Patch(selectedUserDto);

            allUserSessions = await userManagementController.GetUserSessions(user.Id, CurrentCancellationToken);
        }
        finally
        {
            if (loadingUserKey == item.Key)
            {
                loadingUserKey = null;
            }
        }
    }

    private async Task RevokeUserSession(UserSessionDto session)
    {
        if (selectedUserItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await userManagementController.RevokeUserSession(session.Id, CurrentCancellationToken);

        await HandleOnSelectUser(selectedUserItem);
    }

    private async Task RevokeAllSessions()
    {
        if (selectedUserItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await userManagementController.RevokeAllUserSessions(Guid.Parse(selectedUserItem.Key!), CurrentCancellationToken);

        await HandleOnSelectUser(selectedUserItem);
    }

    private void SearchNavItems()
    {
        var filteredUsers = allUsers;

        if (string.IsNullOrWhiteSpace(userSearchText) is false)
        {
            var t = userSearchText.Trim();
            filteredUsers = [.. allUsers.Where(u => ((u.FullName + u.Email + u.PhoneNumber + u.UserName) ?? string.Empty).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }

        userNavItems = [.. filteredUsers.Select(u => new BitNavItem
        {
            Key = u.Id.ToString(),
            Text = u.DisplayName ?? string.Empty,
            Data = u
        })];
    }
}
