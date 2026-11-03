namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public static class EcommerceSite
{
    public const string Path = "/ecommerce";

    public static LandingSite Info { get; } = new()
    {
        Key = "ecommerce",
        Path = Path,
        Name = "Fernhouse",
        AccentColor = "#1E6B47",
        Layout = typeof(EcommerceLayout),
        NotFound = typeof(EcommerceNotFound),
        Logo = typeof(EcommerceLogo),
        Strings = typeof(EcommerceStrings),
        AppleAppId = "6450611349",
        WebAppUrl = "https://adminpanel.bitplatform.dev/",
        StatusUrl = "https://status.example.com/",
        Platforms =
        [
            new(LandingPlatform.iOS, LandingPlatformStatus.Live, "https://apps.apple.com/app/bit-adminpanel/id6450611349"),
            new(LandingPlatform.Android, LandingPlatformStatus.Live, "https://play.google.com/store/apps/details?id=com.bitplatform.AdminPanel.Template"),
            new(LandingPlatform.Web, LandingPlatformStatus.Live, "https://adminpanel.bitplatform.dev/")
        ],
        Company = new()
        {
            LegalName = "Fernhouse Living B.V.",
            Address = "Keizersgracht 221, 1016 DV Amsterdam, Netherlands",
            Email = "hello@fernhouse.example",
            Phone = "+31 20 555 0142",
            RegistrationNumber = "KvK 81234567",
            VatId = "NL862345671B01",
            Representative = "Noor van Dijk"
        }
    };
}
