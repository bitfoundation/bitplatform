namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public static class HotelSoftwareSite
{
    public const string Path = "/hotel-software";

    public const string TrialUrl = "https://adminpanel.bitplatform.dev/sign-up";

    public const string SalesEmail = "sales@lodgeboard.example";

    public const string PressEmail = "press@lodgeboard.example";

    public const string ReviewsUrl = "https://www.trustpilot.com/review/lodgeboard.example";

    public static LandingSite Info { get; } = new()
    {
        Key = "hotel-software",
        Path = Path,
        Name = "Lodgeboard",
        AccentColor = "#2F3439",
        Layout = typeof(HotelSoftwareLayout),
        NotFound = typeof(HotelSoftwareNotFound),
        Card = typeof(HotelSoftwareCard),
        Logo = typeof(HotelSoftwareLogo),
        Strings = typeof(HotelSoftwareStrings),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        StatusUrl = "https://status.example.com/",
        Platforms =
        [
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/"),
            new(LandingPlatform.Windows, LandingPlatformStatus.Live, "https://windows-admin.bitplatform.dev/AdminPanel.Client.Windows-win-Setup.exe"),
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349")
        ],
        Company = new()
        {
            LegalName = "Lodgeboard, Lda",
            Address = "Rua de Ceuta 118, 4050-190 Porto, Portugal",
            Email = "hello@lodgeboard.example",
            Phone = "+351 22 555 0142",
            RegistrationNumber = "NIPC 516234987",
            VatId = "PT516234987",
            Representative = "Teresa Lobo"
        }
    };
}
