namespace Bit.BlazorUI.Demo.Client.Shared;

public partial class NotFoundComponent
{
    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
