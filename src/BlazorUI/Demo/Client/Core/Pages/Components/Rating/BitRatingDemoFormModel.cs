﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Rating;

public class BitRatingDemoFormModel
{
    [Range(typeof(double), "1", "5", ErrorMessage = "Your rate must be between {1} and {2}")]
    public double Value { get; set; }
}
