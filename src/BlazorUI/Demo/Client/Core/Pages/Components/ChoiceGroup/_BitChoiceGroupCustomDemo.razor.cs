namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ChoiceGroup;

public partial class _BitChoiceGroupCustomDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private readonly List<ChoiceModel> basicCustoms = new()
    {
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C" },
        new() { Name = "Custom D", ItemValue = "D" }
    };
    private readonly List<ChoiceModel> disabledCustoms = new()
    {
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B" },
        new() { Name = "Custom C", ItemValue = "C", IsEnabled = false },
        new() { Name = "Custom D", ItemValue = "D" }
    };
    private readonly List<ChoiceModel> imageCustoms = new()
    {
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageDescription = "alt for Bar image",
            ImageSize = new BitSize(32, 32)
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageDescription = "alt for Pie image",
            ImageSize = new BitSize(32, 32)
        }
    };
    private readonly List<ChoiceModel> iconCustoms = new()
    {
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar, IsEnabled = false }
    };
    private readonly List<ChoiceModel> itemTemplateCustoms = new()
    {
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar }
    };
    private readonly List<ChoiceModel> rtlCustoms = new()
    {
        new() { Name = "ویژه آ", ItemValue = "A" },
        new() { Name = "ویژه ب", ItemValue = "B" },
        new() { Name = "ویژه پ", ItemValue = "C" },
        new() { Name = "ویژه ت", ItemValue = "D" }
    };


    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }


    private readonly string example1HtmlCode = @"
<BitChoiceGroup Label=""Pick one""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")"" />";
    private readonly string example1CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false""
                DefaultValue=""@(""A"")"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                Items=""CustomChoiceGroupDisabledItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")"" />";
    private readonly string example2CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};
private readonly List<ChoiceModel> CustomChoiceGroupDisabledItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"", IsEnabled = false },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""@(""Bar"")"" />

<BitChoiceGroup Label=""Pick one icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""@(""Day"")"" />";
    private readonly string example3CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};";

    private readonly string example4HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>";
    private readonly string example4CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example5HtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;
    }

    .custom-option .option-pointer {
        width: rem(20px);
        height: rem(20px);
        border: 1px solid;
        border-radius: rem(10px);
    }

    .custom-option:hover .option-pointer {
        border-top: rem(5px) solid #C66;
        border-bottom: rem(5px) solid #6C6;
        border-left: rem(5px) solid #66C;
        border-right: rem(5px) solid #CC6;
    }

    .custom-option.selected-option {
        color: #C66;
    }

    .custom-option.selected-option .option-pointer {
        border-top: rem(10px) solid #C66;
        border-bottom: rem(10px) solid #6C6;
        border-left: rem(10px) solid #66C;
        border-right: rem(10px) solid #CC6;
    }
</style>

<BitChoiceGroup Label=""Option Template""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Label Template""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <ItemLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.ItemValue ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>";
    private readonly string example5CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";";

    private readonly string example6HtmlCode = @"
<BitChoiceGroup Label=""One-way""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                Value=""@ChoiceGroupOneWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />

<BitChoiceGroup Label=""Two-way""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                @bind-Value=""ChoiceGroupTwoWayValue"" />
<BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />";
    private readonly string example6CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""Basic""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Disabled""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false"" DefaultValue=""@(""A"")""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""@(""Bar"")""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""@(""Day"")""
                LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template""
                @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example7CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";";

    private readonly string example8HtmlCode = @"
<BitChoiceGroup Label=""Basic""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Disabled""
                Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Image""
                Items=""CustomChoiceGroupImageItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                ImageSrcField=""@nameof(ChoiceModel.ImageAddress)""
                ImageAltField=""@nameof(ChoiceModel.ImageDescription)""
                ImageSizeField=""@nameof(ChoiceModel.ImageSize)""
                SelectedImageSrcField=""@nameof(ChoiceModel.SelectedImageAddress)""
                DefaultValue=""@(""Bar"")""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"" />

<BitChoiceGroup Label=""Icon""
                Items=""CustomChoiceGroupIconItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                DefaultValue=""@(""Day"")""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"" />

<BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                DefaultValue=""@(""A"")""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                Items=""CustomChoiceGroupItemsTemplate""
                TextField=""@nameof(ChoiceModel.Name)""
                ValueField=""@nameof(ChoiceModel.ItemValue)""
                IconNameField=""@nameof(ChoiceModel.IconName)""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.ItemValue ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example8CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> CustomChoiceGroupImageItems = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageDescription = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageDescription = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private readonly List<ChoiceModel> CustomChoiceGroupIconItems = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsEnabled = false }
};

private readonly List<ChoiceModel> CustomChoiceGroupItemsTemplate = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = ""Day"";";

    private readonly string example9HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Items=""CustomChoiceGroupBasicItems""
                        TextField=""@nameof(ChoiceModel.Name)""
                        ValueField=""@nameof(ChoiceModel.ItemValue)""
                        @bind-Value=""ValidationModel.Value"" />
            <ValidationMessage For=""@(() => ValidationModel.Value)"" />
        </div>
        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}
<br />
<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => { ValidationModel = new(); SuccessMessage=string.Empty; }"">Reset</BitButton>";
    private readonly string example9CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string ImageAddress { get; set; }
    public string ImageDescription { get; set; }
    public Size? ImageSize { get; set; }
    public string SelectedImageAddress { get; set; }
    public string? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private readonly List<ChoiceModel> CustomChoiceGroupBasicItems = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

private void HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";
}
