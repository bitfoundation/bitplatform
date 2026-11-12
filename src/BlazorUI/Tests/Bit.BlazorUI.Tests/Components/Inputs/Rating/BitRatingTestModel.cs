using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Tests.Components.Inputs.Rating;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MSTEST0004", Justification = "Public Razor parameter properties require this framework-facing model type to be public.")]
public class BitRatingTestModel
{
    // Combined with AllowZeroStars, a range that starts at 1 is what turns "pick a rating" into a
    // required question: the form starts unrated and stays invalid until something is picked.
    [Range(typeof(double), "1", "5", ErrorMessage = "Your rate must be between {1} and {2}")]
    public double Value { get; set; }
}
