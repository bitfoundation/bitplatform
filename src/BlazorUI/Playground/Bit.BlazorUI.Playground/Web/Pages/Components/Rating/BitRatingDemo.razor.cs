using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Rating
{
    public partial class BitRatingDemo
    {
        private string SuccessMessage = string.Empty;
        public BitRatingDemoFormModel ValidationForm { get; set; }

        private string RatingChangedText = string.Empty;

        private double RatingBoundValue = 2;
        private double RatingLargeValue = 3;
        private double RatingSmallValue = 0;
        private double RatingReadOnlyValue = 2.5;
        private double RatingCustomIconValue = 2.5;
        private double RatingOutsideValue = 0;

        protected override void OnInitialized()
        {
            ValidationForm = new BitRatingDemoFormModel();
        }

        private async void HandleValidSubmit()
        {
            SuccessMessage = "Form Submitted Successfully!";
            await Task.Delay(3000);
            SuccessMessage = string.Empty;
            ValidationForm.Value = default;
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
                Name = "ValueChanged",
                Type = "EventCallback<double>",
                DefaultValue = "",
                Description = "Callback that is called when the rating value changed.",
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
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
            },
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
                        Name= "Small",
                        Description="Display rating icon using small size.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Large",
                        Description=" Display rating icon using large size.",
                        Value="1",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "component-visibility-enum",
                Title = "BitComponentVisibility Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Visible",
                        Description="Show content of the component.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Hidden",
                        Description="Hide content of the component,though the space it takes on the page remains.",
                        Value="1",
                    },
                    new EnumItem()
                    {
                        Name= "Collapsed",
                        Description="Hide content of the component,though the space it takes on the page gone.",
                        Value="2",
                    }
                }
            }
        };

        private readonly string example1HTMLCode = @"<div>Large stars</div>
<BitRating Size=""BitRatingSize.Large"" @bind-Rating=""RatingLargeValue"" AriaLabelFormat=""Select {0} of {1} stars"" />
<div>Small stars, with 0 stars allowed</div>
<BitRating AllowZeroStars=""true"" @bind-Rating=""RatingSmallValue"" />
<div>10 small stars</div>
<div>
    <BitRating Max=""10"" OnChange=""@(v => RatingChangedText = $""Rating value changed to {v}"")"" @bind-Rating=""RatingBoundValue"" />
    <div>
        <span>@RatingChangedText</span>
    </div>
    <div>Disabled</div>
    <BitRating IsEnabled=""false"" @bind-Rating=""RatingReadOnlyValue"" />
    <div>Half star in readOnly mode</div>
    <BitRating IsReadOnly=""true"" @bind-Rating=""RatingReadOnlyValue"" GetAriaLabel=""@((value, max) => $""Half star in readOnly mode rating value is {value.ToString()} of {max.ToString()}"")"" />
    <div>Custom icons</div>
    <BitRating Icon=""BitIconName.HeartFill"" UnselectedIcon=""BitIconName.Heart"" AllowZeroStars=""true"" @bind-Rating=""RatingCustomIconValue"" />
</div>";

        private readonly string example1CSharpCode = @"
private string RatingChangedText = string.Empty;
private double RatingBoundValue = 2;
private double RatingLargeValue = 3;
private double RatingSmallValue = 0;
private double RatingReadOnlyValue = 2.5;
private double RatingCustomIconValue = 2.5;";

        private readonly string example2HTMLCode = @"<div>
    <BitRating Icon=""BitIconName.Emoji2"" UnselectedIcon=""BitIconName.EmojiNeutral"" AllowZeroStars=""true"" @bind-Rating=""RatingOutsideValue"" />
    <div>
        <span>RatingOutsideValue: @RatingOutsideValue</span>
    </div>
    <div>
        <BitButton OnClick=""() => RatingOutsideValue = 5"">Fully satisfied</BitButton>
        <BitButton OnClick=""() => RatingOutsideValue = 0"">Have no idea</BitButton>
    </div>
</div>";

        private readonly string example2CSharpCode = @"
private double RatingOutsideValue = 0; ";

        private readonly string example3HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
                {
                    <EditForm Model=""ValidationForm"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator/>

        <BitRating AllowZeroStars=""true"" @bind-Value=""ValidationForm.Value""/>
        <ValidationMessage For=""@(() => ValidationForm.Value)"" />

            <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
        </EditForm>
    }
    else
    {
        <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
            @SuccessMessage
            </BitMessageBar>
    }";

        private readonly string example3CSharpCode = @"private string SuccessMessage = string.Empty;
        public BitRatingDemoFormModel ValidationForm { get; set; }

        public class BitRatingDemoFormModel
        {
            [Range(typeof(double), ""1"", ""5"", ErrorMessage=""Your rate must be between {1}
        and {0}
        "")]
        public double Value { get; set; }
    }

    protected override void OnInitialized()
    {
        ValidationForm = new BitRatingDemoFormModel();
    }

    private async void HandleValidSubmit()
    {
        SuccessMessage = ""Form Submitted Successfully!"";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        ValidationForm.Value = default;
        StateHasChanged();
    }";
    }
}
