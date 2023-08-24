namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.SpinButton;

public partial class BitSpinButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the input for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            DefaultValue = "null",
            Description = "The position in the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total size of the parent set (if in a set).",
        },
        new()
        {
            Name = "AriaValueNow",
            Type = "double?",
            DefaultValue = "null",
            Description = "Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.",
        },
        new()
        {
            Name = "AriaValueText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Sets the control's aria-valuetext.",
        },
        new()
        {
            Name = "ChangeHandler",
            Type = "EventCallback<BitSpinButtonAction>",
            Description = "",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "Initial value of the spin button.",
        },
        new()
        {
            Name = "DecrementButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label text for the decrement button (for screen reader users).",
        },
        new()
        {
            Name = "DecrementButtonIconName",
            Type = "string",
            DefaultValue = "ChevronDownSmall",
            Description = "Custom icon name for the decrement button.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Icon name for an icon to display alongside the spin button's label.",
        },
        new()
        {
            Name = "IconAriaLabel",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new()
        {
            Name = "IncrementButtonAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Accessible label text for the increment button (for screen reader users).",
        },
        new()
        {
            Name = "IncrementButtonIconName",
            Type = "string",
            DefaultValue = "ChevronUpSmall",
            Description = "Custom icon name for the increment button.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Descriptive label for the spin button, Label displayed above the spin button and read by screen readers.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom Label for spin button. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id..",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitSpinButtonLabelPosition",
            LinkType = LinkType.Link,
            Href = "#labelPosition-enum",
            DefaultValue = "BitSpinButtonLabelPosition.Top",
            Description = "The position of the label in regards to the spin button.",
        },
        new()
        {
            Name = "Min",
            Type = "double?",
            DefaultValue = "null",
            Description = "Min value of the spin button. If not provided, the spin button has minimum value of double type.",
        },
        new()
        {
            Name = "Max",
            Type = "double?",
            DefaultValue = "null",
            Description = "Max value of the spin button. If not provided, the spin button has max value of double type.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnBlur",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when the control loses focus.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            Description = "Callback for when the spin button value change.",
        },
        new()
        {
            Name = "OnDecrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            Description = "Callback for when the decrement button or down arrow key is pressed.",
        },
        new()
        {
            Name = "OnIncrement",
            Type = "EventCallback<BitSpinButtonChangeEventArgs>",
            Description = "Callback for when the increment button or up arrow key is pressed.",
        },
        new()
        {
            Name = "Precision",
            Type = "int?",
            DefaultValue = "null",
            Description = "How many decimal places the value should be rounded to.",
        },
        new()
        {
            Name = "Step",
            Type = "double",
            DefaultValue = "1",
            Description = "Difference between two adjacent values of the spin button.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "A text is shown after the spin button value.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "A more descriptive title for the control, visible on its tooltip.",
        },
    };
    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "labelPosition-enum",
            Name = "BitSpinButtonLabelPosition",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spin button.",
                    Value="0",
                },
                new()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spin button.",
                    Value="1",
                }
            }
        }
    };



    private readonly string example1HTMLCode = @"
<BitSpinButton Label=""Basic"" />

<BitSpinButton Label=""Disabled"" IsEnabled=""false"" />";

    private readonly string example2HTMLCode = @"
<BitSpinButton Label=""Label & Icon"" IconName=""@BitIconName.Lightbulb"" />

<BitSpinButton Label=""Left Label"" IconName=""@BitIconName.Lightbulb"" LabelPosition=""BitSpinButtonLabelPosition.Left"" />";

    private readonly string example3HTMLCode = @"
<BitSpinButton>
    <LabelTemplate>
        <div style=""display:flex; align-items: center; gap: 10px"">
            <BitLabel Style=""color: green;"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitSpinButton>";

    private readonly string example4HTMLCode = @"
<BitSpinButton Label=""Like and Dislike""
               IncrementButtonIconName=""@BitIconName.LikeSolid""
               DecrementButtonIconName=""@BitIconName.DislikeSolid"" />";

    private readonly string example5HTMLCode = @"
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
               Step=""0.1"" />";

    private readonly string example6HTMLCode = @"
<BitSpinButton Label=""Height""
               IconName=""@BitIconName.AutoHeight""
               DefaultValue=""150""
               Suffix="" cm"" />

<BitSpinButton Label=""Weight""
               IconName=""@BitIconName.Weights""
               DefaultValue=""50""
               Step=""0.5""
               Suffix="" kg"" />";

    private readonly string example7HTMLCode = @"
<BitSpinButton Label=""One-way"" Value=""OneWayValue"" />
<BitRating @bind-Value=""OneWayValue"" />

<BitSpinButton Label=""Two-way"" Step=""0.5"" @bind-Value=""TwoWayValue"" />
<BitRating @bind-Value=""TwoWayValue"" />";
    private readonly string example7CSharpCode = @"
private double OneWayValue = 3;
private double TwoWayValue = 5;";

    private readonly string example8HTMLCode = @"
<BitSpinButton @bind-Value=""IncrementEventValue""
               Label=""OnIncrement / OnDecrement""
               Step=""0.1""
               OnIncrement=""() => OnIncrementCounter++""
               OnDecrement=""() => OnDecrementCounter++"" />
<BitLabel>OnIncrement Counter: @OnIncrementCounter</BitLabel>
<BitLabel>OnDecrement Counter: @OnDecrementCounter</BitLabel>

<BitSpinButton @bind-Value=""OnChangeEventBindedValue""
               Label=""OnChange""
               Step=""0.1"" 
               OnChange=""HandleOnChangeEvent""/>
<BitLabel>OnChange Clicked Counter: @OnChangeClickedCounter</BitLabel>
<BitLabel>OnChange Returned Value: @OnChangeEventReturnedValue</BitLabel>";
    private readonly string example8CSharpCode = @"
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
}";

    private readonly string example9HTMLCode = @"
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
}";
    private readonly string example9CSharpCode = @"
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
}";
}
