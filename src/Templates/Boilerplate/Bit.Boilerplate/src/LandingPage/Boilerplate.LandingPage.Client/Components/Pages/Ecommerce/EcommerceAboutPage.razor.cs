namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public partial class EcommerceAboutPage
{
    private static readonly (string Name, string Role)[] team =
    [
        (nameof(EcommerceStrings.Team1Name), nameof(EcommerceStrings.Team1Role)),
        (nameof(EcommerceStrings.Team2Name), nameof(EcommerceStrings.Team2Role)),
        (nameof(EcommerceStrings.Team3Name), nameof(EcommerceStrings.Team3Role)),
        (nameof(EcommerceStrings.Team4Name), nameof(EcommerceStrings.Team4Role))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<EcommerceStrings> E = default!;

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
