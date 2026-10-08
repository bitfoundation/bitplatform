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
    public BitPlacement? ExpanderIconPlacement { get; set; }

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

        if (Background.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(Background), Background.Value, static a => a.Background, static (a, v) => a.Background = v);
        }

        if (Border.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(Border), Border.Value, static a => a.Border, static (a, v) => a.Border = v);
        }

        if (Classes is not null)
        {
            bitAccordion.TakeFromCascade(nameof(Classes), Classes, static a => a.Classes, static (a, v) => a.Classes = v);
        }

        if (ExpandedExpanderIcon is not null)
        {
            bitAccordion.TakeFromCascade(nameof(ExpandedExpanderIcon), ExpandedExpanderIcon, static a => a.ExpandedExpanderIcon, static (a, v) => a.ExpandedExpanderIcon = v);
        }

        if (ExpandedExpanderIconName.HasValue())
        {
            bitAccordion.TakeFromCascade(nameof(ExpandedExpanderIconName), ExpandedExpanderIconName, static a => a.ExpandedExpanderIconName, static (a, v) => a.ExpandedExpanderIconName = v);
        }

        if (ExpanderIcon is not null)
        {
            bitAccordion.TakeFromCascade(nameof(ExpanderIcon), ExpanderIcon, static a => a.ExpanderIcon, static (a, v) => a.ExpanderIcon = v);
        }

        if (ExpanderIconName.HasValue())
        {
            bitAccordion.TakeFromCascade(nameof(ExpanderIconName), ExpanderIconName, static a => a.ExpanderIconName, static (a, v) => a.ExpanderIconName = v);
        }

        if (ExpanderIconPlacement.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(ExpanderIconPlacement), ExpanderIconPlacement.Value, static a => a.ExpanderIconPlacement, static (a, v) => a.ExpanderIconPlacement = v);
        }

        if (ExpandOnPrint.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(ExpandOnPrint), ExpandOnPrint.Value, static a => a.ExpandOnPrint, static (a, v) => a.ExpandOnPrint = v);
        }

        // A nested accordion takes the level below the one holding it, so only a top-level one is given the default.
        if (HeadingLevel.HasValue)
        {
            if (bitAccordion.ParentHeadingLevel is null)
            {
                bitAccordion.TakeFromCascade(nameof(HeadingLevel), HeadingLevel.Value, static a => a.HeadingLevel, static (a, v) => a.HeadingLevel = v);
            }
            else
            {
                bitAccordion.ReleaseFromCascade(nameof(HeadingLevel));
            }
        }

        if (HiddenUntilFound.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(HiddenUntilFound), HiddenUntilFound.Value, static a => a.HiddenUntilFound, static (a, v) => a.HiddenUntilFound = v);
        }

        if (HideExpanderIcon.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(HideExpanderIcon), HideExpanderIcon.Value, static a => a.HideExpanderIcon, static (a, v) => a.HideExpanderIcon = v);
        }

        if (LazyContent.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(LazyContent), LazyContent.Value, static a => a.LazyContent, static (a, v) => a.LazyContent = v);
        }

        if (MaxHeight.HasValue())
        {
            bitAccordion.TakeFromCascade(nameof(MaxHeight), MaxHeight, static a => a.MaxHeight, static (a, v) => a.MaxHeight = v);
        }

        if (NoBorder.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static a => a.NoBorder, static (a, v) => a.NoBorder = v);
        }

        if (NoContentRegion.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(NoContentRegion), NoContentRegion.Value, static a => a.NoContentRegion, static (a, v) => a.NoContentRegion = v);
        }

        if (NoExpanderRotation.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(NoExpanderRotation), NoExpanderRotation.Value, static a => a.NoExpanderRotation, static (a, v) => a.NoExpanderRotation = v);
        }

        if (Size.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(Size), Size.Value, static a => a.Size, static (a, v) => a.Size = v);
        }

        if (Styles is not null)
        {
            bitAccordion.TakeFromCascade(nameof(Styles), Styles, static a => a.Styles, static (a, v) => a.Styles = v);
        }

        if (TransitionDuration.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(TransitionDuration), TransitionDuration.Value, static a => a.TransitionDuration, static (a, v) => a.TransitionDuration = v);
        }

        if (UnmountOnCollapse.HasValue)
        {
            bitAccordion.TakeFromCascade(nameof(UnmountOnCollapse), UnmountOnCollapse.Value, static a => a.UnmountOnCollapse, static (a, v) => a.UnmountOnCollapse = v);
        }
    }
}
