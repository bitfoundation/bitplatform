using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCommandPalette"/> component.
/// </summary>
/// <remarks>
/// What belongs here is what the command palettes of an app agree on: their look, their words and how they search and
/// navigate. Whether a palette is open or inline, its commands, templates and callbacks are the state of a single palette.
/// <see cref="BitCommandPaletteOptions"/> stays the app-wide default these override, as the palette's own parameters do.
/// </remarks>
public partial class BitCommandPaletteParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCommandPalette"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCommandPalette value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.BitCommandPalette";



    // What BitParams reads of a params object by reflection is kept by its constructor rather than by the component
    // reading it, so the trimmer keeps it only for the params types an app creates.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCommandPaletteParams))]
    public BitCommandPaletteParams() { }



    public string Name => ParamName;



    /// <summary>
    /// Custom CSS classes for the different parts of the command palette.
    /// </summary>
    public BitCommandPaletteClassStyles? Classes { get; set; }

    /// <summary>
    /// Ignores the pointer hovering a row for choosing the active one.
    /// </summary>
    public bool? DisablePointerSelection { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.EnableHistory: recents, pins and frecency.
    /// </summary>
    public bool? EnableHistory { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.GroupWhenSearching.
    /// </summary>
    public bool? GroupWhenSearching { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.HighlightMatches.
    /// </summary>
    public bool? HighlightMatches { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.Loop.
    /// </summary>
    public bool? Loop { get; set; }

    /// <summary>
    /// The placeholder of the search box on the root page.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.SearchNestedCommands.
    /// </summary>
    public bool? SearchNestedCommands { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.ShowFooter.
    /// </summary>
    public bool? ShowFooter { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.ShowShortcuts.
    /// </summary>
    public bool? ShowShortcuts { get; set; }

    /// <summary>
    /// The words of the palette.
    /// </summary>
    public BitCommandPaletteStrings? Strings { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the command palette.
    /// </summary>
    public BitCommandPaletteClassStyles? Styles { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.TypoTolerance.
    /// </summary>
    public bool? TypoTolerance { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.VimBindings.
    /// </summary>
    public bool? VimBindings { get; set; }

    /// <summary>
    /// Overrides BitCommandPaletteOptions.Virtualize.
    /// </summary>
    public bool? Virtualize { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCommandPalette"/> instance with any values that have been set
    /// on this object, if those properties have not already been set on the <see cref="BitCommandPalette"/> itself.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCommandPalette"/>
    /// will be updated. This method does not overwrite existing values on <paramref name="bitCommandPalette"/>.
    /// </remarks>
    /// <param name="bitCommandPalette">
    /// The <see cref="BitCommandPalette"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCommandPalette bitCommandPalette)
    {
        if (bitCommandPalette is null) return;

        UpdateBaseParameters(bitCommandPalette);

        if (Classes is not null)
        {
            bitCommandPalette.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (DisablePointerSelection.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(DisablePointerSelection), DisablePointerSelection.Value, static c => c.DisablePointerSelection, static (c, v) => c.DisablePointerSelection = v);
        }

        if (EnableHistory.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(EnableHistory), EnableHistory, static c => c.EnableHistory, static (c, v) => c.EnableHistory = v);
        }

        if (GroupWhenSearching.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(GroupWhenSearching), GroupWhenSearching, static c => c.GroupWhenSearching, static (c, v) => c.GroupWhenSearching = v);
        }

        if (HighlightMatches.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(HighlightMatches), HighlightMatches, static c => c.HighlightMatches, static (c, v) => c.HighlightMatches = v);
        }

        if (Loop.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(Loop), Loop, static c => c.Loop, static (c, v) => c.Loop = v);
        }

        if (Placeholder is not null)
        {
            bitCommandPalette.TakeFromCascade(nameof(Placeholder), Placeholder, static c => c.Placeholder, static (c, v) => c.Placeholder = v);
        }

        if (SearchNestedCommands.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(SearchNestedCommands), SearchNestedCommands, static c => c.SearchNestedCommands, static (c, v) => c.SearchNestedCommands = v);
        }

        if (ShowFooter.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(ShowFooter), ShowFooter, static c => c.ShowFooter, static (c, v) => c.ShowFooter = v);
        }

        if (ShowShortcuts.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(ShowShortcuts), ShowShortcuts, static c => c.ShowShortcuts, static (c, v) => c.ShowShortcuts = v);
        }

        if (Strings is not null)
        {
            bitCommandPalette.TakeFromCascade(nameof(Strings), Strings, static c => c.Strings, static (c, v) => c.Strings = v);
        }

        if (Styles is not null)
        {
            bitCommandPalette.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (TypoTolerance.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(TypoTolerance), TypoTolerance, static c => c.TypoTolerance, static (c, v) => c.TypoTolerance = v);
        }

        if (VimBindings.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(VimBindings), VimBindings, static c => c.VimBindings, static (c, v) => c.VimBindings = v);
        }

        if (Virtualize.HasValue)
        {
            bitCommandPalette.TakeFromCascade(nameof(Virtualize), Virtualize, static c => c.Virtualize, static (c, v) => c.Virtualize = v);
        }
    }
}
