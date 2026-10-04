namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class Header : AppComponentBase
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }


    [AutoInject] private History history = default!;


    private string? pageTitle;
    private string? pageSubtitle;
    private bool showGoBackButton;
    private ElementReference headerRef;
    private Action unsubscribePageTitleChanged = default!;


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        unsubscribePageTitleChanged = PubSubService.Subscribe(ClientAppMessages.PAGE_DATA_CHANGED, async payload =>
        {
            (pageTitle, pageSubtitle, showGoBackButton) = ((string?, string?, bool))payload!;

            StateHasChanged();
        });
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        await JSRuntime.InvokeVoidAsync("App.trackHeight", headerRef, "--app-header-height");
    }


    private void OpenNavPanel()
    {
        PubSubService.Publish(ClientAppMessages.OPEN_NAV_PANEL);
    }

    private async Task GoBack()
    {
        await history.GoBack();
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribePageTitleChanged?.Invoke();

        await base.DisposeAsync(disposing);
    }
}
