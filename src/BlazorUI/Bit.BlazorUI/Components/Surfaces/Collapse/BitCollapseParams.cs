namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCollapse"/> component.
/// </summary>
public class BitCollapseParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCollapse"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCollapse value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCollapse)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the background of the collapse.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the collapse.
    /// </summary>
    public BitCollapseClassStyles? Classes { get; set; }

    /// <summary>
    /// The duration of the collapse transition in ms, overriding <see cref="Duration"/> while the collapse is closing.
    /// </summary>
    public int? CollapseDuration { get; set; }

    /// <summary>
    /// The size the collapse keeps while it is collapsed, as any CSS length, which leaves a peek of the content on the page.
    /// </summary>
    public string? CollapsedSize { get; set; }

    /// <summary>
    /// The delay of the expand/collapse transition in ms.
    /// </summary>
    public int? Delay { get; set; }

    /// <summary>
    /// The duration of the expand/collapse transition in ms.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// The timing function of the expand/collapse transition, as any CSS easing value.
    /// </summary>
    public string? Easing { get; set; }

    /// <summary>
    /// The duration of the expand transition in ms, overriding <see cref="Duration"/> while the collapse is opening.
    /// </summary>
    public int? ExpandDuration { get; set; }

    /// <summary>
    /// Prints the collapse expanded, whatever its state on the screen.
    /// </summary>
    public bool? ExpandOnPrint { get; set; }

    /// <summary>
    /// Hands the closed content to the browser as <c>hidden="until-found"</c>, so find-in-page reaches into it and opens it.
    /// </summary>
    public bool? HiddenUntilFound { get; set; }

    /// <summary>
    /// Collapses the content along the inline axis instead of the block one, so it opens sideways.
    /// </summary>
    public bool? Horizontal { get; set; }

    /// <summary>
    /// Keeps the content out of the DOM until the collapse is expanded for the first time.
    /// </summary>
    public bool? LazyRender { get; set; }

    /// <summary>
    /// Removes the expand/collapse transition, so the content appears and disappears at once.
    /// </summary>
    public bool? NoAnimation { get; set; }

    /// <summary>
    /// Stops clipping the content once the collapse has finished opening.
    /// </summary>
    public bool? NoClip { get; set; }

    /// <summary>
    /// Removes the fade of the content, leaving the size on its own to open and close the collapse.
    /// </summary>
    public bool? NoFade { get; set; }

    /// <summary>
    /// Removes the padding the collapse puts around its content.
    /// </summary>
    public bool? NoPadding { get; set; }

    /// <summary>
    /// The ARIA role of the content region of the collapse. An empty string renders no role at all.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the collapse.
    /// </summary>
    public BitCollapseClassStyles? Styles { get; set; }

    /// <summary>
    /// Takes the content back out of the DOM once the collapse has closed.
    /// </summary>
    public bool? UnmountOnCollapse { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCollapse"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCollapse"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCollapse"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCollapse"/>.
    /// </remarks>
    /// <param name="bitCollapse">
    /// The <see cref="BitCollapse"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCollapse bitCollapse)
    {
        if (bitCollapse is null) return;

        UpdateBaseParameters(bitCollapse);

        if (Background.HasValue && bitCollapse.HasNotBeenSet(nameof(Background)) && bitCollapse.Background != Background)
        {
            bitCollapse.Background = Background.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (Classes is not null && bitCollapse.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitCollapse.Classes, Classes) is false)
        {
            bitCollapse.Classes = Classes;

            bitCollapse.ClassBuilder.Reset();
        }

        if (CollapseDuration.HasValue && bitCollapse.HasNotBeenSet(nameof(CollapseDuration)) && bitCollapse.CollapseDuration != CollapseDuration)
        {
            bitCollapse.CollapseDuration = CollapseDuration.Value;

            bitCollapse.StyleBuilder.Reset();
        }

        if (CollapsedSize.HasValue() && bitCollapse.HasNotBeenSet(nameof(CollapsedSize)) && bitCollapse.CollapsedSize != CollapsedSize)
        {
            bitCollapse.CollapsedSize = CollapsedSize;

            bitCollapse.ClassBuilder.Reset();
            bitCollapse.StyleBuilder.Reset();
        }

        if (Delay.HasValue && bitCollapse.HasNotBeenSet(nameof(Delay)) && bitCollapse.Delay != Delay)
        {
            bitCollapse.Delay = Delay.Value;

            bitCollapse.StyleBuilder.Reset();
        }

        if (Duration.HasValue && bitCollapse.HasNotBeenSet(nameof(Duration)) && bitCollapse.Duration != Duration)
        {
            bitCollapse.Duration = Duration.Value;

            bitCollapse.StyleBuilder.Reset();
        }

        if (Easing.HasValue() && bitCollapse.HasNotBeenSet(nameof(Easing)) && bitCollapse.Easing != Easing)
        {
            bitCollapse.Easing = Easing;

            bitCollapse.StyleBuilder.Reset();
        }

        if (ExpandDuration.HasValue && bitCollapse.HasNotBeenSet(nameof(ExpandDuration)) && bitCollapse.ExpandDuration != ExpandDuration)
        {
            bitCollapse.ExpandDuration = ExpandDuration.Value;

            bitCollapse.StyleBuilder.Reset();
        }

        if (ExpandOnPrint.HasValue && bitCollapse.HasNotBeenSet(nameof(ExpandOnPrint)) && bitCollapse.ExpandOnPrint != ExpandOnPrint)
        {
            bitCollapse.ExpandOnPrint = ExpandOnPrint.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (HiddenUntilFound.HasValue && bitCollapse.HasNotBeenSet(nameof(HiddenUntilFound)) && bitCollapse.HiddenUntilFound != HiddenUntilFound)
        {
            bitCollapse.HiddenUntilFound = HiddenUntilFound.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (Horizontal.HasValue && bitCollapse.HasNotBeenSet(nameof(Horizontal)) && bitCollapse.Horizontal != Horizontal)
        {
            bitCollapse.Horizontal = Horizontal.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (LazyRender.HasValue && bitCollapse.HasNotBeenSet(nameof(LazyRender)))
        {
            bitCollapse.LazyRender = LazyRender.Value;
        }

        if (NoAnimation.HasValue && bitCollapse.HasNotBeenSet(nameof(NoAnimation)) && bitCollapse.NoAnimation != NoAnimation)
        {
            bitCollapse.NoAnimation = NoAnimation.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (NoClip.HasValue && bitCollapse.HasNotBeenSet(nameof(NoClip)) && bitCollapse.NoClip != NoClip)
        {
            bitCollapse.NoClip = NoClip.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (NoFade.HasValue && bitCollapse.HasNotBeenSet(nameof(NoFade)) && bitCollapse.NoFade != NoFade)
        {
            bitCollapse.NoFade = NoFade.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (NoPadding.HasValue && bitCollapse.HasNotBeenSet(nameof(NoPadding)) && bitCollapse.NoPadding != NoPadding)
        {
            bitCollapse.NoPadding = NoPadding.Value;

            bitCollapse.ClassBuilder.Reset();
        }

        if (Role is not null && bitCollapse.HasNotBeenSet(nameof(Role)))
        {
            bitCollapse.Role = Role;
        }

        if (Styles is not null && bitCollapse.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitCollapse.Styles, Styles) is false)
        {
            bitCollapse.Styles = Styles;

            bitCollapse.StyleBuilder.Reset();
        }

        if (UnmountOnCollapse.HasValue && bitCollapse.HasNotBeenSet(nameof(UnmountOnCollapse)))
        {
            bitCollapse.UnmountOnCollapse = UnmountOnCollapse.Value;
        }
    }
}
