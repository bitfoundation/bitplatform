namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private string? eventsChangedItem;
    private string? eventsClickedItem;

    private bool itemIsToggled;
    private bool itemToggledValue;

    private BitMenuButtonItem twoWaySelectedItem = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private bool itemIsLoading;
    private bool itemAutoIsLoading;

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
        new() { Text = "Item A (Default)", Key = "A", IconName = BitIconName.Emoji, Style = "color: brown"  },
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

    private static List<BitMenuButtonItem> externalIconItems =
    [
        new() { Text = "Add", Icon = "fa-solid fa-plus" },
        new() { Text = "Edit", Icon = BitIconInfo.Css("fa-solid fa-pen") },
        new() { Text = "Delete", Icon = BitIconInfo.Fa("solid trash") }
    ];

    private static List<BitMenuButtonItem> groupedItems =
    [
        new() { Text = "Document", IsHeader = true },
        new() { Text = "New", Key = "new", IconName = BitIconName.Add, SecondaryText = "Ctrl+N" },
        new() { Text = "Open", Key = "open", IconName = BitIconName.OpenFile, SecondaryText = "Ctrl+O" },
        new() { IsSeparator = true },
        new() { Text = "Save", Key = "save", IconName = BitIconName.Save, SecondaryText = "Ctrl+S" },
        new() { Text = "Save as", Key = "save-as", IconName = BitIconName.SaveAs },
        new() { IsSeparator = true },
        new() { Text = "Danger zone", IsHeader = true },
        new() { Text = "Delete", Key = "delete", IconName = BitIconName.Delete, SecondaryText = "Del" }
    ];

    private static List<BitMenuButtonItem> checkableItems =
    [
        new() { Text = "Name", Key = "name", Checkable = true, IsChecked = true },
        new() { Text = "Status", Key = "status", Checkable = true, IsChecked = true },
        new() { Text = "Owner", Key = "owner", Checkable = true },
        new() { Text = "Reset to defaults", Key = "reset", IconName = BitIconName.Refresh }
    ];

    private static List<BitMenuButtonItem> checkableItems2 =
    [
        new() { Text = "Wrap lines", Key = "wrap", Checkable = true, IsChecked = true },
        new() { Text = "Show whitespace", Key = "whitespace", Checkable = true }
    ];

    private static List<BitMenuButtonItem> ariaLabelItems =
    [
        new() { Key = "share", IconName = BitIconName.Share, AriaLabel = "Share this page" },
        new() { Key = "print", IconName = BitIconName.Print, AriaLabel = "Print this page" }
    ];

    private static List<BitMenuButtonItem> longItems =
        Enumerable.Range(1, 20).Select(i => new BitMenuButtonItem { Text = $"Item {i}", Key = i.ToString() }).ToList();

    private static List<BitMenuButtonItem> linkItems =
    [
        new() { Text = "bit platform", Key = "bit", IconName = BitIconName.Globe, Href = "https://bitplatform.dev", Target = "_blank", Title = "The bit platform website" },
        new() { Text = "GitHub repo", Key = "github", IconName = BitIconName.Link, Href = "https://github.com/bitfoundation/bitplatform", Target = "_blank", Title = "The bit platform GitHub repository" },
        new() { IsSeparator = true },
        new() { Text = "Item C", Key = "C", IconName = BitIconName.Emoji2, Title = "A regular item" }
    ];

    private static List<BitMenuButtonItem> dropDirectionItems =
        Enumerable.Range(1, 8).Select(i => new BitMenuButtonItem { Text = $"Item {i}", Key = i.ToString() }).ToList();

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

        checkableItems[^1].OnClick = _ =>
        {
            checkableItems[0].IsChecked = true;
            checkableItems[1].IsChecked = true;
            checkableItems[2].IsChecked = false;
        };
    }

    private async Task HandleOnLoadingClick()
    {
        itemAutoIsLoading = true;
        await Task.Delay(2000);
        itemAutoIsLoading = false;
    }
}
