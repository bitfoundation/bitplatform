namespace Bit.BlazorUI.Demo.Client.Core.Components;
public partial class PageOutlet
{
    private Action _unsubscribe = default!;
    private string title = "Bit BlazorUI";

    [Parameter]
    public string Title
    {
        get => title;
        set
        {
            title = value;
            _pubSubService.Pub("PageTitleChanged", $"{title} - Bit BlazorUI");
        }
    }

    [Parameter] public string? Description { get; set; }
    [Parameter] public string? Url { get; set; }
    [Parameter] public string ImageUrl { get; set; } = "https://components.bitplatform.dev/images/og-image.svg";

    [Inject] private IPubSubService _pubSubService { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        _unsubscribe = _pubSubService.Sub("PageTitleChanged", UpdatePageTitle);
        return base.OnInitAsync();
    }

    private void UpdatePageTitle(object? payload)
    {
        title = (string)payload;
        StateHasChanged();
    }
}
