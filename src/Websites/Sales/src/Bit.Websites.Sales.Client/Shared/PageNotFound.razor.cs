namespace Bit.Websites.Sales.Client.Shared;

public partial class PageNotFound
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private void BackToHome()
    {
        navigationManager.NavigateTo("/");
    }
}
