namespace Boilerplate.LandingPage.Client.Components.Pages.Crm;

public partial class CrmAboutPage
{
    private static readonly (string Value, string Label)[] facts =
    [
        (nameof(CrmStrings.Fact1Value), nameof(CrmStrings.Fact1Label)),
        (nameof(CrmStrings.Fact2Value), nameof(CrmStrings.Fact2Label)),
        (nameof(CrmStrings.Fact3Value), nameof(CrmStrings.Fact3Label)),
        (nameof(CrmStrings.Fact4Value), nameof(CrmStrings.Fact4Label))
    ];

    private static readonly (string Year, string Text)[] milestones =
    [
        (nameof(CrmStrings.Milestone1Year), nameof(CrmStrings.Milestone1)),
        (nameof(CrmStrings.Milestone2Year), nameof(CrmStrings.Milestone2)),
        (nameof(CrmStrings.Milestone3Year), nameof(CrmStrings.Milestone3)),
        (nameof(CrmStrings.Milestone4Year), nameof(CrmStrings.Milestone4))
    ];

    private static readonly (string Name, string Role, string Place)[] team =
    [
        (nameof(CrmStrings.Team1Name), nameof(CrmStrings.Team1Role), nameof(CrmStrings.Team1Place)),
        (nameof(CrmStrings.Team2Name), nameof(CrmStrings.Team2Role), nameof(CrmStrings.Team2Place)),
        (nameof(CrmStrings.Team3Name), nameof(CrmStrings.Team3Role), nameof(CrmStrings.Team3Place)),
        (nameof(CrmStrings.Team4Name), nameof(CrmStrings.Team4Role), nameof(CrmStrings.Team4Place))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<CrmStrings> R = default!;

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
