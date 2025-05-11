//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class UsersPage
{
    private UserDto newUserDto = new() { UserName = Guid.NewGuid().ToString() };

    private UserDto editUserDto = new();

    private bool isLoadingUsers;
    private int? onlineUsersCount;
    private bool isLoadingSessions;
    private string? loadingUserKey;
    private bool isLoadingUsersCount;
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
        await Task.WhenAll(LoadAllUsers(), LoadOnlineUsersCount());
    }

    private async Task LoadAllUsers()
    {
        if (isLoadingUsers) return;

        try
        {
            isLoadingUsers = true;

            var allUsers = await userManagementController.GetAllUsers(CurrentCancellationToken);

            onlineUsersCount = await userManagementController.GetOnlineUsersCount(CurrentCancellationToken);

            userNavItems = [.. allUsers.Select(r => new BitNavItem
            {
                Key = r.Id.ToString(),
                Text = r.DisplayName ?? string.Empty,
                Data = r
            })];

            editUserDto = new();
            selectedUserItem = null;
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private async Task LoadOnlineUsersCount()
    {
        if (isLoadingUsersCount) return;

        try
        {
            isLoadingUsersCount = true;
            onlineUsersCount = await userManagementController.GetOnlineUsersCount(CurrentCancellationToken);
        }
        finally
        {
            isLoadingUsersCount = false;
        }
    }

    private async Task AddUser()
    {
        if (string.IsNullOrWhiteSpace(newUserDto.UserName)) return;
        if (string.IsNullOrWhiteSpace(newUserDto.Email) && string.IsNullOrWhiteSpace(newUserDto.PhoneNumber)) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        //TODO: validate inputs further here?

        await userManagementController.Create(newUserDto, CurrentCancellationToken);

        newUserDto = new() { UserName = Guid.NewGuid().ToString() };

        await LoadAllUsers();
    }

    private async Task EditUser()
    {
        if (selectedUserItem is null) return;
        if (string.IsNullOrWhiteSpace(editUserDto.UserName)) return;
        if (string.IsNullOrWhiteSpace(editUserDto.Email) && string.IsNullOrWhiteSpace(editUserDto.PhoneNumber)) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        //TODO: validate inputs here?

        editUserDto.Password = Guid.NewGuid().ToString();

        await userManagementController.Update(editUserDto, CurrentCancellationToken);

        await LoadAllUsers();
    }

    private async Task DeleteUser()
    {
        if (selectedUserItem is null) return;
        //TODO: check proper access like:
        //can't delete current user (user can't remove its own user)
        //only super admin should be able to remove other super admins

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

            user.Patch(editUserDto);

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
