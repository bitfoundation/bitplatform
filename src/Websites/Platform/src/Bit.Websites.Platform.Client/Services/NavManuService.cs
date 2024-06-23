namespace Bit.Websites.Platform.Client.Services;

public class NavManuService
{
    public event Func<Task> OnToggleMenu = default!;

    public void ToggleMenu()
    {
        OnToggleMenu.Invoke();
    }
}
