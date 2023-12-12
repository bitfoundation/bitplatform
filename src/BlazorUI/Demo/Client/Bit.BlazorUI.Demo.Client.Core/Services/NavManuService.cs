namespace Bit.BlazorUI.Demo.Client.Core.Services;

public class NavManuService
{
    public event Func<Task> OnToggleMenu;

    public async Task ToggleMenu()
    {
        await OnToggleMenu.Invoke();
    }
}
