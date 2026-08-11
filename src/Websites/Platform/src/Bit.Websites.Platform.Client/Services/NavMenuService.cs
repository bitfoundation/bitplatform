namespace Bit.Websites.Platform.Client.Services;

public class NavMenuService
{
    public event Func<Task> OnToggleMenu = default!;

    public void ToggleMenu()
    {
        OnToggleMenu.Invoke();
    }
}
