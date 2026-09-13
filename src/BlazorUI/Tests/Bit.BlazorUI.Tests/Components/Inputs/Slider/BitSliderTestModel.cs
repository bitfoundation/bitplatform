using System;
using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Slider;

public class BitSliderTestModel
{
    // A slider always holds some number, so the interesting annotation is a range narrower than the one the
    // slider itself allows: the form starts below it and stays invalid until the thumb is moved far enough.
    [Range(typeof(double), "4", "7", ErrorMessage = "Pick at least {1}")]
    public double Days { get; set; }
}

/// <summary>
/// The pair a ranged slider selects is one field of the model rather than two, which is what lets it be
/// annotated - and validated - the way any other field is. It starts too narrow to pass, so the form is
/// invalid until the thumbs are dragged apart.
/// </summary>
public class BitSliderRangeTestModel
{
    [MinimumRangeLength(3, ErrorMessage = "Pick a band at least 3 wide")]
    public BitSliderRangeValue? Band { get; set; } = new(2, 3);
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class MinimumRangeLengthAttribute(double minimum) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is BitSliderRangeValue range && range.Length >= minimum;
    }
}
