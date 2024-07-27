namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private string? eventsChangedItem;
    private string? eventsClickedItem;

    private BitMenuButtonItem twoWaySelectedItem = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private static List<BitMenuButtonItem> basicItems =
    [
        new() { Text = "Item A", Key = "A" },
        new() { Text = "Item B", Key = "B", IsEnabled = false },
        new() { Text = "Item C", Key = "C" }
    ];

    private static List<BitMenuButtonItem> basicItemsIcon =
    [
        new() { Text = "Item A", Key = "A", IconName = BitIconName.Emoji },
        new() { Text = "Item B", Key = "B", IconName = BitIconName.Emoji, IsEnabled = false },
        new() { Text = "Item C", Key = "C", IconName = BitIconName.Emoji2 }
    ];

    private static List<BitMenuButtonItem> itemStyleClassItems =
    [
        new() { Text = "Item A (Default)", Key = "A", IconName = BitIconName.Emoji },
        new() { Text = "Item C (Styled)", Key = "B", IconName = BitIconName.Emoji, Style = "color: tomato; border-color: brown; background-color: peachpuff;" },
        new() { Text = "Item B (Classed)", Key = "C", IconName = BitIconName.Emoji2, Class = "custom-item" }
    ];

    private static List<BitMenuButtonItem> basicItemsOnClick =
    [
        new() { Text = "Item A", Key = "A", IconName = BitIconName.Emoji },
        new() { Text = "Item B", Key = "B", IconName = BitIconName.Emoji, IsEnabled = false },
        new() { Text = "Item C", Key = "C", IconName = BitIconName.Emoji2 }
    ];

    private static List<BitMenuButtonItem> itemTemplateItems =
    [
        new() { Text = "Add", Key = "add-key", IconName = BitIconName.Add },
        new() { Text = "Edit", Key = "edit-key", IconName = BitIconName.Edit },
        new() { Text = "Delete", Key = "delete-key", IconName = BitIconName.Delete }
    ];

    private static List<BitMenuButtonItem> isSelectedItems =
    [
        new() { Text = "Item A", Key = "A", IconName = BitIconName.Emoji },
        new() { Text = "Item B", Key = "B", IconName = BitIconName.Emoji },
        new() { Text = "Item C", Key = "C", IconName = BitIconName.Emoji2, IsSelected = true }
    ];

    private static List<BitMenuButtonItem> rtlItemsIcon =
    [
        new() { Text = "گزینه الف", Key = "A", IconName = BitIconName.Emoji },
        new() { Text = "گزینه ب", Key = "B", IconName = BitIconName.Emoji },
        new() { Text = "گزینه ج", Key = "C", IconName = BitIconName.Emoji2 }
    ];

    private static IEnumerable<BitChoiceGroupItem<BitMenuButtonItem>> choiceGroupItems =
        basicItems.Select(i => new BitChoiceGroupItem<BitMenuButtonItem>() { Id = i.Key, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

    protected override void OnInitialized()
    {
        twoWaySelectedItem = basicItems[2];

        Action<BitMenuButtonItem> onClick = item =>
        {
            eventsClickedItem = $"{item.Text}";
            StateHasChanged();
        };

        basicItemsOnClick.ForEach(i => i.OnClick = onClick);
    }
}
