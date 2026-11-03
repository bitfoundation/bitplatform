namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwareLayout
{
    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private LandingLink[] links = [];
    private string[] suggestions = [];

    protected override void OnParametersSet()
    {
        links =
        [
            new(H[nameof(HotelSoftwareStrings.NavDesk)], $"{Page(string.Empty)}#desk"),
            new(H[nameof(HotelSoftwareStrings.NavAudiences)], $"{Page(string.Empty)}#audiences"),
            new(H[nameof(HotelSoftwareStrings.NavPricing)], Page("/pricing")),
            new(H[nameof(HotelSoftwareStrings.NavPress)], Page("/press")),
            new(L[nameof(LandingStrings.About)], Page("/about"))
        ];

        suggestions =
        [
            H[nameof(HotelSoftwareStrings.ChatSuggestion1)],
            H[nameof(HotelSoftwareStrings.ChatSuggestion2)],
            H[nameof(HotelSoftwareStrings.ChatSuggestion3)]
        ];

        base.OnParametersSet();
    }

    private static string Page(string path) => LandingCultures.Localize($"{HotelSoftwareSite.Path}{path}", CultureInfo.CurrentUICulture);
}
