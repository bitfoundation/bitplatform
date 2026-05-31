namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListCustomDemo
{
    private int clickCounter;
    private string? expandedName;
    private string? collapsedName;
    private string? toggledName;
    private string? boundExpandedKey = "users";

    private readonly BitAccordionListNameSelectors<Section> nameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        Title = { Selector = i => i.Name },
        Description = { Selector = i => i.Info },
        IsExpanded = { Selector = i => i.Open },
        IsEnabled = { Selector = i => i.IsEnabled },
        Body = { Selector = i => i.Content },
    };

    private readonly List<Section> basicSections =
    [
        new() { Id = "general", Name = "General settings", Info = "The general settings", Content = BodyFor("The general settings of the application.") },
        new() { Id = "users", Name = "Users", Info = "You are currently not an owner", Content = BodyFor("Manage the users of the application.") },
        new() { Id = "advanced", Name = "Advanced settings", Info = "Be careful here", Content = BodyFor("The advanced settings of the application.") },
    ];

    private readonly List<Section> iconSections =
    [
        new() { Id = "profile", Name = "Profile", Image = BitIconName.Contact, Content = BodyFor("Your profile information.") },
        new() { Id = "settings", Name = "Settings", Image = BitIconName.Settings, Content = BodyFor("The application settings.") },
        new() { Id = "notifications", Name = "Notifications", Image = BitIconName.Ringer, Content = BodyFor("Your notification preferences.") },
    ];

    private readonly List<Section> styleClassSections =
    [
        new() { Id = "styled", Name = "Styled", Style = "color: tomato;", Content = BodyFor("This item header has a custom style.") },
        new() { Id = "classed", Name = "Classed", Class = "custom-item", Content = BodyFor("This item has a custom class.") },
    ];

    private List<BitButtonGroupItem> bindingButtons =>
    [
        new() { Text = "General", OnClick = _ => boundExpandedKey = "general" },
        new() { Text = "Users", OnClick = _ => boundExpandedKey = "users" },
        new() { Text = "Advanced", OnClick = _ => boundExpandedKey = "advanced" },
        new() { Text = "None", OnClick = _ => boundExpandedKey = null },
    ];

    private List<Section> eventsSections =
    [
        new() { Id = "s1", Name = "Section 1", Content = BodyFor("Click my header to increase the counter.") },
        new() { Id = "s2", Name = "Section 2", Content = BodyFor("Click my header to increase the counter.") },
        new() { Id = "s3", Name = "Section 3", Content = BodyFor("Click my header to increase the counter.") },
    ];

    protected override void OnInitialized()
    {
        foreach (var section in eventsSections)
        {
            section.Clicked = _ => { clickCounter++; StateHasChanged(); };
        }
    }

    private static RenderFragment<Section> BodyFor(string? text) => section => builder =>
    {
        builder.AddContent(0, text);
    };
}
