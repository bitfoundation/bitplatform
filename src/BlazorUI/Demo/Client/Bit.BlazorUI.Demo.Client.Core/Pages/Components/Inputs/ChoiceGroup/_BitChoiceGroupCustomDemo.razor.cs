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
        new() { Name = "Custom C", ItemValue = "C", IsDisabled = true },
        new() { Name = "Custom D", ItemValue = "D" }
    };
    private readonly List<ChoiceModel> imageCustoms = new()
    {
        new()
        {
            Name = "Bar",
            ItemValue = "Bar",
            ImageSize = new BitSize(32, 32),
            ImageDescription = "alt for Bar image",
            ImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
        },
        new()
        {
            Name = "Pie",
            ItemValue = "Pie",
            ImageSize = new BitSize(32, 32),
            ImageDescription = "alt for Pie image",
            ImageAddress= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageAddress = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
        }
    };
    private readonly List<ChoiceModel> iconCustoms = new()
    {
        new() { Name = "Day", ItemValue = "Day", IconName = BitIconName.CalendarDay },
        new() { Name = "Week", ItemValue = "Week", IconName = BitIconName.CalendarWeek },
        new() { Name = "Month", ItemValue = "Month", IconName = BitIconName.Calendar, IsDisabled = true }
    };
    private readonly List<ChoiceModel> itemStyleClassCustoms = new()
    {
        new() { Name = "Custom A", ItemValue = "A" },
        new() { Name = "Custom B", ItemValue = "B", Style = "color:red" },
        new() { Name = "Custom C", ItemValue = "C", Class = "custom-item" },
        new() { Name = "Custom D", ItemValue = "D", Style = "color:green" }
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


    private readonly string example1RazorCode = @"
<BitChoiceGroup Label=""Basic Customs""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />";
    private readonly string example1CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example2RazorCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Custom""
                Items=""disabledCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, 
                                         Value = { Selector = i => i.ItemValue },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
    private readonly string example2CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public bool IsDisabled { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> disabledCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"", IsDisabled = true },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example3RazorCode = @"
<BitChoiceGroup Label=""Image Customs""
                DefaultValue=""@(""Bar"")""
                Items=""imageCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) },
                                         Value = { Name = nameof(ChoiceModel.ItemValue) },
                                         ImageSrc = { Name = nameof(ChoiceModel.ImageAddress) },
                                         ImageAlt = { Name = nameof(ChoiceModel.ImageDescription) },
                                         ImageSize = { Name = nameof(ChoiceModel.ImageSize) },
                                         SelectedImageSrc = { Name = nameof(ChoiceModel.SelectedImageAddress) }})"" />

<BitChoiceGroup Label=""Icon Customs""
                DefaultValue=""@(""Day"")""
                Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Value = { Selector = i => i.ItemValue },
                                         IconName = { Selector = i => i.IconName },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
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
    public bool IsDisabled { get; set; }
}

private readonly List<ChoiceModel> imageCustoms = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageSize = new BitSize(32, 32),
        ImageDescription = ""alt for Bar image"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageSize = new BitSize(32, 32),
        ImageDescription = ""alt for Pie image"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
};

private readonly List<ChoiceModel> iconCustoms = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsDisabled = true }
};";

    private readonly string example4RazorCode = @"
<BitChoiceGroup Label=""Basic""
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />

<BitChoiceGroup Label=""Disabled""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                Items=""basicCustoms""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />

<BitChoiceGroup Label=""Image""
                DefaultValue=""@(""Bar"")""
                Items=""imageCustoms""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) },
                                         Value = { Name = nameof(ChoiceModel.ItemValue) },
                                         ImageSrc = { Name = nameof(ChoiceModel.ImageAddress) },
                                         ImageAlt = { Name = nameof(ChoiceModel.ImageDescription) },
                                         ImageSize = { Name = nameof(ChoiceModel.ImageSize) },
                                         SelectedImageSrc = { Name = nameof(ChoiceModel.SelectedImageAddress) }})"" />

<BitChoiceGroup Label=""Icon""
                DefaultValue=""@(""Day"")""
                Items=""iconCustoms""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Value = { Selector = i => i.ItemValue },
                                         IconName = { Selector = i => i.IconName },
                                         IsEnabled = { Selector = i => i.IsDisabled is false } })"" />";
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
    public bool IsDisabled { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> imageCustoms = new()
{
    new()
    {
        Name = ""Bar"",
        ItemValue = ""Bar"",
        ImageSize = new BitSize(32, 32),
        ImageDescription = ""alt for Bar image"",
        ImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
    },
    new()
    {
        Name = ""Pie"",
        ItemValue = ""Pie"",
        ImageSize = new BitSize(32, 32),
        ImageDescription = ""alt for Pie image"",
        ImageAddress= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageAddress = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
    }
};

private readonly List<ChoiceModel> iconCustoms = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar, IsDisabled = true }
};";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        width: 17rem;
        padding: 2rem;
        border-radius: 25%;
        background-color: red;
    }

    .custom-item {
        color: brown;
    }

    .custom-label {
        color: red;
        font-size: 18px;
        font-weight: bold;
    }

    .custom-text {
        color: blue;
        font-size: 16px;
        font-weight: bold;
    }
</style>


<BitChoiceGroup Label=""Styled ChoiceGroup""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Style=""width:200px;height:200px;background-color:#888;padding:1rem;border-radius:1rem;""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />

<BitChoiceGroup Label=""Classed ChoiceGroup""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Class=""custom-class""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />


<BitChoiceGroup Items=""itemStyleClassCustoms""
                DefaultValue=""itemStyleClassCustoms[1].ItemValue""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, 
                                         Value = { Name = nameof(ChoiceModel.ItemValue) },
                                         Class = { Name = nameof(ChoiceModel.Class) },
                                         Style = { Name = nameof(ChoiceModel.Style) } })"" />


<BitChoiceGroup Label=""Styles""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Styles=""@(new() { Label = ""color:tomato"",
                                  ItemIcon = ""color:red"" ,
                                  ItemText = ""color:yellowgreen;font-size:12px;font-weight:bold"" })""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />

<BitChoiceGroup Label=""Classes""
                Items=""basicCustoms""
                DefaultValue=""basicCustoms[1].ItemValue""
                Classes=""@(new() { Label = ""custom-label"" , ItemText = ""custom-text"" })""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })""/>";
    private readonly string example5CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> itemStyleClassCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"", Style = ""color:red"" },
    new() { Name = ""Custom C"", ItemValue = ""C"", Class = ""custom-item"" },
    new() { Name = ""Custom D"", ItemValue = ""D"", Style = ""color:green"" }
};";

    private readonly string example6RazorCode = @"
<style>
    .custom-label {
        color: red;
        font-size: 18px;
        font-weight: bold;
    }
</style>

<BitChoiceGroup Items=""basicCustoms""
                DefaultValue=""@(""A"")""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>";
    private readonly string example6CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example7RazorCode = @"
<BitChoiceGroup Label=""One-way"" Value=""@oneWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />
<BitTextField @bind-Value=""oneWayValue"" />

<BitChoiceGroup Label=""Two-way"" @bind-Value=""twoWayValue""
                Items=""basicCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";

public class ChoiceModel
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";

    private readonly string example8RazorCode = @"
<style>
    .custom-container {
        display: flex;
        align-items: center;
        gap: 10px;
        cursor: pointer;
    }

    .custom-circle {
        width: 20px;
        height: 20px;
        border: 1px solid;
        border-radius: 10px;
    }

    .custom-container:hover .custom-circle {
        border-top: 5px solid #C66;
        border-bottom: 5px solid #6C6;
        border-left: 5px solid #66C;
        border-right: 5px solid #CC6;
    }

    .custom-container.selected {
        color: #C66;
    }

    .custom-container.selected .custom-circle {
        border-top: 10px solid #C66;
        border-bottom: 10px solid #6C6;
        border-left: 10px solid #66C;
        border-right: 10px solid #CC6;
    }
</style>

<BitChoiceGroup Label=""ItemPrefixTemplate"" Items=""basicCustoms"" DefaultValue=""@string.Empty""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name},
                                         Value = { Selector = i => i.ItemValue },
                                         Index = nameof(ChoiceModel.Idx) })"">
    <ItemPrefixTemplate Context=""item"">
        @(item.Idx + 1).&nbsp;
    </ItemPrefixTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemLabelTemplate"" @bind-Value=""itemLabelTemplateValue""
                Items=""itemTemplateCustoms""
                NameSelectors=""@(new() { Value = { Selector = i => i.ItemValue } })"">
    <ItemLabelTemplate Context=""custom"">
        <div class=""custom-container @(itemLabelTemplateValue == custom.ItemValue ? ""selected"" : string.Empty)"">
            <BitIcon IconName=""@custom.IconName"" />
            <span>@custom.Name</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemTemplate"" @bind-Value=""itemTemplateValue""
                Items=""itemTemplateCustoms""
                NameSelectors=""@(new() { Value = { Name = nameof(ChoiceModel.ItemValue) } })"">
    <ItemTemplate Context=""custom"">
        <div class=""custom-container @(itemTemplateValue == custom.ItemValue ? ""selected"" : string.Empty)"">
            <div class=""custom-circle""></div>
            <span>@custom.Name</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>


<BitChoiceGroup Label=""Item's Template"" Items=""itemTemplateCustoms2"" @bind-Value=""itemTemplateValue2""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, 
                                         Value = { Name = nameof(ChoiceModel.ItemValue) },
                                         Template = { Name = nameof(ChoiceModel.Fragment) } })"" />";
    private readonly string example8CsharpCode = @"
private string itemLabelTemplateValue = ""Day"";
private string itemTemplateValue = ""Day"";
private string itemTemplateValue2 = ""Day"";

public class ChoiceModel
{
    public string? Name { get; set; }
    public string? ItemValue { get; set; }
    public string? IconName { get; set; }
    public RenderFragment<ChoiceModel>? Fragment { get; set; }
    public int Idx { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};

private readonly List<ChoiceModel> itemTemplateCustoms = new()
{
    new() { Name = ""Day"", ItemValue = ""Day"", IconName = BitIconName.CalendarDay },
    new() { Name = ""Week"", ItemValue = ""Week"", IconName = BitIconName.CalendarWeek },
    new() { Name = ""Month"", ItemValue = ""Month"", IconName = BitIconName.Calendar }
};

private List<ChoiceModel> itemTemplateCustoms2 = default!;    
protected override void OnInitialized()
{
    itemTemplateCustoms2 = new()
    {
        new()
        {
            Name = ""Day"",
            ItemValue = ""Day"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:red"">@item.Name</span>
                                 </div>)
        },
        new()
        {
            Name = ""Week"",
            ItemValue = ""Week"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:green"">@item.Name</span>
                                 </div>)
        },
        new()
        {
            Name = ""Month"",
            ItemValue = ""Month"",
            Fragment = (item => @<div class=""custom-container @(itemTemplateValue2 == item.ItemValue ? ""selected"" : """")"">
                                     <div class=""custom-circle"" />
                                     <span style=""color:blue"">@item.Name</span>
                                 </div>)
        }
    };
}";

    private readonly string example9RazorCode = @"
<BitChoiceGroup Label=""ساده""
                Dir=""BitDir.Rtl""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />

<BitChoiceGroup Label=""غیرفعال""
                Dir=""BitDir.Rtl""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                Items=""rtlCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name }, Value = { Selector = i => i.ItemValue } })"" />";
    private readonly string example9CsharpCode = @"
public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<ChoiceModel> rtlCustoms = new()
{
    new() { Name = ""ویژه آ"", ItemValue = ""A"" },
    new() { Name = ""ویژه ب"", ItemValue = ""B"" },
    new() { Name = ""ویژه پ"", ItemValue = ""C"" },
    new() { Name = ""ویژه ت"", ItemValue = ""D"" }
};";

    private readonly string example10RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    
    <BitChoiceGroup @bind-Value=""validationModel.Value""
                    Items=""basicCustoms""
                    NameSelectors=""@(new() { Text = { Name = nameof(ChoiceModel.Name) }, Value = { Name = nameof(ChoiceModel.ItemValue) } })"" />
    <ValidationMessage For=""@(() => validationModel.Value)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }

public class ChoiceModel
{
    public string Name { get; set; }
    public string ItemValue { get; set; }
}

private readonly List<ChoiceModel> basicCustoms = new()
{
    new() { Name = ""Custom A"", ItemValue = ""A"" },
    new() { Name = ""Custom B"", ItemValue = ""B"" },
    new() { Name = ""Custom C"", ItemValue = ""C"" },
    new() { Name = ""Custom D"", ItemValue = ""D"" }
};";
}
