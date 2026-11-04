namespace Boilerplate.LandingPage.Client.Components.Pages.Restaurant;

public partial class RestaurantMachine
{
    private static readonly int[] groups = [130, 230];

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Class { get; set; }
}
