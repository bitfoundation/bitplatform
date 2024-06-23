namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class NotFoundComponent
{
    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
