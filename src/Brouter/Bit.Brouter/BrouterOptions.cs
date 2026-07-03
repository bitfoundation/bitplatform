namespace Bit.Brouter;

/// <summary>
/// Global options for Bit.Brouter. Register via <c>builder.Services.AddBitBrouterServices(o =&gt; ...)</c>.
/// </summary>
public sealed class BrouterOptions
{
    /// <summary>
    /// Whether literal segment matching is case sensitive. Defaults to <c>false</c>
    /// to match React Router and Vue Router conventions (URLs are case-insensitive).
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Whether <c>/users</c> and <c>/users/</c> are treated as the same path.
    /// Defaults to <c>true</c>; trailing slashes are ignored.
    /// </summary>
    public bool IgnoreTrailingSlash { get; set; } = true;

    /// <summary>
    /// Whether to scroll to the top of the page after a successful navigation.
    /// Defaults to <see cref="BrouterScrollMode.None"/>.
    /// </summary>
    public BrouterScrollMode ScrollBehavior { get; set; } = BrouterScrollMode.None;

    /// <summary>
    /// Whether route <c>Loader</c> results are persisted across the SSR/prerender -&gt; interactive
    /// transition using <see cref="Microsoft.AspNetCore.Components.PersistentComponentState"/>, so a
    /// loader that ran during prerender is not run again (double-fetched) when the component becomes
    /// interactive. Defaults to <c>false</c>.
    /// </summary>
    /// <remarks>
    /// Enabling this serializes loader results with reflection-based <c>System.Text.Json</c>, which is
    /// not trimming/AOT-safe for arbitrary types. Only enable it when your loader data types are
    /// JSON-serializable and preserved under trimming. Restoration degrades gracefully: if a value can't
    /// be rehydrated the loader simply runs again, so a serialization mismatch never breaks navigation.
    /// </remarks>
    public bool PersistLoaderState { get; set; } = false;
}
