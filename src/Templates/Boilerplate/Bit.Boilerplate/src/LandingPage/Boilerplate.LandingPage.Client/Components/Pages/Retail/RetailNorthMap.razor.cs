namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailNorthMap
{
    private static readonly (RetailShop Shop, int X, int Y, int LabelX, int LabelY, string Anchor)[] points =
    [
        (RetailShops.All[0], 168, 172, 18, 30, "start"),
        (RetailShops.All[1], 96, 234, 0, 34, "middle"),
        (RetailShops.All[2], 278, 92, 0, -22, "middle"),
        (RetailShops.All[3], 200, 96, -18, 6, "end"),
        (RetailShops.All[4], 380, 132, 0, 36, "middle"),
        (RetailShops.All[5], 540, 128, 0, -22, "middle")
    ];

    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    [Parameter] public string? Selected { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }
}
