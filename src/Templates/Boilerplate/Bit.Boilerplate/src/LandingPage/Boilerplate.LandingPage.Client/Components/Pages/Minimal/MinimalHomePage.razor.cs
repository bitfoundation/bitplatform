using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Minimal;

public partial class MinimalHomePage
{
    private static readonly (string Title, string Body)[] features =
    [
        (nameof(MinimalStrings.Feature1Title), nameof(MinimalStrings.Feature1Body)),
        (nameof(MinimalStrings.Feature2Title), nameof(MinimalStrings.Feature2Body)),
        (nameof(MinimalStrings.Feature3Title), nameof(MinimalStrings.Feature3Body))
    ];

    [AutoInject] private IStringLocalizer<MinimalStrings> M = default!;

    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        var site = MinimalSite.Info;
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
                    ["address"] = site.Company.Address
                },
                new JsonObject
                {
                    ["@type"] = "MobileApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "iOS, Android",
                    ["applicationCategory"] = "LifestyleApplication",
                    ["description"] = M[nameof(MinimalStrings.HomeDescription)].Value
                })
        };

        base.OnParametersSet();
    }
}
