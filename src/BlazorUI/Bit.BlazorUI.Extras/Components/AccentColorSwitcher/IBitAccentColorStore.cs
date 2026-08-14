namespace Bit.BlazorUI;

/// <summary>
/// An app-supplied store for the accent preference, for hosts where the built-in web stores are not
/// the right home for it - e.g. a Blazor Hybrid app persisting through native preferences (MAUI
/// Preferences, isolated storage) instead of the webview's localStorage. Register an implementation
/// in DI (with a lifetime compatible with <see cref="BitAccentColorService"/>'s) and the service
/// restores from it first - ahead of the stores <see cref="BitAccentColorPersistence"/> enables -
/// and (re)writes it on every apply, independently of those flags, so an app can combine it with
/// the built-in stores or run on it alone with <see cref="BitAccentColorPersistence.None"/>.
/// </summary>
/// <remarks>
/// A restored value goes through the same validation as the built-in stores: an accent outside the
/// configured ones is still restored (as <see cref="BitAccentColorService.ApplyAsync"/> can apply
/// one), re-validated as plain hex, and anything that is not hex at all is treated as "nothing
/// persisted". Store failures are logged and swallowed - losing the preference is never an error
/// the app sees.
/// </remarks>
public interface IBitAccentColorStore
{
    /// <summary>
    /// Reads the persisted accent color, or <see langword="null"/> when none is stored.
    /// </summary>
    Task<string?> GetAccentAsync();

    /// <summary>
    /// Persists the accent color.
    /// </summary>
    Task SetAccentAsync(string accent);

    /// <summary>
    /// Removes the persisted accent color. Called when the accent reverts to the packaged
    /// palette's own primary, which is stored as "no preference" rather than as a value.
    /// </summary>
    Task RemoveAccentAsync();
}
