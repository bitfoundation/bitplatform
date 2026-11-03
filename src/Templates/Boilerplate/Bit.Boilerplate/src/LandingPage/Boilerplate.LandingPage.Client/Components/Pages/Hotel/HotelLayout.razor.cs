namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(H[nameof(HotelStrings.NavRooms)], LandingCultures.Localize($"{HotelSite.Path}/rooms", CultureInfo.CurrentUICulture)),
            new(H[nameof(HotelStrings.NavDirect)], Link("#direct")),
            new(H[nameof(HotelStrings.NavApp)], Link("#app")),
            new(H[nameof(HotelStrings.NavReviews)], Link("#reviews")),
            new(L[nameof(LandingStrings.About)], LandingCultures.Localize($"{HotelSite.Path}/about", CultureInfo.CurrentUICulture))
        ];

        suggestions =
        [
            H[nameof(HotelStrings.ChatSuggestion1)],
            H[nameof(HotelStrings.ChatSuggestion2)],
            H[nameof(HotelStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Link(string hash) => $"{LandingCultures.Localize(HotelSite.Path, CultureInfo.CurrentUICulture)}{hash}";
}
