namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public static class AccountingSite
{
    public const string Path = "/accounting";

    public const string SignUpUrl = "https://adminpanel.bitplatform.dev/sign-up";

    public static LandingSite Info { get; } = new()
    {
        Key = "accounting",
        Path = Path,
        Name = "Sumwise",
        AccentColor = "#21468A",
        Layout = typeof(AccountingLayout),
        NotFound = typeof(AccountingNotFound),
        Card = typeof(AccountingCard),
        Logo = typeof(AccountingLogo),
        Strings = typeof(AccountingStrings),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        StatusUrl = "https://status.example.com/",
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.Android, LandingPlatformStatus.Live, "https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template"),
            new(LandingPlatform.Windows, LandingPlatformStatus.Live, "https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe"),
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/")
        ],
        Company = new()
        {
            LegalName = "Sumwise GmbH",
            Address = "Margaretenstraße 70, 1050 Vienna, Austria",
            Email = "hello@sumwise.example",
            Phone = "+43 1 555 0193",
            RegistrationNumber = "FN 512384 k",
            VatId = "ATU71234567",
            Representative = "Clara Weninger"
        }
    };
}
