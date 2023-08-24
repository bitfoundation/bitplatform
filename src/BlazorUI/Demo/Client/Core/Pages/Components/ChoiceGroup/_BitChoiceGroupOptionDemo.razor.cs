namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.ChoiceGroup;

public partial class _BitChoiceGroupOptionDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string itemTemplateValue = "Day";
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


    private readonly string example1HtmlCode = @"
<BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Label=""Pick one"" DefaultValue=""@(""A"")"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                Label=""Disabled ChoiceGroup""
                IsEnabled=""false""
                DefaultValue=""@(""A"")"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                DefaultValue=""@(""A"")"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" IsEnabled=""false"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                DefaultValue=""@(""Bar"")"">
    <BitChoiceGroupOption Text=""Bar""
                          Value=""@(""Bar"")""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                          ImageAlt=""Alt for Bar image""
                          ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                          Value=""@(""Pie"")""
                          ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                          SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                          ImageAlt=""Alt for Pie image""
                          ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Pick one icon""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                DefaultValue=""@(""Day"")"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>";

    private readonly string example4HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </ChildContent>
</BitChoiceGroup>";

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
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""ChoiceGroupWithOptionTemplateValue"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Label Template""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </ChildContent>
    <ItemLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.Value ? ""selected-option"" : string.Empty)"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemLabelTemplate>
</BitChoiceGroup>";
    private readonly string example5CsharpCode = @"
private string ChoiceGroupWithOptionTemplateValue = ""Day"";
private string ChoiceGroupWithOptionLabelTemplateValue = ""Day"";";

    private readonly string example6HtmlCode = @"
<BitChoiceGroup TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                Label=""One-way""
                Value=""@ChoiceGroupOneWayValue"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<BitTextField @bind-Value=""ChoiceGroupOneWayValue"" />
    
<BitChoiceGroup TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                Label=""Two-way""
                @bind-Value=""ChoiceGroupTwoWayValue"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<BitTextField @bind-Value=""ChoiceGroupTwoWayValue"" />";
    private readonly string example6CsharpCode = @"
private string ChoiceGroupOneWayValue = ""A"";
private string ChoiceGroupTwoWayValue = ""A"";";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""Basic"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Disabled"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" IsEnabled=""false"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Image"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Bar"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Bar""
                            Value=""@(""Bar"")""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageAlt=""Alt for Bar image""
                            ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                            Value=""@(""Pie"")""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                            ImageAlt=""Alt for Pie image""
                            ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Day"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </ChildContent>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupLayoutFlowWithOptionTemplateValue""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value?.ToString() ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example7CsharpCode = @"
private string ChoiceGroupLayoutFlowWithOptionTemplateValue = ""Day"";";

    private readonly string example8HtmlCode = @"
<BitChoiceGroup Label=""Basic"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Disabled"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" IsEnabled=""false"" DefaultValue=""@(""A"")"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Image"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Bar"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Bar""
                            Value=""@(""Bar"")""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png""
                            ImageAlt=""Alt for Bar image""
                            ImageSize=""@(new Size(32, 32))"" />
    <BitChoiceGroupOption Text=""Pie""
                            Value=""@(""Pie"")""
                            ImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png""
                            SelectedImageSrc=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png""
                            ImageAlt=""Alt for Pie image""
                            ImageSize=""@(new Size(32, 32))"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Icon"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""Day"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
    <ChildContent>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </ChildContent>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template"" @bind-Value=""@ChoiceGroupRtlLayoutFlowWithOptionTemplateValue""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <ChildContent>
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" />
    </ChildContent>
    <ItemTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupRtlLayoutFlowWithOptionTemplateValue == option.Value?.ToString() ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </ItemTemplate>
</BitChoiceGroup>";
    private readonly string example8CsharpCode = @"
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
            <BitChoiceGroup TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""ValidationModel.Value"">
                <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
                <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
                <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
                <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
            </BitChoiceGroup>
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
