namespace Bit.BlazorUI.Demo.Client.Shared.Services;

public class NavManuService
{
    public event Func<Task> OnToggleMenu;

    public async Task ToggleMenu()
    {
        await OnToggleMenu.Invoke();
    }
}
