using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Rating;

public partial class BitRatingDemo
{
    private double RatingBasicValue;
    private double RatingDisabledValue = 2;
    private double RatingReadonlyValue = 3.5;

    private double RatingMaxValue1 = 2.5;
    private double RatingMaxValue2 = 5;
    private double RatingMaxValue3 = 15;

    private double RatingCustomIconValue1 = 1.5;
    private double RatingCustomIconValue2 = 2;
    private double RatingCustomIconValue3 = 3;

    private double RatingSmallValue = 3;
    private double RatingLargeValue = 3;

    private double RatingControlledValue1 = 0;
    private double RatingControlledValue2 = 3;
    private double RatingControlledValue3;

    public BitRatingDemoFormModel ValidationModel = new();
    public string SuccessMessage;

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(2000);
        SuccessMessage = string.Empty;
        ValidationModel.Value = default;
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
            Name = "AllowZeroStars",
            Type = "bool",
            DefaultValue = "false",
            Description = "Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard.",
        },
        new ComponentParameter()
        {
            Name = "AriaLabelFormat",
            Type = "string",
            DefaultValue = "",
            Description = "Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "double",
            DefaultValue = "0",
            Description = "Default rating. Must be a number between min and max. Only provide this if the Rating is an uncontrolled component; otherwise, use the rating property.",
        },
        new ComponentParameter()
        {
            Name = "GetAriaLabel",
            Type = "Func<double, double, string>",
            DefaultValue = "",
            Description = "Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.",
        },
        new ComponentParameter()
        {
            Name = "Icon",
            Type = "BitIcon",
            DefaultValue = "FavoriteStarFill",
            Description = "Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon.",
        },
        new ComponentParameter()
        {
            Name = "IsReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "A flag to mark rating control as readOnly.",
        },
        new ComponentParameter()
        {
            Name = "Max",
            Type = "int",
            DefaultValue = "5",
            Description = "Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise).",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<double>",
            DefaultValue = "",
            Description = "Callback that is called when the rating has changed.",
        },
        new ComponentParameter()
        {
            Name = "Value",
            Type = "double",
            DefaultValue = "0",
            Description = "Current rating value. Must be a number between min (0 if AllowZeroStars is true, 1 otherwise) and max.",
        },
        new ComponentParameter()
        {
            Name = "Size",
            Type = "BitRatingSize",
            DefaultValue = "BitRatingSize.small",
            LinkType = LinkType.Link,
            Href = "#rating-size-enum",
            Description = "Size of rating.",
        },
        new ComponentParameter()
        {
            Name = "UnselectedIcon",
            Type = "BitIconName",
            DefaultValue = "BitIconName.FavoriteStar",
            Description = "Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon.",
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "rating-size-enum",
            Title = "BitRatingSize Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Small",
                    Description = "Display rating icon using small size.",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Large",
                    Description = " Display rating icon using large size.",
                    Value = "1",
                },
            }
        }
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"
<div>
    <BitLabel>Basic:</BitLabel>
    <BitRating @bind-Value=""RatingBasicValue"" />
    <span>Rate: @RatingBasicValue</span>
</div>
<div>
    <BitLabel>Disabled:</BitLabel>
    <BitRating IsEnabled=""false"" @bind-Value=""RatingDisabledValue"" />
    <span>Rate: @RatingDisabledValue</span>
</div>
<div>
    <BitLabel>Readonly:</BitLabel>
    <BitRating IsReadOnly=""true"" @bind-Value=""RatingReadonlyValue"" />
    <span>Rate: @RatingReadonlyValue</span>
</div>
";

    private readonly string example1CSharpCode = @"
private double RatingBasicValue;
private double RatingDisabledValue = 2;
private double RatingReadonlyValue = 3.5;
";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"
<div>
    <BitLabel>Max is 6</BitLabel>
    <BitRating Max=""6"" @bind-Value=""RatingMaxValue1"" />
    <span>Rate: @RatingMaxValue1</span>
</div>
<div>
    <BitLabel>Max is 10</BitLabel>
    <BitRating Max=""10"" @bind-Value=""RatingMaxValue2"" />
    <span>Rate: @RatingMaxValue2</span>
</div>
<div style=""width: 200px;"">
    <BitLabel>Max is 100</BitLabel>
    <BitRating Max=""100"" @bind-Value=""RatingMaxValue3"" />
    <span>Rate: @RatingMaxValue3</span>
</div>
";

    private readonly string example2CSharpCode = @"
private double RatingMaxValue1 = 2.5;
private double RatingMaxValue2 = 5;
private double RatingMaxValue3 = 15;
";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<div>
    <BitLabel>Heart:</BitLabel>
    <BitRating Icon=""BitIconName.HeartFill"" UnselectedIcon=""BitIconName.Heart"" @bind-Value=""RatingCustomIconValue1"" />
    <span>Rate: @RatingCustomIconValue1</span>
</div>
<div>
    <BitLabel>Checkbox:</BitLabel>
    <BitRating Icon=""BitIconName.CheckboxCompositeReversed"" UnselectedIcon=""BitIconName.Checkbox"" @bind-Value=""RatingCustomIconValue2"" />
    <span>Rate: @RatingCustomIconValue2</span>
</div>
<div>
    <BitLabel>Like:</BitLabel>
    <BitRating Icon=""BitIconName.LikeSolid"" UnselectedIcon=""BitIconName.Dislike"" @bind-Value=""RatingCustomIconValue3"" />
    <span>Rate: @RatingCustomIconValue3</span>
</div>
";

    private readonly string example3CSharpCode = @"
private double RatingCustomIconValue1 = 1.5;
private double RatingCustomIconValue2 = 2;
private double RatingCustomIconValue3 = 3;
";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"
<div>
    <BitLabel>Small:</BitLabel>
    <BitRating Size=""BitRatingSize.Small"" @bind-Value=""RatingSmallValue"" />
    <span>Rate: @RatingSmallValue</span>
</div>
<div>
    <BitLabel>Large:</BitLabel>
    <BitRating Size=""BitRatingSize.Large"" @bind-Value=""RatingLargeValue"" />
    <span>Rate: @RatingLargeValue</span>
</div>
";

    private readonly string example4CSharpCode = @"
private double RatingSmallValue = 3;
private double RatingLargeValue = 3;
";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
<div>
    <BitLabel>One-way:</BitLabel>
    <BitRating AllowZeroStars=""true"" Value=""RatingControlledValue1"" />
    <BitToggleButton OnChange=""(v) =>  RatingControlledValue1 = v ? 5 : 0"" Label=""@(RatingControlledValue1 == 5 ? ""Unstar All"" : ""Star All"")"" />
</div>
<div>
    <BitLabel>Two-way:</BitLabel>
    <BitRating Max=""6"" @bind-Value=""RatingControlledValue2"" />
    <BitSpinButton Step=""0.1"" @bind-Value=""RatingControlledValue2"" />
</div>
<div>
    <BitLabel>OnChange:</BitLabel>
    <BitRating DefaultValue=""2"" OnChange=""(v) => RatingControlledValue3 = v"" />
    <span>Rate: @RatingControlledValue3</span>
</div>
";

    private readonly string example5CSharpCode = @"
private double RatingControlledValue1 = 0;
private double RatingControlledValue2 = 3;
private double RatingControlledValue3;
";

    #endregion

    #region Example Code 6

    private readonly string example6HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""ValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">

        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <BitRating AllowZeroStars=""true"" @bind-Value=""ValidationModel.Value"" />
        <ValidationMessage For=""@(() => ValidationModel.Value)"" />

        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}
";

    private readonly string example6CSharpCode = @"
public class BitRatingDemoFormModel
{
    [Range(typeof(double), ""1"", ""5"", ErrorMessage = ""Your rate must be between {1} and {2}"")]
    public double Value { get; set; }
}

public BitRatingDemoFormModel ValidationModel = new();
public string SuccessMessage;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(2000);
    SuccessMessage = string.Empty;
    ValidationModel.Value = default;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";

    #endregion
}
