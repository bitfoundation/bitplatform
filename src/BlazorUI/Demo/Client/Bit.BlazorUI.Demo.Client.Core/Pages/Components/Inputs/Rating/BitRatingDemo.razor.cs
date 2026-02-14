namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Rating;

public partial class BitRatingDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowZeroStars",
            Type = "bool",
            DefaultValue = "false",
            Description = "Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard.",
        },
        new()
        {
            Name = "AriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitRatingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitRating.",
            LinkType = LinkType.Link,
            Href = "#rating-class-styles",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "double?",
            DefaultValue = "null",
            Description = "Default rating. Must be a number between min and max. Only provide this if the Rating is an uncontrolled component; otherwise, use the rating property.",
        },
        new()
        {
            Name = "GetAriaLabel",
            Type = "Func<double, double, string>?",
            DefaultValue = "null",
            Description = "Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.",
        },
        new()
        {
            Name = "Max",
            Type = "int",
            DefaultValue = "5",
            Description = "Maximum rating. Must be >= min (0 if AllowZeroStars is true, 1 otherwise).",
        },
        new()
        {
            Name = "SelectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for selected rating elements using external icon libraries (e.g. FontAwesome, Bootstrap Icons). Takes precedence over SelectedIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "SelectedIconName",
            Type = "string?",
            DefaultValue = "FavoriteStarFill",
            Description = "Custom icon name for selected rating elements (Fluent UI). For external icon libraries, use SelectedIcon instead.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Size of rating elements.",
            LinkType = LinkType.Link,
            Href = "#ratingSize-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitRatingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitRating.",
            LinkType = LinkType.Link,
            Href = "#rating-class-styles",
        },
        new()
        {
            Name = "UnselectedIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Icon for unselected rating elements using external icon libraries (e.g. FontAwesome, Bootstrap Icons). Takes precedence over UnselectedIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "UnselectedIconName",
            Type = "string?",
            DefaultValue = "FavoriteStar",
            Description = "Custom icon name for unselected rating elements (Fluent UI). For external icon libraries, use UnselectedIcon instead.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "rating-class-styles",
            Title = "BitRatingClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the rating.",
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating's button.",
                },
                new()
                {
                    Name = "IconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating icon container.",
                },
                new()
                {
                    Name = "SelectedIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating selected icon.",
                },
                new()
                {
                    Name = "UnselectedIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the rating unselected icon.",
                }
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Description = "Represents icon information for rendering icons. Supports built-in Fluent UI icons and external icon libraries (FontAwesome, Bootstrap Icons, etc.). Use BitIconInfo.Css(\"fa-solid fa-star\"), BitIconInfo.Fa(\"solid star\"), or BitIconInfo.Bi(\"star-fill\") for external icons.",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon.",
                },
                new()
                {
                    Name = "BaseClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the base CSS class for the icon. For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty.",
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class prefix used before the icon name. For external icon libraries, you might set this to \"fa-\" or leave empty.",
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "ratingSize-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Small",
                    Description = "Display rating icon using small size.",
                    Value = "0",
                },
                new()
                {
                    Name = "Medium",
                    Description = "Display rating icon using medium size.",
                    Value = "1",
                },
                new()
                {
                    Name = "Large",
                    Description = "Display rating icon using large size.",
                    Value = "2",
                }
            ]
        }
    ];



    private double oneWayBinding = 0;
    private double twoWayBinding = 3;
    private double onChangeValue;

    public BitRatingDemoFormModel ValidationModel = new();
    public string? SuccessMessage;

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


    private readonly string example1RazorCode = @"
<BitRating />

<BitRating IsEnabled=""false"" DefaultValue=""2"" />

<BitRating ReadOnly DefaultValue=""3.5"" />";

    private readonly string example2RazorCode = @"
Visible: [ <BitRating Visibility=""""BitVisibility.Visible"""" /> ]
Hidden: [ <BitRating Visibility=""""BitVisibility.Hidden"""" /> ]
Collapsed: [ <BitRating Visibility=""""BitVisibility.Collapsed"""" /> ]";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 0.25rem;
        padding-inline: 0.5rem;
        border: 1px solid dodgerblue;
        box-shadow: dodgerblue 0 0 1rem;
    }

    .custom-selected {
        color: seagreen;
    }

    .custom-unselected {
        color: mediumseagreen;
    }

    .custom-unselected:hover {
        color: lightseagreen;
    }
</style>


<BitRating Style=""padding-inline: 0.5rem; margin-inline: 1rem; box-shadow: tomato 0 0 1rem; border-radius: 1rem;"" />

<BitRating Class=""custom-class"" />


<BitRating Styles=""@(new() { SelectedIcon = ""color: blueviolet;"", UnselectedIcon = ""color: blueviolet;"" })"" />

<BitRating Classes=""@(new() { SelectedIcon = ""custom-selected"", UnselectedIcon = ""custom-unselected"" })"" />";

    private readonly string example4RazorCode = @"
<BitRating Max=""6"" />

<div style=""width: 200px;"">
    <BitRating Max=""100"" />
</div>";

    private readonly string example5RazorCode = @"
<BitRating SelectedIconName=""@BitIconName.HeartFill"" UnselectedIconName=""@BitIconName.Heart"" />

<BitRating SelectedIconName=""@BitIconName.CheckboxCompositeReversed"" UnselectedIconName=""@BitIconName.Checkbox"" />

<BitRating SelectedIconName=""@BitIconName.LikeSolid"" UnselectedIconName=""@BitIconName.Dislike"" />";

    private readonly string example6RazorCode = @"
<BitRating Size=""BitSize.Small"" />

<BitRating Size=""BitSize.Medium"" />

<BitRating Size=""BitSize.Large"" />";

    private readonly string example7RazorCode = @"
<BitRating AllowZeroStars=""true"" Value=""oneWayBinding"" />
<BitToggleButton OnChange=""v => oneWayBinding = v ? 5 : 0"" Text=""@(oneWayBinding == 5 ? ""Unstar All"" : ""Star All"")"" />

<BitRating @bind-Value=""twoWayBinding"" />
<BitNumberField @bind-Value=""twoWayBinding"" />

<BitRating DefaultValue=""2"" OnChange=""v => onChangeValue = v"" />
<BitLabel>Changed Value: @onChangeValue</BitLabel>";
    private readonly string example7CsharpCode = @"
private double oneWayBinding = 0;
private double twoWayBinding = 3;
private double onChangeValue;";

    private readonly string example8RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""ValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">

    <DataAnnotationsValidator />

    <BitRating AllowZeroStars=""true"" @bind-Value=""ValidationModel.Value"" />
    <ValidationMessage For=""@(() => ValidationModel.Value)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class BitRatingDemoFormModel
{
    [Range(typeof(double), ""1"", ""5"", ErrorMessage = ""Your rate must be between {1} and {2}"")]
    public double Value { get; set; }
}

public BitRatingDemoFormModel ValidationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example9RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitRating SelectedIcon=""@(""fa-solid fa-star"")"" UnselectedIcon=""@(""fa-regular fa-star"")"" />

<BitRating SelectedIcon=""@BitIconInfo.Fa(""solid heart"")"" UnselectedIcon=""@BitIconInfo.Fa(""regular heart"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitRating SelectedIcon=""@BitIconInfo.Css(""bi bi-star-fill"")"" UnselectedIcon=""@BitIconInfo.Css(""bi bi-star"")"" />

<BitRating SelectedIcon=""@BitIconInfo.Bi(""heart-fill"")"" UnselectedIcon=""@BitIconInfo.Bi(""heart"")"" />";

    private readonly string example10RazorCode = @"
<BitRating Dir=""BitDir.Rtl"" />";
}
