namespace Boilerplate.Client.Core.Components.Layout;

public partial class AuthorizedHeader : AppComponentBase
{
    private string? pageTitle;
    private string? pageSubtitle;
    private Action unsubscribePageTitleChanged = default!;


    protected override async Task OnInitAsync()
    {
        unsubscribePageTitleChanged = PubSubService.Subscribe(PubSubMessages.PAGE_TITLE_CHANGED, async payload =>
        {
            (pageTitle, pageSubtitle) = (ValueTuple<string?, string?>)payload!;

            StateHasChanged();
        });
    }


    private void OpenNavPanel()
    {
        PubSubService.Publish(PubSubMessages.OPEN_NAV_PANEL);
    }


    private int clickCount = 0;
    private async Task HandleTitleClick()
    {
        if (++clickCount == 7)
        {
            clickCount = 0;
            PubSubService.Publish(PubSubMessages.SHOW_DIAGNOSTIC_MODAL);
        }
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribePageTitleChanged?.Invoke();
    }
}
