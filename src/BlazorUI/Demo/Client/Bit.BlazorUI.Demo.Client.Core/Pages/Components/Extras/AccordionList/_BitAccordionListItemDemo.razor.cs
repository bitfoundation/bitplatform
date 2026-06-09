namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListItemDemo
{
    private int clickCounter;
    private string? expandedTitle;
    private string? collapsedTitle;
    private string? toggledTitle;
    private string? boundExpandedKey = "users";
    private BitAccordionList<BitAccordionListItem> accordionListRef = default!;

    private readonly List<BitAccordionListItem> basicItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Body = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Title = "Users", Description = "You are currently not an owner", Body = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<BitAccordionListItem> keyedItems =
    [
        new() { Key = "general", Title = "General settings", Description = "The general settings of the application", Body = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Key = "users", Title = "Users", Description = "You are currently not an owner", Body = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Key = "advanced", Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<BitAccordionListItem> iconItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", ExpanderIconName = BitIconName.Settings, Body = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Title = "Users", Description = "You are currently not an owner", ExpanderIconName = BitIconName.Contact, Body = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", ExpanderIconName = BitIconName.Ringer, Body = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<BitAccordionListItem> rtlItems =
    [
        new() { Title = "تنظیمات عمومی", Description = "تنظیمات کلی برنامه", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
        new() { Title = "کاربران", Description = "شما در حال حاضر مالک نیستید", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
    ];

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Key = "general", Text = "General" },
        new() { Key = "users", Text = "Users" },
        new() { Key = "advanced", Text = "Advanced" },
    ];

    private List<BitAccordionListItem> eventsItems =
    [
        new() { Title = "General settings", Description = "The general settings of the application", Body = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Title = "Users", Description = "You are currently not an owner", Body = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Title = "Advanced settings", Description = "Filtering has been entirely disabled", Body = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    protected override void OnInitialized()
    {
        foreach (var item in eventsItems)
        {
            item.OnClick = _ => { clickCounter++; StateHasChanged(); };
        }
    }

    private static RenderFragment<BitAccordionListItem> BodyFor(string? text) => item => builder =>
    {
        builder.AddContent(0, text);
    };
}
