using Bit.Butil;

namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingPricingPage
{
    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private string? countryCode = AccountingPricing.HomeCountry;
    private bool countryChosen;
    private bool withVat;
    private List<BitDropdownItem<string>> countryItems = [];
    private LandingFaq[] faq = [];

    private AccountingCountry Country => AccountingPricing.Find(countryCode);

    private string? CountryCode
    {
        get => countryCode;
        set
        {
            countryCode = value;
            countryChosen = true;
        }
    }

    protected override void OnParametersSet()
    {
        countryItems = [.. AccountingPricing.Countries
            .Select(c => new BitDropdownItem<string> { Value = c.Code, Text = A[$"Country{c.Code}"] })
            .OrderBy(i => i.Text, StringComparer.Create(CurrentCulture, true))];

        faq =
        [
            new(A[nameof(AccountingStrings.BillingFaq1Question)], A[nameof(AccountingStrings.BillingFaq1Answer)]),
            new(A[nameof(AccountingStrings.BillingFaq2Question)], A[nameof(AccountingStrings.BillingFaq2Answer)]),
            new(A[nameof(AccountingStrings.BillingFaq3Question)], A[nameof(AccountingStrings.BillingFaq3Answer)]),
            new(A[nameof(AccountingStrings.BillingFaq4Question)], A[nameof(AccountingStrings.BillingFaq4Answer)]),
            new(A[nameof(AccountingStrings.BillingFaq5Question)], A[nameof(AccountingStrings.BillingFaq5Answer)]),
            new(A[nameof(AccountingStrings.BillingFaq6Question)], A[nameof(AccountingStrings.BillingFaq6Answer)])
        ];

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var detected = AccountingPricing.FromLanguages(await navigator.GetLanguages());

            if (countryChosen is false && detected is not null && detected != countryCode)
            {
                countryCode = detected;
                StateHasChanged();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private string Money(decimal value) => A[$"Money{Country.Currency}", AccountingPricing.Amount(value, CurrentCulture)];

    private string Rate => Country.VatRate.ToString("0.#", CurrentCulture);

    private static string Page(string path) => LandingCultures.Localize($"{AccountingSite.Path}{path}", CultureInfo.CurrentUICulture);
}
