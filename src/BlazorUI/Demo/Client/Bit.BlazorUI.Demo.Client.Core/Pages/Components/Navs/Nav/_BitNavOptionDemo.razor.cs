namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Nav;

public partial class _BitNavOptionDemo
{
    private bool iconOnly;

    private BitNavOption ClickedOption = default!;
    private BitNavOption ToggledOption = default!;
    private BitNavOption SelectedOption = default!;

    private BitNav<BitNavOption>? apiNavRef;

    private void ExpandAllApiOptions() => apiNavRef?.ExpandAll();
    private void CollapseAllApiOptions() => apiNavRef?.CollapseAll();

    private static readonly BitIconInfo fontAwesomeHomeIcon = BitIconInfo.Css("fa-solid fa-house");
    private static readonly BitIconInfo fontAwesomeCodeIcon = BitIconInfo.Fa("solid code");
    private static readonly BitIconInfo fontAwesomeTagIcon = BitIconInfo.Css("fa-solid fa-tag");
    private static readonly BitIconInfo fontAwesomeIconsIcon = BitIconInfo.Css("fa-solid fa-icons");

    private static readonly BitIconInfo bootstrapChevronIcon = BitIconInfo.Bi("chevron-right");
    private static readonly BitIconInfo bootstrapHomeIcon = BitIconInfo.Bi("house-fill");
    private static readonly BitIconInfo bootstrapCodeIcon = BitIconInfo.Bi("code-slash");
    private static readonly BitIconInfo bootstrapTagIcon = BitIconInfo.Bi("tag-fill");
    private static readonly BitIconInfo bootstrapSmileIcon = BitIconInfo.Bi("emoji-smile");

    private string? SelectedOptionKey = "French Fries";
    private static readonly List<BitDropdownItem<string>> FoodMenuDropdownItems =
    [
        new() { Text = "Beef Burger", Value = "Beef Burger" },
        new() { Text = "Veggie Burger", Value = "Veggie Burger" },
        new() { Text = "Cheese Pizza", Value = "Cheese Pizza" },
        new() { Text = "Meat Pizza", Value = "Meat Pizza" },
        new() { Text = "French Fries", Value = "French Fries" },
        new() { Text = "Apple", Value = "Apple" },
        new() { Text = "Orange", Value = "Orange" },
        new() { Text = "Banana", Value = "Banana" },
        new() { Text = "Ice Cream", Value = "Ice Cream" },
        new() { Text = "Cookie", Value = "Cookie" },
    ];
}
