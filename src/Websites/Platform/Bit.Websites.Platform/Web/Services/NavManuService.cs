using System;

namespace Bit.Websites.Platform.Web.Services;

public class NavManuService
{
    public event Action OnToggleMenu;

    public void ToggleMenu()
    {
        OnToggleMenu.Invoke();
    }
}
