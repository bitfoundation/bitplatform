namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarOptionDemo
{
    private static readonly BitNavBarParams[] navBarParams =
    [
        new()
        {
            Mode = BitNavMode.Manual,
            Filled = true,
            Color = BitColor.Info,
            Indicator = BitNavBarIndicator.Pill,
        }
    ];

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

    private int clickCount;
    private int selectCount;
    private bool reselectable;
    private BitNavBarOption? eventsClickedOption;
    private BitNavBarOption? eventsSelectedOption;

    private string? twoWaySelectedKey;
    private string? scrollableSelectedKey;
}
