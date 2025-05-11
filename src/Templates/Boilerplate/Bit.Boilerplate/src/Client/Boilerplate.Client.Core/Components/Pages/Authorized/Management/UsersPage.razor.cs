//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class UsersPage
{
    private UserDto selectedUserDto = new();

    //#if (signalR == true)
    private int? onlineUsersCount;
    private bool isLoadingOnlineUsersCount;
    //#endif

    private bool isLoadingUsers;
    private bool isLoadingSessions;
    private string? loadingUserKey;
    private bool isDeleteUserDialogOpen;
    private BitNavItem? selectedUserItem;
    private List<BitNavItem> userNavItems = [];
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

            var allUsers = await userManagementController.GetAllUsers(CurrentCancellationToken);

            userNavItems = [.. allUsers.Select(r => new BitNavItem
            {
                Key = r.Id.ToString(),
                Text = r.DisplayName ?? string.Empty,
                Data = r
            })];

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

    private async Task DeleteSession(UserSessionDto session)
    {
        if (selectedUserItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        await userManagementController.DeleteUserSession(session.Id, CurrentCancellationToken);

        await HandleOnSelectUser(selectedUserItem);
    }
}
