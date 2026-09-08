namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
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
    private BitAccordionList<Section>? accordionListRef;

    private const string Story1 = "Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.";
    private const string Story2 = "Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.";
    private const string Story3 = "In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.";

    private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        Title = { Selector = i => i.Name },
        Description = { Selector = i => i.Info },
        IsEnabled = { Selector = i => i.IsEnabled },
        ReadOnly = { Selector = i => i.Locked },
        ExpanderIconName = { Selector = i => i.Image },
        IconName = { Selector = i => i.Glyph },
        Icon = { Selector = i => i.CustomGlyph },
        Style = { Selector = i => i.Style },
        Class = { Selector = i => i.Class },
        OnClick = { Selector = i => i.Clicked },
        Actions = { Selector = i => i.Extra },
        Body = { Selector = i => i.Content },
    };

    private readonly List<Section> basicItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor(Story2) },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor(Story3) },
    ];

    private readonly List<Section> keyedItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor(Story2) },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor(Story3) },
    ];

    private readonly List<Section> iconItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Glyph = BitIconName.Settings, Image = BitIconName.ChevronDownSmall, Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Glyph = BitIconName.Contact, Image = BitIconName.ChevronDownSmall, Content = BodyFor(Story2) },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Glyph = BitIconName.Ringer, Content = BodyFor(Story3) },
    ];

    private readonly List<Section> stateItems =
    [
        new() { Id = "normal", Name = "General settings", Info = "A live item", Content = BodyFor(Story1) },
        new() { Id = "disabled", Name = "Users", Info = "Turned off altogether", IsEnabled = false, Content = BodyFor(Story2) },
        new() { Id = "locked", Name = "Advanced settings", Info = "Open on purpose and staying that way", Locked = true, Content = BodyFor(Story3) },
    ];

    private readonly List<Section> lazyItems =
    [
        new() { Id = "lazy-1", Name = "Lazy panel", Info = "Rendered the first time it is opened, and kept afterwards", Content = TimestampBody() },
    ];

    private readonly List<Section> unmountItems =
    [
        new() { Id = "unmount-1", Name = "Unmounted panel", Info = "Rendered again on every open", Content = TimestampBody() },
    ];

    private readonly List<Section> longItems =
    [
        new() { Id = "long-1", Name = "A long panel", Info = "Scrolls inside the item", Content = BodyFor($"{Story1} {Story2} {Story3} {Story1} {Story2} {Story3}") },
        new() { Id = "long-2", Name = "Another long panel", Info = "Scrolls inside the item", Content = BodyFor($"{Story3} {Story2} {Story1} {Story3} {Story2} {Story1}") },
    ];

    private readonly List<Section> faItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", CustomGlyph = BitIconInfo.Fa("solid gear"), Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", CustomGlyph = BitIconInfo.Fa("solid user"), Content = BodyFor(Story2) },
    ];

    private readonly List<Section> biItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", CustomGlyph = BitIconInfo.Bi("gear"), Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", CustomGlyph = BitIconInfo.Bi("person"), Content = BodyFor(Story2) },
    ];

    private readonly List<Section> rtlItems =
    [
        new() { Id = "general", Name = "تنظیمات عمومی", Info = "تنظیمات کلی برنامه", Content = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
        new() { Id = "users", Name = "کاربران", Info = "شما در حال حاضر مالک نیستید", Content = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
    ];

    private readonly List<Section> noItems = [];

    private readonly List<Section> scrollItems =
    [
        new() { Id = "scroll-1", Name = "First section", Info = "Opens without moving anything", Content = BodyFor(Story1) },
        new() { Id = "scroll-2", Name = "Second section", Info = "Sits just below the fold", Content = BodyFor($"{Story2} {Story3}") },
        new() { Id = "scroll-3", Name = "Third section", Info = "Is scrolled to when it opens", Content = BodyFor($"{Story3} {Story1}") },
        new() { Id = "scroll-4", Name = "Fourth section", Info = "Is scrolled to when it opens", Content = BodyFor($"{Story1} {Story2}") },
    ];

    private readonly List<Section> eventsItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor(Story1) },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor(Story2) },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor(Story3) },
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
            item.Clicked = _ => { clickCounter++; StateHasChanged(); };
        }
    }

    private async Task HandleOnToggling(BitAccordionListToggleArgs<Section> args)
    {
        togglingReport = $"{args.Item.Name} is {(args.IsExpanding ? "expanding" : "collapsing")} ({args.Reason})";

        // The header of this item reports itself as aria-busy for as long as the callback is awaited.
        if (slowToggling)
        {
            await Task.Delay(1000);
        }

        args.Cancel = lockToggling;
    }

    private static RenderFragment<Section> BodyFor(string? text) => section => builder =>
    {
        builder.AddContent(0, text);
    };

    private static RenderFragment<Section> TimestampBody() => section => builder =>
    {
        builder.AddContent(0, $"This panel was rendered at {DateTime.Now:HH:mm:ss.fff}");
    };
}
