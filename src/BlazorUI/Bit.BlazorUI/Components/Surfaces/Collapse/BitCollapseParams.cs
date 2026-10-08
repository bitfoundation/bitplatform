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

        if (Background.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(Background), Background.Value, static c => c.Background, static (c, v) => c.Background = v);
        }

        if (Classes is not null)
        {
            bitCollapse.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (CollapseDuration.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(CollapseDuration), CollapseDuration.Value, static c => c.CollapseDuration, static (c, v) => c.CollapseDuration = v);
        }

        if (CollapsedSize.HasValue())
        {
            bitCollapse.TakeFromCascade(nameof(CollapsedSize), CollapsedSize, static c => c.CollapsedSize, static (c, v) => c.CollapsedSize = v);
        }

        if (Delay.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(Delay), Delay.Value, static c => c.Delay, static (c, v) => c.Delay = v);
        }

        if (Duration.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(Duration), Duration.Value, static c => c.Duration, static (c, v) => c.Duration = v);
        }

        if (Easing.HasValue())
        {
            bitCollapse.TakeFromCascade(nameof(Easing), Easing, static c => c.Easing, static (c, v) => c.Easing = v);
        }

        if (ExpandDuration.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(ExpandDuration), ExpandDuration.Value, static c => c.ExpandDuration, static (c, v) => c.ExpandDuration = v);
        }

        if (ExpandOnPrint.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(ExpandOnPrint), ExpandOnPrint.Value, static c => c.ExpandOnPrint, static (c, v) => c.ExpandOnPrint = v);
        }

        if (HiddenUntilFound.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(HiddenUntilFound), HiddenUntilFound.Value, static c => c.HiddenUntilFound, static (c, v) => c.HiddenUntilFound = v);
        }

        if (Horizontal.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(Horizontal), Horizontal.Value, static c => c.Horizontal, static (c, v) => c.Horizontal = v);
        }

        if (LazyRender.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(LazyRender), LazyRender.Value, static c => c.LazyRender, static (c, v) => c.LazyRender = v);
        }

        if (NoAnimation.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(NoAnimation), NoAnimation.Value, static c => c.NoAnimation, static (c, v) => c.NoAnimation = v);
        }

        if (NoClip.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(NoClip), NoClip.Value, static c => c.NoClip, static (c, v) => c.NoClip = v);
        }

        if (NoFade.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(NoFade), NoFade.Value, static c => c.NoFade, static (c, v) => c.NoFade = v);
        }

        if (NoPadding.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(NoPadding), NoPadding.Value, static c => c.NoPadding, static (c, v) => c.NoPadding = v);
        }

        if (Role is not null)
        {
            bitCollapse.TakeFromCascade(nameof(Role), Role, static c => c.Role, static (c, v) => c.Role = v);
        }

        if (Styles is not null)
        {
            bitCollapse.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (UnmountOnCollapse.HasValue)
        {
            bitCollapse.TakeFromCascade(nameof(UnmountOnCollapse), UnmountOnCollapse.Value, static c => c.UnmountOnCollapse, static (c, v) => c.UnmountOnCollapse = v);
        }
    }
}
