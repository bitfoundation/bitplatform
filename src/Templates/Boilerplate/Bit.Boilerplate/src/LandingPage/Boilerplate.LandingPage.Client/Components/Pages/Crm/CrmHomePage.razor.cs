using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmHomePage
{
    private static readonly (string Value, string? Unit, string Label)[] stats =
    [
        (nameof(CrmStrings.Stat1Value), nameof(CrmStrings.Stat1Unit), nameof(CrmStrings.Stat1Label)),
        (nameof(CrmStrings.Stat2Value), nameof(CrmStrings.Stat2Unit), nameof(CrmStrings.Stat2Label)),
        (nameof(CrmStrings.Stat3Value), null, nameof(CrmStrings.Stat3Label)),
        (nameof(CrmStrings.Stat4Value), null, nameof(CrmStrings.Stat4Label))
    ];

    private static readonly (string Tag, string Title, string Body, string Detail)[] capabilities =
    [
        (nameof(CrmStrings.Cap1Tag), nameof(CrmStrings.Cap1Title), nameof(CrmStrings.Cap1Body), nameof(CrmStrings.Cap1Detail)),
        (nameof(CrmStrings.Cap2Tag), nameof(CrmStrings.Cap2Title), nameof(CrmStrings.Cap2Body), nameof(CrmStrings.Cap2Detail)),
        (nameof(CrmStrings.Cap3Tag), nameof(CrmStrings.Cap3Title), nameof(CrmStrings.Cap3Body), nameof(CrmStrings.Cap3Detail)),
        (nameof(CrmStrings.Cap4Tag), nameof(CrmStrings.Cap4Title), nameof(CrmStrings.Cap4Body), nameof(CrmStrings.Cap4Detail)),
        (nameof(CrmStrings.Cap5Tag), nameof(CrmStrings.Cap5Title), nameof(CrmStrings.Cap5Body), nameof(CrmStrings.Cap5Detail)),
        (nameof(CrmStrings.Cap6Tag), nameof(CrmStrings.Cap6Title), nameof(CrmStrings.Cap6Body), nameof(CrmStrings.Cap6Detail))
    ];

    private static readonly (string Value, string? Unit, string Label)[] caseNumbers =
    [
        (nameof(CrmStrings.CaseNumber1Value), null, nameof(CrmStrings.CaseNumber1Label)),
        (nameof(CrmStrings.CaseNumber2Value), nameof(CrmStrings.CaseNumber2Unit), nameof(CrmStrings.CaseNumber2Label)),
        (nameof(CrmStrings.CaseNumber3Value), null, nameof(CrmStrings.CaseNumber3Label))
    ];

    private static readonly (string Row, string Sheet, string Us, string Suite)[] comparison =
    [
        (nameof(CrmStrings.CompareRow1), nameof(CrmStrings.CompareRow1Sheet), nameof(CrmStrings.CompareRow1Us), nameof(CrmStrings.CompareRow1Suite)),
        (nameof(CrmStrings.CompareRow2), nameof(CrmStrings.CompareRow2Sheet), nameof(CrmStrings.CompareRow2Us), nameof(CrmStrings.CompareRow2Suite)),
        (nameof(CrmStrings.CompareRow3), nameof(CrmStrings.CompareRow3Sheet), nameof(CrmStrings.CompareRow3Us), nameof(CrmStrings.CompareRow3Suite)),
        (nameof(CrmStrings.CompareRow4), nameof(CrmStrings.CompareRow4Sheet), nameof(CrmStrings.CompareRow4Us), nameof(CrmStrings.CompareRow4Suite)),
        (nameof(CrmStrings.CompareRow5), nameof(CrmStrings.CompareRow5Sheet), nameof(CrmStrings.CompareRow5Us), nameof(CrmStrings.CompareRow5Suite))
    ];

    private static readonly (string Quote, string Name, string Role)[] reviews =
    [
        (nameof(CrmStrings.Review1Quote), nameof(CrmStrings.Review1Name), nameof(CrmStrings.Review1Role)),
        (nameof(CrmStrings.Review2Quote), nameof(CrmStrings.Review2Name), nameof(CrmStrings.Review2Role)),
        (nameof(CrmStrings.Review3Quote), nameof(CrmStrings.Review3Name), nameof(CrmStrings.Review3Role))
    ];

    private static readonly (string Icon, string Title, string Body)[] security =
    [
        (BitIconName.World, nameof(CrmStrings.Security1Title), nameof(CrmStrings.Security1Body)),
        (BitIconName.Lock, nameof(CrmStrings.Security2Title), nameof(CrmStrings.Security2Body)),
        (BitIconName.Permissions, nameof(CrmStrings.Security3Title), nameof(CrmStrings.Security3Body)),
        (BitIconName.DocumentApproval, nameof(CrmStrings.Security4Title), nameof(CrmStrings.Security4Body))
    ];

    private static readonly (string Name, string Detail)[] seals =
    [
        (nameof(CrmStrings.Seal1Name), nameof(CrmStrings.Seal1Detail)),
        (nameof(CrmStrings.Seal2Name), nameof(CrmStrings.Seal2Detail)),
        (nameof(CrmStrings.Seal3Name), nameof(CrmStrings.Seal3Detail))
    ];

    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        var site = CrmSite.Info;
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
                    ["operatingSystem"] = "Web, Windows, macOS, iOS, Android",
                    ["applicationCategory"] = "BusinessApplication",
                    ["description"] = R[nameof(CrmStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "15", ["priceCurrency"] = "EUR" },
                    ["url"] = url
                })
        };

        base.OnParametersSet();
    }

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));

    private static string Page(string path) => LandingCultures.Localize($"{CrmSite.Path}{path}", CultureInfo.CurrentUICulture);
}
