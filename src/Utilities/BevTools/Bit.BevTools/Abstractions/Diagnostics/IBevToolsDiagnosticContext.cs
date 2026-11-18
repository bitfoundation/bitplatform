
namespace Bit.BevTools;

/// <summary>Everything a rule may look at. Rules must not mutate state and should be cheap (they run on the UI refresh cadence).</summary>
public interface IBevToolsDiagnosticContext
{
    DateTimeOffset Now { get; }

    IReadOnlyList<BevToolsComponentSnapshot> Components { get; }

    /// <summary>True when the component is a framework or library internal the application cannot change.</summary>
    bool IsFrameworkInternal(long instanceId);

    /// <summary>Recent timeline events, oldest first.</summary>
    IReadOnlyList<BevToolsEvent> Events { get; }

    /// <summary>Events of a kind within the trailing window.</summary>
    IEnumerable<BevToolsEvent> EventsInWindow(BevToolsEventKind kind, TimeSpan window);

    /// <summary>Access to configured thresholds. Returns null when the option type is unknown.</summary>
    T? GetOptions<T>() where T : class;

    /// <summary>Platform name reported by the renderer ("Server", "WebAssembly", "Static", "WebView") or null when unknown.</summary>
    string? Platform { get; }
}
