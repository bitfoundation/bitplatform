using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Fitness;

public partial class FitnessAboutPage
{
    private static readonly (string Value, string Label)[] numbers =
    [
        (nameof(FitnessStrings.AboutNumber1Value), nameof(FitnessStrings.AboutNumber1Label)),
        (nameof(FitnessStrings.AboutNumber2Value), nameof(FitnessStrings.AboutNumber2Label)),
        (nameof(FitnessStrings.AboutNumber3Value), nameof(FitnessStrings.AboutNumber3Label)),
        (nameof(FitnessStrings.AboutNumber4Value), nameof(FitnessStrings.AboutNumber4Label))
    ];

    private static readonly (string Icon, string Title, string Body)[] principles =
    [
        (BitIconName.Stopwatch, nameof(FitnessStrings.Principle1Title), nameof(FitnessStrings.Principle1Body)),
        (BitIconName.ClearNight, nameof(FitnessStrings.Principle2Title), nameof(FitnessStrings.Principle2Body)),
        (BitIconName.Lock, nameof(FitnessStrings.Principle3Title), nameof(FitnessStrings.Principle3Body)),
        (BitIconName.Health, nameof(FitnessStrings.Principle4Title), nameof(FitnessStrings.Principle4Body))
    ];

    private static readonly (string State, string When, string What)[] launch =
    [
        ("done", nameof(FitnessStrings.Launch1When), nameof(FitnessStrings.Launch1What)),
        ("done", nameof(FitnessStrings.Launch2When), nameof(FitnessStrings.Launch2What)),
        ("now", nameof(FitnessStrings.Launch3When), nameof(FitnessStrings.Launch3What)),
        ("planned", nameof(FitnessStrings.Launch4When), nameof(FitnessStrings.Launch4What)),
        ("later", nameof(FitnessStrings.Launch5When), nameof(FitnessStrings.Launch5What))
    ];

    private static readonly (string Name, string Role, string Bio)[] team =
    [
        (nameof(FitnessStrings.Team1Name), nameof(FitnessStrings.Team1Role), nameof(FitnessStrings.Team1Bio)),
        (nameof(FitnessStrings.Team2Name), nameof(FitnessStrings.Team2Role), nameof(FitnessStrings.Team2Bio)),
        (nameof(FitnessStrings.Team3Name), nameof(FitnessStrings.Team3Role), nameof(FitnessStrings.Team3Bio))
    ];

    [AutoInject] private IStringLocalizer<FitnessStrings> F = default!;

    private JsonObject? structuredData;
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        var site = FitnessSite.Info;

        structuredData = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "AboutPage",
            ["url"] = NavigationManager.ToAbsoluteUri(LocalUrl($"{site.Path}/about")).ToString(),
            ["mainEntity"] = new JsonObject
            {
                ["@type"] = "Organization",
                ["name"] = site.Company.LegalName,
                ["url"] = NavigationManager.ToAbsoluteUri(LocalUrl(site.Path)).ToString(),
                ["email"] = site.Company.Email,
                ["telephone"] = site.Company.Phone,
                ["address"] = site.Company.Address,
                ["foundingDate"] = "2023",
                ["founder"] = new JsonArray(
                    new JsonObject { ["@type"] = "Person", ["name"] = F[nameof(FitnessStrings.Team1Name)].Value },
                    new JsonObject { ["@type"] = "Person", ["name"] = F[nameof(FitnessStrings.Team2Name)].Value })
            }
        };

        suggestions =
        [
            F[nameof(FitnessStrings.ChatSuggestion1)],
            F[nameof(FitnessStrings.ChatSuggestion4)]
        ];

        base.OnParametersSet();
    }

    private static string Initials(string name) => string.Join('\u200c', name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
