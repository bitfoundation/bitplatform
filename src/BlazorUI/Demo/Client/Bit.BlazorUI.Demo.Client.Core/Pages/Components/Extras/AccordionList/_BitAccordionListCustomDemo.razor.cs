namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
{
    private int clickCounter;
    private string? expandedTitle;
    private string? collapsedTitle;
    private string? toggledTitle;
    private string? boundExpandedKey = "users";
    private BitAccordionList<Section> accordionListRef = default!;

    private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        Title = { Selector = i => i.Name },
        Description = { Selector = i => i.Info },
        IsEnabled = { Selector = i => i.IsEnabled },
        ExpanderIconName = { Selector = i => i.Image },
        Style = { Selector = i => i.Style },
        Class = { Selector = i => i.Class },
        OnClick = { Selector = i => i.Clicked },
        Body = { Selector = i => i.Content },
    };

    private readonly List<Section> basicItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<Section> keyedItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<Section> iconItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Image = BitIconName.Settings, Content = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Image = BitIconName.Contact, Content = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Image = BitIconName.Ringer, Content = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<Section> rtlItems =
    [
        new() { Id = "general", Name = "تنظیمات عمومی", Info = "تنظیمات کلی برنامه", Content = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
        new() { Id = "users", Name = "کاربران", Info = "شما در حال حاضر مالک نیستید", Content = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
    ];

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Key = "general", Text = "General" },
        new() { Key = "users", Text = "Users" },
        new() { Key = "advanced", Text = "Advanced" },
    ];

    private List<Section> eventsItems =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings of the application", Content = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Filtering has been entirely disabled", Content = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    protected override void OnInitialized()
    {
        foreach (var item in eventsItems)
        {
            item.Clicked = _ => { clickCounter++; StateHasChanged(); };
        }
    }

    private static RenderFragment<Section> BodyFor(string? text) => section => builder =>
    {
        builder.AddContent(0, text);
    };
}
