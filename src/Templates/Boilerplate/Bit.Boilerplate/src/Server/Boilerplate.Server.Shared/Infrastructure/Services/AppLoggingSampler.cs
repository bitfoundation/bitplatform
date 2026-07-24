using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

public class AppLoggingSampler(IServiceProvider serviceProvider) : LoggingSampler
{
    // A thread-safe counter per category to sample ~5% of logs (every 20th entry per category)
    private static readonly ConcurrentDictionary<string, LogCounter> _categoryCounters = new();

    /// <summary>
    /// Evaluates whether a specific log entry should be sampled and recorded.
    /// Critical or unhandled errors are always recorded (100%), while warnings, 
    /// information logs, and known/transient errors are sampled at a 5% rate.
    /// </summary>
    public override bool ShouldSample<TState>(in LogEntry<TState> logEntry)
    {
        var logLevel = logEntry.LogLevel;
        var exception = logEntry.Exception;

        // 1. Identify high-severity logs (Errors and Criticals)
        bool isCriticalOrError = logLevel is LogLevel.Critical or LogLevel.Error;

        // 2. Determine if the exception is an expected business/known error or a temporary transient failure
        bool isKnownOrTransient = false;
        if (exception != null)
        {
            isKnownOrTransient = exception is KnownException ||
                                 serviceProvider.GetRequiredService<SharedExceptionHandler>().IsTransientException(exception);
        }

        // 3. Always capture 100% of critical/error logs that are neither known nor transient (unhandled/unexpected bugs)
        if (isCriticalOrError && !isKnownOrTransient)
        {
            return true;
        }

        // 4. For the rest of the logs (Info, Warning, or expected errors), apply a consistent 5% sampling rate
        return ShouldSample(logEntry.Category);
    }

    /// <summary>
    /// Computes a deterministic 5% sampling decision. 
    /// If an active trace context exists, it samples consistently by TraceId to keep whole requests together.
    /// If outside a trace context, it samples every 20th log entry per category using an in-memory counter.
    /// </summary>
    private static bool ShouldSample(string category)
    {
        var currentActivity = Activity.Current;

        // --- SCENARIO A: Active Telemetry Trace Context Exists ---
        if (currentActivity != null && currentActivity.TraceId != default)
        {
            // Re-use the existing OpenTelemetry sampling logic to ensure consistent sampling across the entire trace
            return AppOpenTelemetryProcessor.ShouldSample(currentActivity);
        }

        // --- SCENARIO B: Fallback (No active trace context) ---
        // Get or create a lightweight counter for this specific category
        var counter = _categoryCounters.GetOrAdd(category, _ => new LogCounter());

        // Atomically increment the counter and safely wrap around
        long currentCount = counter.Increment();

        // Return true for every 20th log entry (5% sampling)
        return (currentCount % 20) == 0;
    }

    // Helper class to encapsulate atomic counting
    private sealed class LogCounter
    {
        private long _value = -1;

        public long Increment()
        {
            return Interlocked.Increment(ref _value);
        }
    }
}
