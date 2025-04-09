namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupItemDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";

    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";

    private string? successMessage;
    private ChoiceGroupValidationModel validationModel = new();


    private readonly List<BitChoiceGroupItem<string>> basicItems =
    [
        new() { Text = "Item A", Value = "A" },
    new() { Text = "Item B", Value = "B" },
    new() { Text = "Item C", Value = "C" },
    new() { Text = "Item D", Value = "D" }
    ];

    private readonly List<BitChoiceGroupItem<string>> disabledItems =
    [
        new() { Text = "Item A", Value = "A" },
    new() { Text = "Item B", Value = "B" },
    new() { Text = "Item C", Value = "C", IsEnabled = false },
    new() { Text = "Item D", Value = "D" }
    ];

    private readonly List<BitChoiceGroupItem<string>> imageItems =
    [
        new()
        {
            Text = "Bar",
            Value = "Bar",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitImageSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitImageSize(32, 32),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<BitChoiceGroupItem<string>> inlineImageItems =
    [
        new()
        {
            Text = "Bar",
            Value = "Bar",
            ImageAlt = "alt for Bar image",
            ImageSize = new BitImageSize(20, 20),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Text = "Pie",
            Value = "Pie",
            ImageAlt = "alt for Pie image",
            ImageSize = new BitImageSize(20, 20),
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    ];

    private readonly List<BitChoiceGroupItem<string>> iconItems =
    [
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    ];

    private readonly List<BitChoiceGroupItem<string>> itemStyleClassItems =
    [
        new() { Text = "Item A", Value = "A", Class = "custom-item" },
        new() { Text = "Item B", Value = "B", Style = "padding: 8px; border-radius: 20px; border: 1px solid gray;" },
        new() { Text = "Item C", Value = "C", Class = "custom-item" },
        new() { Text = "Item D", Value = "D", Class = "custom-item" }
    ];

    private readonly List<BitChoiceGroupItem<string>> itemTemplateItems =
    [
        new() { Text = "Day", Value = "Day", IconName = BitIconName.CalendarDay },
        new() { Text = "Week", Value = "Week", IconName = BitIconName.CalendarWeek },
        new() { Text = "Month", Value = "Month", IconName = BitIconName.Calendar }
    ];

    private readonly List<BitChoiceGroupItem<string>> rtlItems =
    [
        new() { Text = "بخش آ", Value = "A" },
        new() { Text = "بخش ب", Value = "B" },
        new() { Text = "بخش پ", Value = "C" },
        new() { Text = "بخش ت", Value = "D" }
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
