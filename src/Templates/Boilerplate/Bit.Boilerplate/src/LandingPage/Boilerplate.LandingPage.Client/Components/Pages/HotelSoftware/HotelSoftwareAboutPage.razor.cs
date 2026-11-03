namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwareAboutPage
{
    private static readonly (string Title, string Body)[] principles =
    [
        (nameof(HotelSoftwareStrings.Principle1Title), nameof(HotelSoftwareStrings.Principle1Body)),
        (nameof(HotelSoftwareStrings.Principle2Title), nameof(HotelSoftwareStrings.Principle2Body)),
        (nameof(HotelSoftwareStrings.Principle3Title), nameof(HotelSoftwareStrings.Principle3Body))
    ];

    private static readonly (string Name, string Role)[] team =
    [
        (nameof(HotelSoftwareStrings.Founder1Name), nameof(HotelSoftwareStrings.Founder1Role)),
        (nameof(HotelSoftwareStrings.Founder2Name), nameof(HotelSoftwareStrings.Founder2Role)),
        (nameof(HotelSoftwareStrings.Team3Name), nameof(HotelSoftwareStrings.Team3Role)),
        (nameof(HotelSoftwareStrings.Team4Name), nameof(HotelSoftwareStrings.Team4Role))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> H = default!;

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
