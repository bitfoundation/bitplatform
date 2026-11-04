namespace Boilerplate.LandingPage.Client.Components.Pages.Ecommerce;

public partial class EcommercePlant
{
    private static readonly (int X, int Y, int Angle, double Size, bool Deep)[] leaves =
    [
        (-62, -96, -48, .9, false),
        (14, -140, -4, 1.05, true),
        (70, -84, 46, .95, false),
        (-30, -66, -22, .7, true),
        (36, -52, 30, .62, false)
    ];

    [Parameter] public string PotColor { get; set; } = "var(--lp-clay)";

    [Parameter] public double Scale { get; set; } = 1;

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }
}
