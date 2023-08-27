namespace Bit.Websites.Platform.Web.Services;

public class NavManuService
{
    public event Func<Task> OnToggleMenu;

    public void ToggleMenu()
    {
        OnToggleMenu.Invoke();
    }
}
