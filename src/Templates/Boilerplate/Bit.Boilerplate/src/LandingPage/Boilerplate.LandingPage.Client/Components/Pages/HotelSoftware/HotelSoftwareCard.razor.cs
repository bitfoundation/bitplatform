namespace Boilerplate.LandingPage.Client.Components.Pages.HotelSoftware;

public partial class HotelSoftwareCard
{
    private static readonly (string Number, string State)[] tiles =
    [
        ("101", "is-occupied"),
        ("102", "is-dirty"),
        ("103", "is-arriving"),
        ("104", "is-occupied"),
        ("201", "is-clean"),
        ("202", "is-occupied"),
        ("203", "is-arriving"),
        ("204", "is-clean")
    ];

    [AutoInject] private IStringLocalizer<LandingStrings> L = default!;
    [AutoInject] private IStringLocalizer<HotelSoftwareStrings> R = default!;
}
