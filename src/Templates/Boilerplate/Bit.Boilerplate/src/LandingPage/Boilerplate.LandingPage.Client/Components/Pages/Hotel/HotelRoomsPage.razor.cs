namespace Boilerplate.LandingPage.Client.Components.Pages.Hotel;

public partial class HotelRoomsPage
{
    private static readonly (HotelSceneKind Scene, string Caption)[] gallery =
    [
        (HotelSceneKind.Dining, nameof(HotelStrings.GalleryDining)),
        (HotelSceneKind.Sauna, nameof(HotelStrings.GallerySauna)),
        (HotelSceneKind.Beach, nameof(HotelStrings.GalleryBeach))
    ];

    [AutoInject] private IStringLocalizer<HotelStrings> H = default!;

    private string Money(decimal amount) => H[nameof(HotelStrings.Money), amount.ToString("N0", CurrentCulture)];
}
