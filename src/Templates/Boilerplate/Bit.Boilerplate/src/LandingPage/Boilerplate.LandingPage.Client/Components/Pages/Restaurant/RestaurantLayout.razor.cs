namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public partial class RestaurantLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<RestaurantStrings> C = default!;

    private LandingLink[] links = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(C[nameof(RestaurantStrings.NavApp)], Link("#app")),
            new(C[nameof(RestaurantStrings.NavWaitlist)], Link("#waitlist")),
            new(C[nameof(RestaurantStrings.NavCafes)], Link("#cafes")),
            new(L[nameof(LandingStrings.About)], LandingCultures.Localize($"{RestaurantSite.Path}/about", CultureInfo.CurrentUICulture))
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(RestaurantSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
