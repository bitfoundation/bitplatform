namespace Bit.Websites.Sales.Client.Shared;

public partial class PageNotFound
{
    [AutoInject] private NavigationManager _navigationManager = default!;

    private void BackToHome()
    {
        _navigationManager.NavigateTo("/");
    }
}
