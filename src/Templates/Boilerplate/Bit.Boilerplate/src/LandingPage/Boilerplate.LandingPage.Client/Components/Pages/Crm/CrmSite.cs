namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public static class CrmSite
{
    public const string Path = "/crm";

    public const string TrialUrl = "https://adminpanel.bitplatform.dev/sign-up";

    public const string SalesEmail = "sales@brightlane.example";

    public static LandingSite Info { get; } = new()
    {
        Key = "crm",
        Path = Path,
        Name = "Brightlane",
        AccentColor = "#C8371D",
        Layout = typeof(CrmLayout),
        NotFound = typeof(CrmNotFound),
        Card = typeof(CrmCard),
        Logo = typeof(CrmLogo),
        Strings = typeof(CrmStrings),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        StatusUrl = "https://status.example.com/",
        Platforms =
        [
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/"),
            new(LandingPlatform.Windows, LandingPlatformStatus.Live, "https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe"),
            new(LandingPlatform.macOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.Android, LandingPlatformStatus.Live, "https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template")
        ],
        Company = new()
        {
            LegalName = "Brightlane Software Limited",
            Address = "14 Hanover Quay, Dublin 2, D02 VY60, Ireland",
            Email = "hello@brightlane.example",
            Phone = "+353 1 555 0187",
            RegistrationNumber = "CRO 712345",
            VatId = "IE3712345LH",
            Representative = "Aoife Brennan"
        }
    };
}
