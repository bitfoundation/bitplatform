namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAccordion"/> component.
/// </summary>
public class BitAccordionParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAccordion"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAccordion value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAccordion)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the background of the accordion.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the accordion.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the accordion.
    /// </summary>
    public BitAccordionClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon to show in place of the expander icon while the accordion is expanded, using custom CSS classes for
    /// external icon libraries.
    /// </summary>
    public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// The name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon while the
    /// accordion is expanded.
    /// </summary>
    public string? ExpandedExpanderIconName { get; set; }

    /// <summary>
    /// The icon to display as expander using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// The name of the icon to display as expander from the built-in Fluent UI icons.
    /// </summary>
    public string? ExpanderIconName { get; set; }

    /// <summary>
    /// The side of the header the expander icon sits on.
    /// </summary>
    public BitIconPosition? ExpanderIconPosition { get; set; }

    /// <summary>
    /// Opens the panel of the accordion while the page is being printed.
    /// </summary>
    public bool? ExpandOnPrint { get; set; }

    /// <summary>
    /// The heading level (aria-level) reported for the header of the accordion.
    /// </summary>
    /// <remarks>
    /// It only reaches the accordions that are not nested in the panel of another one: a nested accordion keeps
    /// taking the level below the one holding it, which is what keeps the heading outline of the page in order.
    /// </remarks>
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// Hands the collapsed panel to the browser as <c>hidden="until-found"</c>, so find-in-page reaches into it.
    /// </summary>
    public bool? HiddenUntilFound { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of the accordion.
    /// </summary>
    public bool? HideExpanderIcon { get; set; }

    /// <summary>
    /// Delays the first render of the content of the accordion until it is expanded for the first time.
    /// </summary>
    public bool? LazyContent { get; set; }

    /// <summary>
    /// The maximum height of the content of the accordion (any CSS length), beyond which the content scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Removes the default border of the accordion and gives a background color to the body.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Removes the <c>region</c> role from the panel of the accordion.
    /// </summary>
    public bool? NoContentRegion { get; set; }

    /// <summary>
    /// Keeps the expander icon still instead of turning it over when the accordion is expanded.
    /// </summary>
    public bool? NoExpanderRotation { get; set; }

    /// <summary>
    /// The size of the accordion, which drives the padding of the header and of the content and the size of the title.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the accordion.
    /// </summary>
    public BitAccordionClassStyles? Styles { get; set; }

    /// <summary>
    /// The duration of the expand/collapse transition in milliseconds.
    /// </summary>
    public int? TransitionDuration { get; set; }

    /// <summary>
    /// Removes the content of the accordion from the DOM while it is collapsed.
    /// </summary>
    public bool? UnmountOnCollapse { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAccordion"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAccordion"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAccordion"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAccordion"/>.
    /// </remarks>
    /// <param name="bitAccordion">
    /// The <see cref="BitAccordion"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitAccordion bitAccordion)
    {
        if (bitAccordion is null) return;

        UpdateBaseParameters(bitAccordion);

        if (Background.HasValue && bitAccordion.HasNotBeenSet(nameof(Background)))
        {
            bitAccordion.Background = Background.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        if (Border.HasValue && bitAccordion.HasNotBeenSet(nameof(Border)))
        {
            bitAccordion.Border = Border.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        if (Classes is not null && bitAccordion.HasNotBeenSet(nameof(Classes)))
        {
            bitAccordion.Classes = Classes;

            bitAccordion.ClassBuilder.Reset();
        }

        if (ExpandedExpanderIcon is not null && bitAccordion.HasNotBeenSet(nameof(ExpandedExpanderIcon)))
        {
            bitAccordion.ExpandedExpanderIcon = ExpandedExpanderIcon;
        }

        if (ExpandedExpanderIconName.HasValue() && bitAccordion.HasNotBeenSet(nameof(ExpandedExpanderIconName)))
        {
            bitAccordion.ExpandedExpanderIconName = ExpandedExpanderIconName;
        }

        if (ExpanderIcon is not null && bitAccordion.HasNotBeenSet(nameof(ExpanderIcon)))
        {
            bitAccordion.ExpanderIcon = ExpanderIcon;
        }

        if (ExpanderIconName.HasValue() && bitAccordion.HasNotBeenSet(nameof(ExpanderIconName)))
        {
            bitAccordion.ExpanderIconName = ExpanderIconName;
        }

        if (ExpanderIconPosition.HasValue && bitAccordion.HasNotBeenSet(nameof(ExpanderIconPosition)))
        {
            bitAccordion.ExpanderIconPosition = ExpanderIconPosition.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        if (ExpandOnPrint.HasValue && bitAccordion.HasNotBeenSet(nameof(ExpandOnPrint)))
        {
            bitAccordion.ExpandOnPrint = ExpandOnPrint.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        // A nested accordion takes the level below the one holding it, so only a top-level one is given the default.
        if (HeadingLevel.HasValue && bitAccordion.HasNotBeenSet(nameof(HeadingLevel)) && bitAccordion.ParentHeadingLevel is null)
        {
            bitAccordion.HeadingLevel = HeadingLevel.Value;
        }

        if (HiddenUntilFound.HasValue && bitAccordion.HasNotBeenSet(nameof(HiddenUntilFound)))
        {
            bitAccordion.HiddenUntilFound = HiddenUntilFound.Value;
        }

        if (HideExpanderIcon.HasValue && bitAccordion.HasNotBeenSet(nameof(HideExpanderIcon)))
        {
            bitAccordion.HideExpanderIcon = HideExpanderIcon.Value;
        }

        if (LazyContent.HasValue && bitAccordion.HasNotBeenSet(nameof(LazyContent)))
        {
            bitAccordion.LazyContent = LazyContent.Value;
        }

        if (MaxHeight.HasValue() && bitAccordion.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitAccordion.MaxHeight = MaxHeight;

            bitAccordion.ClassBuilder.Reset();
            bitAccordion.StyleBuilder.Reset();
        }

        if (NoBorder.HasValue && bitAccordion.HasNotBeenSet(nameof(NoBorder)))
        {
            bitAccordion.NoBorder = NoBorder.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        if (NoContentRegion.HasValue && bitAccordion.HasNotBeenSet(nameof(NoContentRegion)))
        {
            bitAccordion.NoContentRegion = NoContentRegion.Value;
        }

        if (NoExpanderRotation.HasValue && bitAccordion.HasNotBeenSet(nameof(NoExpanderRotation)))
        {
            bitAccordion.NoExpanderRotation = NoExpanderRotation.Value;
        }

        if (Size.HasValue && bitAccordion.HasNotBeenSet(nameof(Size)))
        {
            bitAccordion.Size = Size.Value;

            bitAccordion.ClassBuilder.Reset();
        }

        if (Styles is not null && bitAccordion.HasNotBeenSet(nameof(Styles)))
        {
            bitAccordion.Styles = Styles;

            bitAccordion.StyleBuilder.Reset();
        }

        if (TransitionDuration.HasValue && bitAccordion.HasNotBeenSet(nameof(TransitionDuration)))
        {
            bitAccordion.TransitionDuration = TransitionDuration.Value;

            bitAccordion.StyleBuilder.Reset();
        }

        if (UnmountOnCollapse.HasValue && bitAccordion.HasNotBeenSet(nameof(UnmountOnCollapse)))
        {
            bitAccordion.UnmountOnCollapse = UnmountOnCollapse.Value;
        }
    }
}
