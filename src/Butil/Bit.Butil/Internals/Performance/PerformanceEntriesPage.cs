using System.Text.Json;

namespace Bit.Butil;

/// <summary>
/// One reply of a <see cref="Performance"/> entry read: as many entries as fit in one interop message,
/// and the token that fetches the rest when they did not all fit.
/// </summary>
/// <remarks>
/// On Blazor Server every reply is a single SignalR message, and the hub closes the connection on one
/// over its receive limit (32 KB by default) rather than failing the call - and a full resource buffer
/// is a couple of hundred KB of JSON. So <c>performance.ts</c> answers a large read a page at a time and
/// the service stitches the pages back together; the caller sees one array either way.
/// <br/>
/// The entries stay <see cref="JsonElement"/>s because one read can carry several entry shapes, and the
/// typed reads convert them the same way the typed observer does.
/// </remarks>
internal sealed class PerformanceEntriesPage
{
    public JsonElement[] Entries { get; set; } = [];

    /// <summary>The token for <c>BitButil.performance.nextEntries</c>, or null on the last page.</summary>
    public string? Next { get; set; }
}
