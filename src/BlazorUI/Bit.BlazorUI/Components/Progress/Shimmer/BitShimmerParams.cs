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

        // This runs on every render of every shimmer under the BitParams, so a value that drives the class or
        // the style of its root only resets the builder when it differs from the one it already holds: an
        // unchanged one would rebuild both strings on every render for nothing.
        if (Animation.HasValue && bitShimmer.HasNotBeenSet(nameof(Animation)) && bitShimmer.Animation != Animation)
        {
            bitShimmer.Animation = Animation.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Background.HasValue && bitShimmer.HasNotBeenSet(nameof(Background)))
        {
            bitShimmer.Background = Background.Value;
        }

        if (Circle.HasValue && bitShimmer.HasNotBeenSet(nameof(Circle)) && bitShimmer.Circle != Circle)
        {
            bitShimmer.Circle = Circle.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Classes is not null && bitShimmer.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitShimmer.Classes, Classes) is false)
        {
            bitShimmer.Classes = Classes;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitShimmer.HasNotBeenSet(nameof(Color)) && bitShimmer.Color != Color)
        {
            bitShimmer.Color = Color.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Delay.HasValue && bitShimmer.HasNotBeenSet(nameof(Delay)))
        {
            bitShimmer.Delay = Delay.Value;
        }

        if (Duration.HasValue && bitShimmer.HasNotBeenSet(nameof(Duration)))
        {
            bitShimmer.Duration = Duration.Value;
        }

        if (Gap.HasValue() && bitShimmer.HasNotBeenSet(nameof(Gap)) && bitShimmer.Gap != Gap)
        {
            bitShimmer.Gap = Gap;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Height.HasValue() && bitShimmer.HasNotBeenSet(nameof(Height)) && bitShimmer.Height != Height)
        {
            bitShimmer.Height = Height;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Inline.HasValue && bitShimmer.HasNotBeenSet(nameof(Inline)) && bitShimmer.Inline != Inline)
        {
            bitShimmer.Inline = Inline.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (LastLineWidth.HasValue() && bitShimmer.HasNotBeenSet(nameof(LastLineWidth)) && bitShimmer.LastLineWidth != LastLineWidth)
        {
            bitShimmer.LastLineWidth = LastLineWidth;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Lines.HasValue && bitShimmer.HasNotBeenSet(nameof(Lines)) && bitShimmer.Lines != Lines)
        {
            bitShimmer.Lines = Lines.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (LineWidths is not null && bitShimmer.HasNotBeenSet(nameof(LineWidths)))
        {
            bitShimmer.LineWidths = LineWidths;
        }

        if (MinShowTime.HasValue && bitShimmer.HasNotBeenSet(nameof(MinShowTime)))
        {
            bitShimmer.MinShowTime = MinShowTime.Value;
        }

        if (Overlay.HasValue && bitShimmer.HasNotBeenSet(nameof(Overlay)) && bitShimmer.Overlay != Overlay)
        {
            bitShimmer.Overlay = Overlay.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Politeness.HasValue && bitShimmer.HasNotBeenSet(nameof(Politeness)))
        {
            bitShimmer.Politeness = Politeness.Value;
        }

        if (Pulse.HasValue && bitShimmer.HasNotBeenSet(nameof(Pulse)) && bitShimmer.Pulse != Pulse)
        {
            bitShimmer.Pulse = Pulse.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Radius.HasValue() && bitShimmer.HasNotBeenSet(nameof(Radius)) && bitShimmer.Radius != Radius)
        {
            bitShimmer.Radius = Radius;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Shape.HasValue && bitShimmer.HasNotBeenSet(nameof(Shape)) && bitShimmer.Shape != Shape)
        {
            bitShimmer.Shape = Shape.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (ShowDelay.HasValue && bitShimmer.HasNotBeenSet(nameof(ShowDelay)) && bitShimmer.ShowDelay != ShowDelay)
        {
            bitShimmer.ShowDelay = ShowDelay.Value;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Size.HasValue && bitShimmer.HasNotBeenSet(nameof(Size)) && bitShimmer.Size != Size)
        {
            bitShimmer.Size = Size.Value;

            bitShimmer.ClassBuilder.Reset();
        }

        if (Stagger.HasValue && bitShimmer.HasNotBeenSet(nameof(Stagger)))
        {
            bitShimmer.Stagger = Stagger.Value;
        }

        if (Styles is not null && bitShimmer.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitShimmer.Styles, Styles) is false)
        {
            bitShimmer.Styles = Styles;

            bitShimmer.StyleBuilder.Reset();
        }

        if (Width.HasValue() && bitShimmer.HasNotBeenSet(nameof(Width)) && bitShimmer.Width != Width)
        {
            bitShimmer.Width = Width;

            bitShimmer.StyleBuilder.Reset();
        }
    }
}
