using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

public class BitRatingTestModel
{
    // Combined with AllowZeroStars, a range that starts at 1 is what turns "pick a rating" into a
    // required question: the form starts unrated and stays invalid until something is picked.
    [Range(typeof(double), "1", "5", ErrorMessage = "Your rate must be between {1} and {2}")]
    public double Value { get; set; }
}
