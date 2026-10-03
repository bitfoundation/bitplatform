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
    /// Every parameter a params object can supply: how it is read off the params object, and how it is read off and
    /// written onto the boundary, which is what lets the boundary put back the value a parameter held before once the
    /// params object stops supplying it.
    /// </summary>
    internal static readonly CascadedParameter[] Parameters =
    [
        new(nameof(AdditionalButtons), p => p.AdditionalButtons, b => b.AdditionalButtons, (b, v) => b.AdditionalButtons = (RenderFragment?)v),
        new(nameof(AutoFocus), p => p.AutoFocus, b => b.AutoFocus, (b, v) => b.AutoFocus = (bool)v!),
        new(nameof(Class), p => p.Class, b => b.Class, (b, v) => b.Class = (string?)v),
        new(nameof(Classes), p => p.Classes, b => b.Classes, (b, v) => b.Classes = (BitErrorBoundaryClassStyles?)v),
        new(nameof(CopiedText), p => p.CopiedText, b => b.CopiedText, (b, v) => b.CopiedText = (string?)v),
        new(nameof(CopyText), p => p.CopyText, b => b.CopyText, (b, v) => b.CopyText = (string?)v),
        // Dir reads through to the direction cascaded from above while it is not set, so what the boundary holds of
        // its own is the backing field, which is what has to be put back rather than the direction it happens to show.
        new(nameof(Dir), p => p.Dir, b => b.OwnDir, (b, v) => b.OwnDir = (BitDir?)v),
        new(nameof(ExceptionLabel), p => p.ExceptionLabel, b => b.ExceptionLabel, (b, v) => b.ExceptionLabel = (string?)v),
        new(nameof(ErrorTemplate), p => p.ErrorTemplate, b => b.ErrorTemplate, (b, v) => b.ErrorTemplate = (RenderFragment<BitErrorBoundaryContext>?)v),
        new(nameof(Footer), p => p.Footer, b => b.Footer, (b, v) => b.Footer = (RenderFragment?)v),
        new(nameof(HeadingLevel), p => p.HeadingLevel, b => b.HeadingLevel, (b, v) => b.HeadingLevel = (int?)v),
        new(nameof(HideHomeButton), p => p.HideHomeButton, b => b.HideHomeButton, (b, v) => b.HideHomeButton = (bool)v!),
        new(nameof(HideIcon), p => p.HideIcon, b => b.HideIcon, (b, v) => b.HideIcon = (bool)v!),
        new(nameof(HideRecoverButton), p => p.HideRecoverButton, b => b.HideRecoverButton, (b, v) => b.HideRecoverButton = (bool)v!),
        new(nameof(HideRefreshButton), p => p.HideRefreshButton, b => b.HideRefreshButton, (b, v) => b.HideRefreshButton = (bool)v!),
        new(nameof(HomeText), p => p.HomeText, b => b.HomeText, (b, v) => b.HomeText = (string?)v),
        new(nameof(HomeUrl), p => p.HomeUrl, b => b.HomeUrl, (b, v) => b.HomeUrl = (string?)v),
        new(nameof(Icon), p => p.Icon, b => b.Icon, (b, v) => b.Icon = (BitIconInfo?)v),
        new(nameof(IconName), p => p.IconName, b => b.IconName, (b, v) => b.IconName = (string?)v),
        new(nameof(IconTemplate), p => p.IconTemplate, b => b.IconTemplate, (b, v) => b.IconTemplate = (RenderFragment?)v),
        new(nameof(MaximumErrorCount), p => p.MaximumErrorCount, b => b.MaximumErrorCount, (b, v) => b.MaximumErrorCount = (int)v!),
        new(nameof(Message), p => p.Message, b => b.Message, (b, v) => b.Message = (string?)v),
        new(nameof(NoLogging), p => p.NoLogging, b => b.NoLogging, (b, v) => b.NoLogging = (bool)v!),
        new(nameof(RecoverOnNavigation), p => p.RecoverOnNavigation, b => b.RecoverOnNavigation, (b, v) => b.RecoverOnNavigation = (bool)v!),
        new(nameof(RecoverText), p => p.RecoverText, b => b.RecoverText, (b, v) => b.RecoverText = (string?)v),
        new(nameof(RefreshText), p => p.RefreshText, b => b.RefreshText, (b, v) => b.RefreshText = (string?)v),
        new(nameof(ShowCopyButton), p => p.ShowCopyButton, b => b.ShowCopyButton, (b, v) => b.ShowCopyButton = (bool)v!),
        new(nameof(ShowException), p => p.ShowException, b => b.ShowException, (b, v) => b.ShowException = (bool)v!),
        new(nameof(Style), p => p.Style, b => b.Style, (b, v) => b.Style = (string?)v),
        new(nameof(Styles), p => p.Styles, b => b.Styles, (b, v) => b.Styles = (BitErrorBoundaryClassStyles?)v),
        new(nameof(Title), p => p.Title, b => b.Title, (b, v) => b.Title = (string?)v),
    ];



    /// <summary>
    /// Updates the parameters of the specified <see cref="BitErrorBoundary"/> instance with every value that has
    /// been set on this object, wherever the boundary's own markup has not set that parameter.
    /// </summary>
    /// <remarks>
    /// This does not overwrite a value the markup of <paramref name="bitErrorBoundary"/> set. A boundary under a
    /// <see cref="BitParams"/> calls it on its own as its parameters are set.
    /// </remarks>
    /// <param name="bitErrorBoundary">The <see cref="BitErrorBoundary"/> instance whose parameters will be updated.</param>
    public void UpdateParameters(BitErrorBoundary bitErrorBoundary)
    {
        if (bitErrorBoundary is null) return;

        foreach (var parameter in Parameters)
        {
            var value = parameter.FromParams(this);

            if (value is null || bitErrorBoundary.IsSetByMarkup(parameter.Name)) continue;

            parameter.SetOnBoundary(bitErrorBoundary, value);
        }
    }



    /// <summary>
    /// One parameter a <see cref="BitErrorBoundaryParams"/> can supply to a <see cref="BitErrorBoundary"/>.
    /// </summary>
    internal sealed record CascadedParameter(string Name,
                                             Func<BitErrorBoundaryParams, object?> FromParams,
                                             Func<BitErrorBoundary, object?> FromBoundary,
                                             Action<BitErrorBoundary, object?> SetOnBoundary);
}
