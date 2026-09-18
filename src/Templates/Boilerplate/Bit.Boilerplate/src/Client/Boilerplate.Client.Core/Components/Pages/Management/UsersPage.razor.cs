//+:cnd:noEmit
namespace Boilerplate.Client.Core.Components.Pages.Management;

public partial class UsersPage
{
    private UserDto selectedUserDto = new();


    private bool isLoadingUsers;
    private int? onlineUsersCount;
    private string? loadingUserKey;
    private string? userSearchText;
    private string? sessionSearchText;
    private List<UserDto> allUsers = [];
    private bool isDeleteUserDialogOpen;
    private BitNavItem? selectedUserItem;
    private bool isLoadingOnlineUsersCount;
    private List<BitNavItem> userNavItems = [];
    private bool isRevokeAllUserSessionsDialogOpen;
    private CancellationTokenSource? loadUserSessionsCts;
    private List<UserSessionDto> allUserSessions = [];
    private List<UserSessionDto> filteredUserSessions = [];


    [AutoInject] HttpClient httpClient = default!;
    [AutoInject] FileSaveService fileSaveService = default!;
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

            // Any in-flight user session load is now irrelevant: the user list has changed, so the selected user may have been deleted or renamed.
            if (loadUserSessionsCts is not null)
            {
                using var previousCts = loadUserSessionsCts;
                loadUserSessionsCts = null;
                await previousCts.TryCancel();
            }

            loadingUserKey = null;

            allUsers = await userManagementController.GetAllUsers(CurrentCancellationToken);

            SearchUsers();

            allUserSessions = [];
            filteredUserSessions = [];
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

    /// <summary>
    /// The same zip the user can download for themselves (See <c>PrivacySection</c>), for a request that arrived by
    /// e-mail, through a representative, or from somebody who can no longer sign in.
    /// </summary>
    private async Task ExportUserPersonalData()
    {
        if (selectedUserItem is null) return;

        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken) is false) return;

        using var response = await httpClient.GetAsync($"{IUserManagementController.ExportPersonalDataUri}/{selectedUserItem.Key}", CurrentCancellationToken);

        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "personal-data.zip";
        var content = await response.Content.ReadAsByteArrayAsync(CurrentCancellationToken);

        await fileSaveService.Save(fileName, "application/zip", content);
    }

    private async Task HandleOnSelectUser(BitNavItem? item)
    {
        if (item is null) return;

        CancellationTokenSource? loadCts = null;

        try
        {
            if (loadUserSessionsCts is not null)
            {
                using var previousCts = loadUserSessionsCts;
                loadUserSessionsCts = null;
                await previousCts.TryCancel();
            }

            loadCts = loadUserSessionsCts = new();

            loadingUserKey = item.Key;
            selectedUserItem = item;
            var user = (item.Data as UserDto)!;

            user.Patch(selectedUserDto);

            allUserSessions = [];
            filteredUserSessions = [];

            var userSessions = await userManagementController.GetUserSessions(user.Id, loadCts.Token);

            if (ReferenceEquals(loadUserSessionsCts, loadCts) is false) return; // Selected user changed while we were loading, so don't assign the sessions to the previous user.

            allUserSessions = userSessions;

            SearchSessions();
        }
        finally
        {
            // Select Bob, then revoke one of his sessions before the first load lands: RevokeUserSession re-enters here
            // with Bob's key again. Comparing keys, the first (now superseded) call would find its own key and null the
            // flag while the second is still fetching - Bob's row stops spinning and his Sessions tab says he has none.
            if (loadCts is not null && ReferenceEquals(loadUserSessionsCts, loadCts))
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

    private void SearchUsers()
    {
        var filteredUsers = allUsers;

        if (string.IsNullOrWhiteSpace(userSearchText) is false)
        {
            var t = userSearchText.Trim();
            filteredUsers = [.. allUsers.Where(u => string.Join('|', u.FullName, u.Email, u.PhoneNumber, u.UserName).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }

        userNavItems = [.. filteredUsers.Select(u => new BitNavItem
        {
            Key = u.Id.ToString(),
            Text = u.DisplayName ?? string.Empty,
            Data = u
        })];
    }

    private void SearchSessions()
    {
        filteredUserSessions = allUserSessions;

        if (string.IsNullOrWhiteSpace(sessionSearchText) is false)
        {
            var t = sessionSearchText.Trim();
            filteredUserSessions = [.. allUserSessions.Where(us => string.Join('|', us.IP, us.Address, us.DeviceInfo, TimeZoneService.ToLocalTime(us.RenewedOnDateTimeOffset), us.Id).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (loadUserSessionsCts is not null)
        {
            await loadUserSessionsCts.TryCancel();
            loadUserSessionsCts.Dispose();
        }

        await base.DisposeAsync(disposing);
    }
}
