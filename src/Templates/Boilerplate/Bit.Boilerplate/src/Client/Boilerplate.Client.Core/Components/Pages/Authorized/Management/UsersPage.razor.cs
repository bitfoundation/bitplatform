//-:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

public partial class UsersPage
{
    private string? newUserEmail;
    private string? newUserFullName;
    private string? newUserPhoneNumber;

    private string? editUserEmail;
    private string? editUserFullName;
    private string? editUserPhoneNumber;

    private bool isLoadingSessions;
    private string? loadingUserKey;
    private bool isLoadingUsers = true;
    private bool isDeleteUserDialogOpen;
    private BitNavItem? selectedUserItem;
    private List<BitNavItem> userNavItems = [];
    private bool isDeleteUserSessionDialogOpen;
    private UserSessionDto? userSessionToDelete;
    private List<UserSessionDto> allUserSessions = [];


    [AutoInject] IUserManagementController userManagementController = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadAllUsers();
    }


    private async Task LoadAllUsers()
    {
        try
        {
            var allRoles = await userManagementController.GetAllUsers(CurrentCancellationToken);

            userNavItems = [.. allRoles.Select(r => new BitNavItem
            {
                Key = r.Id.ToString(),
                Text = r.DisplayName ?? string.Empty,
                Data = r
            })];
        }
        finally
        {
            isLoadingUsers = false;
        }
    }

    private async Task AddUser()
    {
        if (string.IsNullOrWhiteSpace(newUserEmail) && string.IsNullOrWhiteSpace(newUserPhoneNumber)) return;

        var newUser = new UserDto
        {
            FullName = newUserFullName,
            Email = newUserEmail,
            PhoneNumber = newUserPhoneNumber
        };
    }

    private async Task EditUser()
    {
        if (selectedUserItem is null) return;
        if (string.IsNullOrWhiteSpace(editUserEmail) && string.IsNullOrWhiteSpace(editUserPhoneNumber)) return;

        var user = new UserDto
        {
            Id = Guid.Parse(selectedUserItem.Key!),
            FullName = newUserFullName,
            Email = newUserEmail,
            PhoneNumber = newUserPhoneNumber
        };
    }

    private async Task HandleOnSelectUser(BitNavItem item)
    {
        try
        {
            isLoadingSessions = true;
            selectedUserItem = item;
            var user = item.Data as UserDto;

            editUserEmail = user?.Email;
            editUserFullName = user?.FullName;
            editUserPhoneNumber = user?.PhoneNumber;

            allUserSessions = await userManagementController.GetUserSessions(Guid.Parse(item.Key!), CurrentCancellationToken);
        }
        finally
        {
            isLoadingSessions = false;
        }
    }

    private async Task DeleteSession(UserSessionDto session)
    {
        await userManagementController.DeleteUserSession(session.Id, CurrentCancellationToken);
    }
}
