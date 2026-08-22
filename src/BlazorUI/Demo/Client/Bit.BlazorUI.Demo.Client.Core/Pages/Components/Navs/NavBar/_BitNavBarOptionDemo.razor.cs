namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarOptionDemo
{
    private bool dynamicAutoReorder = true;
    private int dynamicOptionsCount = 3;
    private BitNavBarOption? dynamicSelectedOption;
    // The options are children of the navbar rather than a collection it is handed, so a dynamic set of
    // them is rendered from a collection of the plain data each one is built from.
    private readonly List<DynamicOption> dynamicOptions =
    [
        new("Home", BitIconName.Home),
        new("Products", BitIconName.ProductVariant),
        new("Profile", BitIconName.Contact),
    ];

    private void AddDynamicOption()
    {
        dynamicOptionsCount++;
        dynamicOptions.Add(new($"Item {dynamicOptionsCount}", BitIconName.Tag));
    }

    private void RemoveDynamicOption()
    {
        if (dynamicOptions.Count == 0) return;

        dynamicOptions.RemoveAt(dynamicOptions.Count - 1);
    }

    private void ReverseDynamicOptions() => dynamicOptions.Reverse();

    private record DynamicOption(string Text, string IconName);

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
    private BitNavBarOption? bindingSelectedOption;
    private BitNavBarOption bindingOptionProducts = default!;

    private BitNavBarOption? hideTextSelectedOption;
    private BitNavBarOption hideTextOptionHome = default!;

    private BitNavBarOption? accentSelectedOption;
    private BitNavBarOption accentOptionHome = default!;

    private BitNavBarOption? tabStopSelectedOption;
    private BitNavBarOption tabStopOptionProducts = default!;

    private BitNavBarOption? wrapSelectedOption;
    private BitNavBarOption wrapOptionProducts = default!;

    private BitNavBarOption? selectedIconSelectedOption;
    private BitNavBarOption selectedIconOptionHome = default!;

    private BitNavBarOption? advancedSelectedOption;
    private BitNavBarOption advancedOptionProducts = default!;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            bindingSelectedOption ??= bindingOptionProducts;
            hideTextSelectedOption ??= hideTextOptionHome;
            accentSelectedOption ??= accentOptionHome;
            tabStopSelectedOption ??= tabStopOptionProducts;
            wrapSelectedOption ??= wrapOptionProducts;
            selectedIconSelectedOption ??= selectedIconOptionHome;
            advancedSelectedOption ??= advancedOptionProducts;

            StateHasChanged();
        }

        base.OnAfterRender(firstRender);
    }
}
