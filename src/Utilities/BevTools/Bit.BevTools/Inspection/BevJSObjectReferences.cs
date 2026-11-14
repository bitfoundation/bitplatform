using System.Collections;
using Microsoft.JSInterop;

namespace Bit.BevTools;

/// <summary>
/// The JS runtime's table of the <see cref="DotNetObjectReference"/>s it handed to JavaScript, by the id JavaScript
/// knows them by (<c>object#26</c> in a call from JS). Optional reflection, through <see cref="BlazorReflection"/>: null
/// when the table cannot be read.
/// </summary>
internal static class BevJSObjectReferences
{
    /// <summary>The session's table; null when its JS runtime is not a <see cref="JSRuntime"/> or the field is gone.</summary>
    public static IDictionary? Table(BevToolsSession session)
    {
        var runtime = BevRetentionAnalyzer.GetService(session, typeof(IJSRuntime));
        if (runtime is BevTrackingJSRuntime tracking)
        {
            runtime = tracking.Inner;
        }

        return BlazorReflection.TrackedDotNetObjectReferences(runtime);
    }

    /// <summary>What a reference keeps alive: <c>DotNetObjectReference&lt;T&gt;.Value</c>.</summary>
    public static object? Target(object? reference)
    {
        try
        {
            return reference?.GetType().GetProperty("Value")?.GetValue(reference);
        }
        catch
        {
            // A disposed reference throws on Value.
            return null;
        }
    }

    /// <summary>The object behind the id JavaScript called; null once the reference is disposed, or when unreadable.</summary>
    public static object? Target(IDictionary? table, long id)
    {
        try
        {
            return table is not null && table.Contains(id) ? Target(table[id]) : null;
        }
        catch
        {
            return null;
        }
    }
}
