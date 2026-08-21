namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarOptionDemo
{
    private int countClick;
    private bool reselectable = true;
    private BitNavBarOption? eventsClickedOption;
    private BitNavBarOption? eventsSelectedOption;
    private BitNavBarOption? twoWaySelectedOption;

    private BitNavBarOption optionHome = default!;
    private BitNavBarOption optionProducts = default!;
    private BitNavBarOption optionAcademy = default!;
    private BitNavBarOption optionProfile = default!;

    // The options API has no DefaultSelectedItem: an option only exists once it has rendered, which is
    // after the navbar has read its parameters. The sections that need to open on a selection therefore
    // bind the selection and hand it the option they captured a reference to, once that reference exists.
    private BitNavBarOption? hideTextSelectedOption;
    private BitNavBarOption hideTextOptionHome = default!;

    private BitNavBarOption? accentSelectedOption;
    private BitNavBarOption accentOptionHome = default!;

    private BitNavBarOption? tabStopSelectedOption;
    private BitNavBarOption tabStopOptionProducts = default!;

    private BitNavBarOption? advancedSelectedOption;
    private BitNavBarOption advancedOptionProducts = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hideTextSelectedOption ??= hideTextOptionHome;
            accentSelectedOption ??= accentOptionHome;
            tabStopSelectedOption ??= tabStopOptionProducts;
            advancedSelectedOption ??= advancedOptionProducts;

            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
