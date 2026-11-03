using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessHomePage
{
    private static readonly LandingPlatform[] androidOnly = [LandingPlatform.Android];

    private static readonly Release[] releases =
    [
        new("2.4", new(2026, 10, 21), nameof(FitnessStrings.News24Title),
        [
            ("New", nameof(FitnessStrings.News24Note1)),
            ("New", nameof(FitnessStrings.News24Note2)),
            ("Better", nameof(FitnessStrings.News24Note3)),
            ("Fixed", nameof(FitnessStrings.News24Note4))
        ]),
        new("2.3", new(2026, 9, 30), nameof(FitnessStrings.News23Title),
        [
            ("New", nameof(FitnessStrings.News23Note1)),
            ("New", nameof(FitnessStrings.News23Note2)),
            ("Better", nameof(FitnessStrings.News23Note3)),
            ("Fixed", nameof(FitnessStrings.News23Note4))
        ])
    ];

    private static readonly (string State, string Label, string Title, string When, string Body)[] roadmap =
    [
        ("testing", nameof(FitnessStrings.NextStateTesting), nameof(FitnessStrings.Next1Title), nameof(FitnessStrings.Next1When), nameof(FitnessStrings.Next1Body)),
        ("building", nameof(FitnessStrings.NextStateBuilding), nameof(FitnessStrings.Next2Title), nameof(FitnessStrings.Next2When), nameof(FitnessStrings.Next2Body)),
        ("exploring", nameof(FitnessStrings.NextStateExploring), nameof(FitnessStrings.Next3Title), nameof(FitnessStrings.Next3When), nameof(FitnessStrings.Next3Body))
    ];

    private static readonly (string Quote, string Name, string Who, int Streak)[] voices =
    [
        (nameof(FitnessStrings.Voice1Quote), nameof(FitnessStrings.Voice1Name), nameof(FitnessStrings.Voice1Who), 214),
        (nameof(FitnessStrings.Voice2Quote), nameof(FitnessStrings.Voice2Name), nameof(FitnessStrings.Voice2Who), 96),
        (nameof(FitnessStrings.Voice3Quote), nameof(FitnessStrings.Voice3Name), nameof(FitnessStrings.Voice3Who), 37)
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    private JsonObject? structuredData;
    private LandingFaq[] faq = [];
    private string[] suggestions = [];

    private static string Home => LocalUrl(FitnessSite.Path);

    protected override void OnParametersSet()
    {
        var site = FitnessSite.Info;
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
                    ["operatingSystem"] = "iOS 16 or later",
                    ["applicationCategory"] = "HealthApplication",
                    ["description"] = F[nameof(FitnessStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["installUrl"] = FitnessSite.AppStoreUrl,
                    ["url"] = url
                },
                new JsonObject
                {
                    ["@type"] = "WebApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "Any",
                    ["applicationCategory"] = "HealthApplication",
                    ["browserRequirements"] = "Requires JavaScript",
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["url"] = FitnessSite.WebAppUrl
                })
        };

        faq =
        [
            new(F[nameof(FitnessStrings.Faq1Q)], F[nameof(FitnessStrings.Faq1A)]),
            new(F[nameof(FitnessStrings.Faq2Q)], F[nameof(FitnessStrings.Faq2A)]),
            new(F[nameof(FitnessStrings.Faq3Q)], F[nameof(FitnessStrings.Faq3A)]),
            new(F[nameof(FitnessStrings.Faq4Q)], F[nameof(FitnessStrings.Faq4A), Money(4.99m), Money(39.99m)]),
            new(F[nameof(FitnessStrings.Faq5Q)], F[nameof(FitnessStrings.Faq5A)]),
            new(F[nameof(FitnessStrings.Faq6Q)], F[nameof(FitnessStrings.Faq6A)]),
            new(F[nameof(FitnessStrings.Faq7Q)], F[nameof(FitnessStrings.Faq7A)])
        ];

        suggestions =
        [
            F[nameof(FitnessStrings.ChatSuggestion1)],
            F[nameof(FitnessStrings.ChatSuggestion2)],
            F[nameof(FitnessStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private string Money(decimal value) => F[nameof(FitnessStrings.MoneyEUR), value.ToString("N2", CurrentCulture)];

    private static string Initials(string name) => string.Join('\u200c', name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));

    private sealed record Release(string Version, DateTime Date, string Title, (string Kind, string Text)[] Notes);
}
