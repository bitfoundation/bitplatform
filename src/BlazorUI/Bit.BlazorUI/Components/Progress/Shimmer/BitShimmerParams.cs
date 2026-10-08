namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitShimmer"/> component.
/// </summary>
/// <remarks>
/// It carries what a group of placeholders shares - their look, their size and their timing - so a card or a list
/// sets it once. <see cref="BitShimmer.Loaded"/>, the content and the <see cref="BitShimmer.Template"/> stay on
/// the shimmer itself, since each placeholder swaps for its own content, and so do <see cref="BitShimmer.Label"/>
/// and <see cref="BitShimmer.LoadedLabel"/>, which belong on the one shimmer that speaks for a region rather than
/// on every shimmer in it.
/// </remarks>
public class BitShimmerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitShimmer"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitShimmer value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitShimmer)}";



    public string Name => ParamName;



    /// <summary>
    /// The animation the shimmer plays while it stands in for content that has not arrived yet.
    /// </summary>
    public BitShimmerAnimation? Animation { get; set; }

    /// <summary>
    /// The resting color of the placeholder, which the animation plays over.
    /// </summary>
    public BitColor? Background { get; set; }

    /// <summary>
    /// Renders the shimmer as circle instead of a rectangle.
    /// </summary>
    public bool? Circle { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the shimmer.
    /// </summary>
    public BitShimmerClassStyles? Classes { get; set; }

    /// <summary>
    /// The color of the animated part of the shimmer.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The pause in ms before the first loop of the animation.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// The length in ms of one loop of the animation.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// The gap between the lines of a multi-line shimmer, as a CSS length.
    /// </summary>
    public string? Gap { get; set; }

    /// <summary>
    /// The height of the placeholder, or of each line of a multi-line one, as a CSS length.
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Lays the shimmer out in the flow of a line of text instead of as a block of its own.
    /// </summary>
    public bool? Inline { get; set; }

    /// <summary>
    /// The width of the last line of a multi-line shimmer, as a CSS length.
    /// </summary>
    public string? LastLineWidth { get; set; }

    /// <summary>
    /// The number of placeholder lines rendered as a stack.
    /// </summary>
    public int? Lines { get; set; }

    /// <summary>
    /// The width of each line of a multi-line shimmer, as a list of CSS lengths.
    /// </summary>
    public IList<string>? LineWidths { get; set; }

    /// <summary>
    /// The shortest time in ms a placeholder that has been seen stays on the page.
    /// </summary>
    public int? MinShowTime { get; set; }

    /// <summary>
    /// Draws the placeholder over the content instead of in place of it.
    /// </summary>
    public bool? Overlay { get; set; }

    /// <summary>
    /// How urgently the live region of the shimmer interrupts a screen reader.
    /// </summary>
    public BitPoliteness? Politeness { get; set; }

    /// <summary>
    /// Changes the animation type of the shimmer to pulse.
    /// </summary>
    public bool? Pulse { get; set; }

    /// <summary>
    /// The corner radius of the placeholder, as a CSS length.
    /// </summary>
    public string? Radius { get; set; }

    /// <summary>
    /// The shape of the placeholder the shimmer draws.
    /// </summary>
    public BitShape? Shape { get; set; }

    /// <summary>
    /// The wait in ms before the placeholder appears.
    /// </summary>
    public int? ShowDelay { get; set; }

    /// <summary>
    /// The size of the shimmer, which is the height of a line and the diameter of a circle.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// The offset in ms between the animation of one line of a multi-line shimmer and the next.
    /// </summary>
    public int? Stagger { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the shimmer.
    /// </summary>
    public BitShimmerClassStyles? Styles { get; set; }

    /// <summary>
    /// The width of the shimmer, as a CSS length.
    /// </summary>
    public string? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitShimmer"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitShimmer"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitShimmer"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitShimmer"/>.
    /// </remarks>
    /// <param name="bitShimmer">
    /// The <see cref="BitShimmer"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitShimmer bitShimmer)
    {
        if (bitShimmer is null) return;

        UpdateBaseParameters(bitShimmer);

        if (Animation.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Animation), Animation.Value, static s => s.Animation, static (s, v) => s.Animation = v);
        }

        if (Background.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Background), Background.Value, static s => s.Background, static (s, v) => s.Background = v);
        }

        if (Circle.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Circle), Circle.Value, static s => s.Circle, static (s, v) => s.Circle = v);
        }

        if (Classes is not null)
        {
            bitShimmer.TakeFromCascade(nameof(Classes), Classes, static s => s.Classes, static (s, v) => s.Classes = v);
        }

        if (Color.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Color), Color.Value, static s => s.Color, static (s, v) => s.Color = v);
        }

        if (Delay.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Delay), Delay.Value, static s => s.Delay, static (s, v) => s.Delay = v);
        }

        if (Duration.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Duration), Duration.Value, static s => s.Duration, static (s, v) => s.Duration = v);
        }

        if (Gap.HasValue())
        {
            bitShimmer.TakeFromCascade(nameof(Gap), Gap, static s => s.Gap, static (s, v) => s.Gap = v);
        }

        if (Height.HasValue())
        {
            bitShimmer.TakeFromCascade(nameof(Height), Height, static s => s.Height, static (s, v) => s.Height = v);
        }

        if (Inline.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Inline), Inline.Value, static s => s.Inline, static (s, v) => s.Inline = v);
        }

        if (LastLineWidth.HasValue())
        {
            bitShimmer.TakeFromCascade(nameof(LastLineWidth), LastLineWidth, static s => s.LastLineWidth, static (s, v) => s.LastLineWidth = v);
        }

        if (Lines.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Lines), Lines.Value, static s => s.Lines, static (s, v) => s.Lines = v);
        }

        if (LineWidths is not null)
        {
            bitShimmer.TakeFromCascade(nameof(LineWidths), LineWidths, static s => s.LineWidths, static (s, v) => s.LineWidths = v);
        }

        if (MinShowTime.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(MinShowTime), MinShowTime.Value, static s => s.MinShowTime, static (s, v) => s.MinShowTime = v);
        }

        if (Overlay.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Overlay), Overlay.Value, static s => s.Overlay, static (s, v) => s.Overlay = v);
        }

        if (Politeness.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Politeness), Politeness.Value, static s => s.Politeness, static (s, v) => s.Politeness = v);
        }

        if (Pulse.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Pulse), Pulse.Value, static s => s.Pulse, static (s, v) => s.Pulse = v);
        }

        if (Radius.HasValue())
        {
            bitShimmer.TakeFromCascade(nameof(Radius), Radius, static s => s.Radius, static (s, v) => s.Radius = v);
        }

        if (Shape.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Shape), Shape.Value, static s => s.Shape, static (s, v) => s.Shape = v);
        }

        if (ShowDelay.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(ShowDelay), ShowDelay.Value, static s => s.ShowDelay, static (s, v) => s.ShowDelay = v);
        }

        if (Size.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Size), Size.Value, static s => s.Size, static (s, v) => s.Size = v);
        }

        if (Stagger.HasValue)
        {
            bitShimmer.TakeFromCascade(nameof(Stagger), Stagger.Value, static s => s.Stagger, static (s, v) => s.Stagger = v);
        }

        if (Styles is not null)
        {
            bitShimmer.TakeFromCascade(nameof(Styles), Styles, static s => s.Styles, static (s, v) => s.Styles = v);
        }

        if (Width.HasValue())
        {
            bitShimmer.TakeFromCascade(nameof(Width), Width, static s => s.Width, static (s, v) => s.Width = v);
        }
    }
}
