namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingSavings
{
    public const double TimeSaved = .7;

    public const double SoloYearly = 9 * 12;

    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private double hours = 6;
    private double rate = 60;

    private double HoursBack => Math.Round(hours * TimeSaved * 12);

    private double MoneyBack => Math.Max(0, HoursBack * rate - SoloYearly);

    private string Money(double value) => A[nameof(AccountingStrings.MoneyEUR), value.ToString("N0", CurrentCulture)];

    private string HoursText(double value) => A[nameof(AccountingStrings.SavingsHoursValue), value.ToString("N0", CurrentCulture)];

    private string RateText(double value) => A[nameof(AccountingStrings.SavingsRateValue), value.ToString("N0", CurrentCulture)];
}
