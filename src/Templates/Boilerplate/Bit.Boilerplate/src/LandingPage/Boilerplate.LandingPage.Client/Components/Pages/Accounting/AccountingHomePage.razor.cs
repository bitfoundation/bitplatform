using System.Text.Json.Nodes;

namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingHomePage
{
    private static readonly (string When, string Title, string Body)[] month =
    [
        (nameof(AccountingStrings.Month1When), nameof(AccountingStrings.Month1Title), nameof(AccountingStrings.Month1Body)),
        (nameof(AccountingStrings.Month2When), nameof(AccountingStrings.Month2Title), nameof(AccountingStrings.Month2Body)),
        (nameof(AccountingStrings.Month3When), nameof(AccountingStrings.Month3Title), nameof(AccountingStrings.Month3Body)),
        (nameof(AccountingStrings.Month4When), nameof(AccountingStrings.Month4Title), nameof(AccountingStrings.Month4Body)),
        (nameof(AccountingStrings.Month5When), nameof(AccountingStrings.Month5Title), nameof(AccountingStrings.Month5Body))
    ];

    private static readonly (string Icon, string Title, string Body, string Tag)[] features =
    [
        (BitIconName.Invoice, nameof(AccountingStrings.Feature1Title), nameof(AccountingStrings.Feature1Body), nameof(AccountingStrings.Feature1Tag)),
        (BitIconName.Bank, nameof(AccountingStrings.Feature2Title), nameof(AccountingStrings.Feature2Body), nameof(AccountingStrings.Feature2Tag)),
        (BitIconName.Camera, nameof(AccountingStrings.Feature3Title), nameof(AccountingStrings.Feature3Body), nameof(AccountingStrings.Feature3Tag)),
        (BitIconName.CalculatorPercentage, nameof(AccountingStrings.Feature4Title), nameof(AccountingStrings.Feature4Body), nameof(AccountingStrings.Feature4Tag)),
        (BitIconName.MailReminder, nameof(AccountingStrings.Feature5Title), nameof(AccountingStrings.Feature5Body), nameof(AccountingStrings.Feature5Tag)),
        (BitIconName.People, nameof(AccountingStrings.Feature6Title), nameof(AccountingStrings.Feature6Body), nameof(AccountingStrings.Feature6Tag))
    ];

    private static readonly (string Trade, string Quote, string Name)[] voices =
    [
        (nameof(AccountingStrings.Voice1Trade), nameof(AccountingStrings.Voice1Quote), nameof(AccountingStrings.Voice1Name)),
        (nameof(AccountingStrings.Voice2Trade), nameof(AccountingStrings.Voice2Quote), nameof(AccountingStrings.Voice2Name)),
        (nameof(AccountingStrings.Voice3Trade), nameof(AccountingStrings.Voice3Quote), nameof(AccountingStrings.Voice3Name))
    ];

    private static readonly (string Icon, string Title, string Body)[] security =
    [
        (BitIconName.Lock, nameof(AccountingStrings.Security1Title), nameof(AccountingStrings.Security1Body)),
        (BitIconName.World, nameof(AccountingStrings.Security2Title), nameof(AccountingStrings.Security2Body)),
        (BitIconName.Fingerprint, nameof(AccountingStrings.Security3Title), nameof(AccountingStrings.Security3Body)),
        (BitIconName.Archive, nameof(AccountingStrings.Security4Title), nameof(AccountingStrings.Security4Body))
    ];

    private static readonly (string Name, string Price, string Note)[] teaser =
    [
        (nameof(AccountingStrings.PlanFree), nameof(AccountingStrings.TeaserFreePrice), nameof(AccountingStrings.TeaserFreeNote)),
        (nameof(AccountingStrings.PlanSolo), nameof(AccountingStrings.TeaserSoloPrice), nameof(AccountingStrings.TeaserSoloNote)),
        (nameof(AccountingStrings.PlanStudio), nameof(AccountingStrings.TeaserStudioPrice), nameof(AccountingStrings.TeaserStudioNote)),
        (nameof(AccountingStrings.PlanCompany), nameof(AccountingStrings.TeaserCompanyPrice), nameof(AccountingStrings.TeaserCompanyNote))
    ];

    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private JsonObject? structuredData;

    protected override void OnParametersSet()
    {
        var site = AccountingSite.Info;
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
                    ["@type"] = "SoftwareApplication",
                    ["name"] = site.Name,
                    ["operatingSystem"] = "Web, Windows, iOS, Android",
                    ["applicationCategory"] = "FinanceApplication",
                    ["description"] = A[nameof(AccountingStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["url"] = url
                })
        };

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{AccountingSite.Path}{path}", CultureInfo.CurrentUICulture);
}
