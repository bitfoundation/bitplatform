namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private string? eventsChangedCustom;
    private string? eventsClickedCustom;

    private Operation twoWaySelectedCustom = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private static BitMenuButtonNameSelectors<Operation> nameSelectors = new()
    {
        Text = { Name = nameof(Operation.Name) },
        Key = { Name = nameof(Operation.Id) },
        IconName = { Name = nameof(Operation.Icon) },
        IsEnabled = { Selector = m => m.Disabled is false },
        Template = { Name = nameof(Operation.Fragment) }
    };


    private static List<Operation> basicCustoms =
    [
        new() { Name = "Custom A", Id = "A" },
        new() { Name = "Custom B", Id = "B", Disabled = true },
        new() { Name = "Custom C", Id = "C" }
    ];

    private static List<Operation> basicIconCustoms =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static List<Operation> basicCustomsOnClick =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static List<Operation> itemTemplateCustoms =
    [
        new() { Name = "Add", Id = "add-key", Icon = BitIconName.Add },
        new() { Name = "Edit", Id = "edit-key", Icon = BitIconName.Edit },
        new() { Name = "Delete", Id = "delete-key", Icon = BitIconName.Delete }
    ];

    private static List<Operation> itemStyleClassCustoms =
    [
        new() { Name = "Custom A (Default)", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B (Styled)", Id = "B", Icon = BitIconName.Emoji, Style = "color: tomato; border-color: brown; background-color: peachpuff;" },
        new() { Name = "Custom C (Classed)", Id = "C", Icon = BitIconName.Emoji2, Class = "custom-item" },
    ];

    private static List<Operation> isSelectedCustoms =
    [
        new() { Name = "Custom A", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Icon = BitIconName.Emoji2, IsSelected = true }
    ];

    private static List<Operation> rtlCustoms =
    [
        new() { Name = "گزینه الف", Id = "A", Icon = BitIconName.Emoji },
        new() { Name = "گزینه ب", Id = "B", Icon = BitIconName.Emoji },
        new() { Name = "گزینه ج", Id = "C", Icon = BitIconName.Emoji2 }
    ];

    private static IEnumerable<BitChoiceGroupItem<Operation>> choiceGroupCustoms =
        basicCustoms.Select(i => new BitChoiceGroupItem<Operation>() { Id = i.Id, Text = i.Name, IsEnabled = i.Disabled is false, Value = i });

    protected override void OnInitialized()
    {
        twoWaySelectedCustom = basicCustoms[2];

        Action<Operation> onClick = item =>
        {
            eventsClickedCustom = $"{item.Name}";
            StateHasChanged();
        };

        basicCustomsOnClick.ForEach(i => i.Clicked = onClick);
    }
}
