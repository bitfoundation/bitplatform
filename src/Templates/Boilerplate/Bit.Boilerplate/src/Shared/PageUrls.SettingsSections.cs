//+:cnd:noEmit
using System.ComponentModel;

namespace Boilerplate.Shared;

public static partial class PageUrls
{
    public static class SettingsSections
    {
        [Description("Manage personal information: name, profile picture, birthdate and gender. Requires sign-in.")]
        public static readonly string Profile = nameof(Profile).ToLower();

        [Description("Manage email, phone number, passwordless sign-in and account deletion. Requires sign-in.")]
        public static readonly string Account = nameof(Account).ToLower();

        [Description("Two-factor authentication (2FA) settings. Requires sign-in.")]
        public static readonly string Tfa = nameof(Tfa).ToLower();

        [Description("View all devices/browsers you are signed in on and revoke sessions remotely. Requires sign-in.")]
        public static readonly string Sessions = nameof(Sessions).ToLower();

        //#if (ads == true)
        [Description("Upgrade your account. Requires sign-in.")]
        //#endif
        public static readonly string UpgradeAccount = nameof(UpgradeAccount).ToLower();
    }
}
