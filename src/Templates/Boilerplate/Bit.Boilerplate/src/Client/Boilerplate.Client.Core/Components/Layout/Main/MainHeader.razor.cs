namespace Boilerplate.Client.Core.Components.Layout.Main;

public partial class MainHeader : AppComponentBase
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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribePageTitleChanged();
    }
}
