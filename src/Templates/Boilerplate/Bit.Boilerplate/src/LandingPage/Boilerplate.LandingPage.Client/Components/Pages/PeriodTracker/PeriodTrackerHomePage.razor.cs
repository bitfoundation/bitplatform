using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.PeriodTracker;

public partial class PeriodTrackerHomePage
{
    private static readonly (string Icon, string Title, string Body)[] promises =
    [
        (BitIconName.BlockContact, nameof(PeriodTrackerStrings.Promise1Title), nameof(PeriodTrackerStrings.Promise1Body)),
        (BitIconName.CellPhone, nameof(PeriodTrackerStrings.Promise2Title), nameof(PeriodTrackerStrings.Promise2Body)),
        (BitIconName.Encryption, nameof(PeriodTrackerStrings.Promise3Title), nameof(PeriodTrackerStrings.Promise3Body)),
        (BitIconName.Blocked2, nameof(PeriodTrackerStrings.Promise4Title), nameof(PeriodTrackerStrings.Promise4Body)),
        (BitIconName.Delete, nameof(PeriodTrackerStrings.Promise5Title), nameof(PeriodTrackerStrings.Promise5Body))
    ];

    private static readonly (string Label, string Note, int Spread)[] windows =
    [
        (nameof(PeriodTrackerStrings.HowRow1), nameof(PeriodTrackerStrings.HowRow1Note), 3),
        (nameof(PeriodTrackerStrings.HowRow2), nameof(PeriodTrackerStrings.HowRow2Note), 2),
        (nameof(PeriodTrackerStrings.HowRow3), nameof(PeriodTrackerStrings.HowRow3Note), 1)
    ];

    private static readonly string[] betaNow =
    [
        nameof(PeriodTrackerStrings.BetaNow1),
        nameof(PeriodTrackerStrings.BetaNow2),
        nameof(PeriodTrackerStrings.BetaNow3),
        nameof(PeriodTrackerStrings.BetaNow4),
        nameof(PeriodTrackerStrings.BetaNow5),
        nameof(PeriodTrackerStrings.BetaNow6),
        nameof(PeriodTrackerStrings.BetaNow7),
        nameof(PeriodTrackerStrings.BetaNow8)
    ];

    private static readonly string[] betaNext =
    [
        nameof(PeriodTrackerStrings.BetaNext1),
        nameof(PeriodTrackerStrings.BetaNext2),
        nameof(PeriodTrackerStrings.BetaNext3),
        nameof(PeriodTrackerStrings.BetaNext4)
    ];

    private static readonly (string Quote, string Name, string Meta)[] stories =
    [
        (nameof(PeriodTrackerStrings.Story1Quote), nameof(PeriodTrackerStrings.Story1Name), nameof(PeriodTrackerStrings.Story1Meta)),
        (nameof(PeriodTrackerStrings.Story2Quote), nameof(PeriodTrackerStrings.Story2Name), nameof(PeriodTrackerStrings.Story2Meta)),
        (nameof(PeriodTrackerStrings.Story3Quote), nameof(PeriodTrackerStrings.Story3Name), nameof(PeriodTrackerStrings.Story3Meta))
    ];

    private static readonly LandingPlatform[] waitlistPlatforms = [LandingPlatform.Android, LandingPlatform.iOS];

    [AutoInject] private IStringLocalizer<PeriodTrackerStrings> P = default!;

    private string[] suggestions = [];
    private LandingFaq[] faq = [];
    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        suggestions =
        [
            P[nameof(PeriodTrackerStrings.ChatSuggestion1)],
            P[nameof(PeriodTrackerStrings.ChatSuggestion2)],
            P[nameof(PeriodTrackerStrings.ChatSuggestion3)]
        ];

        faq =
        [
            new(P[nameof(PeriodTrackerStrings.Faq1Question)], P[nameof(PeriodTrackerStrings.Faq1Answer)]),
            new(P[nameof(PeriodTrackerStrings.Faq2Question)], P[nameof(PeriodTrackerStrings.Faq2Answer)]),
            new(P[nameof(PeriodTrackerStrings.Faq3Question)], P[nameof(PeriodTrackerStrings.Faq3Answer)]),
            new(P[nameof(PeriodTrackerStrings.Faq4Question)], P[nameof(PeriodTrackerStrings.Faq4Answer)]),
            new(P[nameof(PeriodTrackerStrings.Faq5Question)], P[nameof(PeriodTrackerStrings.Faq5Answer)]),
            new(P[nameof(PeriodTrackerStrings.Faq6Question)], P[nameof(PeriodTrackerStrings.Faq6Answer)])
        ];

        var site = PeriodTrackerSite.Info;
        var url = NavigationManager.ToAbsoluteUri(LocalUrl(site.Path)).ToString();

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
                    ["operatingSystem"] = "iOS 17",
                    ["applicationCategory"] = "HealthApplication",
                    ["description"] = P[nameof(PeriodTrackerStrings.HomeDescription)].Value,
                    ["softwareVersion"] = "0.9",
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["installUrl"] = PeriodTrackerSite.BetaUrl
                })
        };

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(PeriodTrackerSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
