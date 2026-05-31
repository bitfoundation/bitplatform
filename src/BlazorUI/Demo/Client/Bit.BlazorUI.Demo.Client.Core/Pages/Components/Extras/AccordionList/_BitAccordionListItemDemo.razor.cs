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
        new() { Title = "Accordion 1", Description = "The first item", Body = BodyFor("Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.") },
        new() { Title = "Accordion 2", Description = "The second item", Body = BodyFor("Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.") },
        new() { Title = "Accordion 3", Description = "The third item", Body = BodyFor("In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.") },
    ];

    private readonly List<BitAccordionListItem> keyedItems =
    [
        new() { Key = "general", Title = "General settings", Body = BodyFor("The general settings of the application.") },
        new() { Key = "users", Title = "Users", Body = BodyFor("You are currently not an owner.") },
        new() { Key = "advanced", Title = "Advanced settings", Body = BodyFor("Filtering has been entirely disabled for the whole web server.") },
    ];

    private readonly List<BitAccordionListItem> iconItems =
    [
        new() { Title = "Profile", ExpanderIconName = BitIconName.Contact, Body = BodyFor("Your profile information.") },
        new() { Title = "Settings", ExpanderIconName = BitIconName.Settings, Body = BodyFor("The application settings.") },
        new() { Title = "Notifications", ExpanderIconName = BitIconName.Ringer, Body = BodyFor("Your notification preferences.") },
    ];

    private readonly List<BitAccordionListItem> rtlItems =
    [
        new() { Title = "تنظیمات عمومی", Description = "تنظیمات کلی برنامه", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
        new() { Title = "کاربران", Description = "شما در حال حاضر مالک نیستید", Body = BodyFor("لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.") },
    ];

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Text = "General", OnClick = _ => boundExpandedKey = "general" },
        new() { Text = "Users", OnClick = _ => boundExpandedKey = "users" },
        new() { Text = "Advanced", OnClick = _ => boundExpandedKey = "advanced" },
        new() { Text = "None", OnClick = _ => boundExpandedKey = null },
    ];

    private List<BitAccordionListItem> eventsItems =
    [
        new() { Title = "Accordion 1", Body = BodyFor("Click my header to increase the counter.") },
        new() { Title = "Accordion 2", Body = BodyFor("Click my header to increase the counter.") },
        new() { Title = "Accordion 3", Body = BodyFor("Click my header to increase the counter.") },
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
