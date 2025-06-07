//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
//#if (signalR == true)
using Boilerplate.Shared.Dtos.Diagnostic;
using Boilerplate.Client.Core.Services.DiagnosticLog;
using Microsoft.AspNetCore.SignalR.Client;
//#endif

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Management;

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
    private CancellationTokenSource? loadRoleDataCts;
    private List<UserSessionDto> allUserSessions = [];
    private List<UserSessionDto> filteredUserSessions = [];


    [AutoInject] IUserManagementController userManagementController = default!;
    //#if (signalR == true)
    [AutoInject] HubConnection hubConnection = default!;
    //#endif

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

            SearchUsers();

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

    private async Task HandleOnSelectUser(BitNavItem? item)
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

            SearchSessions();
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

    private void SearchUsers()
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

    private void SearchSessions()
    {
        filteredUserSessions = allUserSessions;

        if (string.IsNullOrWhiteSpace(sessionSearchText) is false)
        {
            var t = sessionSearchText.Trim();
            filteredUserSessions = [.. allUserSessions.Where(us => ((us.IP + us.Address + us.DeviceInfo + us.RenewedOnDateTimeOffset + us.Id) ?? string.Empty).Contains(t, StringComparison.InvariantCultureIgnoreCase))];
        }
    }

    //#if (signalR == true)
    /// <summary>
    /// <inheritdoc cref="SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE"/>
    /// </summary>
    private async Task ReadUserSessionLogs(Guid userSessionId)
    {
        var logs = await hubConnection.InvokeAsync<DiagnosticLogDto[]>("GetUserSessionLogs", userSessionId);

        DiagnosticLogger.Store.Clear();
        foreach (var log in logs)
        {
            DiagnosticLogger.Store.Enqueue(log);
        }

        PubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
    //#endif
}
