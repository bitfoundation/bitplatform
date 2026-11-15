using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The view's own account behind the app's settings page (See <see cref="SettingsSampleAccount"/>), saved per view and per
/// demo user (WorkbenchStateStorage), so what was changed on the page survives a reload and shows in a screenshot.
/// </summary>
[BlexStore(Name = StoreName, Persist = true, Version = 1)]
public partial class SettingsStore
{
    public const string StoreName = "settings";

    /// <summary>The user, language and switches the account was made for; another starts it over.</summary>
    [BlexState] private string? _shapedFor;
    [BlexState] private UserDto? _user;
    [BlexState] private UserSessionDto[] _sessions = [];
    [BlexState] private bool _isTwoFactorEnabled;
    [BlexState] private string _authenticatorKey = "";
    [BlexState] private int _recoveryCodesLeft;
    [BlexState] private bool _hasPasskey;
    /// <summary>The id of the uploaded picture the account shows last (See WorkbenchProfilePictures.LastUpload), so it notices a new one.</summary>
    [BlexState] private string? _seenUpload;

    [BlexAction]
    private void OnStartOver(string shapedFor, UserDto user, UserSessionDto[] sessions, bool isTwoFactorEnabled, string authenticatorKey, int recoveryCodesLeft, bool hasPasskey, string? seenUpload)
    {
        ShapedFor = shapedFor;
        User = user;
        Sessions = sessions;
        IsTwoFactorEnabled = isTwoFactorEnabled;
        AuthenticatorKey = authenticatorKey;
        RecoveryCodesLeft = recoveryCodesLeft;
        HasPasskey = hasPasskey;
        SeenUpload = seenUpload;
    }

    [BlexAction] private void OnChangeUser(UserDto user) => User = user;

    [BlexAction] private void OnShowUpload(UserDto user, string? seenUpload) => (User, SeenUpload) = (user, seenUpload);

    [BlexAction] private void OnChangeSessions(UserSessionDto[] sessions) => Sessions = sessions;

    [BlexAction]
    private void OnChangeTwoFactor(bool isEnabled, string authenticatorKey, int recoveryCodesLeft)
    {
        IsTwoFactorEnabled = isEnabled;
        AuthenticatorKey = authenticatorKey;
        RecoveryCodesLeft = recoveryCodesLeft;
    }

    [BlexAction] private void OnChangePasskey(bool hasPasskey) => HasPasskey = hasPasskey;
}
