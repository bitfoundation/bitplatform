using System.Text.Json.Nodes;
using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public partial class EcommerceHomePage
{
    private static readonly (string Icon, string Title, string Body)[] features =
    [
        (BitIconName.Sunny, nameof(EcommerceStrings.Feature1Title), nameof(EcommerceStrings.Feature1Body)),
        (BitIconName.Clock, nameof(EcommerceStrings.Feature2Title), nameof(EcommerceStrings.Feature2Body)),
        (BitIconName.DeliveryTruck, nameof(EcommerceStrings.Feature3Title), nameof(EcommerceStrings.Feature3Body)),
        (BitIconName.Shield, nameof(EcommerceStrings.Feature4Title), nameof(EcommerceStrings.Feature4Body)),
        (BitIconName.CheckMark, nameof(EcommerceStrings.Feature5Title), nameof(EcommerceStrings.Feature5Body)),
        (BitIconName.Health, nameof(EcommerceStrings.Feature6Title), nameof(EcommerceStrings.Feature6Body))
    ];

    private static readonly (string Task, string When)[] careTasks =
    [
        (nameof(EcommerceStrings.CareItem1), nameof(EcommerceStrings.CareItem1When)),
        (nameof(EcommerceStrings.CareItem2), nameof(EcommerceStrings.CareItem2When)),
        (nameof(EcommerceStrings.CareItem3), nameof(EcommerceStrings.CareItem3When))
    ];

    private static readonly (string Title, string Body)[] steps =
    [
        (nameof(EcommerceStrings.Step1Title), nameof(EcommerceStrings.Step1Body)),
        (nameof(EcommerceStrings.Step2Title), nameof(EcommerceStrings.Step2Body)),
        (nameof(EcommerceStrings.Step3Title), nameof(EcommerceStrings.Step3Body))
    ];

    private static readonly (string Quote, string Name, string Place)[] reviews =
    [
        (nameof(EcommerceStrings.Review1Quote), nameof(EcommerceStrings.Review1Name), nameof(EcommerceStrings.Review1Place)),
        (nameof(EcommerceStrings.Review2Quote), nameof(EcommerceStrings.Review2Name), nameof(EcommerceStrings.Review2Place)),
        (nameof(EcommerceStrings.Review3Quote), nameof(EcommerceStrings.Review3Name), nameof(EcommerceStrings.Review3Place))
    ];

    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private IStringLocalizer<EcommerceStrings> E = default!;

    private LandingFaq[] faq = [];
    private JsonObject? structuredData;
    private bool couponCopied;

    protected override void OnParametersSet()
    {
        faq =
        [
            new(E[nameof(EcommerceStrings.Faq1Question)], E[nameof(EcommerceStrings.Faq1Answer)]),
            new(E[nameof(EcommerceStrings.Faq2Question)], E[nameof(EcommerceStrings.Faq2Answer)]),
            new(E[nameof(EcommerceStrings.Faq3Question)], E[nameof(EcommerceStrings.Faq3Answer)]),
            new(E[nameof(EcommerceStrings.Faq4Question)], E[nameof(EcommerceStrings.Faq4Answer)]),
            new(E[nameof(EcommerceStrings.Faq5Question)], E[nameof(EcommerceStrings.Faq5Answer)])
        ];

        var site = EcommerceSite.Info;
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
                    ["description"] = E[nameof(EcommerceStrings.HomeDescription)].Value,
                    ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "0", ["priceCurrency"] = "EUR" },
                    ["installUrl"] = site.Find(LandingPlatform.iOS)?.Url
                })
        };

        base.OnParametersSet();
    }

    private async Task CopyCoupon()
    {
        await clipboard.WriteText("FERN15");
        couponCopied = true;
    }
}
