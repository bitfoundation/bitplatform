namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupCustomDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string readOnlyValue = "A";
    private bool showAutoFocus;
    private string? uncontrolledValue = "A";

    private string? changedValue;
    private string? clickedCustom;
    private string? focusedCustom;
    private string? blurredCustom;

    private int dynamicCounter = 3;
    private string? dynamicValue = "1";
    private List<Order> dynamicCustoms =
    [
        new() { Name = "Custom 1", ItemValue = "1" },
        new() { Name = "Custom 2", ItemValue = "2" },
        new() { Name = "Custom 3", ItemValue = "3" }
    ];
    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";
    private ChoiceGroupValidationModel validationModel = new();
    private string? successMessage;


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

    private readonly List<Order> externalIconCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", Icon = BitIconInfo.Fa("solid sun") },
        new() { Name = "Week", ItemValue = "Week", Icon = BitIconInfo.Css("fa-solid fa-calendar-week") },
        new() { Name = "Month", ItemValue = "Month", Icon = BitIconInfo.Bi("calendar-month") }
    ];

    private readonly List<Order> itemStyleClassCustoms =
    [
        new() { Name = "Custom A", ItemValue = "A", Class = "custom-item" },
        new() { Name = "Custom B", ItemValue = "B", Style = "padding: 8px; border-radius: 20px; border: 1px solid gray;" },
        new() { Name = "Custom C", ItemValue = "C", Class = "custom-item" },
        new() { Name = "Custom D", ItemValue = "D", Class = "custom-item" }
    ];

    private readonly List<Order> prefixCustoms =
    [
        new() { Name = "Standard", ItemValue = "Standard", Prefix = "$0 — " },
        new() { Name = "Express", ItemValue = "Express", Prefix = "$10 — " },
        new() { Name = "Overnight", ItemValue = "Overnight", Prefix = "$25 — " }
    ];

    private readonly List<Order> suffixCustoms =
    [
        new() { Name = "Standard", ItemValue = "Standard", Fee = "Free" },
        new() { Name = "Express", ItemValue = "Express", Fee = "$10" },
        new() { Name = "Overnight", ItemValue = "Overnight", Fee = "$25" }
    ];

    private readonly List<Order> descriptionCustoms =
    [
        new() { Name = "Daily", ItemValue = "Daily", Summary = "Backs up every night at 2 AM." },
        new() { Name = "Weekly", ItemValue = "Weekly", Summary = "Backs up every Sunday at 2 AM." },
        new() { Name = "Monthly", ItemValue = "Monthly", Summary = "Backs up on the first day of each month." }
    ];

    private readonly List<Order> deploymentCustoms =
    [
        new() { Name = "Development", ItemValue = "Development" },
        new() { Name = "Staging", ItemValue = "Staging" },
        new() { Name = "Production", ItemValue = "Production" }
    ];

    private readonly List<Order> titleCustoms =
    [
        new() { Name = "1 h", ItemValue = "1h", Tooltip = "Delivered within one hour of dispatch" },
        new() { Name = "24 h", ItemValue = "24h", Tooltip = "Delivered within one business day" },
        new() { Name = "72 h", ItemValue = "72h", Tooltip = "Delivered within three business days" }
    ];

    private readonly List<Order> itemLabelTemplateCustoms =
    [
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar }
    ];

    private readonly List<Order> itemTemplateCustoms =
    [
        new() { Name = "Day", ItemValue = "Day" },
        new() { Name = "Week", ItemValue = "Week" },
        new() { Name = "Month", ItemValue = "Month" }
    ];

    private readonly List<Order> rtlCustoms =
    [
        new() { Name = "ویژه آ", ItemValue = "A" },
        new() { Name = "ویژه ب", ItemValue = "B" },
        new() { Name = "ویژه پ", ItemValue = "C" },
        new() { Name = "ویژه ت", ItemValue = "D" }
    ];


    private void AddDynamicCustom()
    {
        dynamicCounter++;
        dynamicCustoms = [.. dynamicCustoms, new Order { Name = $"Custom {dynamicCounter}", ItemValue = $"{dynamicCounter}" }];
    }

    private void RemoveDynamicCustom()
    {
        if (dynamicCustoms.Count <= 1) return;

        dynamicCustoms = [.. dynamicCustoms.Take(dynamicCustoms.Count - 1)];
    }

    private void ReverseDynamicCustoms()
    {
        dynamicCustoms = [.. Enumerable.Reverse(dynamicCustoms)];
    }

    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }
}
