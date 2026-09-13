namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Slider;

public class BitSliderDemoFormModel
{
    // A slider always holds some number, so the interesting annotation is a range narrower than the one
    // the slider itself allows: it starts below the minimum the form accepts and stays invalid until moved.
    [Range(typeof(double), "4", "7", ErrorMessage = "Pick at least {1} days a week")]
    public double Days { get; set; }

    // The pair a ranged slider selects is one field of the model rather than two, so it carries its own
    // annotation the way any other field does. It starts too narrow to pass.
    [MinimumRangeLength(20, ErrorMessage = "Cover a span of at least 20")]
    public BitSliderRangeValue? Budget { get; set; } = new(40, 50);
}

/// <summary>
/// Asks a <see cref="BitSliderRangeValue"/> for a minimum width, which is the kind of rule a range has and
/// a single number does not.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MinimumRangeLengthAttribute(double minimum) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is BitSliderRangeValue range && range.Length >= minimum;
    }
}
