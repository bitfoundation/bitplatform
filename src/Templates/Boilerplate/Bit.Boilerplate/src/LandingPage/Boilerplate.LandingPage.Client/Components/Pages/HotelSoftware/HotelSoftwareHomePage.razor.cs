using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwareHomePage
{
    private static readonly (string Value, string Label)[] stats =
    [
        (nameof(HotelSoftwareStrings.Stat1Value), nameof(HotelSoftwareStrings.Stat1Label)),
        (nameof(HotelSoftwareStrings.Stat2Value), nameof(HotelSoftwareStrings.Stat2Label)),
        (nameof(HotelSoftwareStrings.Stat3Value), nameof(HotelSoftwareStrings.Stat3Label)),
        (nameof(HotelSoftwareStrings.Stat4Value), nameof(HotelSoftwareStrings.Stat4Label))
    ];

    private static readonly Audience[] audiences =
    [
        new("independent", nameof(HotelSoftwareStrings.AudienceHotelsTab), nameof(HotelSoftwareStrings.AudienceHotelsWho), nameof(HotelSoftwareStrings.AudienceHotelsTitle), nameof(HotelSoftwareStrings.AudienceHotelsBody),
            [nameof(HotelSoftwareStrings.AudienceHotelsPoint1), nameof(HotelSoftwareStrings.AudienceHotelsPoint2), nameof(HotelSoftwareStrings.AudienceHotelsPoint3), nameof(HotelSoftwareStrings.AudienceHotelsPoint4)],
            nameof(HotelSoftwareStrings.AudienceHotelsFigure), nameof(HotelSoftwareStrings.AudienceHotelsFigureLabel), nameof(HotelSoftwareStrings.AudienceHotelsPlan)),
        new("groups", nameof(HotelSoftwareStrings.AudienceGroupsTab), nameof(HotelSoftwareStrings.AudienceGroupsWho), nameof(HotelSoftwareStrings.AudienceGroupsTitle), nameof(HotelSoftwareStrings.AudienceGroupsBody),
            [nameof(HotelSoftwareStrings.AudienceGroupsPoint1), nameof(HotelSoftwareStrings.AudienceGroupsPoint2), nameof(HotelSoftwareStrings.AudienceGroupsPoint3), nameof(HotelSoftwareStrings.AudienceGroupsPoint4)],
            nameof(HotelSoftwareStrings.AudienceGroupsFigure), nameof(HotelSoftwareStrings.AudienceGroupsFigureLabel), nameof(HotelSoftwareStrings.AudienceGroupsPlan)),
        new("bnb", nameof(HotelSoftwareStrings.AudienceBnbTab), nameof(HotelSoftwareStrings.AudienceBnbWho), nameof(HotelSoftwareStrings.AudienceBnbTitle), nameof(HotelSoftwareStrings.AudienceBnbBody),
            [nameof(HotelSoftwareStrings.AudienceBnbPoint1), nameof(HotelSoftwareStrings.AudienceBnbPoint2), nameof(HotelSoftwareStrings.AudienceBnbPoint3), nameof(HotelSoftwareStrings.AudienceBnbPoint4)],
            nameof(HotelSoftwareStrings.AudienceBnbFigure), nameof(HotelSoftwareStrings.AudienceBnbFigureLabel), nameof(HotelSoftwareStrings.AudienceBnbPlan))
    ];

    private static readonly (string Time, string Title, string Body)[] day =
    [
        (nameof(HotelSoftwareStrings.Day1Time), nameof(HotelSoftwareStrings.Day1Title), nameof(HotelSoftwareStrings.Day1Body)),
        (nameof(HotelSoftwareStrings.Day2Time), nameof(HotelSoftwareStrings.Day2Title), nameof(HotelSoftwareStrings.Day2Body)),
        (nameof(HotelSoftwareStrings.Day3Time), nameof(HotelSoftwareStrings.Day3Title), nameof(HotelSoftwareStrings.Day3Body)),
        (nameof(HotelSoftwareStrings.Day4Time), nameof(HotelSoftwareStrings.Day4Title), nameof(HotelSoftwareStrings.Day4Body)),
        (nameof(HotelSoftwareStrings.Day5Time), nameof(HotelSoftwareStrings.Day5Title), nameof(HotelSoftwareStrings.Day5Body))
    ];

    private static readonly (string Value, string Label)[] caseNumbers =
    [
        (nameof(HotelSoftwareStrings.CaseNumber1Value), nameof(HotelSoftwareStrings.CaseNumber1Label)),
        (nameof(HotelSoftwareStrings.CaseNumber2Value), nameof(HotelSoftwareStrings.CaseNumber2Label)),
        (nameof(HotelSoftwareStrings.CaseNumber3Value), nameof(HotelSoftwareStrings.CaseNumber3Label))
    ];

    private static readonly (string Quote, string Name, string Place)[] reviews =
    [
        (nameof(HotelSoftwareStrings.Review1Quote), nameof(HotelSoftwareStrings.Review1Name), nameof(HotelSoftwareStrings.Review1Place)),
        (nameof(HotelSoftwareStrings.Review2Quote), nameof(HotelSoftwareStrings.Review2Name), nameof(HotelSoftwareStrings.Review2Place))
    ];

    private static readonly (string Year, string Name, string By)[] awards =
    [
        (nameof(HotelSoftwareStrings.Award1Year), nameof(HotelSoftwareStrings.Award1Name), nameof(HotelSoftwareStrings.Award1By)),
        (nameof(HotelSoftwareStrings.Award2Year), nameof(HotelSoftwareStrings.Award2Name), nameof(HotelSoftwareStrings.Award2By)),
        (nameof(HotelSoftwareStrings.Award3Year), nameof(HotelSoftwareStrings.Award3Name), nameof(HotelSoftwareStrings.Award3By))
    ];

    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        var site = HotelSoftwareSite.Info;
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
                    ["@type"] = "SoftwareApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "Web, Windows, iPadOS",
                    ["applicationCategory"] = "BusinessApplication",
                    ["description"] = H[nameof(HotelSoftwareStrings.HomeDescription)].Value,
                    ["url"] = url
                })
        };

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{HotelSoftwareSite.Path}{path}", CultureInfo.CurrentUICulture);

    private sealed record Audience(string Key, string Tab, string Who, string Title, string Body, string[] Points, string Figure, string FigureLabel, string Plan);
}
