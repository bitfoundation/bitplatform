namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarOptionDemo
{
    private int countClick;
    private bool reselectable = true;
    private BitNavBarOption? selectedOption;
    private BitNavBarOption? eventsClickedOption;
    private BitNavBarOption? twoWaySelectedOption;
    private BitNavBarOption? advanceSelectedOption;

    private BitNavBarOption optionHome = default!;
    private BitNavBarOption optionProducts = default!;
    private BitNavBarOption optionAcademy = default!;
    private BitNavBarOption optionProfile = default!;
}
