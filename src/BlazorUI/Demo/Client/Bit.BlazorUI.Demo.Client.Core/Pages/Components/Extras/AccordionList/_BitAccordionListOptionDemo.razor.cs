namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private int clickCounter;
    private string? expandedTitle;
    private string? collapsedTitle;
    private string? toggledTitle;
    private string? boundExpandedKey = "users";

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Text = "General", OnClick = _ => boundExpandedKey = "general" },
        new() { Text = "Users", OnClick = _ => boundExpandedKey = "users" },
        new() { Text = "Advanced", OnClick = _ => boundExpandedKey = "advanced" },
        new() { Text = "None", OnClick = _ => boundExpandedKey = null },
    ];
}
