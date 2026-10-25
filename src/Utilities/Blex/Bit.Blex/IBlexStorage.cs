using System.Text.Json.Nodes;

namespace Bit.Blex;

/// <summary>
/// Storage-agnostic, asynchronous key/value sink used to persist store state. Implementations
/// might wrap browser <c>localStorage</c>/<c>sessionStorage</c>, a file, a database, etc. Browser
/// storage ships as <see cref="Blazor.BlexBrowserStorage"/>; <c>Bit.Blex.Maui</c> adds a <c>Preferences</c>-backed one.
/// </summary>
public interface IBlexStorage
{
    /// <summary>Reads the raw string stored under <paramref name="key"/>, or <c>null</c> if absent.</summary>
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Writes <paramref name="value"/> under <paramref name="key"/>.</summary>
    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes any value stored under <paramref name="key"/>.</summary>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
