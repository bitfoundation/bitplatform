namespace Boilerplate.LandingPage.Client.Components.Pages.Retail;

public partial class RetailOpenState
{
    [AutoInject] private IStringLocalizer<RetailStrings> R = default!;

    [Parameter, EditorRequired] public RetailShop Shop { get; set; } = default!;

    [Parameter] public DateTime? Now { get; set; }

    [Parameter] public string? Class { get; set; }

    private string Text(RetailShopStatus status)
    {
        var time = status.Time.ToString("HH:mm", CurrentCulture);

        return status.State switch
        {
            RetailShopState.Open => R[nameof(RetailStrings.OpenUntil), time],
            RetailShopState.ClosingSoon => R[nameof(RetailStrings.ClosesSoon), time],
            RetailShopState.OpensToday => R[nameof(RetailStrings.OpensAt), time],
            _ => R[nameof(RetailStrings.OpensTomorrow), time]
        };
    }
}
