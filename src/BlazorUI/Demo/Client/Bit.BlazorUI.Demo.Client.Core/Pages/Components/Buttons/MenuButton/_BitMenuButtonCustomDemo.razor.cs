namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private string? eventsChangedCustom;
    private string? eventsClickedCustom;

    private MenuActionItem twoWaySelectedCustom = default!;

    private static BitMenuButtonNameSelectors<MenuActionItem> nameSelectors = new()
    {
        Text = { Name = nameof(MenuActionItem.Name) },
        Key = { Name = nameof(MenuActionItem.Id) },
        IconName = { Name = nameof(MenuActionItem.Icon) },
        IsEnabled = { Selector = m => m.Disabled is false },
        Template = { Name = nameof(MenuActionItem.Fragment) }
    };


    private static List<MenuActionItem> basicCustoms =
    [
        new() { Name = "Custom A", Id = "A" },
        new() { Name = "Custom B", Id = "B", Disabled = true },
        new() { Name = "Custom C", Id = "C" }
    ];

    private static List<MenuActionItem> basicIconCustoms =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static List<MenuActionItem> basicCustomsOnClick =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static List<MenuActionItem> itemTemplateCustoms =
    [
        new() { Name = "Add", Id = "add-key", Icon = BitIconName.Add },
        new() { Name = "Edit", Id = "edit-key", Icon = BitIconName.Edit },
        new() { Name = "Delete", Id = "delete-key", Icon = BitIconName.Delete }
    ];

    private static List<MenuActionItem> itemStyleClassCustoms =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji, Style = "color:red" },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji, Class = "custom-item" },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2, Style = "background:blue" }
    ];

    private static List<MenuActionItem> isSelectedCustoms =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2, IsSelected = true }
    ];

    private static List<MenuActionItem> rtlCustoms =
    [
        new() { Name = "گزینه الف", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "گزینه ب", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "گزینه ج", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static IEnumerable<BitChoiceGroupItem<MenuActionItem>> choiceGroupCustoms =
        basicCustoms.Select(i => new BitChoiceGroupItem<MenuActionItem>() { Id = i.Id, Text = i.Name, IsEnabled = i.Disabled is false, Value = i });

    protected override void OnInitialized()
    {
        twoWaySelectedCustom = basicCustoms[2];

        Action<MenuActionItem> onClick = item =>
        {
            eventsClickedCustom = $"{item.Name}";
            StateHasChanged();
        };

        basicCustomsOnClick.ForEach(i => i.Clicked = onClick);
    }
}
