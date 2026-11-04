namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public partial class RestaurantAboutPage
{
    private static readonly (string Name, string Role, string Note)[] team =
    [
        (nameof(RestaurantStrings.Team1Name), nameof(RestaurantStrings.Team1Role), nameof(RestaurantStrings.Team1Note)),
        (nameof(RestaurantStrings.Team2Name), nameof(RestaurantStrings.Team2Role), nameof(RestaurantStrings.Team2Note)),
        (nameof(RestaurantStrings.Team3Name), nameof(RestaurantStrings.Team3Role), nameof(RestaurantStrings.Team3Note)),
        (nameof(RestaurantStrings.Team4Name), nameof(RestaurantStrings.Team4Role), nameof(RestaurantStrings.Team4Note))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<RestaurantStrings> C = default!;

    private string Range(TimeOnly from, TimeOnly to) => C[nameof(RestaurantStrings.TimeRange), from.ToString("HH:mm", CurrentCulture), to.ToString("HH:mm", CurrentCulture)];

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
