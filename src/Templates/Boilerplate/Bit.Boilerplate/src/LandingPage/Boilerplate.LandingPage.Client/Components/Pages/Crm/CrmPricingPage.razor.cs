using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmPricingPage
{
    private const string MonthlyBilling = "monthly";
    private const string YearlyBilling = "yearly";

    private const string Included = "+";
    private const string Excluded = "-";

    private static readonly (string Day, string Title, string Body)[] trialSteps =
    [
        (nameof(CrmStrings.Trial1Day), nameof(CrmStrings.Trial1Title), nameof(CrmStrings.Trial1Body)),
        (nameof(CrmStrings.Trial2Day), nameof(CrmStrings.Trial2Title), nameof(CrmStrings.Trial2Body)),
        (nameof(CrmStrings.Trial3Day), nameof(CrmStrings.Trial3Title), nameof(CrmStrings.Trial3Body)),
        (nameof(CrmStrings.Trial4Day), nameof(CrmStrings.Trial4Title), nameof(CrmStrings.Trial4Body)),
        (nameof(CrmStrings.Trial5Day), nameof(CrmStrings.Trial5Title), nameof(CrmStrings.Trial5Body))
    ];

    private static readonly (string Group, (string Feature, string[] Values)[] Rows)[] matrix =
    [
        (nameof(CrmStrings.MatrixGroupPipeline),
        [
            (nameof(CrmStrings.MatrixPipelines), ["1", "5", "20", nameof(CrmStrings.MatrixUnlimited)]),
            (nameof(CrmStrings.MatrixContacts), [nameof(CrmStrings.MatrixUnlimited), nameof(CrmStrings.MatrixUnlimited), nameof(CrmStrings.MatrixUnlimited), nameof(CrmStrings.MatrixUnlimited)]),
            (nameof(CrmStrings.MatrixCustomFields), ["10", "50", nameof(CrmStrings.MatrixUnlimited), nameof(CrmStrings.MatrixUnlimited)]),
            (nameof(CrmStrings.MatrixProducts), [Excluded, Included, Included, Included])
        ]),
        (nameof(CrmStrings.MatrixGroupEmail),
        [
            (nameof(CrmStrings.MatrixSync), [Included, Included, Included, Included]),
            (nameof(CrmStrings.MatrixTemplates), [Excluded, Included, Included, Included]),
            (nameof(CrmStrings.MatrixScheduling), [Excluded, Included, Included, Included])
        ]),
        (nameof(CrmStrings.MatrixGroupForecast),
        [
            (nameof(CrmStrings.MatrixWeighted), [Included, Included, Included, Included]),
            (nameof(CrmStrings.MatrixAdjust), [Excluded, Included, Included, Included]),
            (nameof(CrmStrings.MatrixReports), ["3", "25", nameof(CrmStrings.MatrixUnlimited), nameof(CrmStrings.MatrixUnlimited)]),
            (nameof(CrmStrings.MatrixScheduled), [Excluded, Excluded, Included, Included])
        ]),
        (nameof(CrmStrings.MatrixGroupAutomation),
        [
            (nameof(CrmStrings.MatrixReminders), [Included, Included, Included, Included]),
            (nameof(CrmStrings.MatrixWorkflows), [Excluded, "20", "100", nameof(CrmStrings.MatrixUnlimited)]),
            (nameof(CrmStrings.MatrixApi), [Excluded, Included, Included, Included])
        ]),
        (nameof(CrmStrings.MatrixGroupAdmin),
        [
            (nameof(CrmStrings.MatrixTwoFactor), [Included, Included, Included, Included]),
            (nameof(CrmStrings.MatrixTeams), [Excluded, Excluded, Included, Included]),
            (nameof(CrmStrings.MatrixSso), [Excluded, Excluded, Included, Included]),
            (nameof(CrmStrings.MatrixSandbox), [Excluded, Excluded, Included, Included]),
            (nameof(CrmStrings.MatrixAudit), [Excluded, Excluded, Excluded, Included])
        ]),
        (nameof(CrmStrings.MatrixGroupSupport),
        [
            (nameof(CrmStrings.MatrixResponse), [nameof(CrmStrings.MatrixResponseStarter), nameof(CrmStrings.MatrixResponseTeam), nameof(CrmStrings.MatrixResponseBusiness), nameof(CrmStrings.MatrixResponseEnterprise)]),
            (nameof(CrmStrings.MatrixOnboarding), [nameof(CrmStrings.MatrixOnboardingSelf), nameof(CrmStrings.MatrixOnboardingSelf), nameof(CrmStrings.MatrixOnboardingSessions), nameof(CrmStrings.MatrixOnboardingManager)]),
            (nameof(CrmStrings.MatrixSla), [Excluded, Excluded, Excluded, "99.9%"])
        ])
    ];

    private static readonly (string Icon, string Text)[] enterpriseItems =
    [
        (BitIconName.Permissions, nameof(CrmStrings.Enterprise1)),
        (BitIconName.History, nameof(CrmStrings.Enterprise2)),
        (BitIconName.DocumentApproval, nameof(CrmStrings.Enterprise3)),
        (BitIconName.SpeedHigh, nameof(CrmStrings.Enterprise4)),
        (BitIconName.ContactCard, nameof(CrmStrings.Enterprise5))
    ];

    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private string? billing = YearlyBilling;
    private string? currencyKey;
    private string? matrixPlan = "team";
    private bool currencyChosen;
    private BitChoiceGroupItem<string>[] billingItems = [];
    private BitChoiceGroupItem<string>[] currencyItems = [];
    private BitChoiceGroupItem<string>[] planItems = [];
    private LandingFaq[] faq = [];

    private bool IsYearly => billing == YearlyBilling;

    private CrmCurrency Currency => Enum.TryParse<CrmCurrency>(currencyKey, out var parsed) ? parsed : CrmCurrency.USD;

    private int MatrixColumn => Math.Max(0, Array.FindIndex(CrmPricing.Plans, p => p.Key == matrixPlan));

    protected override void OnParametersSet()
    {
        currencyKey ??= CrmPricing.FromCulture(CurrentCulture).ToString();

        billingItems =
        [
            new() { Value = MonthlyBilling, Text = R[nameof(CrmStrings.BillingMonthly)] },
            new() { Value = YearlyBilling, Text = R[nameof(CrmStrings.BillingYearly)] }
        ];

        currencyItems = [.. Enum.GetValues<CrmCurrency>().Select(c => new BitChoiceGroupItem<string> { Value = c.ToString(), Text = c.ToString(), AriaLabel = R[$"Currency{c}"] })];

        planItems = [.. CrmPricing.Plans.Select(p => new BitChoiceGroupItem<string> { Value = p.Key, Text = R[p.Name] })];

        faq =
        [
            new(R[nameof(CrmStrings.BillingFaq1Question)], R[nameof(CrmStrings.BillingFaq1Answer)]),
            new(R[nameof(CrmStrings.BillingFaq2Question)], R[nameof(CrmStrings.BillingFaq2Answer)]),
            new(R[nameof(CrmStrings.BillingFaq3Question)], R[nameof(CrmStrings.BillingFaq3Answer)]),
            new(R[nameof(CrmStrings.BillingFaq4Question)], R[nameof(CrmStrings.BillingFaq4Answer)]),
            new(R[nameof(CrmStrings.BillingFaq5Question)], R[nameof(CrmStrings.BillingFaq5Answer)]),
            new(R[nameof(CrmStrings.BillingFaq6Question)], R[nameof(CrmStrings.BillingFaq6Answer)])
        ];

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var detected = CrmPricing.FromLanguages(await navigator.GetLanguages());

            if (currencyChosen is false && detected is { } currency && currency.ToString() != currencyKey)
            {
                currencyKey = currency.ToString();
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string? CurrencyKey
    {
        get => currencyKey;
        set
        {
            currencyKey = value;
            currencyChosen = true;
        }
    }

    private string Money(decimal value) => R[$"Price{Currency}", CrmPricing.Amount(value, CurrentCulture)];

    private (string Prefix, string Whole, string? Cents, string Suffix) PriceParts(decimal value)
    {
        var pattern = R[$"Price{Currency}"].Value;
        var slot = pattern.IndexOf("{0}", StringComparison.Ordinal);
        var amount = CrmPricing.Amount(value, CurrentCulture);
        var separator = amount.IndexOf(CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);

        return (pattern[..slot], separator < 0 ? amount : amount[..separator], separator < 0 ? null : amount[separator..], pattern[(slot + 3)..]);
    }

    private string YearTotal(CrmPlan plan) => Money(CrmPricing.Yearly(plan, Currency) * 12);

    private string YearSaving(CrmPlan plan) => Money((CrmPricing.Monthly(plan, Currency) - CrmPricing.Yearly(plan, Currency)) * 12);

    private string Cell(string value) => value switch
    {
        Included or Excluded => string.Empty,
        _ when value.All(c => char.IsDigit(c) || c is '.' or '%') => value,
        _ => R[value]
    };

    private static string Page(string path) => LandingCultures.Localize($"{CrmSite.Path}{path}", CultureInfo.CurrentUICulture);
}
