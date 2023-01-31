using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.SpinButton;

public partial class BitSpinButtonDemo
{
    private double OneWayValue = 3;
    private double TwoWayValue = 5;

    private double IncrementEventValue;
    private int OnIncrementCounter;
    private int OnDecrementCounter;

    private double OnChangeEventBindedValue;
    private double OnChangeEventReturnedValue;
    private int OnChangeClickedCounter;
    private void HandleOnChangeEvent(double value)
    {
        OnChangeEventReturnedValue = value;

        OnChangeClickedCounter++;
    }

    private string SuccessMessage = string.Empty;
    private BitSpinButtonValidationModel ValidationModel = new();

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaPositionInSet",
            Type = "int",
            DefaultValue = "0",
            Description = "The position in the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaSetSize",
            Type = "int",
            DefaultValue = "0",
            Description = "The total size of the parent set (if in a set).",
        },
        new ComponentParameter()
        {
            Name = "AriaValueNow",
            Type = "double",
            DefaultValue = "0",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new ComponentParameter()
        {
            Name = "AriaValueText",
            Type = "string",
            Description = "Sets the control's aria-valuetext.",
        },
        new ComponentParameter()
        {
            Name = "ChangeHandler",
            Type = "EventCallback<BitSpinButtonAction>",
            Description = "",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "double",
            DefaultValue = "0",
            Description = "Initial value of the spin button.",
        },
        new ComponentParameter()
        {
            Name = "DecrementButtonAriaLabel",
            Type = "string",
            Description = "Accessible label text for the decrement button (for screen reader users).",
        },
        new ComponentParameter()
        {
            Name = "DecrementButtonIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronDownSmall",
            Description = "Custom icon name for the decrement button.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIconName",
            Description = "Icon name for an icon to display alongside the spin button's label.",
        },
        new ComponentParameter()
        {
            Name = "IconAriaLabel",
            Type = "string",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "IncrementButtonAriaLabel",
            Type = "string",
            Description = "Accessible label text for the increment button (for screen reader users).",
        },
        new ComponentParameter()
        {
            Name = "IncrementButtonIconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.ChevronUpSmall",
            Description = "Custom icon name for the increment button.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            Description = "Descriptive label for the spin button, Label displayed above the spin button and read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom Label for spin button. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id..",
        },
        new ComponentParameter()
        {
            Name = "LabelPosition",
            Type = "BitSpinButtonLabelPosition",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
            DefaultValue = "BitSpinButtonLabelPosition.Top",
            Description = "The position of the label in regards to the spin button.",
        },
        new ComponentParameter()
        {
            Name = "Min",
            Type = "double",
            DefaultValue = "0",
            Description = "Min value of the spin button. If not provided, the spin button has minimum value of double type.",
        },
        new ComponentParameter()
        {
            Name = "Max",
            Type = "double",
            DefaultValue = "0",
            Description = "Max value of the spin button. If not provided, the spin button has max value of double type.",
        },
        new ComponentParameter()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the control loses focus.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback for when the spin button value change.",
        },
        new ComponentParameter()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the increment button or up arrow key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "Precision",
            Type = "int",
            Description = "How many decimal places the value should be rounded to.",
        },
        new ComponentParameter()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "Difference between two adjacent values of the spin button.",
        },
        new ComponentParameter()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "A text is shown after the spin button value.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "ChevronUpSmall",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "labelPosition-enum",
            Title = "BitSpinButtonLabelPosition Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spin button.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spin button.",
                    Value="1",
                }
            }
        }
    };

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<div class=""example-box"">
    <BitSpinButton Label=""Basic"" />
    <BitSpinButton Label=""Disabled"" IsEnabled=""false"" />
    <BitSpinButton Label=""Label & Icon"" IconName=""BitIconName.Lightbulb"" />
    <BitSpinButton Label=""Left Label"" IconName=""BitIconName.Lightbulb"" LabelPosition=""BitSpinButtonLabelPosition.Left"" />
</div>
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<div class=""example-box"">
    <BitSpinButton>
        <LabelTemplate>
            <label style=""color: green;"">This is custom Label</label>
            <BitIcon IconName=""BitIconName.Filter"" />
        </LabelTemplate>
    </BitSpinButton>
</div>
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<div class=""example-box"">
    <BitSpinButton Label=""Like and Dislike""
                    IncrementButtonIconName=""BitIconName.LikeSolid""
                    DecrementButtonIconName=""BitIconName.DislikeSolid"" />
</div>
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<div class=""example-box"">
    <BitSpinButton Label=""Min: -10, Max: 10""
                    Min=""-10""
                    Max=""10"" />

    <BitSpinButton Label=""Min: -20, Max: 20, Step: 2""
                    Min=""-20""
                    Max=""20""
                    Step=""2"" />

    <BitSpinButton Label=""Min: -1, Max: 1, Step: 0.1""
                    Min=""-1""
                    Max=""1""
                    Step=""0.1"" />
</div>
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<div class=""example-box"">
    <BitSpinButton Label=""Height""
                    IconName=""BitIconName.AutoHeight""
                    DefaultValue=""150""
                    Suffix="" cm"" />

    <BitSpinButton Label=""Weight""
                    IconName=""BitIconName.Weights""
                    DefaultValue=""50""
                    Step=""0.5""
                    Suffix="" kg"" />
</div>
";

    #endregion

    #region Sample Code 6

    private readonly string example6HTMLCode = @"
<div class=""example-box"">
    <div>
        <BitSpinButton Label=""One-way"" Value=""OneWayValue"" />
        <BitRating @bind-Value=""OneWayValue"" />
    </div>

    <div>
        <BitSpinButton Label=""Two-way"" Step=""0.5"" @bind-Value=""TwoWayValue"" />
        <BitRating @bind-Value=""TwoWayValue"" />
    </div>
</div>
";

    private readonly string example6CSharpCode = @"
private double OneWayValue = 3;
private double TwoWayValue = 5;
";

    #endregion

    #region Sample Code 7

    private readonly string example7HTMLCode = @"
<div class=""column"">
    <BitSpinButton @bind-Value=""IncrementEventValue""
                    Label=""OnIncrement / OnDecrement""
                    Step=""0.1""
                    OnIncrement=""() => OnIncrementCounter++""
                    OnDecrement=""() => OnDecrementCounter++"" />
    <span>OnIncrement Counter: @OnIncrementCounter</span>
    <span>OnDecrement Counter: @OnDecrementCounter</span>
</div>

<div class=""column"">
    <BitSpinButton @bind-Value=""OnChangeEventBindedValue""
                    Label=""OnChange""
                    Step=""0.1"" 
                    OnChange=""HandleOnChangeEvent""/>
    <span>OnChange Clicked Counter: @OnChangeClickedCounter</span>
    <span>OnChange Returned Value: @OnChangeEventReturnedValue</span>
</div>
";

    private readonly string example7CSharpCode = @"
private double IncrementEventValue;
private int OnIncrementCounter;
private int OnDecrementCounter;

private double OnChangeEventBindedValue;
private double OnChangeEventReturnedValue;
private int OnChangeClickedCounter;
private void HandleOnChangeEvent(double value)
{
    OnChangeEventReturnedValue = value;

    OnChangeClickedCounter++;
}
";

    #endregion

    #region Sample Code 8

    private readonly string example8HTMLCode = @"
<div class=""example-box"">
    @if (string.IsNullOrEmpty(SuccessMessage))
    {
        <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">

            <DataAnnotationsValidator />

            <div class=""validation-summary"">
                <ValidationSummary />
            </div>

            <BitSpinButton Label=""Age"" @bind-Value=""@ValidationModel.AgeInYears""></BitSpinButton>
            <ValidationMessage For=""@(() => ValidationModel.AgeInYears)"" />

            <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">
                Submit
            </BitButton>
        </EditForm>
    }
    else
    {
        <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
            @SuccessMessage
        </BitMessageBar>
    }
</div>
";

    private readonly string example8CSharpCode = @"
public class BitSpinButtonValidationModel
{
    [Required(ErrorMessage = ""Enter an age"")]
    [Range(1, 200, ErrorMessage = ""Nobody is that old"")]
    public double AgeInYears { get; set; }
}

private string SuccessMessage = string.Empty;
private BitSpinButtonValidationModel ValidationModel = new();

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";

    #endregion
}
