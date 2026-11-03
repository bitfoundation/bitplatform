namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelScene
{
    private static readonly (int X, int Y, bool Flip)[] chairs = [(110, 236, false), (320, 242, true)];

    [Parameter] public HotelSceneKind Kind { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }

    private string? Role => Label is null ? null : "img";

    private string? Hidden => Label is null ? "true" : null;
}

public enum HotelSceneKind
{
    Panorama,
    DuneRoom,
    SeaRoom,
    Loft,
    Suite,
    Dining,
    Sauna,
    Beach
}
