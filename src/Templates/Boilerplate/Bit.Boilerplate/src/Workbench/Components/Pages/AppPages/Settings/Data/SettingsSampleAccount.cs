//+:cnd:noEmit
using System.Text.Json;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Shared.Features.Identity.OAuth;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings.Data;

/// <summary>
/// The view's own account behind the app's settings page: the demo user's profile, the devices and apps it is signed in on,
/// its two-factor setup and its passkey, as the page's switches set them up. What the page changes is kept in the view's
/// <see cref="SettingsStore"/>; another demo user, language or switch starts it over. What it hands out is a copy, as an
/// answer over HTTP is, so nothing the app's page does to one changes the account behind the stand-ins' back.
/// </summary>
public sealed class SettingsSampleAccount(WorkbenchScenarioState scenarioState, SettingsStore store, IStringLocalizer<WorkbenchStrings> localizer)
{
    public const string TwoFactorSwitch = "two-factor";

    public const string PasskeySwitch = "passkey";

    /// <summary>The value of <see cref="TwoFactorSwitch"/> and <see cref="PasskeySwitch"/> that has it set up already.</summary>
    public const string On = "on";

    /// <summary>The key before the first reset, as the server keeps one: 16 base32 characters.</summary>
    public const string FirstKey = "JBSWY3DPEHPK3PXP";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The profile, as GetCurrentUser answers it; a picture uploaded since the last read shows from here on.</summary>
    public UserDto User
    {
        get
        {
            var user = Current();
            var uploads = WorkbenchProfilePictures.Uploads(user.Id);

            if (uploads > store.SeenUploads)
            {
                user.HasProfilePicture = true;
                user.Version = $"{int.Parse(user.Version ?? "0", CultureInfo.InvariantCulture) + 1}";
                store.ShowUpload(user, uploads);
            }
            else if (uploads < store.SeenUploads)
            {
                // The Workbench restarted, which forgets the uploads but not what the view saved: a picture removed stays removed.
                store.ShowUpload(user, uploads);
            }

            return Copy(user);
        }
    }

    /// <summary>A user who signed up with their email and has filled in nothing else.</summary>
    public UserDto NewUser
    {
        get
        {
            var user = Current();
            return new() { Id = user.Id, UserName = user.UserName, Email = user.Email, Version = user.Version };
        }
    }

    public List<UserSessionDto> Sessions
    {
        get
        {
            Shape();
            return Copy(store.Sessions.ToList());
        }
    }

    public bool IsTwoFactorEnabled
    {
        get
        {
            Shape();
            return store.IsTwoFactorEnabled;
        }
    }

    public string AuthenticatorKey
    {
        get
        {
            Shape();
            return store.AuthenticatorKey;
        }
    }

    public int RecoveryCodesLeft
    {
        get
        {
            Shape();
            return store.RecoveryCodesLeft;
        }
    }

    public bool HasPasskey
    {
        get
        {
            Shape();
            return store.HasPasskey;
        }
    }

    /// <summary>The key as the server shows it (See <see cref="Format"/>).</summary>
    public string FormattedKey => Format(AuthenticatorKey);

    /// <summary>A key as the server shows it: groups of four, lower case (UserController.FormatKey).</summary>
    public static string Format(string key) => string.Join(' ', key.Chunk(4).Select(group => new string(group))).ToLowerInvariant();

    /// <summary>Changes the profile, as a copy the change is made on.</summary>
    public UserDto ChangeUser(Action<UserDto> change)
    {
        var user = Copy(Current());
        change(user);
        store.ChangeUser(user);
        return Copy(user);
    }

    /// <summary>The picture is gone, until one is uploaded.</summary>
    public void RemovePicture() => ChangeUser(user =>
    {
        user.HasProfilePicture = false;
        user.Version = $"{int.Parse(user.Version ?? "0", CultureInfo.InvariantCulture) + 1}";
    });

    public void RemoveSession(Guid id)
    {
        Shape();
        store.ChangeSessions([.. store.Sessions.Where(session => session.Id != id || id == WorkbenchClaims.CurrentSessionId)]);
    }

    public void ChangeTwoFactor(bool isEnabled, string authenticatorKey, int recoveryCodesLeft)
    {
        Shape();
        store.ChangeTwoFactor(isEnabled, authenticatorKey, recoveryCodesLeft);
    }

    public void ChangePasskey(bool hasPasskey)
    {
        Shape();
        store.ChangePasskey(hasPasskey);
    }

    private UserDto Current()
    {
        Shape();
        return store.User!;
    }

    /// <summary>Starts the account over when the view's user, language or switches are not the ones it was made for.</summary>
    private void Shape()
    {
        var scenario = scenarioState.Scenario;
        var twoFactor = scenario.GetSwitch(TwoFactorSwitch) is On;
        var passkey = scenario.GetSwitch(PasskeySwitch) is On;
        var shapedFor = $"{scenario.User}/{CultureInfo.CurrentUICulture.Name}/{twoFactor}/{passkey}";

        if (store.ShapedFor == shapedFor && store.User is not null)
            return;

        var (user, sessions) = Sample(scenario.User, localizer[WorkbenchDemoUsers.Find(scenario.User).NameKey], DateTimeOffset.UtcNow);

        store.StartOver(shapedFor, user, [.. sessions], twoFactor, FirstKey, twoFactor ? 8 : 0, passkey, WorkbenchProfilePictures.Uploads(user.Id));
    }

    /// <summary>The demo user's profile and the devices and apps they are signed in on, before anything was changed.</summary>
    public static (UserDto User, List<UserSessionDto> Sessions) Sample(int userIndex, string fullName, DateTimeOffset now)
    {
        var demoUser = WorkbenchDemoUsers.Find(userIndex);

        UserDto user = new()
        {
            Id = WorkbenchClaims.UserIdOf(userIndex),
            UserName = demoUser.Email,
            Email = demoUser.Email,
            PhoneNumber = "+31648217735",
            FullName = fullName,
            BirthDate = new DateTimeOffset(1991, 4, 18, 0, 0, 0, TimeSpan.Zero),
            Gender = Gender.Female,
            HasProfilePicture = true,
            Version = "0"
        };

        List<UserSessionDto> sessions =
        [
            Session(WorkbenchClaims.CurrentSessionId, "Amsterdam, Netherlands", "203.0.113.24", "Chrome 141 on Windows 11", now, privileged: true, notifications: true),
            Session(new(2, 0, 0, new byte[8]), "Rotterdam, Netherlands", "2001:db8:85a3::8a2e:370:7334", "Safari on iPhone, iOS 26", now.AddMinutes(-9), privileged: true, notifications: true),
            Session(new(3, 0, 0, new byte[8]), "Utrecht, Netherlands", "198.51.100.7", "Boilerplate 4.2 on Android 16", now.AddHours(-26), privileged: false, notifications: false),
            Session(new(4, 0, 0, new byte[8]), "Berlin, Germany", "198.51.100.82", "Firefox 144 on Linux", now.AddDays(-4), privileged: false, notifications: false),
            // An uptime monitor the user let read the app's health report at /healthz, which signs in as an external application.
            Session(new(5, 0, 0, new byte[8]), "Amsterdam, Netherlands", "203.0.113.91", null, now.AddDays(-2), privileged: false, notifications: false,
                    oAuthClient: ("status.example.com", "Uptime monitor", OAuthScopes.Healthz))
        ];

        return (user, sessions);
    }

    private static UserSessionDto Session(Guid id, string address, string ip, string? device, DateTimeOffset lastSeen, bool privileged, bool notifications,
                                          (string Id, string Name, string Scope)? oAuthClient = null)
    {
        return new()
        {
            Id = id,
            Address = address,
            IP = ip,
            DeviceInfo = device,
            RenewedOn = lastSeen.ToUnixTimeSeconds(),
            Privileged = privileged,
            AuthenticationMethod = "Password",
            //#if (signalR == true || notification == true)
            NotificationStatus = notifications ? UserSessionNotificationStatus.Allowed : UserSessionNotificationStatus.Muted,
            //#endif
            CultureName = "en-US",
            OAuthClientId = oAuthClient?.Id,
            OAuthClientName = oAuthClient?.Name,
            OAuthScope = oAuthClient?.Scope
        };
    }

    private static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, jsonOptions), jsonOptions)!;
}
