using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Workbench.Components.Pages.AppPages.Settings;

public static class SettingsDemoData
{
    private const string OsImagesUrl = "_content/Boilerplate.Client.Core/images/os";

    public static string? ProfilePhotoUrl => WorkbenchPhotos.Find(WorkbenchDemoUsers.All[0].NameKey);

    public const string QrCodeUrl = "images/two-factor-qr.svg";

    public const string SharedKey = "jbsw y3dp ehpk 3pxp";

    public static readonly Guid CurrentSessionId = new(1, 0, 0, new byte[8]);

    public static SettingsProfile CreateProfile(string fullName, string email) => new(fullName,
                                                                                         email,
                                                                                         "+31 6 4821 7735",
                                                                                         new DateTimeOffset(1991, 4, 18, 0, 0, 0, TimeSpan.Zero),
                                                                                         Gender.Female,
                                                                                         HasPicture: true);

    public static SettingsProfile CreateEmptyProfile(string email) => new(string.Empty, email, null, null, null, HasPicture: false);

    public static IReadOnlyList<SettingsSession> CreateSessions(DateTimeOffset now) =>
    [
        new(CurrentSessionId, "Amsterdam, Netherlands", "203.0.113.24", "Chrome 141 · Windows 11", $"{OsImagesUrl}/windows.png", now, IsPrivileged: true, IsNotificationAllowed: true),
        new(new(2, 0, 0, new byte[8]), "Rotterdam, Netherlands", "2001:db8:85a3::8a2e:370:7334", "Safari · iPhone", $"{OsImagesUrl}/apple.png", now.AddMinutes(-9), IsPrivileged: true, IsNotificationAllowed: true),
        new(new(3, 0, 0, new byte[8]), "Utrecht, Netherlands", "198.51.100.7", "bit Boilerplate · Android 16", $"{OsImagesUrl}/android.png", now.AddHours(-26), IsPrivileged: false, IsNotificationAllowed: false),
        new(new(4, 0, 0, new byte[8]), "Berlin, Germany", "198.51.100.82", "Firefox 144 · Linux", $"{OsImagesUrl}/linux.png", now.AddDays(-4), IsPrivileged: false, IsNotificationAllowed: false),
        new(new(5, 0, 0, new byte[8]), "Amsterdam, Netherlands", "203.0.113.91", "Sales reports", $"{OsImagesUrl}/oauth.png", now.AddDays(-2), IsPrivileged: false, IsNotificationAllowed: false, OAuthClientName: "Sales reports")
    ];

    public static string[] CreateRecoveryCodes()
    {
        return [.. Enumerable.Range(0, 10).Select(index => $"{Random.Shared.Next(10000, 99999)}-{Random.Shared.Next(10000, 99999)}")];
    }
}
