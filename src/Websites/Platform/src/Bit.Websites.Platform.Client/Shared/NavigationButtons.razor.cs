namespace Bit.Websites.Platform.Client.Shared;

public partial class NavigationButtons
{
    [Parameter] public string? Prev { get; set; }
    [Parameter] public string? PrevUrl { get; set; }

    [Parameter] public string? Next { get; set; }
    [Parameter] public string? NextUrl { get; set; }
}
