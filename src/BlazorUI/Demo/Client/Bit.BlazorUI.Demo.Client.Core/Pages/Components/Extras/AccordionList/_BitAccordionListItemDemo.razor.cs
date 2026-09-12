namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListItemDemo
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
    private BitAccordionList<BitAccordionListItem>? accordionListRef;

    private const string Story1 = "Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.";
    private const string Story2 = "Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.";
    private const string Story3 = "In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.";

    private readonly List<BitAccordionListItem> basicItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Body = BodyFor(Story1) },
        new() { Title = "Users", Description = "You are currently not an owner", Body = BodyFor(Story2) },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor(Story3) },
    ];

    private readonly List<BitAccordionListItem> keyedItems =
    [
        new() { Key = "general", Title = "General settings", Description = "The general settings of the application", Body = BodyFor(Story1) },
        new() { Key = "users", Title = "Users", Description = "You are currently not an owner", Body = BodyFor(Story2) },
        new() { Key = "advanced", Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor(Story3) },
    ];

    private readonly List<BitAccordionListItem> iconItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", IconName = BitIconName.Settings, ExpanderIconName = BitIconName.ChevronDownSmall, Body = BodyFor(Story1) },
        new() { Title = "Users", Description = "You are currently not an owner", IconName = BitIconName.Contact, ExpanderIconName = BitIconName.ChevronDownSmall, Body = BodyFor(Story2) },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", IconName = BitIconName.Ringer, Body = BodyFor(Story3) },
    ];

    // No Body: an item that carries one of its own takes precedence over the list's BodyTemplate.
    private readonly List<BitAccordionListItem> templateItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application" },
        new() { Title = "Users", Description = "You are currently not an owner" },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled" },
    ];

    private readonly List<BitAccordionListItem> stateItems =
    [
        new() { Key = "normal", Title = "General settings", Description = "A live item", Body = BodyFor(Story1) },
        new() { Key = "disabled", Title = "Users", Description = "Turned off altogether", IsEnabled = false, Body = BodyFor(Story2) },
        new() { Key = "locked", Title = "Advanced settings", Description = "Open on purpose and staying that way", ReadOnly = true, Body = BodyFor(Story3) },
    ];

    private readonly List<BitAccordionListItem> lazyItems =
    [
        new() { Key = "lazy-1", Title = "Lazy panel", Description = "Rendered the first time it is opened, and kept afterwards", Body = TimestampBody() },
    ];

    private readonly List<BitAccordionListItem> unmountItems =
    [
        new() { Key = "unmount-1", Title = "Unmounted panel", Description = "Rendered again on every open", Body = TimestampBody() },
    ];

    private readonly List<BitAccordionListItem> longItems =
    [
        new() { Key = "long-1", Title = "A long panel", Description = "Scrolls inside the item", Body = BodyFor($"{Story1} {Story2} {Story3} {Story1} {Story2} {Story3}") },
        new() { Key = "long-2", Title = "Another long panel", Description = "Scrolls inside the item", Body = BodyFor($"{Story3} {Story2} {Story1} {Story3} {Story2} {Story1}") },
    ];

    private readonly List<BitAccordionListItem> faItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Icon = BitIconInfo.Fa("solid gear"), Body = BodyFor(Story1) },
        new() { Title = "Users", Description = "You are currently not an owner", Icon = BitIconInfo.Fa("solid user"), Body = BodyFor(Story2) },
    ];

    private readonly List<BitAccordionListItem> biItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Icon = BitIconInfo.Bi("gear"), Body = BodyFor(Story1) },
        new() { Title = "Users", Description = "You are currently not an owner", Icon = BitIconInfo.Bi("person"), Body = BodyFor(Story2) },
    ];

    private readonly List<BitAccordionListItem> rtlItems =
    [
        new() { Title = "تنظیمات عمومی", Description = "تنظیمات کلی برنامه", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
        new() { Title = "کاربران", Description = "شما در حال حاضر مالک نیستید", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
    ];

    private readonly List<BitAccordionListItem> noItems = [];

    private readonly List<BitAccordionListItem> scrollItems =
    [
        new() { Key = "scroll-1", Title = "First section", Description = "Opens without moving anything", Body = BodyFor(Story1) },
        new() { Key = "scroll-2", Title = "Second section", Description = "Sits just below the fold", Body = BodyFor($"{Story2} {Story3}") },
        new() { Key = "scroll-3", Title = "Third section", Description = "Is scrolled to when it opens", Body = BodyFor($"{Story3} {Story1}") },
        new() { Key = "scroll-4", Title = "Fourth section", Description = "Is scrolled to when it opens", Body = BodyFor($"{Story1} {Story2}") },
    ];

    private readonly List<BitAccordionListItem> eventsItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Body = BodyFor(Story1) },
        new() { Title = "Users", Description = "You are currently not an owner", Body = BodyFor(Story2) },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor(Story3) },
    ];

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Key = "general", Text = "General" },
        new() { Key = "users", Text = "Users" },
        new() { Key = "advanced", Text = "Advanced" },
    ];

    protected override void OnInitialized()
    {
        foreach (var item in eventsItems)
        {
            item.OnClick = _ => { clickCounter++; StateHasChanged(); };
        }
    }

    private async Task HandleOnToggling(BitAccordionListToggleArgs<BitAccordionListItem> args)
    {
        togglingReport = $"{args.Item.Title} is {(args.IsExpanding ? "expanding" : "collapsing")} ({args.Reason})";

        // The header of this item reports itself as aria-busy for as long as the callback is awaited.
        if (slowToggling)
        {
            await Task.Delay(1000);
        }

        args.Cancel = lockToggling;
    }

    private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder =>
    {
        builder.AddContent(0, text);
    };

    private static RenderFragment<BitAccordionListItem> TimestampBody() => item => builder =>
    {
        builder.AddContent(0, $"This panel was rendered at {DateTime.Now:HH:mm:ss.fff}");
    };
}
