using System;

namespace Bit.BlazorUI.Playground.Web.Services
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
