namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>The Utilities category: elements, icons, images, links, media queries, overlays, pull to refresh, separators, sticky parts, swipes and text.</summary>
public partial class UtilitiesSection
{
    /// <summary>The demo people whose avatars the circular images show, by their name's resource key.</summary>
    private static readonly string[] imagePeople = [nameof(WorkbenchStrings.DemoUserSaleh), nameof(WorkbenchStrings.DemoUserZahra), nameof(WorkbenchStrings.DemoUserYas)];

    private string[] orders = [];

    /// <summary>The resource key that says which way the last swipe in the swipe box went; the hint to swipe before the first.</summary>
    private string swipeKey = nameof(WorkbenchStrings.ThemeUtilitiesSwipeOrDragHere);

    protected override void OnInitialized()
    {
        orders = [.. Enumerable.Range(1, 8).Select(index => $"PO-{1040 + index} · {Localizer[nameof(WorkbenchStrings.ThemeListsBreakfastBox)]} × {index % 3 + 1}")];
    }

    /// <summary>Stands in for a reload, which the pull waits for.</summary>
    private Task Refresh() => Task.Delay(800, CurrentCancellationToken);

    // The swipe trap reports one of the four physical directions.
    private void OnSwiped(BitSwipeTrapTriggerArgs args) => swipeKey = args.Direction switch
    {
        BitPlacement.Top => nameof(WorkbenchStrings.ThemeUtilitiesSwipedUp),
        BitPlacement.Bottom => nameof(WorkbenchStrings.ThemeUtilitiesSwipedDown),
        BitPlacement.Left => nameof(WorkbenchStrings.ThemeUtilitiesSwipedLeft),
        _ => nameof(WorkbenchStrings.ThemeUtilitiesSwipedRight)
    };
}
