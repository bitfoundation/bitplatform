namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private int clickCounter;
    private string? expandedTitle;
    private string? collapsedTitle;
    private string? toggledTitle;
    private string? boundExpandedKey = "users";
    private BitAccordionList<BitAccordionListOption> accordionListRef = default!;

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Key = "general", Text = "General" },
        new() { Key = "users", Text = "Users" },
        new() { Key = "advanced", Text = "Advanced" },
    ];
}
