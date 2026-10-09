namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitErrorBoundary"/> component.
/// </summary>
/// <remarks>
/// What a <see cref="BitParams"/> carries is a default and never an override: a boundary takes each of these only
/// where its own markup leaves that parameter unset, so one wrapped around the body of a layout sets the look, the
/// texts and the behavior of every boundary in the app once, and any of them steps out by writing that parameter.
/// <br />
/// The callbacks are left out on purpose. Reporting every caught exception app-wide is what the
/// <see cref="Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger"/> the boundary logs through is for, and
/// <see cref="BitErrorBoundary.RecoverKeys"/> describes the state behind one boundary rather than a default for many.
/// </remarks>
public class BitErrorBoundaryParams : IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitErrorBoundary"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitErrorBoundary value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitErrorBoundary)}";



    public string Name => ParamName;



    /// <summary>
    /// The extra content of the footer of the default error UI, rendered after the default buttons.
    /// </summary>
    public RenderFragment? AdditionalButtons { get; set; }

    /// <summary>
    /// Moves the browser focus to the error UI as it appears.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// The CSS class of the root element of the error UI.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the default error UI.
    /// </summary>
    public BitErrorBoundaryClassStyles? Classes { get; set; }

    /// <summary>
    /// The text the Copy button carries while what it copied is still on the clipboard.
    /// </summary>
    public string? CopiedText { get; set; }

    /// <summary>
    /// The text of the Copy button.
    /// </summary>
    public string? CopyText { get; set; }

    /// <summary>
    /// The text directionality of the error UI.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// The accessible name of the exception details block.
    /// </summary>
    public string? ExceptionLabel { get; set; }

    /// <summary>
    /// The template of the error UI, receiving the caught exception along with the boundary's own actions.
    /// </summary>
    public RenderFragment<BitErrorBoundaryContext>? ErrorTemplate { get; set; }

    /// <summary>
    /// The footer content of the default error UI, replacing the default buttons.
    /// </summary>
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The heading level (1 to 6) of the title of the default error UI.
    /// </summary>
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// Prevents rendering the Home button of the default error UI.
    /// </summary>
    public bool? HideHomeButton { get; set; }

    /// <summary>
    /// Prevents rendering the icon of the default error UI.
    /// </summary>
    public bool? HideIcon { get; set; }

    /// <summary>
    /// Prevents rendering the Recover button of the default error UI.
    /// </summary>
    public bool? HideRecoverButton { get; set; }

    /// <summary>
    /// Prevents rendering the Refresh button of the default error UI.
    /// </summary>
    public bool? HideRefreshButton { get; set; }

    /// <summary>
    /// The text of the Home button.
    /// </summary>
    public string? HomeText { get; set; }

    /// <summary>
    /// The url of the home page for the Home button.
    /// </summary>
    public string? HomeUrl { get; set; }

    /// <summary>
    /// The icon to display using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon to render in place of the built-in illustration.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The template of the icon, replacing both the built-in illustration and the icon name.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The number of errors the boundary handles before it lets the next one through as fatal.
    /// </summary>
    public int? MaximumErrorCount { get; set; }

    /// <summary>
    /// The message rendered under the title of the default error UI.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Prevents the boundary from logging the caught exception through the app's IErrorBoundaryLogger.
    /// </summary>
    public bool? NoLogging { get; set; }

    /// <summary>
    /// Recovers the boundary when the reader navigates to another location.
    /// </summary>
    public bool? RecoverOnNavigation { get; set; }

    /// <summary>
    /// The text of the Recover button.
    /// </summary>
    public string? RecoverText { get; set; }

    /// <summary>
    /// The text of the Refresh button.
    /// </summary>
    public string? RefreshText { get; set; }

    /// <summary>
    /// Renders a Copy button in the footer of the default error UI.
    /// </summary>
    public bool? ShowCopyButton { get; set; }

    /// <summary>
    /// Renders the full text of the caught exception in the default error UI.
    /// </summary>
    public bool? ShowException { get; set; }

    /// <summary>
    /// The CSS style of the root element of the error UI.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the default error UI.
    /// </summary>
    public BitErrorBoundaryClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the default error UI.
    /// </summary>
    public string? Title { get; set; }



    /// <summary>
    /// Updates the parameters of the specified <see cref="BitErrorBoundary"/> instance with every value that has
    /// been set on this object, wherever the boundary's own markup has not set that parameter.
    /// </summary>
    /// <remarks>
    /// This does not overwrite a value the markup of <paramref name="bitErrorBoundary"/> set. A boundary under a
    /// <see cref="BitParams"/> calls it on its own as its parameters are set, and is what puts back a value this
    /// object stops supplying; a value written by calling this directly is the boundary's own from then on, exactly
    /// as with the params object of every other component.
    /// <br />
    /// A text is applied whenever it is not null, so an empty one is supplied too: an empty <see cref="Title"/> is
    /// how the heading is dropped.
    /// </remarks>
    /// <param name="bitErrorBoundary">The <see cref="BitErrorBoundary"/> instance whose parameters will be updated.</param>
    public void UpdateParameters(BitErrorBoundary bitErrorBoundary)
    {
        if (bitErrorBoundary is null) return;

        if (AdditionalButtons is not null && bitErrorBoundary.HasNotBeenSet(nameof(AdditionalButtons)))
        {
            bitErrorBoundary.AdditionalButtons = AdditionalButtons;
        }

        if (AutoFocus.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitErrorBoundary.AutoFocus = AutoFocus.Value;
        }

        if (Class is not null && bitErrorBoundary.HasNotBeenSet(nameof(Class)))
        {
            bitErrorBoundary.Class = Class;
        }

        if (Classes is not null && bitErrorBoundary.HasNotBeenSet(nameof(Classes)))
        {
            bitErrorBoundary.Classes = Classes;
        }

        if (CopiedText is not null && bitErrorBoundary.HasNotBeenSet(nameof(CopiedText)))
        {
            bitErrorBoundary.CopiedText = CopiedText;
        }

        if (CopyText is not null && bitErrorBoundary.HasNotBeenSet(nameof(CopyText)))
        {
            bitErrorBoundary.CopyText = CopyText;
        }

        if (Dir.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(Dir)))
        {
            bitErrorBoundary.Dir = Dir;
        }

        if (ExceptionLabel is not null && bitErrorBoundary.HasNotBeenSet(nameof(ExceptionLabel)))
        {
            bitErrorBoundary.ExceptionLabel = ExceptionLabel;
        }

        if (ErrorTemplate is not null && bitErrorBoundary.HasNotBeenSet(nameof(ErrorTemplate)))
        {
            bitErrorBoundary.ErrorTemplate = ErrorTemplate;
        }

        if (Footer is not null && bitErrorBoundary.HasNotBeenSet(nameof(Footer)))
        {
            bitErrorBoundary.Footer = Footer;
        }

        if (HeadingLevel.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(HeadingLevel)))
        {
            bitErrorBoundary.HeadingLevel = HeadingLevel;
        }

        if (HideHomeButton.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(HideHomeButton)))
        {
            bitErrorBoundary.HideHomeButton = HideHomeButton.Value;
        }

        if (HideIcon.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(HideIcon)))
        {
            bitErrorBoundary.HideIcon = HideIcon.Value;
        }

        if (HideRecoverButton.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(HideRecoverButton)))
        {
            bitErrorBoundary.HideRecoverButton = HideRecoverButton.Value;
        }

        if (HideRefreshButton.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(HideRefreshButton)))
        {
            bitErrorBoundary.HideRefreshButton = HideRefreshButton.Value;
        }

        if (HomeText is not null && bitErrorBoundary.HasNotBeenSet(nameof(HomeText)))
        {
            bitErrorBoundary.HomeText = HomeText;
        }

        if (HomeUrl is not null && bitErrorBoundary.HasNotBeenSet(nameof(HomeUrl)))
        {
            bitErrorBoundary.HomeUrl = HomeUrl;
        }

        // Icon and IconName are one setting - which icon is shown - and Icon wins over IconName, so a boundary that
        // picked its icon through either of them keeps it: a cascaded Icon filled in beside an IconName of its own
        // would replace the icon the boundary asked for. One the cascade wrote before the boundary set the other is
        // put back.
        var ownIcon = bitErrorBoundary.HasNotBeenSet(nameof(Icon)) is false || bitErrorBoundary.HasNotBeenSet(nameof(IconName)) is false;

        if (Icon is not null)
        {
            if (ownIcon)
            {
                bitErrorBoundary.ReleaseCascadeParameter(nameof(Icon));
            }
            else
            {
                bitErrorBoundary.Icon = Icon;
            }
        }

        if (IconName is not null)
        {
            if (ownIcon)
            {
                bitErrorBoundary.ReleaseCascadeParameter(nameof(IconName));
            }
            else
            {
                bitErrorBoundary.IconName = IconName;
            }
        }

        if (IconTemplate is not null && bitErrorBoundary.HasNotBeenSet(nameof(IconTemplate)))
        {
            bitErrorBoundary.IconTemplate = IconTemplate;
        }

        if (MaximumErrorCount.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(MaximumErrorCount)))
        {
            bitErrorBoundary.MaximumErrorCount = MaximumErrorCount.Value;
        }

        if (Message is not null && bitErrorBoundary.HasNotBeenSet(nameof(Message)))
        {
            bitErrorBoundary.Message = Message;
        }

        if (NoLogging.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(NoLogging)))
        {
            bitErrorBoundary.NoLogging = NoLogging.Value;
        }

        if (RecoverOnNavigation.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(RecoverOnNavigation)))
        {
            bitErrorBoundary.RecoverOnNavigation = RecoverOnNavigation.Value;
        }

        if (RecoverText is not null && bitErrorBoundary.HasNotBeenSet(nameof(RecoverText)))
        {
            bitErrorBoundary.RecoverText = RecoverText;
        }

        if (RefreshText is not null && bitErrorBoundary.HasNotBeenSet(nameof(RefreshText)))
        {
            bitErrorBoundary.RefreshText = RefreshText;
        }

        if (ShowCopyButton.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(ShowCopyButton)))
        {
            bitErrorBoundary.ShowCopyButton = ShowCopyButton.Value;
        }

        if (ShowException.HasValue && bitErrorBoundary.HasNotBeenSet(nameof(ShowException)))
        {
            bitErrorBoundary.ShowException = ShowException.Value;
        }

        if (Style is not null && bitErrorBoundary.HasNotBeenSet(nameof(Style)))
        {
            bitErrorBoundary.Style = Style;
        }

        if (Styles is not null && bitErrorBoundary.HasNotBeenSet(nameof(Styles)))
        {
            bitErrorBoundary.Styles = Styles;
        }

        if (Title is not null && bitErrorBoundary.HasNotBeenSet(nameof(Title)))
        {
            bitErrorBoundary.Title = Title;
        }
    }
}
