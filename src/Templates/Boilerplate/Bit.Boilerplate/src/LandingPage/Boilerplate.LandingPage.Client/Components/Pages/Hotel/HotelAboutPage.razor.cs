namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelAboutPage
{
    private static readonly (string Name, string Role)[] team =
    [
        (nameof(HotelStrings.Team1Name), nameof(HotelStrings.Team1Role)),
        (nameof(HotelStrings.Team2Name), nameof(HotelStrings.Team2Role)),
        (nameof(HotelStrings.Team3Name), nameof(HotelStrings.Team3Role)),
        (nameof(HotelStrings.Team4Name), nameof(HotelStrings.Team4Role))
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    private BitTimelineItem[] journey = [];

    protected override void OnParametersSet()
    {
        journey =
        [
            new() { PrimaryText = H[nameof(HotelStrings.Journey1Time)], SecondaryText = H[nameof(HotelStrings.Journey1)] },
            new() { PrimaryText = H[nameof(HotelStrings.Journey2Time)], SecondaryText = H[nameof(HotelStrings.Journey2)] },
            new() { PrimaryText = H[nameof(HotelStrings.Journey3Time)], SecondaryText = H[nameof(HotelStrings.Journey3)], IconName = BitIconName.Home }
        ];

        base.OnParametersSet();
    }

    private static string Initials(string name) => string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]));
}
