using Microsoft.JSInterop;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Asks the browser to keep origin storage out of the LRU eviction bucket.
/// </summary>
/// <remarks>
/// Not on the frozen admin contract - persistence is an origin setting, not a per-backend one.
/// </remarks>
public static class BsqlitePersistence
{
    public static async Task<bool> RequestAsync(
        IJSRuntime js,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(js);
        var module = await js
            .InvokeAsync<IJSObjectReference>(
                "import",
                cancellationToken,
                "./_content/Bit.Bsqlite/bit-bsqlite-persist.js")
            .ConfigureAwait(false);

        try
        {
            return await module
                .InvokeAsync<bool>("requestPersistence", cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
    }
}
