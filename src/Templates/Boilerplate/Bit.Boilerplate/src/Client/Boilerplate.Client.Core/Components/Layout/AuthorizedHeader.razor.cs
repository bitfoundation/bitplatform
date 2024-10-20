namespace Boilerplate.Client.Core.Components.Layout;

<<<<<<<< HEAD:src/Templates/Boilerplate/Bit.Boilerplate/src/Client/Boilerplate.Client.Core/Components/Layout/AuthenticatedHeader.razor.cs
public partial class AuthenticatedHeader : AppComponentBase
========
public partial class AuthorizedHeader : AppComponentBase
>>>>>>>> 196d1ef14557dc3d723cd6ce6f8c73456bfd7ec4:src/Templates/Boilerplate/Bit.Boilerplate/src/Client/Boilerplate.Client.Core/Components/Layout/AuthorizedHeader.razor.cs
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


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribePageTitleChanged?.Invoke();
    }
}
