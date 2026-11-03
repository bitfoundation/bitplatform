namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public static class RetailSite
{
    public const string Path = "/retail";

    public const double GoogleRating = 4.7;

    public const int GoogleReviewCount = 1284;

    public const string GoogleReviewsUrl = "https://www.google.com/maps/search/?api=1&query=Northside+Market+Dublin";

    public static LandingSite Info { get; } = new()
    {
        Key = "retail",
        Path = Path,
        Name = "Northside Market",
        AccentColor = "#C8331C",
        Layout = typeof(RetailLayout),
        NotFound = typeof(RetailNotFound),
        Card = typeof(RetailCard),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.Android, LandingPlatformStatus.Live, "https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template"),
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/")
        ],
        Company = new()
        {
            LegalName = "Northside Market Limited",
            Address = "41 Phibsborough Road, Dublin 7, D07 R2K4, Ireland",
            Email = "hello@northside.example",
            Phone = "+353 1 555 0198",
            RegistrationNumber = "CRO 284517",
            VatId = "IE9688425R",
            Representative = "Siobhán Byrne"
        }
    };
}
