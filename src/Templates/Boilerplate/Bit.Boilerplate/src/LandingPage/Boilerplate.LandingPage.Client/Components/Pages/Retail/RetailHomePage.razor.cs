using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailHomePage
{
    private static readonly RetailOffer[] offers =
    [
        new(RetailOfferKind.Offer, nameof(RetailStrings.OfferPotatoes), nameof(RetailStrings.OfferPotatoesPack), 2.49m, 3.29m, nameof(RetailStrings.OfferPerKg), 1.00m),
        new(RetailOfferKind.Card, nameof(RetailStrings.OfferHake), nameof(RetailStrings.OfferHakePack), 5.99m, 7.50m, nameof(RetailStrings.OfferPerKg), 19.97m),
        new(RetailOfferKind.Local, nameof(RetailStrings.OfferSourdough), nameof(RetailStrings.OfferSourdoughPack), 3.80m, null, nameof(RetailStrings.OfferPerKg), 4.75m),
        new(RetailOfferKind.Offer, nameof(RetailStrings.OfferEggs), nameof(RetailStrings.OfferEggsPack), 3.79m, 4.49m, nameof(RetailStrings.OfferEach), 0.32m),
        new(RetailOfferKind.Local, nameof(RetailStrings.OfferApples), nameof(RetailStrings.OfferApplesPack), 1.99m, null, nameof(RetailStrings.OfferPerKg), 1.99m),
        new(RetailOfferKind.Card, nameof(RetailStrings.OfferCoffee), nameof(RetailStrings.OfferCoffeePack), 6.50m, 7.95m, nameof(RetailStrings.OfferPerKg), 26.00m)
    ];

    private static readonly (int Stars, int Percent)[] distribution = [(5, 81), (4, 12), (3, 4), (2, 1), (1, 2)];

    private static readonly (string Name, string Shop, string When, string Text, int Stars)[] reviews =
    [
        (nameof(RetailStrings.Review1Name), nameof(RetailStrings.ShopDrumcondra), nameof(RetailStrings.Review1When), nameof(RetailStrings.Review1Text), 5),
        (nameof(RetailStrings.Review2Name), nameof(RetailStrings.ShopPhibsborough), nameof(RetailStrings.Review2When), nameof(RetailStrings.Review2Text), 5),
        (nameof(RetailStrings.Review3Name), nameof(RetailStrings.ShopClontarf), nameof(RetailStrings.Review3When), nameof(RetailStrings.Review3Text), 4)
    ];

    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private DateTime? now;
    private LandingFaq[] faq = [];
    private BitTimelineItem[] timeline = [];
    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        timeline =
        [
            new() { PrimaryText = Time(12, 40), SecondaryText = R[nameof(RetailStrings.Ticket1)] },
            new() { PrimaryText = Time(15, 10), SecondaryText = R[nameof(RetailStrings.Ticket2)] },
            new() { PrimaryText = Time(16, 45), SecondaryText = R[nameof(RetailStrings.Ticket3)] },
            new() { PrimaryText = Time(17, 20), SecondaryText = R[nameof(RetailStrings.Ticket4)], IconName = BitIconName.CheckMark }
        ];

        faq =
        [
            new(R[nameof(RetailStrings.Faq1Question)], R[nameof(RetailStrings.Faq1Answer)]),
            new(R[nameof(RetailStrings.Faq2Question)], R[nameof(RetailStrings.Faq2Answer)]),
            new(R[nameof(RetailStrings.Faq3Question)], R[nameof(RetailStrings.Faq3Answer)]),
            new(R[nameof(RetailStrings.Faq4Question)], R[nameof(RetailStrings.Faq4Answer)]),
            new(R[nameof(RetailStrings.Faq5Question)], R[nameof(RetailStrings.Faq5Answer)])
        ];

        var site = RetailSite.Info;
        var url = AbsoluteUrl(LocalUrl(site.Path));

        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new JsonArray(
                new JsonObject
                {
                    ["@type"] = "Organization",
                    ["name"] = site.Company.LegalName,
                    ["url"] = url,
                    ["email"] = site.Company.Email,
                    ["telephone"] = site.Company.Phone,
                    ["address"] = site.Company.Address
                },
                new JsonObject
                {
                    ["@type"] = "MobileApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "iOS, Android",
                    ["applicationCategory"] = "ShoppingApplication",
                    ["description"] = R[nameof(RetailStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["installUrl"] = site.Find(LandingPlatform.iOS)?.Url
                })
        };

        base.OnParametersSet();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            now = RetailShops.DublinNow();
            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }

    private string Money(decimal amount) => R[nameof(RetailStrings.Money), amount.ToString(amount % 1 is 0 ? "0" : "0.00", CurrentCulture)];

    private static string Time(int hour, int minute) => new TimeOnly(hour, minute).ToString("HH:mm", CurrentCulture);

    private enum RetailOfferKind
    {
        Offer,
        Card,
        Local
    }

    private sealed record RetailOffer(RetailOfferKind Kind, string Name, string Pack, decimal Price, decimal? Was, string UnitKey, decimal UnitPrice);
}
