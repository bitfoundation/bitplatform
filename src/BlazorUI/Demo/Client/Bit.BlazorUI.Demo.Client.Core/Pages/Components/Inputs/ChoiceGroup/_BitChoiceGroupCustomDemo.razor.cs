namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupCustomDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private readonly List<Order> basicCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C" },
        new() { Name = "Custom D", ItemValue = "D" }
    ];

    private readonly List<Order> disabledCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C", IsDisabled = true },
        new() { Name = "Custom D", ItemValue = "D" }
    ];

    private readonly List<Order> imageCustoms =
    [
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageSize = new BitImageSize(32, 32),
            ImageDescription = "alt for Bar image",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageSize = new BitImageSize(32, 32),
            ImageDescription = "alt for Pie image",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<Order> inlineImageCustoms =
    [
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageSize = new BitImageSize(20, 20),
            ImageDescription = "alt for Bar image",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageSize = new BitImageSize(20, 20),
            ImageDescription = "alt for Pie image",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<Order> iconCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar, IsDisabled = true }
    ];

    private readonly List<Order> itemStyleClassCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A", Class = "custom-item" },
        new() { Name = "Custom B", ItemValue = "B", Style = "padding: 8px; border-radius: 20px; border: 1px solid gray;" },
        new() { Name = "Custom C", ItemValue = "C", Class = "custom-item" },
        new() { Name = "Custom D", ItemValue = "D", Class = "custom-item" }
    ];

    private readonly List<Order> itemTemplateCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar }
    ];

    private readonly List<Order> rtlCustoms =
    [
        new() { Name = "ویژه آ", ItemValue = "A" },
        new() { Name = "ویژه ب", ItemValue = "B" },
        new() { Name = "ویژه پ", ItemValue = "C" },
        new() { Name = "ویژه ت", ItemValue = "D" }
    ];


    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }
}
