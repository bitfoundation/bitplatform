namespace Bit.BlazorUI.Demo.Client.Core;

public partial class NotFoundComponent
{
    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
