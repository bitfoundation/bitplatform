using Microsoft.JSInterop;

namespace Bit.Blex.Blazor;

/// <summary>
/// Which Web Storage area a <see cref="BlexBrowserStorage"/> instance targets.
/// </summary>
public enum BlexBrowserStorageKind
{
    /// <summary>Persists across browser sessions (<c>window.localStorage</c>).</summary>
    Local,

    /// <summary>Cleared when the tab/session ends (<c>window.sessionStorage</c>).</summary>
    Session,
}
