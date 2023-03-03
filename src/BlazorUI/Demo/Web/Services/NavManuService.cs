using System;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Demo.Web.Services;

public class NavManuService
{
    public event Func<Task> OnToggleMenu;

    public async Task ToggleMenu()
    {
        await OnToggleMenu.Invoke();
    }
}
