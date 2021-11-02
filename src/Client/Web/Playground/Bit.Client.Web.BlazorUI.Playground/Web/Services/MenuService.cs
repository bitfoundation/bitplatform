using System;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Services
{
    public class MenuService
    {
        public event Action OnToggleMenu;

        public void ToggleMenu()
        {
            OnToggleMenu.Invoke();
        }
    }
}
