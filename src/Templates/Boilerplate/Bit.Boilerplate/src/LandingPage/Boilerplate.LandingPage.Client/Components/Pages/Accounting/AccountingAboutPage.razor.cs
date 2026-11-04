namespace Boilerplate.LandingPage.Client.Components.Pages.Accounting;

public partial class AccountingAboutPage
{
    private static readonly (string Title, string Body)[] promises =
    [
        (nameof(AccountingStrings.Promise1Title), nameof(AccountingStrings.Promise1Body)),
        (nameof(AccountingStrings.Promise2Title), nameof(AccountingStrings.Promise2Body)),
        (nameof(AccountingStrings.Promise3Title), nameof(AccountingStrings.Promise3Body))
    ];

    private static readonly (string Name, string Role)[] team =
    [
        (nameof(AccountingStrings.Team1Name), nameof(AccountingStrings.Team1Role)),
        (nameof(AccountingStrings.Team2Name), nameof(AccountingStrings.Team2Role)),
        (nameof(AccountingStrings.Team3Name), nameof(AccountingStrings.Team3Role)),
        (nameof(AccountingStrings.Team4Name), nameof(AccountingStrings.Team4Role))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<AccountingStrings> A = default!;

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
