namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonCustomDemo
{
    private string? eventsChangedCustom;
    private string? eventsClickedCustom;

    private bool customIsToggled;
    private bool customToggledValue;

    private Operation twoWaySelectedCustom = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private bool customIsLoading;
    private bool customAutoIsLoading;

    private static BitMenuButtonNameSelectors<Operation> nameSelectors = new()
    {
        Text = { Name = nameof(Operation.Name) },
        Key = { Name = nameof(Operation.Id) },
        IconName = { Name = nameof(Operation.Image) },
        IsEnabled = { Selector = m => m.Disabled is false },
        IsSeparator = { Name = nameof(Operation.IsDivider) },
        Href = { Name = nameof(Operation.Url) },
        Target = { Name = nameof(Operation.UrlTarget) },
        Title = { Name = nameof(Operation.Tooltip) },
        Template = { Name = nameof(Operation.Fragment) }
    };

    private static BitMenuButtonNameSelectors<Operation> nameSelectors2 = new()
    {
        Text = { Name = nameof(Operation.Name) },
        Key = { Name = nameof(Operation.Id) },
        Icon = { Selector = i => i.IconInfo },
        IsEnabled = { Selector = m => m.Disabled is false }
    };


    private static List<Operation> basicCustoms =
    [
        new() { Name = "Custom A", Id = "A" },
        new() { Name = "Custom B", Id = "B", Disabled = true },
        new() { Name = "Custom C", Id = "C" }
    ];

    private static List<Operation> basicIconCustoms =
    [
        new() { Name = "Custom A", Id = "A", Image = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Image = BitIconName.Emoji, Disabled = true },
        new() { Name = "Custom C", Id = "C", Image = BitIconName.Emoji2 }
    ];

    private static List<Operation> basicCustomsOnClick =
    [
        new() { Name = "Custom A", Id = "A", Image = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Image = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Image = BitIconName.Emoji2 }
    ];

    private static List<Operation> itemTemplateCustoms =
    [
        new() { Name = "Add", Id = "add-key", Image = BitIconName.Add },
        new() { Name = "Edit", Id = "edit-key", Image = BitIconName.Edit },
        new() { Name = "Delete", Id = "delete-key", Image = BitIconName.Delete }
    ];

    private static List<Operation> itemStyleClassCustoms =
    [
        new() { Name = "Custom A (Default)", Id = "A", Image = BitIconName.Emoji, Style = "color: brown" },
        new() { Name = "Custom B (Styled)", Id = "B", Image = BitIconName.Emoji, Style = "color: tomato; border-color: brown; background-color: peachpuff;" },
        new() { Name = "Custom C (Classed)", Id = "C", Image = BitIconName.Emoji2, Class = "custom-item" },
    ];

    private static List<Operation> isSelectedCustoms =
    [
        new() { Name = "Custom A", Id = "A", Image = BitIconName.Emoji },
        new() { Name = "Custom B", Id = "B", Image = BitIconName.Emoji },
        new() { Name = "Custom C", Id = "C", Image = BitIconName.Emoji2, IsSelected = true }
    ];

    private static List<Operation> externalIconCustoms =
    [
        new() { Name = "Add", IconInfo = "fa-solid fa-plus" },
        new() { Name = "Edit", IconInfo = BitIconInfo.Css("fa-solid fa-pen") },
        new() { Name = "Delete", IconInfo = BitIconInfo.Fa("solid trash") }
    ];

    private static List<Operation> separatorCustoms =
    [
        new() { Name = "New", Id = "new", Image = BitIconName.Add },
        new() { Name = "Open", Id = "open", Image = BitIconName.OpenFile },
        new() { IsDivider = true },
        new() { Name = "Save", Id = "save", Image = BitIconName.Save },
        new() { Name = "Save as", Id = "save-as", Image = BitIconName.SaveAs },
        new() { IsDivider = true },
        new() { Name = "Delete", Id = "delete", Image = BitIconName.Delete }
    ];

    private static List<Operation> linkCustoms =
    [
        new() { Name = "bit platform", Id = "bit", Image = BitIconName.Globe, Url = "https://bitplatform.dev", UrlTarget = "_blank", Tooltip = "The bit platform website" },
        new() { Name = "GitHub repo", Id = "github", Image = BitIconName.Link, Url = "https://github.com/bitfoundation/bitplatform", UrlTarget = "_blank", Tooltip = "The bit platform GitHub repository" },
        new() { IsDivider = true },
        new() { Name = "Custom C", Id = "C", Image = BitIconName.Emoji2, Tooltip = "A regular item" }
    ];

    private static List<Operation> dropDirectionCustoms =
        Enumerable.Range(1, 8).Select(i => new Operation { Name = $"Custom {i}", Id = i.ToString() }).ToList();

    private static List<Operation> rtlCustoms =
    [
        new() { Name = "گزینه الف", Id = "A", Image = BitIconName.Emoji },
        new() { Name = "گزینه ب", Id = "B", Image = BitIconName.Emoji },
        new() { Name = "گزینه ج", Id = "C", Image = BitIconName.Emoji2 }
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

    private async Task HandleOnLoadingClick()
    {
        customAutoIsLoading = true;
        await Task.Delay(2000);
        customAutoIsLoading = false;
    }
}
