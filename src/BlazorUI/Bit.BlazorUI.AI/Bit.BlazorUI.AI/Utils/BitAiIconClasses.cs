namespace Bit.BlazorUI;

/// <summary>
/// Puts a glyph of this package's icon font on the icon of a core component an AI component renders: the classes of the
/// glyph (<c>bit-icon-ai bit-icon-ai--&lt;name&gt;</c>) go ahead of the ones the app gave that icon, and every other class the
/// app gave the component is kept as it was.
/// </summary>
/// <remarks>
/// Most of the glyphs the AI components draw are not in the core font, which carries only the ones its own components
/// draw, so they come from the subset this package ships (<c>fabric.mdl2.bit.blazoui.ai.scss</c>) - the way BitNavPanel
/// draws its toggle from the Extras' one. The glyph classes are handed in whole, as a string literal at the call site:
/// the publish-time trimming keeps a glyph by the literals it finds in the code an app can run, and
/// <c>BitAiGlyphFontTests</c> reads them out of the sources.
/// </remarks>
internal static class BitAiIconClasses
{
    /// <summary>
    /// The glyph classes followed by the app's own classes of the icon, if it gave any.
    /// </summary>
    internal static string Join(string iconClasses, string? classes) =>
        classes is { Length: > 0 } ? $"{iconClasses} {classes}" : iconClasses;

    /// <summary>
    /// The app's classes of a <see cref="BitButton"/>, with the glyph classes added to the icon.
    /// </summary>
    internal static BitButtonClassStyles With(string iconClasses, BitButtonClassStyles? classes) => new()
    {
        Root = classes?.Root,
        Icon = Join(iconClasses, classes?.Icon),
        Container = classes?.Container,
        Primary = classes?.Primary,
        Secondary = classes?.Secondary,
        HiddenContent = classes?.HiddenContent,
        LoadingContainer = classes?.LoadingContainer,
        Spinner = classes?.Spinner,
        LoadingLabel = classes?.LoadingLabel,
    };

    /// <summary>
    /// The app's classes of a <see cref="BitActionButton"/>, with the glyph classes added to the icon.
    /// </summary>
    internal static BitActionButtonClassStyles With(string iconClasses, BitActionButtonClassStyles? classes) => new()
    {
        Root = classes?.Root,
        Icon = Join(iconClasses, classes?.Icon),
        Content = classes?.Content,
        LoadingLabel = classes?.LoadingLabel,
        Spinner = classes?.Spinner,
    };

    /// <summary>
    /// The app's classes of a <see cref="BitToggleButton"/>, with the glyph classes added to the icon.
    /// </summary>
    internal static BitToggleButtonClassStyles With(string iconClasses, BitToggleButtonClassStyles? classes) => new()
    {
        Root = classes?.Root,
        CheckMark = classes?.CheckMark,
        Icon = Join(iconClasses, classes?.Icon),
        Checked = classes?.Checked,
        HiddenContent = classes?.HiddenContent,
        LoadingContainer = classes?.LoadingContainer,
        LoadingLabel = classes?.LoadingLabel,
        Spinner = classes?.Spinner,
        Text = classes?.Text,
    };

    /// <summary>
    /// The app's classes of a <see cref="BitTag"/>, with the glyph classes added to the icon.
    /// </summary>
    internal static BitTagClassStyles With(string iconClasses, BitTagClassStyles? classes) => new()
    {
        Root = classes?.Root,
        Content = classes?.Content,
        Label = classes?.Label,
        Text = classes?.Text,
        SecondaryText = classes?.SecondaryText,
        Icon = Join(iconClasses, classes?.Icon),
        Image = classes?.Image,
        SecondaryIcon = classes?.SecondaryIcon,
        Selected = classes?.Selected,
        SelectedIcon = classes?.SelectedIcon,
        DismissButton = classes?.DismissButton,
        DismissIcon = classes?.DismissIcon,
    };
}

/// <summary>
/// One set of classes built by <see cref="BitAiIconClasses"/>, kept while the app's own (compared by reference) and the
/// glyph stay the same.
/// </summary>
/// <remarks>
/// A core component resets its class builder whenever it is handed another instance of its classes, so an AI component
/// that re-renders on every streamed delta hands it the same instance until something actually changed. A field of
/// the component, never a copy: <see cref="Get"/> updates the cache it is called on.
/// </remarks>
internal struct BitAiIconClassesCache<TClassStyles> where TClassStyles : class
{
    private TClassStyles? _classes;
    private string? _iconClasses;
    private TClassStyles? _merged;

    /// <summary>
    /// The classes built for this glyph and these classes of the app, built again only when either changed.
    /// </summary>
    internal TClassStyles Get(string iconClasses, TClassStyles? classes, Func<string, TClassStyles?, TClassStyles> merge)
    {
        if (_merged is not null && ReferenceEquals(_classes, classes) && string.Equals(_iconClasses, iconClasses, StringComparison.Ordinal))
        {
            return _merged;
        }

        _classes = classes;
        _iconClasses = iconClasses;

        return _merged = merge(iconClasses, classes);
    }
}
