namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public enum AccountingCurrency
{
    EUR,
    GBP,
    CHF,
    PLN,
    SEK
}

public sealed record AccountingCountry(string Code, AccountingCurrency Currency, decimal VatRate);

public sealed record AccountingPlan(string Key, string Name, string Summary, decimal[] Monthly, string[] Features, bool IsPopular = false);

public static class AccountingPricing
{
    public const string HomeCountry = "AT";

    public static AccountingCountry[] Countries { get; } =
    [
        new("AT", AccountingCurrency.EUR, 20m),
        new("BE", AccountingCurrency.EUR, 21m),
        new("FR", AccountingCurrency.EUR, 20m),
        new("DE", AccountingCurrency.EUR, 19m),
        new("IE", AccountingCurrency.EUR, 23m),
        new("IT", AccountingCurrency.EUR, 22m),
        new("NL", AccountingCurrency.EUR, 21m),
        new("PL", AccountingCurrency.PLN, 23m),
        new("PT", AccountingCurrency.EUR, 23m),
        new("ES", AccountingCurrency.EUR, 21m),
        new("SE", AccountingCurrency.SEK, 25m),
        new("CH", AccountingCurrency.CHF, 8.1m),
        new("GB", AccountingCurrency.GBP, 20m)
    ];

    // Each plan's monthly prices follow AccountingCurrency's order: EUR, GBP, CHF, PLN, SEK
    public static AccountingPlan[] Plans { get; } =
    [
        new("free", nameof(AccountingStrings.PlanFree), nameof(AccountingStrings.PlanFreeSummary), [0m, 0m, 0m, 0m, 0m],
            [nameof(AccountingStrings.PlanFreeFeature1), nameof(AccountingStrings.PlanFreeFeature2), nameof(AccountingStrings.PlanFreeFeature3), nameof(AccountingStrings.PlanFreeFeature4)]),
        new("solo", nameof(AccountingStrings.PlanSolo), nameof(AccountingStrings.PlanSoloSummary), [9m, 8m, 9m, 39m, 99m],
            [nameof(AccountingStrings.PlanSoloFeature1), nameof(AccountingStrings.PlanSoloFeature2), nameof(AccountingStrings.PlanSoloFeature3), nameof(AccountingStrings.PlanSoloFeature4), nameof(AccountingStrings.PlanSoloFeature5)], IsPopular: true),
        new("studio", nameof(AccountingStrings.PlanStudio), nameof(AccountingStrings.PlanStudioSummary), [19m, 16m, 19m, 85m, 219m],
            [nameof(AccountingStrings.PlanStudioFeature1), nameof(AccountingStrings.PlanStudioFeature2), nameof(AccountingStrings.PlanStudioFeature3), nameof(AccountingStrings.PlanStudioFeature4), nameof(AccountingStrings.PlanStudioFeature5)]),
        new("company", nameof(AccountingStrings.PlanCompany), nameof(AccountingStrings.PlanCompanySummary), [39m, 34m, 39m, 169m, 449m],
            [nameof(AccountingStrings.PlanCompanyFeature1), nameof(AccountingStrings.PlanCompanyFeature2), nameof(AccountingStrings.PlanCompanyFeature3), nameof(AccountingStrings.PlanCompanyFeature4), nameof(AccountingStrings.PlanCompanyFeature5)])
    ];

    public static AccountingCountry Find(string? code) => Countries.FirstOrDefault(c => c.Code == code) ?? Countries[0];

    public static string? FromLanguages(IEnumerable<string> languages)
    {
        foreach (var language in languages)
        {
            var parts = language.Split('-', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1 && Countries.FirstOrDefault(c => c.Code == parts[^1].ToUpperInvariant()) is { } byRegion)
                return byRegion.Code;

            var byLanguage = parts[0].ToLowerInvariant() switch
            {
                "de" => "DE",
                "fr" => "FR",
                "nl" => "NL",
                "it" => "IT",
                "es" => "ES",
                "pt" => "PT",
                "pl" => "PL",
                "sv" => "SE",
                _ => null
            };

            if (byLanguage is not null)
                return byLanguage;
        }

        return null;
    }

    public static decimal Price(AccountingPlan plan, AccountingCountry country, bool withVat)
    {
        var net = plan.Monthly[(int)country.Currency];

        return withVat ? Math.Round(net * (100 + country.VatRate) / 100, 2) : net;
    }

    public static string Amount(decimal value, CultureInfo culture) => value.ToString(decimal.Truncate(value) == value ? "N0" : "N2", culture);
}
