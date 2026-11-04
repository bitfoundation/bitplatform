namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelIslandMap
{
    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    [Parameter] public string? Class { get; set; }
}
