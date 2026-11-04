namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(R[nameof(CrmStrings.NavProduct)], $"{Page(string.Empty)}#product"),
            new(R[nameof(CrmStrings.NavCustomers)], $"{Page(string.Empty)}#customers"),
            new(R[nameof(CrmStrings.NavPricing)], Page("/pricing")),
            new(R[nameof(CrmStrings.NavSpreadsheets)], Page("/vs-spreadsheets")),
            new(L[nameof(LandingStrings.About)], Page("/about"))
        ];

        suggestions =
        [
            R[nameof(CrmStrings.ChatSuggestion1)],
            R[nameof(CrmStrings.ChatSuggestion2)],
            R[nameof(CrmStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{CrmSite.Path}{path}", CultureInfo.CurrentUICulture);
}
