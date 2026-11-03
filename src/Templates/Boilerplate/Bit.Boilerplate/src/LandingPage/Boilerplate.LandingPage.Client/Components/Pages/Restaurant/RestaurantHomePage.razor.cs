using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public partial class RestaurantHomePage
{
    private static readonly (string Title, string When, string Body)[] features =
    [
        (nameof(RestaurantStrings.Feature1Title), nameof(RestaurantStrings.FeatureAtLaunch), nameof(RestaurantStrings.Feature1Body)),
        (nameof(RestaurantStrings.Feature2Title), nameof(RestaurantStrings.FeatureAtLaunch), nameof(RestaurantStrings.Feature2Body)),
        (nameof(RestaurantStrings.Feature3Title), nameof(RestaurantStrings.FeatureAtLaunch), nameof(RestaurantStrings.Feature3Body)),
        (nameof(RestaurantStrings.Feature4Title), nameof(RestaurantStrings.FeatureSpring), nameof(RestaurantStrings.Feature4Body))
    ];

    [AutoInject] private IStringLocalizer<RestaurantStrings> C = default!;

    private string[] units = [];
    private string[] suggestions = [];
    private LandingFaq[] faq = [];
    private JsonObject? structuredData;

    private static readonly DateTimeOffset PerkEnds = new(2027, 2, 28, 0, 0, 0, TimeSpan.FromHours(1));

    private static readonly DateTimeOffset PaperCardsEnd = new(2027, 6, 30, 0, 0, 0, TimeSpan.FromHours(2));

    private static string LaunchDay => Day(RestaurantSite.LaunchDate);

    protected override void OnParametersSet()
    {
        units =
        [
            C[nameof(RestaurantStrings.CountdownDays)],
            C[nameof(RestaurantStrings.CountdownHours)],
            C[nameof(RestaurantStrings.CountdownMinutes)],
            C[nameof(RestaurantStrings.CountdownSeconds)]
        ];

        suggestions =
        [
            C[nameof(RestaurantStrings.ChatSuggestion1)],
            C[nameof(RestaurantStrings.ChatSuggestion2)],
            C[nameof(RestaurantStrings.ChatSuggestion3)]
        ];

        faq =
        [
            new(C[nameof(RestaurantStrings.Faq1Question)], C[nameof(RestaurantStrings.Faq1Answer), LaunchDay]),
            new(C[nameof(RestaurantStrings.Faq2Question)], C[nameof(RestaurantStrings.Faq2Answer)]),
            new(C[nameof(RestaurantStrings.Faq3Question)], C[nameof(RestaurantStrings.Faq3Answer), LaunchDay, Day(PaperCardsEnd)]),
            new(C[nameof(RestaurantStrings.Faq4Question)], C[nameof(RestaurantStrings.Faq4Answer)]),
            new(C[nameof(RestaurantStrings.Faq5Question)], C[nameof(RestaurantStrings.Faq5Answer)])
        ];

        var site = RestaurantSite.Info;
        var url = NavigationManager.ToAbsoluteUri(LocalUrl(site.Path)).ToString();

        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new JsonArray([
                new JsonObject
                {
                    ["@type"] = "Organization",
                    ["name"] = site.Company.LegalName,
                    ["url"] = url,
                    ["email"] = site.Company.Email,
                    ["telephone"] = site.Company.Phone,
                    ["address"] = site.Company.Address
                },
                .. RestaurantCafes.All.Select(cafe => (JsonNode)new JsonObject
                {
                    ["@type"] = "CafeOrCoffeeShop",
                    ["name"] = $"Little Copper {C[cafe.NameKey].Value}",
                    ["address"] = new JsonObject { ["@type"] = "PostalAddress", ["streetAddress"] = cafe.Address, ["addressCountry"] = "DK" },
                    ["openingHoursSpecification"] = new JsonArray(
                        Hours(["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"], cafe.WeekdayOpen, cafe.WeekdayClose),
                        Hours(["Saturday", "Sunday"], cafe.WeekendOpen, cafe.WeekendClose))
                })
            ])
        };

        base.OnParametersSet();
    }

    private static JsonObject Hours(string[] days, TimeOnly opens, TimeOnly closes) => new()
    {
        ["@type"] = "OpeningHoursSpecification",
        ["dayOfWeek"] = new JsonArray([.. days.Select(day => (JsonNode)day)]),
        ["opens"] = opens.ToString("HH:mm", CultureInfo.InvariantCulture),
        ["closes"] = closes.ToString("HH:mm", CultureInfo.InvariantCulture)
    };

    private static string Day(DateTimeOffset date) => date.ToString("d MMMM yyyy", CurrentCulture);

    private string Range(TimeOnly from, TimeOnly to) => C[nameof(RestaurantStrings.TimeRange), from.ToString("HH:mm", CurrentCulture), to.ToString("HH:mm", CurrentCulture)];

    private static string Link(string hash) => $"{LandingCultures.Localize(RestaurantSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
