using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bonboarding;

/// <summary>
/// Classifies failures of the browser layer and of the store. Every survivable failure degrades to
/// "no browser available" instead of failing a tour, but only the ones that are a normal part of the
/// Blazor lifecycle are logged quietly: a JavaScript error or an unexpected failure in a custom
/// store is still survivable, and is logged as a warning so it does not go unnoticed.
/// </summary>
internal static class BonboardingInteropFailure
{
    /// <summary>
    /// The start of the message Blazor uses when JavaScript interop is attempted while prerendering
    /// or before the circuit has connected. ASP.NET Core does not localize its exception messages.
    /// </summary>
    private const string PrerenderMessagePrefix = "JavaScript interop calls cannot be issued";

    /// <summary>True when a tour should carry on as if the browser were not there.</summary>
    public static bool IsTransient(Exception ex)
        => ex is JSDisconnectedException
            or JSException
            or ObjectDisposedException
            or OperationCanceledException
            or InvalidOperationException;

    /// <summary>
    /// True for prerendering, a dropped circuit, a disposed scope and cancellation: expected,
    /// routine, and not worth more than a debug message.
    /// </summary>
    public static bool IsExpected(Exception ex)
        => ex is JSDisconnectedException or ObjectDisposedException or OperationCanceledException
            || (ex is InvalidOperationException && ex.Message.StartsWith(PrerenderMessagePrefix, StringComparison.Ordinal));

    /// <summary>The level a survivable failure is logged at.</summary>
    public static LogLevel LevelFor(Exception ex) => IsExpected(ex) ? LogLevel.Debug : LogLevel.Warning;
}
