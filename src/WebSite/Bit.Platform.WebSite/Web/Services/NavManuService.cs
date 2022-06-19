using System;

namespace Bit.Platform.WebSite.Web.Services
{
    public class NavManuService
    {
        public event Action OnToggleMenu;

        public void ToggleMenu()
        {
            OnToggleMenu.Invoke();
        }
    }
}
