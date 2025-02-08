namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupOptionDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }


    private readonly string example1RazorCode = @"
<BitChoiceGroup Label=""Basic Options"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""NoCircle"" NoCircle DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example2RazorCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                DefaultValue=""@(""A"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                DefaultValue=""@(""A"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" IsEnabled=""false"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example3RazorCode = @"
<BitChoiceGroup Label=""Image Options""
                DefaultValue=""@(""Bar"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Bar""
                          Value=""@(""Bar"")""
                          ImageAlt=""Alt for Bar image""
                          ImageSize=""@(new BitImageSize(32, 32))""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" />
    <BitChoiceGroupOption Text=""Pie""
                          Value=""@(""Pie"")""
                          ImageAlt=""Alt for Pie image""
                          ImageSize=""@(new BitImageSize(32, 32))""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon Options""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>


<BitChoiceGroup Label=""Image Options""
                Inline
                DefaultValue=""@(""Bar"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Bar""
                          Value=""@(""Bar"")""
                          ImageAlt=""Alt for Bar image""
                          ImageSize=""@(new BitImageSize(20, 20))""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" />
    <BitChoiceGroupOption Text=""Pie""
                          Value=""@(""Pie"")""
                          ImageAlt=""Alt for Pie image""
                          ImageSize=""@(new BitImageSize(20, 20))""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon Options""
                Inline
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>";

    private readonly string example4RazorCode = @"
<BitChoiceGroup Label=""Basic"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Disabled"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" IsEnabled=""false"" DefaultValue=""@(""A"")"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Image"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Bar"")"" Horizontal>
    <BitChoiceGroupOption Text=""Bar""
                          Value=""@(""Bar"")""
                          ImageAlt=""Alt for Bar image""
                          ImageSize=""@(new BitImageSize(32, 32))"" 
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"" />
    <BitChoiceGroupOption Text=""Pie""
                          Value=""@(""Pie"")""
                          ImageAlt=""Alt for Pie image""
                          ImageSize=""@(new BitImageSize(32, 32))"" 
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Day"")"" Horizontal>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        margin-inline: 16px;
        text-shadow: dodgerblue 0 0 8px;
    }


    .custom-item {
        padding: 8px;
        border-radius: 20px;
        border: 1px solid gray;
    }


    .custom-root {
        margin-inline: 16px;
    }

    .custom-text {
        font-weight: bold;
    }

    .custom-label-wrapper::after {
        width: 8px;
        height: 8px;
        border: none;
        inset-inline-start: 6px;
        background-color: whitesmoke;
    }

    .custom-checked .custom-label-wrapper::after {
        background-color: whitesmoke;
    }

    .custom-label-wrapper::before {
        background-color: whitesmoke;
    }

    .custom-checked .custom-label-wrapper::before {
        background-color: dodgerblue;
    }
</style>


<BitChoiceGroup Label=""Styled ChoiceGroup"" DefaultValue=""@(""B"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                Style=""margin-inline: 16px; text-shadow: red 0 0 8px;"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Classed ChoiceGroup"" DefaultValue=""@(""B"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                Class=""custom-class"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>


<BitChoiceGroup DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" Class=""custom-item"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" Style=""padding: 8px; border-radius: 20px; border: 1px solid gray;"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" Class=""custom-item"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" Class=""custom-item"" />
</BitChoiceGroup>


<BitChoiceGroup Label=""Styles"" DefaultValue=""@(""B"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                Styles=""@(new() { Root = ""margin-inline: 16px; --item-background: #d3d3d347; --item-border: 1px solid gray;"",
                                  ItemLabel = ""width: 100%; cursor: pointer;"",
                                  ItemChecked = ""--item-background: #87cefa24; --item-border: 1px solid dodgerblue;"",
                                  ItemContainer = ""padding: 8px; border-radius: 2px; background-color: var(--item-background); border: var(--item-border);"" })"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Classes"" DefaultValue=""@(""B"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                Classes=""@(new() { Root = ""custom-root"",
                                   ItemText = ""custom-text"",
                                   ItemChecked = ""custom-checked"",
                                   ItemLabelWrapper = ""custom-label-wrapper"" })"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example6RazorCode = @"
<style>
    .custom-label {
        color: red;
        font-size: 18px;
        font-weight: bold;
    }
</style>

<BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <Options>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </Options>
</BitChoiceGroup>";

    private readonly string example7RazorCode = @"
<BitChoiceGroup Label=""One-way"" Value=""@oneWayValue""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<BitTextField @bind-Value=""oneWayValue"" />


<BitChoiceGroup Label=""Two-way"" @bind-Value=""twoWayValue""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<BitTextField @bind-Value=""twoWayValue"" />";
    private readonly string example7CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";";

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

<BitChoiceGroup Label=""ItemPrefixTemplate"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <ItemPrefixTemplate Context=""option"">
        @(option.Index + 1).&nbsp;
    </ItemPrefixTemplate>
    <Options>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </Options>
</BitChoiceGroup>

<BitChoiceGroup Label=""ItemLabelTemplate"" @bind-Value=""itemLabelTemplateValue""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <ItemLabelTemplate Context=""option"">
        <div class=""custom-container @(itemLabelTemplateValue == option.Value ? ""selected"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName"" />
            <span>@option.Text</span>
        </div>
    </ItemLabelTemplate>
    <Options>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </Options>
</BitChoiceGroup>


<BitChoiceGroup Label=""ItemTemplate"" @bind-Value=""itemTemplateValue""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <ItemTemplate Context=""option"">
        <div class=""custom-container @(itemTemplateValue == option.Value ? ""selected"" : string.Empty)"">
            <div class=""custom-circle""></div>
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
    <Options>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </Options>
</BitChoiceGroup>

<BitChoiceGroup Label=""Item's Template"" @bind-Value=""itemTemplateValue2""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"">
        <Template Context=""option"">
            <div class=""custom-container @(itemTemplateValue2 == option.Value ? ""selected"" : """")"">
                <div class=""custom-circle""></div>
                <span style=""color:red"">@option.Text</span>
            </div>
        </Template>
    </BitChoiceGroupOption>
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"">
        <Template Context=""option"">
            <div class=""custom-container @(itemTemplateValue2 == option.Value ? ""selected"" : """")"">
                <div class=""custom-circle""></div>
                <span style=""color:green"">@option.Text</span>
            </div>
        </Template>
    </BitChoiceGroupOption>
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"">
        <Template Context=""option"">
            <div class=""custom-container @(itemTemplateValue2 == option.Value ? ""selected"" : """")"">
                <div class=""custom-circle""></div>
                <span style=""color:blue"">@option.Text</span>
            </div>
        </Template>
    </BitChoiceGroupOption>
</BitChoiceGroup>";
    private readonly string example8CsharpCode = @"
private string itemTemplateValue = ""Day"";
private string itemTemplateValue2 = ""Day"";
private string itemLabelTemplateValue = ""Day"";";

    private readonly string example9RazorCode = @"
<BitChoiceGroup Label=""ساده"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" Dir=""BitDir.Rtl"">
    <BitChoiceGroupOption Text=""گزینه آ"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""گزینه ب"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""گزینه پ"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""گزینه ت"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""غیرفعال"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" IsEnabled=""false"" DefaultValue=""@(""A"")"" Dir=""BitDir.Rtl"">
    <BitChoiceGroupOption Text=""گزینه آ"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""گزینه ب"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""گزینه پ"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""گزینه ت"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example10RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    
    <BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""validationModel.Value"">
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>
    <ValidationMessage For=""@(() => validationModel.Value)"" />
    
    <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";
}
