namespace Bit.BlazorUI;

public class BitSliderClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's root element.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's container.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's box, the element the track and the inputs are laid inside of.
    /// </summary>
    public string? SliderBox { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's track, the full-length rail the thumb travels along.
    /// </summary>
    public string? Track { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's fill, the highlighted part of the track.
    /// </summary>
    public string? Fill { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's thumbs, the handles that travel along the track.
    /// </summary>
    public string? Thumb { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the lower thumb of a ranged BitSlider, added on top of <see cref="Thumb"/>.
    /// </summary>
    public string? LowerThumb { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the upper thumb of a ranged BitSlider, added on top of <see cref="Thumb"/>.
    /// </summary>
    public string? UpperThumb { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's minimum input.
    /// </summary>
    public string? LowerValueInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's maximum value input.
    /// </summary>
    public string? UpperValueInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's value input.
    /// </summary>
    public string? ValueInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the input that makes the filled band of a ranged BitSlider draggable,
    /// which is only rendered while <c>DraggableTrack</c> asks for it.
    /// </summary>
    public string? TrackInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's origin tick.
    /// </summary>
    public string? OriginFromZero { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's value label.
    /// </summary>
    public string? ValueLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's lower value label.
    /// </summary>
    public string? LowerValueLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's upper value label.
    /// </summary>
    public string? UpperValueLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's mark ticks.
    /// </summary>
    public string? Mark { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's mark labels.
    /// </summary>
    public string? MarkLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSlider's floating thumb labels.
    /// </summary>
    public string? ThumbLabel { get; set; }
}
