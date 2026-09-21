namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Rating;

public partial class BitRatingDemo
{
    private readonly string example1RazorCode = @"
<BitRating />

<BitRating DefaultValue=""3"" />

<BitRating IsEnabled=""false"" DefaultValue=""2"" />

<BitRating ReadOnly DefaultValue=""3.5"" />";

    private readonly string example2RazorCode = @"
<BitRating Required
           Label=""How would you rate your stay?""
           Description=""Hover a star to see what it means.""
           ItemTitles=""@([""Terrible"", ""Bad"", ""Normal"", ""Good"", ""Wonderful""])""
           @bind-Value=""labelValue"" />

<BitRating Label=""Cleanliness"" LabelPosition=""BitLabelPosition.Start"" DefaultValue=""4"" />

<BitRating LabelPosition=""BitLabelPosition.Bottom"" @bind-Value=""labelTemplateValue"">
    <LabelTemplate>
        <BitText Typography=""BitTypography.Caption1"">@ratingWords[(int)labelTemplateValue]</BitText>
    </LabelTemplate>
</BitRating>";
    private readonly string example2CsharpCode = @"
private double labelValue = 3;
private double labelTemplateValue = 4;

private readonly string[] ratingWords = [""Not rated yet"", ""Terrible"", ""Bad"", ""Normal"", ""Good"", ""Wonderful""];";

    private readonly string example3RazorCode = @"
<div style=""display:flex;align-items:center;gap:0.5rem"">
    <BitRating Max=""1"" ReadOnly DefaultValue=""1"" />
    <BitLabel>4.2 · 1,034 reviews</BitLabel>
</div>

<BitRating Max=""3"" DefaultValue=""2"" />

<BitRating Max=""6"" DefaultValue=""4"" />

<div style=""width: 200px;"">
    <BitRating Max=""100"" DefaultValue=""37"" />
</div>";

    private readonly string example4RazorCode = @"
<BitRating Vertical @bind-Value=""verticalValue"" />
<BitLabel>Value: @verticalValue</BitLabel>

<BitRating Vertical Precision=""0.5"" Size=""BitSize.Large"" @bind-Value=""verticalPrecisionValue"" />
<BitLabel>Value: @verticalPrecisionValue</BitLabel>";
    private readonly string example4CsharpCode = @"
private double verticalValue = 3;
private double verticalPrecisionValue = 3.5;";

    private readonly string example5RazorCode = @"
<BitRating Precision=""0.5"" @bind-Value=""halfPrecisionValue"" />
<BitLabel>Value: @halfPrecisionValue</BitLabel>

<BitRating Precision=""0.25"" @bind-Value=""quarterPrecisionValue"" />
<BitLabel>Value: @quarterPrecisionValue</BitLabel>

<BitRating Precision=""0.1"" Size=""BitSize.Large"" @bind-Value=""exactPrecisionValue"" />
<BitLabel>Value: @exactPrecisionValue</BitLabel>";
    private readonly string example5CsharpCode = @"
private double halfPrecisionValue = 2.5;
private double quarterPrecisionValue = 3.25;
private double exactPrecisionValue = 3.7;";

    private readonly string example6RazorCode = @"
<BitRating @bind-Value=""noZeroValue"" />
<BitLabel>Value: @noZeroValue</BitLabel>

<BitRating AllowZeroStars @bind-Value=""allowZeroValue"" />
<BitLabel>Value: @allowZeroValue</BitLabel>

<BitRating AllowClear @bind-Value=""allowClearValue"" />
<BitLabel>Value: @allowClearValue</BitLabel>";
    private readonly string example6CsharpCode = @"
private double noZeroValue;
private double allowZeroValue;
private double allowClearValue = 3;";

    private readonly string example7RazorCode = @"
<BitRating @bind-Value=""highlightValue"" />

<BitRating HighlightSelectedOnly @bind-Value=""highlightValue"" />

<BitLabel>Value: @highlightValue</BitLabel>";
    private readonly string example7CsharpCode = @"
private double highlightValue = 3;";

    private readonly string example8RazorCode = @"
<BitRating LabelPosition=""BitLabelPosition.Bottom""
           @bind-Value=""hoverBoundValue""
           OnHoverChange=""v => hoverPreviewValue = v"">
    <LabelTemplate>
        <BitText Typography=""BitTypography.Caption1"">@ratingWords[(int)(hoverPreviewValue ?? hoverBoundValue)]</BitText>
    </LabelTemplate>
</BitRating>

<BitRating NoHoverPreview DefaultValue=""2"" />";
    private readonly string example8CsharpCode = @"
private double hoverBoundValue = 3;
private double? hoverPreviewValue;

private readonly string[] ratingWords = [""Not rated yet"", ""Terrible"", ""Bad"", ""Normal"", ""Good"", ""Wonderful""];";

    private readonly string example9RazorCode = @"
<BitRating DefaultValue=""3"" SelectedIconName=""@BitIconName.HeartFill"" UnselectedIconName=""@BitIconName.Heart"" />

<BitRating DefaultValue=""3"" SelectedIconName=""@BitIconName.CheckboxCompositeReversed"" UnselectedIconName=""@BitIconName.Checkbox"" />

<BitRating @bind-Value=""perItemIconValue""
           GetSelectedIcon=""i => BitIconInfo.Bit(i > 2 ? BitIconName.LikeSolid : BitIconName.DislikeSolid)""
           GetUnselectedIcon=""i => BitIconInfo.Bit(i > 2 ? BitIconName.Like : BitIconName.Dislike)"" />
<BitLabel>Value: @perItemIconValue</BitLabel>

<BitRating HighlightSelectedOnly
           Size=""BitSize.Large""
           @bind-Value=""faceValue""
           GetSelectedIcon=""GetFaceIcon""
           GetUnselectedIcon=""GetFaceIcon""
           ItemTitles=""@([""Terrible"", ""Bad"", ""Normal"", ""Good"", ""Wonderful""])"" />
<BitLabel>Value: @faceValue</BitLabel>";
    private readonly string example9CsharpCode = @"
private double perItemIconValue = 4;
private double faceValue = 3;

private readonly string[] faceIcons =
[
    BitIconName.EmojiDisappointed,
    BitIconName.Sad,
    BitIconName.EmojiNeutral,
    BitIconName.Emoji,
    BitIconName.Emoji2
];

private BitIconInfo GetFaceIcon(int index) => BitIconInfo.Bit(faceIcons[index - 1]);";

    private readonly string example10RazorCode = @"
<BitRating Max=""10"" @bind-Value=""templateValue"">
    <ItemTemplate Context=""item"">
        <span class=""number-item @(item.IsFull ? ""number-item-on"" : null)"">@item.Index</span>
    </ItemTemplate>
</BitRating>
<BitLabel>Value: @templateValue</BitLabel>

<BitRating HighlightSelectedOnly @bind-Value=""moodValue"">
    <ItemTemplate Context=""item"">
        <span class=""emoji-item @(item.IsFull ? ""emoji-item-on"" : null)"">@moodFaces[item.Index - 1]</span>
    </ItemTemplate>
</BitRating>
<BitLabel>Value: @moodValue</BitLabel>";
    private readonly string example10CsharpCode = @"
private double templateValue = 7;
private double moodValue = 4;
private readonly string[] moodFaces = [""😖"", ""😐"", ""🙂"", ""😀"", ""🤩""];";
    private const string example10ScssCode = @"
// A plain badge that fills once its item does.
.number-item {
    width: 1.75rem;
    height: 1.75rem;
    line-height: 1;
    font-size: 0.875rem;
    align-items: center;
    display: inline-flex;
    border-radius: 0.25rem;
    justify-content: center;
    color: $bit-color-foreground-tertiary;
    border: 1px solid $bit-color-border-primary;
}

.number-item-on {
    color: $bit-color-primary-text;
    border-color: $bit-color-primary;
    background-color: $bit-color-primary;
}

// The mood faces, greyed out until their item is the selected one.
.emoji-item {
    opacity: 0.4;
    line-height: 1;
    font-size: 1.75rem;
    display: inline-block;
    filter: grayscale(1);
}

.emoji-item-on {
    opacity: 1;
    filter: none;
}";
    private readonly DemoCodeFile[] example10CodeFiles =
    [
        new("BitRatingDemo.razor.scss", example10ScssCode),
    ];

    private readonly string example11RazorCode = @"
<BitRating AllowZeroStars Value=""oneWayBinding"" />
<BitToggleButton OnChange=""v => oneWayBinding = v ? 5 : 0"" Text=""@(oneWayBinding == 5 ? ""Unstar All"" : ""Star All"")"" />

<BitRating @bind-Value=""twoWayBinding"" />
<BitNumberField Step=""0.5"" @bind-Value=""twoWayBinding"" />";
    private readonly string example11CsharpCode = @"
private double oneWayBinding = 0;
private double twoWayBinding = 3;";

    private readonly string example12RazorCode = @"
<BitRating DefaultValue=""2"" OnChange=""v => onChangeValue = v"" />
<BitLabel>Changed value: @onChangeValue</BitLabel>

<BitRating @bind-Value=""onChangingValue"" OnChanging=""HandleOnChanging"" />
<BitLabel>Value: @onChangingValue @(changeRejected ? ""(lowering was rejected)"" : """")</BitLabel>";
    private readonly string example12CsharpCode = @"
private double onChangeValue;
private double onChangingValue = 3;
private bool changeRejected;

private void HandleOnChanging(BitRatingChangeArgs args)
{
    changeRejected = args.Value < args.OldValue;

    args.Cancel = changeRejected;
}";

    private readonly string example13RazorCode = @"
<EditForm Model=""ValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">

    <DataAnnotationsValidator />

    <BitRating Required AllowZeroStars Label=""Your rate"" @bind-Value=""ValidationModel.Value"" />
    <ValidationMessage For=""@(() => ValidationModel.Value)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class BitRatingDemoFormModel
{
    [Range(typeof(double), ""1"", ""5"", ErrorMessage = ""Your rate must be between {1} and {2}"")]
    public double Value { get; set; }
}

public BitRatingDemoFormModel ValidationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";
    private const string example13ScssCode = @"
.validation-message {
    color: $bit-color-error;
}";
    private readonly DemoCodeFile[] example13CodeFiles =
    [
        new("BitRatingDemo.razor.scss", example13ScssCode),
    ];

    private readonly string example14RazorCode = @"
<BitRating AllowClear
           Precision=""0.5""
           AriaLabel=""Rate this product""
           AriaLabelFormat=""Select {0} of {1} stars""
           ValueTextFormat=""{0} out of {1} stars""
           @bind-Value=""accessibilityValue"" />
<BitLabel>Value: @accessibilityValue</BitLabel>

<BitRating ReadOnly DefaultValue=""4.2"" GetAriaLabel=""GetRatingAriaLabel"" />

<div id=""rating-external-label"">Overall satisfaction</div>
<BitRating AriaLabelledBy=""rating-external-label"" DefaultValue=""4"" />";
    private readonly string example14CsharpCode = @"
private double accessibilityValue = 3;

private string GetRatingAriaLabel(double value, double max) => $""Rated {value} out of {max}"";";

    private readonly string example15RazorCode = @"
Visible: [ <BitRating Visibility=""BitVisibility.Visible"" /> ]
Hidden: [ <BitRating Visibility=""BitVisibility.Hidden"" /> ]
Collapsed: [ <BitRating Visibility=""BitVisibility.Collapsed"" /> ]";

    private readonly string example16RazorCode = @"
<BitRating Color=""GetScoreColor()"" @bind-Value=""scoreValue"" />
<BitLabel>@scoreWords[(int)scoreValue]</BitLabel>

<BitRating Color=""GetScoreColor()""
           GetSelectedIcon=""GetScoreIcon""
           GetUnselectedIcon=""GetScoreIcon""
           @bind-Value=""scoreValue"" />";
    private readonly string example16CsharpCode = @"
private double scoreValue = 2;
private readonly string[] scoreWords = [""Unrated"", ""Poor"", ""Poor"", ""Okay"", ""Great"", ""Great""];

private BitColor GetScoreColor() => scoreValue switch
{
    <= 2 => BitColor.Error,
    <= 3 => BitColor.Warning,
    _ => BitColor.Success
};

private BitIconInfo GetScoreIcon(int index) => BitIconInfo.Bit(scoreValue switch
{
    <= 2 => BitIconName.Sad,
    <= 3 => BitIconName.EmojiNeutral,
    _ => BitIconName.Emoji2
});";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""@ratingParams"">
    <BitRating Label=""Comfort"" DefaultValue=""4"" />

    <BitRating Label=""Value for money"" DefaultValue=""3.5"" />

    <BitRating Label=""Rate it yourself"" ReadOnly=""false"" Color=""BitColor.Success"" @bind-Value=""cascadeValue"" />
</BitParams>


<BitRating DefaultValue=""4"" />";
    private readonly string example17CsharpCode = @"
private double cascadeValue = 3;

private readonly BitRatingParams[] ratingParams =
[
    new()
    {
        ReadOnly = true,
        Precision = 0.5,
        Size = BitSize.Small,
        Color = BitColor.Warning,
        SelectedIconName = BitIconName.HeartFill,
        UnselectedIconName = BitIconName.Heart,
        LabelPosition = BitLabelPosition.Start
    }
];";

    private readonly string example18RazorCode = @"
<BitRating Color=""BitColor.Primary"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Secondary"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Tertiary"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Info"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Success"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Warning"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.SevereWarning"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.Error"" DefaultValue=""3.5"" />

<BitRating Color=""BitColor.PrimaryBackground"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.SecondaryBackground"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.TertiaryBackground"" DefaultValue=""3.5"" />

<BitRating Color=""BitColor.PrimaryForeground"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.SecondaryForeground"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.TertiaryForeground"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.PrimaryBorder"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.SecondaryBorder"" DefaultValue=""3.5"" />
<BitRating Color=""BitColor.TertiaryBorder"" DefaultValue=""3.5"" />";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitRating DefaultValue=""3.5"" SelectedIcon=""@(""fa-solid fa-star"")"" UnselectedIcon=""@(""fa-regular fa-star"")"" />

<BitRating DefaultValue=""3.5"" SelectedIcon=""@BitIconInfo.Fa(""solid heart"")"" UnselectedIcon=""@BitIconInfo.Fa(""regular heart"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitRating DefaultValue=""3.5"" SelectedIcon=""@BitIconInfo.Css(""bi bi-star-fill"")"" UnselectedIcon=""@BitIconInfo.Css(""bi bi-star"")"" />

<BitRating DefaultValue=""3.5"" SelectedIcon=""@BitIconInfo.Bi(""heart-fill"")"" UnselectedIcon=""@BitIconInfo.Bi(""heart"")"" />";

    private readonly string example20RazorCode = @"
<BitRating Size=""BitSize.Small"" DefaultValue=""3"" />

<BitRating Size=""BitSize.Medium"" DefaultValue=""3"" />

<BitRating Size=""BitSize.Large"" DefaultValue=""3"" />";

    private readonly string example21RazorCode = @"
<BitRating DefaultValue=""3"" Style=""padding-inline: 0.5rem; margin-inline: 1rem; box-shadow: tomato 0 0 1rem; border-radius: 1rem;"" />

<BitRating DefaultValue=""3"" Class=""custom-class"" />


<BitRating DefaultValue=""3.5"" Styles=""@(new() { SelectedIcon = ""color: blueviolet;"", UnselectedIcon = ""color: plum;"" })"" />

<BitRating Label=""Classes"" DefaultValue=""3.5"" Classes=""@(new() { Label = ""custom-label"", SelectedIcon = ""custom-selected"", UnselectedIcon = ""custom-unselected"" })"" />


<BitRating DefaultValue=""3.5"" Style=""--bit-Rating-color: goldenrod; --bit-Rating-hover-color: darkorange; --bit-Rating-unselected-color: #d8c9a3;"" />

<BitRating DefaultValue=""3"" Style=""--bit-Rating-size: 2rem; --bit-Rating-gap: 0.5rem; --bit-Rating-hover-scale: 1.25;"" />

<div>
    Rated
    <BitRating Size=""BitSize.Small"" ReadOnly DefaultValue=""4"" Style=""--bit-Rating-target-size: 0; --bit-Rating-padding: 0;"" />
    by 1,034 people.
</div>


<div style=""--bit-Rating-color: crimson; --bit-Rating-unselected-color: pink; --bit-Rating-label-color: crimson;"">
    <BitRating Label=""Story"" LabelPosition=""BitLabelPosition.Start"" ReadOnly DefaultValue=""4.5"" />
    <BitRating Label=""Music"" LabelPosition=""BitLabelPosition.Start"" ReadOnly DefaultValue=""3"" />
</div>";
    private const string example21ScssCode = @"
.custom-class {
    margin-inline: 1rem;
    border-radius: 0.25rem;
    padding-inline: 0.5rem;
    border: 1px solid dodgerblue;
    box-shadow: dodgerblue 0 0 1rem;
}

.custom-label {
    color: dodgerblue;
    text-transform: uppercase;
}

.custom-selected {
    color: seagreen;
}

.custom-unselected {
    color: mediumseagreen;
}";
    private readonly DemoCodeFile[] example21CodeFiles =
    [
        new("BitRatingDemo.razor.scss", example21ScssCode),
    ];

    private readonly string example22RazorCode = @"
<BitRating Dir=""BitDir.Rtl"" DefaultValue=""3"" />

<BitRating Dir=""BitDir.Rtl"" Precision=""0.5"" DefaultValue=""3.5"" />

<BitRating Dir=""BitDir.Rtl"" Label=""امتیاز شما"" LabelPosition=""BitLabelPosition.Start"" DefaultValue=""4"" />";
}
