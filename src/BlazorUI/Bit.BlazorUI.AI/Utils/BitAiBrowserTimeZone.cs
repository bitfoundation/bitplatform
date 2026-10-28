using System.Runtime.CompilerServices;

namespace Bit.BlazorUI;

/// <summary>
/// The time zone of the browser the AI components are drawn in, for the times they show while no
/// <see cref="BitAiUiOptions.TimeZone"/> is set.
/// </summary>
/// <remarks>
/// Under WebAssembly that is the local zone of the process already. Under Blazor Server the local zone is the server's,
/// so the browser is asked for its own once per <see cref="IJSRuntime"/> - once per circuit - and every component of the
/// circuit showing a time shares the answer.
/// </remarks>
internal static class BitAiBrowserTimeZone
{
    private static readonly ConditionalWeakTable<IJSRuntime, Task<TimeZoneInfo?>> _reads = new();

    /// <summary>
    /// The zone of the browser where it is not the local zone of the process: null under WebAssembly, and where the
    /// browser cannot tell or names a zone this machine does not know.
    /// </summary>
    internal static async Task<TimeZoneInfo?> GetAsync(IJSRuntime js)
    {
        if (OperatingSystem.IsBrowser()) return null;

        var read = _reads.GetValue(js, static js => ReadAsync(js));

        try
        {
            return await read;
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            // Asked again by the next component rather than answered with this failure for the rest of the circuit.
            if (_reads.TryGetValue(js, out var stored) && stored == read)
            {
                _reads.Remove(js);
            }

            return null;
        }
    }

    private static async Task<TimeZoneInfo?> ReadAsync(IJSRuntime js)
    {
        var id = await js.Invoke<string?>("BitBlazorUI.AiBrowserTimeZone.get");

        // An id the engine reports from CLDR may be one IANA has since renamed (Asia/Calcutta), which the zone database
        // of the machine still knows as a link.
        return id is { Length: > 0 } && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone) ? zone : null;
    }
}
