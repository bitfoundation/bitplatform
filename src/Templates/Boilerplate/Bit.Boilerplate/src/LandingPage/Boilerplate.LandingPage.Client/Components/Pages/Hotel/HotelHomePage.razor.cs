using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelHomePage
{
    private static readonly (string Row, string Us, string Them)[] compare =
    [
        (nameof(HotelStrings.DirectRow1), nameof(HotelStrings.DirectRow1Us), nameof(HotelStrings.DirectRow1Them)),
        (nameof(HotelStrings.DirectRow2), nameof(HotelStrings.DirectRow2Us), nameof(HotelStrings.DirectRow2Them)),
        (nameof(HotelStrings.DirectRow3), nameof(HotelStrings.DirectRow3Us), nameof(HotelStrings.DirectRow3Them)),
        (nameof(HotelStrings.DirectRow4), nameof(HotelStrings.DirectRow4Us), nameof(HotelStrings.DirectRow4Them)),
        (nameof(HotelStrings.DirectRow5), nameof(HotelStrings.DirectRow5Us), nameof(HotelStrings.DirectRow5Them)),
        (nameof(HotelStrings.DirectRow6), nameof(HotelStrings.DirectRow6Us), nameof(HotelStrings.DirectRow6Them))
    ];

    private static readonly (string Icon, string Title, string Body)[] appFeatures =
    [
        ("ferry", nameof(HotelStrings.App1Title), nameof(HotelStrings.App1Body)),
        ("key", nameof(HotelStrings.App2Title), nameof(HotelStrings.App2Body)),
        ("bell", nameof(HotelStrings.App3Title), nameof(HotelStrings.App3Body)),
        ("compass", nameof(HotelStrings.App4Title), nameof(HotelStrings.App4Body))
    ];

    private static readonly (string Quote, string Name, string Detail)[] stories =
    [
        (nameof(HotelStrings.Story1Quote), nameof(HotelStrings.Story1Name), nameof(HotelStrings.Story1Detail)),
        (nameof(HotelStrings.Story2Quote), nameof(HotelStrings.Story2Name), nameof(HotelStrings.Story2Detail)),
        (nameof(HotelStrings.Story3Quote), nameof(HotelStrings.Story3Name), nameof(HotelStrings.Story3Detail))
    ];

    private static readonly (string Category, double Score)[] categories =
    [
        (nameof(HotelStrings.TripLocation), 4.9),
        (nameof(HotelStrings.TripRooms), 4.8),
        (nameof(HotelStrings.TripService), 4.9),
        (nameof(HotelStrings.TripValue), 4.6),
        (nameof(HotelStrings.TripCleanliness), 4.9),
        (nameof(HotelStrings.TripSleep), 4.8)
    ];

    private static readonly (string Award, string By, int Year)[] awards =
    [
        (nameof(HotelStrings.Award1), nameof(HotelStrings.Award1By), 2025),
        (nameof(HotelStrings.Award2), nameof(HotelStrings.Award2By), 2024),
        (nameof(HotelStrings.Award3), nameof(HotelStrings.Award3By), 2025)
    ];

    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    private LandingFaq[] faq = [];
    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        faq =
        [
            new(H[nameof(HotelStrings.Faq1Question)], H[nameof(HotelStrings.Faq1Answer)]),
            new(H[nameof(HotelStrings.Faq2Question)], H[nameof(HotelStrings.Faq2Answer)]),
            new(H[nameof(HotelStrings.Faq3Question)], H[nameof(HotelStrings.Faq3Answer)]),
            new(H[nameof(HotelStrings.Faq4Question)], H[nameof(HotelStrings.Faq4Answer)]),
            new(H[nameof(HotelStrings.Faq5Question)], H[nameof(HotelStrings.Faq5Answer)])
        ];

        var site = HotelSite.Info;
        var url = AbsoluteUrl(LocalUrl(site.Path));

        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new JsonArray(
                new JsonObject
                {
                    ["@type"] = "Hotel",
                    ["name"] = site.Name,
                    ["url"] = url,
                    ["email"] = site.Company.Email,
                    ["telephone"] = site.Company.Phone,
                    ["numberOfRooms"] = HotelRooms.All.Sum(r => r.Count),
                    ["checkinTime"] = "15:00",
                    ["checkoutTime"] = "11:00",
                    ["petsAllowed"] = true,
                    ["address"] = new JsonObject
                    {
                        ["@type"] = "PostalAddress",
                        ["streetAddress"] = "Strandwai 14",
                        ["postalCode"] = "25946",
                        ["addressLocality"] = "Norddorf auf Amrum",
                        ["addressCountry"] = "DE"
                    }
                },
                new JsonObject
                {
                    ["@type"] = "MobileApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "iOS, Android",
                    ["applicationCategory"] = "TravelApplication",
                    ["description"] = H[nameof(HotelStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["installUrl"] = site.Find(LandingPlatform.iOS)?.Url
                })
        };

        base.OnParametersSet();
    }

    private string Money(decimal amount) => H[nameof(HotelStrings.Money), amount.ToString("N0", CurrentCulture)];
}
