namespace Bit.Bonboarding;

/// <summary>
/// Declares a DOM event that advances the tour, enabling "now click this button yourself" steps.
/// </summary>
public sealed class BonboardingAdvanceTrigger
{
    /// <summary>Advance when the step's own target is clicked.</summary>
    public static BonboardingAdvanceTrigger TargetClick { get; } = new();

    /// <summary>DOM event name. Default <c>"click"</c>.</summary>
    public string EventName { get; set; } = "click";

    /// <summary>
    /// Element to listen on. <see langword="null"/> listens on the step's resolved target.
    /// </summary>
    public string? Selector { get; set; }

    /// <summary>
    /// Only advance when the event's target matches this selector. Useful for delegated events on
    /// a container.
    /// </summary>
    public string? MatchSelector { get; set; }

    /// <summary>Milliseconds to wait after the event before advancing, letting the UI settle. Default <c>150</c>.</summary>
    public int DelayMs { get; set; } = 150;
}
