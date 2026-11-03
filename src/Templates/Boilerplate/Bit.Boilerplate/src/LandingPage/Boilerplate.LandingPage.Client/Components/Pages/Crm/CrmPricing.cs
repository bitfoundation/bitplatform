namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public enum CrmCurrency
{
    EUR,
    USD,
    GBP
}

public sealed record CrmPlan(string Key, string Name, string Summary, decimal[] Monthly, string[] Features, bool IsPopular = false)
{
    public bool IsEnterprise => Monthly.Length is 0;
}

public static class CrmPricing
{
    public const int YearlySavingPercent = 20;

    public static CrmPlan[] Plans { get; } =
    [
        new("starter", nameof(CrmStrings.PlanStarter), nameof(CrmStrings.PlanStarterSummary), [15m, 17.5m, 12.5m],
            [nameof(CrmStrings.PlanStarterFeature1), nameof(CrmStrings.PlanStarterFeature2), nameof(CrmStrings.PlanStarterFeature3), nameof(CrmStrings.PlanStarterFeature4)]),
        new("team", nameof(CrmStrings.PlanTeam), nameof(CrmStrings.PlanTeamSummary), [30m, 35m, 25m],
            [nameof(CrmStrings.PlanTeamFeature1), nameof(CrmStrings.PlanTeamFeature2), nameof(CrmStrings.PlanTeamFeature3), nameof(CrmStrings.PlanTeamFeature4)], IsPopular: true),
        new("business", nameof(CrmStrings.PlanBusiness), nameof(CrmStrings.PlanBusinessSummary), [50m, 58m, 42m],
            [nameof(CrmStrings.PlanBusinessFeature1), nameof(CrmStrings.PlanBusinessFeature2), nameof(CrmStrings.PlanBusinessFeature3), nameof(CrmStrings.PlanBusinessFeature4)]),
        new("enterprise", nameof(CrmStrings.PlanEnterprise), nameof(CrmStrings.PlanEnterpriseSummary), [],
            [nameof(CrmStrings.PlanEnterpriseFeature1), nameof(CrmStrings.PlanEnterpriseFeature2), nameof(CrmStrings.PlanEnterpriseFeature3), nameof(CrmStrings.PlanEnterpriseFeature4)])
    ];

    private static readonly HashSet<string> euroRegions = ["AT", "BE", "BG", "CY", "DE", "EE", "ES", "FI", "FR", "GR", "HR", "IE", "IT", "LT", "LU", "LV", "MT", "NL", "PT", "SI", "SK"];

    private static readonly HashSet<string> euroLanguages = ["de", "fr", "nl", "it", "es", "pt", "fi", "el", "et", "lv", "lt", "sk", "sl", "mt", "ga", "hr", "bg"];

    public static decimal Monthly(CrmPlan plan, CrmCurrency currency) => plan.Monthly[(int)currency];

    public static decimal Yearly(CrmPlan plan, CrmCurrency currency) => Monthly(plan, currency) * (100 - YearlySavingPercent) / 100;

    public static CrmCurrency FromCulture(CultureInfo culture) => culture.Name switch
    {
        "en-GB" => CrmCurrency.GBP,
        "en-US" or "fa-IR" => CrmCurrency.USD,
        _ => CrmCurrency.EUR
    };

    public static CrmCurrency? FromLanguages(IEnumerable<string> languages)
    {
        foreach (var language in languages)
        {
            var parts = language.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var region = parts.Length > 1 ? parts[^1].ToUpperInvariant() : null;

            if (region is "GB" or "GG" or "JE" or "IM")
                return CrmCurrency.GBP;

            if (region is "US")
                return CrmCurrency.USD;

            if (region is not null && euroRegions.Contains(region))
                return CrmCurrency.EUR;

            if (region is null && euroLanguages.Contains(parts[0].ToLowerInvariant()))
                return CrmCurrency.EUR;
        }

        return null;
    }

    public static string Amount(decimal value, CultureInfo culture) => value.ToString(decimal.Truncate(value) == value ? "N0" : "N2", culture);
}
