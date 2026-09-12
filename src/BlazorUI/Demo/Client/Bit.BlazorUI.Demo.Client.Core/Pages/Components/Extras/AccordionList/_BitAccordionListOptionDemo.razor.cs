namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private int clickCounter;
    private int readOnlyClickCount;
    private bool lockToggling;
    private bool slowToggling;
    private bool showEmptyItems;
    private string? expandedTitle;
    private string? collapsedTitle;
    private string? toggledTitle;
    private string? actionedTitle;
    private string? togglingReport;
    private string? boundExpandedKey = "users";
    private IEnumerable<string> boundExpandedKeys = ["general"];
    private IEnumerable<string> programmaticKeys = [];
    private BitAccordionList<BitAccordionListOption>? accordionListRef;

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Key = "general", Text = "General" },
        new() { Key = "users", Text = "Users" },
        new() { Key = "advanced", Text = "Advanced" },
    ];

    private async Task HandleOnToggling(BitAccordionListToggleArgs<BitAccordionListOption> args)
    {
        togglingReport = $"{args.Item.Title} is {(args.IsExpanding ? "expanding" : "collapsing")} ({args.Reason})";

        // The header of this option reports itself as aria-busy for as long as the callback is awaited.
        if (slowToggling)
        {
            await Task.Delay(1000);
        }

        args.Cancel = lockToggling;
    }
}
