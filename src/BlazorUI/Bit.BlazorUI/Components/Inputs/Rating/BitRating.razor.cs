using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Ratings show people’s opinions of a product, helping others make more informed purchasing decisions.
/// </summary>
public partial class BitRating : BitInputBase<double>
{
    /// <summary>
    /// Allow the initial rating value be 0. Note that a value of 0 still won't be selectable by mouse or keyboard.
    /// </summary>
    [Parameter] public bool AllowZeroStars { get; set; }

    /// <summary>
    /// Optional label format for each individual rating star (not the rating control as a whole) that will be read by screen readers. 
    /// Placeholder {0} is the current rating and placeholder {1} is the max: for example, 
    /// "Select {0} of {1} stars". (To set the label for the control as a whole, use getAriaLabel or aria-label.)
    /// </summary>
    [Parameter] public string? AriaLabelFormat { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitRating.
    /// </summary>
    [Parameter] public BitRatingClassStyles? Classes { get; set; }

    /// <summary>
    /// Default value. Must be a number between _min and max. 
    /// Only provide this if the CurrentValue is an uncontrolled component; otherwise, use the Value property.
    /// </summary>
    [Parameter] public double? DefaultValue { get; set; }

    /// <summary>
    /// Optional callback to set the aria-label for rating control in readOnly mode. Also used as a fallback aria-label if ariaLabel prop is not provided.
    /// </summary>
    [Parameter] public Func<double, double, string>? GetAriaLabel { get; set; }

    /// <summary>
    /// Maximum rating. Must be >= Min (0 if AllowZeroStars is true, 1 otherwise).
    /// </summary>
    [Parameter] public int Max { get; set; } = 5;

    /// <summary>
    /// Custom icon name for selected rating elements, If unset, default will be the FavoriteStarFill icon.
    /// </summary>
    [Parameter] public string SelectedIconName { get; set; } = "FavoriteStarFill";

    /// <summary>
    /// Size of rating elements.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitRating.
    /// </summary>
    [Parameter] public BitRatingClassStyles? Styles { get; set; }

    /// <summary>
    /// Custom icon name for unselected rating elements, If unset, default will be the FavoriteStar icon.
    /// </summary>
    [Parameter] public string UnselectedIconName { get; set; } = "FavoriteStar";



    protected override async Task OnInitializedAsync()
    {
        if (ValueHasBeenSet is false && DefaultValue.HasValue)
        {
            Value = DefaultValue.Value;
        }

        await base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-rtg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ReadOnly ? "bit-rtg-rdl" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-rtg-sm",
            BitSize.Medium => "bit-rtg-md",
            BitSize.Large => "bit-rtg-lg",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CurrentValue = Math.Min(Math.Max(CurrentValue, (AllowZeroStars ? 0 : 1)), Max);

        base.OnParametersSet();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out double result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        if (double.TryParse(value, out var parsedValue))
        {
            result = parsedValue;
            parsingErrorMessage = null;
            return true;
        }

        result = default;
        parsingErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }



    private double GetPercentage(int index)
    {
        var fullRating = Math.Ceiling(CurrentValue);

        double fullStar = 0;

        if (index < fullRating)
        {
            fullStar = 100;
        }
        else if (index == fullRating)
        {
            var decimalValue = CurrentValue % 1;
            fullStar = decimalValue == 0 ? 100 : (decimalValue * 100);
        }

        return fullStar;
    }

    private async Task HandleOnClick(int index)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (ReadOnly) return;
        if (index > Max || index < (AllowZeroStars ? 0 : 1)) return;

        CurrentValue = index;
    }
}
