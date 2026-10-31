using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

[BlexStore(Name = "settings", Persist = true)]
public partial class SettingsStore
{
    [BlexState] private string? _expandedSection = PageUrls.SettingsSections.Profile;
    [BlexState] private string? _accountTab;
    [BlexState] private SettingsProfile? _profile;
    [BlexState] private EditUserRequestDto? _profileDraft;
    [BlexState] private string? _pendingEmail;
    [BlexState] private string? _pendingPhoneNumber;
    [BlexState] private bool _isPasswordlessEnabled;
    [BlexState] private bool _isTwoFactorEnabled;
    [BlexState] private string[]? _recoveryCodes;
    [BlexState] private int _recoveryCodesLeft = 10;
    [BlexState] private Guid[] _revokedSessionIds = [];
    [BlexState] private Dictionary<string, bool> _consents = [];
    [BlexState] private bool _isAdWatched;
    [BlexState(Persist = false)] private bool _isDeleteDialogOpen;

    [BlexAction] private void OnExpand(string? section) => ExpandedSection = section;

    [BlexAction] private void OnSelectAccountTab(string? tab) => AccountTab = tab;

    [BlexAction]
    private void OnSaveProfile(SettingsProfile profile)
    {
        Profile = profile;
        ProfileDraft = null;
    }

    [BlexAction] private void OnEditProfile(EditUserRequestDto draft) => ProfileDraft = draft;

    [BlexAction] private void OnRequestEmailChange(string? email) => PendingEmail = email;

    [BlexAction] private void OnRequestPhoneNumberChange(string? phoneNumber) => PendingPhoneNumber = phoneNumber;

    [BlexAction] private void OnSetPasswordless(bool isEnabled) => IsPasswordlessEnabled = isEnabled;

    [BlexAction]
    private void OnSetTwoFactor(bool isEnabled, string[]? recoveryCodes)
    {
        IsTwoFactorEnabled = isEnabled;
        RecoveryCodes = recoveryCodes;
        RecoveryCodesLeft = recoveryCodes?.Length ?? (isEnabled ? RecoveryCodesLeft : 10);
    }

    [BlexAction] private void OnRevokeSession(Guid sessionId) => RevokedSessionIds = [.. RevokedSessionIds, sessionId];

    [BlexAction] private void OnSetConsent(string category, bool isGranted) => Consents = new(Consents) { [category] = isGranted };

    [BlexAction] private void OnWatchAd() => IsAdWatched = true;

    [BlexAction] private void OnOpenDeleteDialog(bool isOpen) => IsDeleteDialogOpen = isOpen;
}
