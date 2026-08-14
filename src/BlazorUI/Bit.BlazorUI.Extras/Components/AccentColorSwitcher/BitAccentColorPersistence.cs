namespace Bit.BlazorUI;

/// <summary>
/// Which stores the accent preference is persisted to when an accent is applied. The two stores
/// serve different readers: localStorage is the client-side copy the runtime (and the inline head
/// script) restores from, while the cookie is the only copy the server can read - it is what lets
/// SSR prerender the page with the visitor's accent (seeding the active swatch through
/// <see cref="BitAccentColorService.SeedFromPrerender"/> and painting via
/// <see cref="BitAccentColorSsr"/>). Writing both also self-heals either store going missing on
/// its own.
/// </summary>
[Flags]
public enum BitAccentColorPersistence
{
    /// <summary>
    /// Nothing is persisted (the default): the accent applies for the current session only and is
    /// gone on the next load. Stores left behind by an earlier configuration are cleaned up on the
    /// next apply.
    /// </summary>
    None = 0,

    /// <summary>The localStorage entry the client restores the accent from.</summary>
    LocalStorage = 1,

    /// <summary>
    /// The cookie that carries the preference to the server, so SSR can prerender the visitor's
    /// accent. Required for the server halves of <see cref="BitAccentColorSsr"/> to see anything.
    /// </summary>
    Cookie = 2,

    /// <summary>Both stores.</summary>
    All = LocalStorage | Cookie,
}
