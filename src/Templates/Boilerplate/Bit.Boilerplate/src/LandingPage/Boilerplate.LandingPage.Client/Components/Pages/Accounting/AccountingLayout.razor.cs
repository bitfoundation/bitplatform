namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(A[nameof(AccountingStrings.NavMonth)], $"{Page(string.Empty)}#month"),
            new(A[nameof(AccountingStrings.NavSavings)], $"{Page(string.Empty)}#savings"),
            new(A[nameof(AccountingStrings.NavSecurity)], $"{Page(string.Empty)}#security"),
            new(A[nameof(AccountingStrings.NavPricing)], Page("/pricing")),
            new(L[nameof(LandingStrings.About)], Page("/about"))
        ];

        suggestions =
        [
            A[nameof(AccountingStrings.ChatSuggestion1)],
            A[nameof(AccountingStrings.ChatSuggestion2)],
            A[nameof(AccountingStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{AccountingSite.Path}{path}", CultureInfo.CurrentUICulture);
}
