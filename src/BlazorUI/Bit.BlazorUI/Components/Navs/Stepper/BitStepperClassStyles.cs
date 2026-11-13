namespace Bit.BlazorUI;

public class BitStepperClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element (the list of steps) of the BitStepper.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each step of the BitStepper.
    /// </summary>
    public string? Step { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each completed step of the BitStepper (the steps before the current one).
    /// </summary>
    public string? CompletedStep { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the current step of the BitStepper.
    /// </summary>
    public string? CurrentStep { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each upcoming step of the BitStepper (the steps after the current one).
    /// </summary>
    public string? UpcomingStep { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each step of the BitStepper that is in error.
    /// </summary>
    public string? ErrorStep { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the part of each step of the BitStepper that is clicked: a button when the step can
    /// be gone to.
    /// </summary>
    public string? Button { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the indicator (the circle with the number or icon) of each step of the BitStepper.
    /// </summary>
    public string? Indicator { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon in the indicator of each step of the BitStepper.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the number in the indicator of each step of the BitStepper.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the text and description of each step of the BitStepper.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the text of each step of the BitStepper.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the description of each step of the BitStepper.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the line connecting each step of the BitStepper to the next one.
    /// </summary>
    public string? Connector { get; set; }
}
