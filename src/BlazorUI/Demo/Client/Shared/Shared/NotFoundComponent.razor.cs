namespace Bit.BlazorUI.Demo.Client.Shared;

public partial class NotFoundComponent
{
    [Inject] public NavigationManager NavigationManager { get; set; }

    private void BackToHome()
    {
        NavigationManager.NavigateTo("/");
    }
}
