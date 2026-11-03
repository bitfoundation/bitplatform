namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailAboutPage
{
    private static readonly (string Name, string Role, string Note)[] team =
    [
        (nameof(RetailStrings.Team1Name), nameof(RetailStrings.Team1Role), nameof(RetailStrings.Team1Note)),
        (nameof(RetailStrings.Team2Name), nameof(RetailStrings.Team2Role), nameof(RetailStrings.Team2Note)),
        (nameof(RetailStrings.Team3Name), nameof(RetailStrings.Team3Role), nameof(RetailStrings.Team3Note)),
        (nameof(RetailStrings.Team4Name), nameof(RetailStrings.Team4Role), nameof(RetailStrings.Team4Note))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
