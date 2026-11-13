//+:cnd:noEmit
namespace Boilerplate.Workbench.Infrastructure.Services.StandIns;

/// <summary>
/// The app's storage in a view: the browser's, through <see cref="WorkbenchStorageService"/>, except what each view keeps for
/// itself, as every view shares the browser's storage:
/// <list type="bullet">
/// <item>What the app's own pages keep on the device, such as the notifications choice (See NotificationPreferenceService),
/// the privacy consents (See ConsentService) or the dashboard's layout, saved with the view in <see cref="WorkbenchDeviceStore"/>,
/// so two views compared side by side can show a device with notifications off and one with them on, and a reload and a
/// screenshot keep what was chosen and arranged.</item>
/// <item>The sign-in's tokens and accounts (See IAuthTokenProvider.IsTokenStorageKey), in memory, so the app's AuthManager
/// runs as on a signed-in device: a signed-in view has a refresh token, <see cref="RefreshToken"/>, which the Workbench's
/// identity stand-in accepts, and nothing the app stores in their place reaches the browser.</item>
/// </list>
/// </summary>
public sealed class WorkbenchViewStorage(WorkbenchStorageService browserStorage, WorkbenchScenarioState scenarioState, WorkbenchDeviceStore deviceStore) : IStorageService
{
    /// <summary>The refresh token of a signed-in view.</summary>
    public const string RefreshToken = "workbench";

    private const string RefreshTokenKey = "refresh_token";

    /// <summary>The keys the app's own pages keep on the device, which each view keeps for itself.</summary>
    private static readonly string[] deviceKeys =
    [
        // NotificationPreferenceService's
        "NotificationsEnabled",
        // ConsentService's
        "consent",
        //#if (module == "Admin")
        // DashboardPage's widget layout
        "dashboard-widgets",
        //#endif
    ];

    private readonly Dictionary<string, (string? Value, bool Persistent)> signInItems = [];

    /// <summary>Who the sign-in items belong to: another user or a sign-in from the inspector starts them over.</summary>
    private (WorkbenchAuthState Auth, int User)? signIn;

    private static bool IsDeviceItem(string key) => deviceKeys.Contains(key, StringComparer.Ordinal);

    private bool IsSignInItem(string key)
    {
        if (IAuthTokenProvider.IsTokenStorageKey(key) is false)
            return false;

        var current = (scenarioState.Scenario.Auth, scenarioState.Scenario.User);

        if (signIn != current)
        {
            signIn = current;
            signInItems.Clear();
        }

        return true;
    }

    public ValueTask<string?> GetItem(string key)
    {
        if (IsDeviceItem(key))
            return ValueTask.FromResult(deviceStore.Items.GetValueOrDefault(key));

        if (IsSignInItem(key) is false)
            return browserStorage.GetItem(key);

        if (signInItems.TryGetValue(key, out var item))
            return ValueTask.FromResult(item.Value);

        return ValueTask.FromResult(key is RefreshTokenKey && scenarioState.Scenario.Auth is not WorkbenchAuthState.SignedOut ? RefreshToken : null);
    }

    public ValueTask RemoveItem(string key)
    {
        if (IsDeviceItem(key))
        {
            deviceStore.SetItem(key, null);
            return ValueTask.CompletedTask;
        }

        if (IsSignInItem(key) is false)
            return browserStorage.RemoveItem(key);

        // Kept as removed, so a view the app signed out of has no refresh token any more.
        signInItems[key] = (null, false);
        return ValueTask.CompletedTask;
    }

    public ValueTask SetItem(string key, string? value, bool persistent = true)
    {
        if (IsDeviceItem(key))
        {
            deviceStore.SetItem(key, value);
            return ValueTask.CompletedTask;
        }

        if (IsSignInItem(key) is false)
            return browserStorage.SetItem(key, value, persistent);

        signInItems[key] = (value, persistent);
        return ValueTask.CompletedTask;
    }

    public async ValueTask<bool> IsPersistent(string key)
    {
        // What a view keeps for its device counts as kept once made, as the browser keeps it.
        if (IsDeviceItem(key))
            return deviceStore.Items.ContainsKey(key);

        if (IsSignInItem(key) is false)
            return await browserStorage.IsPersistent(key);

        return signInItems.TryGetValue(key, out var item) ? item.Value is not null && item.Persistent : await GetItem(key) is not null;
    }

    public async ValueTask Clear()
    {
        deviceStore.Clear();

        foreach (var key in signInItems.Keys.ToArray())
        {
            signInItems[key] = (null, false);
        }

        signInItems[RefreshTokenKey] = (null, false);
        await browserStorage.Clear();
    }
}
