namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupOptionDemo
{
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
<BitChoiceGroup Label=""End (default)"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LabelPosition=""BitLabelPosition.End"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Start"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LabelPosition=""BitLabelPosition.Start"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Top"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LabelPosition=""BitLabelPosition.Top"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Bottom"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" DefaultValue=""@(""A"")"" LabelPosition=""BitLabelPosition.Bottom"" Horizontal>
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

<BitChoiceGroup Label=""ItemPrefixTemplate & ItemSuffixTemplate"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <ItemPrefixTemplate Context=""option"">
        @(option.Index + 1).&nbsp;
    </ItemPrefixTemplate>
    <ItemSuffixTemplate Context=""option"">
        &nbsp;<b>(@option.Value)</b>
    </ItemSuffixTemplate>
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
        <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" />
        <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" />
        <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" />
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
    private readonly string example7CsharpCode = @"
private string itemTemplateValue = ""Day"";
private string itemTemplateValue2 = ""Day"";
private string itemLabelTemplateValue = ""Day"";";

    private readonly string example8RazorCode = @"
<BitChoiceGroup Label=""Uncontrolled (DefaultValue)"" DefaultValue=""@(""A"")""
                OnChange=""(string? value) => uncontrolledValue = value""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<div>Selected: <b>@uncontrolledValue</b></div>


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
    private readonly string example8CsharpCode = @"
private string oneWayValue = ""A"";
private string twoWayValue = ""A"";
private string? uncontrolledValue = ""A"";";

    private readonly string example9RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""@validationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    
    <BitChoiceGroup Label=""Pick one"" Required TItem=""BitChoiceGroupOption<string>"" TValue=""string"" @bind-Value=""validationModel.Value"">
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>
    <ValidationMessage For=""@(() => validationModel.Value)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example9CsharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example10RazorCode = @"
<BitChoiceGroup Label=""Backup frequency"" DefaultValue=""@(""Daily"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Daily"" Value=""@(""Daily"")"" Description=""Backs up every night at 2 AM."" />
    <BitChoiceGroupOption Text=""Weekly"" Value=""@(""Weekly"")"" Description=""Backs up every Sunday at 2 AM."" />
    <BitChoiceGroupOption Text=""Monthly"" Value=""@(""Monthly"")"" Description=""Backs up on the first day of each month."" />
</BitChoiceGroup>";

    private readonly string example11RazorCode = @"
<BitChoiceGroup Label=""ReadOnly"" ReadOnly @bind-Value=""readOnlyValue"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";
    private readonly string example11CsharpCode = @"
private string readOnlyValue = ""A"";";

    private readonly string example12RazorCode = @"
<BitChoiceGroup Label=""1rem gap"" Gap=""1rem"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""3rem gap (Horizontal)"" Gap=""3rem"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example13RazorCode = @"
<BitChoiceGroup Label=""Shipping method (Prefix)"" DefaultValue=""@(""Standard"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Standard"" Value=""@(""Standard"")"" Prefix=""$0 — "" />
    <BitChoiceGroupOption Text=""Express"" Value=""@(""Express"")"" Prefix=""$10 — "" />
    <BitChoiceGroupOption Text=""Overnight"" Value=""@(""Overnight"")"" Prefix=""$25 — "" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Shipping method (Suffix)""
                DefaultValue=""@(""Standard"")""
                FullWidth
                Styles=""@(new() { ItemSuffix = ""margin-inline-start: auto;"" })""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Standard"" Value=""@(""Standard"")"" Suffix=""Free"" />
    <BitChoiceGroupOption Text=""Express"" Value=""@(""Express"")"" Suffix=""$10"" />
    <BitChoiceGroupOption Text=""Overnight"" Value=""@(""Overnight"")"" Suffix=""$25"" />
</BitChoiceGroup>";

    private readonly string example14RazorCode = @"
<BitChoiceGroup Label=""Events"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                OnChange=""(string? value) => changedValue = value""
                OnClick=""(BitChoiceGroupOption<string> option) => clickedOption = option.Text""
                OnFocus=""(BitChoiceGroupOption<string> option) => focusedOption = option.Text""
                OnBlur=""(BitChoiceGroupOption<string> option) => blurredOption = option.Text"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
<div>Changed value: <b>@changedValue</b></div>
<div>Clicked option: <b>@clickedOption</b></div>
<div>Focused option: <b>@focusedOption</b></div>
<div>Blurred option: <b>@blurredOption</b></div>";
    private readonly string example14CsharpCode = @"
private string? changedValue;
private string? clickedOption;
private string? focusedOption;
private string? blurredOption;";

    private readonly string example15RazorCode = @"
<BitButton OnClick=""AddDynamicOption"">Add item</BitButton>
<BitButton OnClick=""RemoveDynamicOption"">Remove item</BitButton>
<BitButton OnClick=""ReverseDynamicOptions"">Reverse items</BitButton>

<BitChoiceGroup Label=""Dynamic options"" AutoReorderOptions @bind-Value=""dynamicValue"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <ItemPrefixTemplate Context=""option"">
        @(option.Index + 1).&nbsp;
    </ItemPrefixTemplate>
    <Options>
        @foreach (var (text, value) in dynamicOptions)
        {
            <BitChoiceGroupOption @key=""value"" Text=""@text"" Value=""@value"" />
        }
    </Options>
</BitChoiceGroup>";
    private readonly string example15CsharpCode = @"
private int dynamicCounter = 3;
private string? dynamicValue = ""1"";
private List<(string Text, string Value)> dynamicOptions =
[
    (""Option 1"", ""1""),
    (""Option 2"", ""2""),
    (""Option 3"", ""3"")
];

private void AddDynamicOption()
{
    dynamicCounter++;
    dynamicOptions = [.. dynamicOptions, ($""Option {dynamicCounter}"", $""{dynamicCounter}"")];
}

private void RemoveDynamicOption()
{
    if (dynamicOptions.Count <= 1) return;

    dynamicOptions = [.. dynamicOptions.Take(dynamicOptions.Count - 1)];
}

private void ReverseDynamicOptions()
{
    dynamicOptions = [.. Enumerable.Reverse(dynamicOptions)];
}";

    private readonly string example16RazorCode = @"
<style>
    .custom-description {
        gap: 0.25rem;
        display: flex;
        align-items: center;
    }
</style>

<BitChoiceGroup Label=""Deployment target""
                Description=""Only the selected environment receives the new build.""
                DefaultValue=""@(""Staging"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Development"" Value=""@(""Development"")"" />
    <BitChoiceGroupOption Text=""Staging"" Value=""@(""Staging"")"" />
    <BitChoiceGroupOption Text=""Production"" Value=""@(""Production"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""Deployment target"" DefaultValue=""@(""Staging"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <DescriptionTemplate>
        <div class=""custom-description"">
            <BitIcon IconName=""@BitIconName.Info"" />
            <span>Only the selected environment receives the new build.</span>
        </div>
    </DescriptionTemplate>
    <Options>
        <BitChoiceGroupOption Text=""Development"" Value=""@(""Development"")"" />
        <BitChoiceGroupOption Text=""Staging"" Value=""@(""Staging"")"" />
        <BitChoiceGroupOption Text=""Production"" Value=""@(""Production"")"" />
    </Options>
</BitChoiceGroup>";

    private readonly string example17RazorCode = @"
<BitChoiceGroup Label=""Default (hugs the widest option)"" DefaultValue=""@(""A"")"" Horizontal TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""FullWidth (horizontal, equal columns)"" DefaultValue=""@(""A"")"" Horizontal FullWidth TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""FullWidth + LabelPosition.Start (options at the far edge)"" DefaultValue=""@(""A"")"" LabelPosition=""BitLabelPosition.Start"" FullWidth TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""FullWidth + LabelPosition.Start + stretched option label (settings list)""
                DefaultValue=""@(""A"")""
                LabelPosition=""BitLabelPosition.Start"" FullWidth
                Styles=""@(new() { ItemLabel = ""width: 100%; justify-content: space-between;"" })""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example18RazorCode = @"
<BitChoiceGroup Label=""Delivery window (hover an option)"" DefaultValue=""@(""24h"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""1 h"" Value=""@(""1h"")"" Title=""Delivered within one hour of dispatch"" />
    <BitChoiceGroupOption Text=""24 h"" Value=""@(""24h"")"" Title=""Delivered within one business day"" />
    <BitChoiceGroupOption Text=""72 h"" Value=""@(""72h"")"" Title=""Delivered within three business days"" />
</BitChoiceGroup>";

    private readonly string example19RazorCode = @"
<BitButton OnClick=""() => showAutoFocus = !showAutoFocus"">@(showAutoFocus ? ""Unmount"" : ""Mount"") the auto focused ChoiceGroup</BitButton>

@if (showAutoFocus)
{
    <BitChoiceGroup AutoFocus Label=""Auto focused"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>
}";
    private readonly string example19CsharpCode = @"
private bool showAutoFocus;";

    private readonly string example20RazorCode = @"
<BitChoiceGroup Color=""BitColor.Primary"" Label=""Primary"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Color=""BitColor.Secondary"" Label=""Secondary"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Color=""BitColor.Tertiary"" Label=""Tertiary"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Color=""BitColor.Info"" Label=""Info"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
            
<BitChoiceGroup Color=""BitColor.Success"" Label=""Success"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
           
<BitChoiceGroup Color=""BitColor.Warning"" Label=""Warning"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
            
<BitChoiceGroup Color=""BitColor.SevereWarning"" Label=""SevereWarning"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
            
<BitChoiceGroup Color=""BitColor.Error"" Label=""Error"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
            
<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitChoiceGroup Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>

    <BitChoiceGroup Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>

    <BitChoiceGroup Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>
</div>
                
<BitChoiceGroup Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
                
<BitChoiceGroup Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
                
<BitChoiceGroup Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground""  DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
                
<BitChoiceGroup Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
                
<BitChoiceGroup Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>
                
<BitChoiceGroup Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Primary"" Label=""Primary"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Secondary"" Label=""Secondary"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Tertiary"" Label=""Tertiary"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Info"" Label=""Info"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Success"" Label=""Success"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Warning"" Label=""Warning"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.SevereWarning"" Label=""SevereWarning"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.Error"" Label=""Error"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<div style=""background:var(--bit-clr-fg-sec);color:var(--bit-clr-bg-sec);padding:1rem"">
    <BitChoiceGroup IsEnabled=""false"" Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>

    <BitChoiceGroup IsEnabled=""false"" Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>

    <BitChoiceGroup IsEnabled=""false"" Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
        <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
        <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
        <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
        <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
    </BitChoiceGroup>
</div>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup IsEnabled=""false"" Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" DefaultValue=""@(""A"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitChoiceGroup Label=""External Icons"" DefaultValue=""@(""Day"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" Icon=""@BitIconInfo.Fa(""solid sun"")"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-calendar-week"")"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" Icon=""@BitIconInfo.Bi(""calendar-month"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""External Icons (Inline)"" DefaultValue=""@(""Day"")"" Inline TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" Icon=""@BitIconInfo.Fa(""solid sun"")"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-calendar-week"")"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" Icon=""@BitIconInfo.Bi(""calendar-month"")"" />
</BitChoiceGroup>

<BitChoiceGroup Label=""External Icons (Horizontal)"" DefaultValue=""@(""Day"")"" Horizontal TItem=""BitChoiceGroupOption<string>"" TValue=""string"">
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" Icon=""@BitIconInfo.Fa(""solid sun"")"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" Icon=""@BitIconInfo.Css(""fa-solid fa-calendar-week"")"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" Icon=""@BitIconInfo.Bi(""calendar-month"")"" />
</BitChoiceGroup>";

    private readonly string example22RazorCode = @"
<BitChoiceGroup Size=""BitSize.Small"" Label=""Small"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Medium"" Label=""Medium"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Large"" Label=""Large"" DefaultValue=""@(""B"")"" TItem=""BitChoiceGroupOption<string>"" TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Small"" 
                Label=""Small"" 
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>"" 
                TValue=""string"" Horizontal Inline>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Medium""
                Label=""Medium""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>"" 
                TValue=""string"" Horizontal Inline>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Large""
                Label=""Large""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>"" 
                TValue=""string"" Horizontal Inline>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Small""
                Label=""Small""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Medium""
                Label=""Medium""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>

<BitChoiceGroup Size=""BitSize.Large""
                Label=""Large""
                DefaultValue=""@(""Day"")""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string"" Horizontal>
    <BitChoiceGroupOption Text=""Day"" Value=""@(""Day"")"" IconName=""@BitIconName.CalendarDay"" />
    <BitChoiceGroupOption Text=""Week"" Value=""@(""Week"")"" IconName=""@BitIconName.CalendarWeek"" />
    <BitChoiceGroupOption Text=""Month"" Value=""@(""Month"")"" IconName=""@BitIconName.Calendar"" IsEnabled=""false"" />
</BitChoiceGroup>";

    private readonly string example23RazorCode = @"
<style>
    .custom-class {
        color: dodgerblue;
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

    .custom-radio-btn::after {
        width: 8px;
        height: 8px;
        background-color: whitesmoke;
    }

    .custom-checked .custom-radio-btn::after {
        background-color: whitesmoke;
    }

    .custom-radio-btn::before {
        background-color: whitesmoke;
    }

    .custom-checked .custom-radio-btn::before {
        background-color: dodgerblue;
    }
</style>


<BitChoiceGroup Label=""Styled ChoiceGroup"" DefaultValue=""@(""B"")""
                TItem=""BitChoiceGroupOption<string>"" TValue=""string""
                Style=""margin-inline: 16px; color:lightseagreen; text-shadow: lightseagreen 0 0 8px;"">
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
                                   ItemRadioButton = ""custom-radio-btn"" })"">
    <BitChoiceGroupOption Text=""Option A"" Value=""@(""A"")"" />
    <BitChoiceGroupOption Text=""Option B"" Value=""@(""B"")"" />
    <BitChoiceGroupOption Text=""Option C"" Value=""@(""C"")"" />
    <BitChoiceGroupOption Text=""Option D"" Value=""@(""D"")"" />
</BitChoiceGroup>";

    private readonly string example24RazorCode = @"
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
}
