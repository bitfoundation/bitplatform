//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;
//#if (signalR == true)
using Boilerplate.Shared.Dtos.Diagnostic;
using Boilerplate.Client.Core.Services.DiagnosticLog;
using Microsoft.AspNetCore.SignalR.Client;
//#endif

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
        DiagnosticLogger.Store.Clear();

        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Receive per-log events
        var d1 = hubConnection.On<DiagnosticLogDto>(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, log =>
        {
            DiagnosticLogger.Store.Enqueue(log);
        });

        // Lifecycle events
        var d2 = hubConnection.On<string>(SignalRMethods.DIAGNOSTIC_LOGS_COMPLETE, _ => completed.TrySetResult());
        var d3 = hubConnection.On<string>(SignalRMethods.DIAGNOSTIC_LOGS_ABORTED, _ => completed.TrySetResult());
        var d4 = hubConnection.On<string>(SignalRMethods.DIAGNOSTIC_LOGS_ERROR, _ => completed.TrySetResult());

        // Join temp group and trigger the target to stream
        var correlationId = await hubConnection.InvokeAsync<string?>(
            "BeginUserSessionLogs", 
            userSessionId, 
            cancellationToken: CurrentCancellationToken);
        if (string.IsNullOrEmpty(correlationId))
        {
            d1.Dispose(); d2.Dispose(); d3.Dispose(); d4.Dispose();
            return;
        }

        try
        {
            // Optional safety timeout for first item or completion
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

            await completed.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            // ignore, just end gracefully
        }
        finally
        {
            d1.Dispose(); d2.Dispose(); d3.Dispose(); d4.Dispose();
            await hubConnection.InvokeAsync("EndUserSessionLogs", correlationId);
        }

        PubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
    //#endif
}
