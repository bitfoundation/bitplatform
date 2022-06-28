using System;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Services
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
