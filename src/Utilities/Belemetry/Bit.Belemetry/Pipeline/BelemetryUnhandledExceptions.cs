using System.Diagnostics;

namespace Bit.Belemetry;

/// <summary>
/// Records the .NET exceptions nothing caught (<see cref="AppDomain.UnhandledException"/>) or observed
/// (<see cref="TaskScheduler.UnobservedTaskException"/>), which reach no <c>ILogger</c>: the crash of a MAUI, WPF or
/// WinForms app, a server's background thread that threw, a faulted task nobody awaited.
/// </summary>
/// <remarks>
/// The process ends right after <see cref="AppDomain.UnhandledException"/>, so nothing async gets to complete there:
/// the record, and everything queued before it, is written to the request store synchronously
/// (<see cref="BelemetryPipeline.PersistForExit"/>), and the next launch sends it with the rest of the store. A crash
/// some platforms report elsewhere (.NET for Android, WinUI, WinForms' UI thread) comes through
/// <see cref="Belemetry.TrackBitBelemetryUnhandledException"/> to the same place.
/// </remarks>
internal sealed class BelemetryUnhandledExceptions(BelemetryPipeline pipeline) : IDisposable
{
    /// <summary>The instrumentation scope of its records; diagnostics count their exceptions as unhandled.</summary>
    internal const string ScopeName = "Bit.Belemetry.UnhandledExceptions";

    private static readonly BelemetryScope _scope = new(ScopeName, BelemetrySettings.SdkVersion);

    private int _subscribed;
    private int _disposed;
    private Exception? _lastUnhandled;

    /// <summary>Whether it listens to <see cref="AppDomain.UnhandledException"/> and <see cref="TaskScheduler.UnobservedTaskException"/> (<see cref="Subscribe"/>).</summary>
    public bool IsSubscribed => Volatile.Read(ref _subscribed) == 1 && Volatile.Read(ref _disposed) == 0;

    /// <summary>Starts listening to the process-wide events (<see cref="AppliesTo"/>). Idempotent.</summary>
    public void Subscribe()
    {
        if (Volatile.Read(ref _disposed) == 1 || Interlocked.Exchange(ref _subscribed, 1) == 1) return;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Whether the settings ask for these records: <see cref="BelemetryErrorOptions.CaptureUnhandledExceptions"/> with
    /// <see cref="BelemetryFeatures.Errors"/> configured, and something to take them - the .NET pipeline's export, or the
    /// diagnostic timeline (<paramref name="diagnostics"/>).
    /// </summary>
    public static bool AppliesTo(BelemetrySettings settings, bool diagnostics) =>
        settings.Options.Errors.CaptureUnhandledExceptions
        && (settings.ConfiguredFeatures & BelemetryFeatures.Errors) != 0
        && ((settings.CollectDotnet && settings.HasExporters) || diagnostics);

    /// <summary>Handles <see cref="AppDomain.UnhandledException"/>.</summary>
    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        // Anything that is not an Exception arrives wrapped in a RuntimeWrappedException.
        if (e.ExceptionObject is Exception exception) OnUnhandled(exception, e.IsTerminating);
    }

    /// <summary>Handles <see cref="TaskScheduler.UnobservedTaskException"/>.</summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) => OnUnobserved(e.Exception);

    /// <summary>
    /// Records an exception nothing caught; when it ends the process (<paramref name="terminating"/>), also saves what is
    /// queued for the next launch. The exception object recorded last is not recorded again: one crash may be reported
    /// twice, by a platform's own event (<see cref="Belemetry.TrackBitBelemetryUnhandledException"/>) and by
    /// <see cref="AppDomain.UnhandledException"/>.
    /// </summary>
    internal void OnUnhandled(Exception exception, bool terminating)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        // Before the record is queued: an export taking it from there would never complete as the process ends.
        if (terminating) pipeline.BeginExitHold();
        try
        {
            if (ReferenceEquals(Interlocked.Exchange(ref _lastUnhandled, exception), exception) is false)
            {
                Record(exception, terminating ? BelemetrySeverity.Fatal : BelemetrySeverity.Error, terminating ? "Critical" : "Error",
                       terminating ? "An unhandled exception ended the process." : "An unhandled exception was thrown.");
            }

            // Again for a crash reported twice: the queue is empty by then, unless the first report did not end the process.
            if (terminating) pipeline.PersistForExit();
        }
        catch
        {
            // The process is ending: nothing is left to tell.
        }
        finally
        {
            if (terminating) pipeline.EndExitHold();
        }
    }

    /// <summary>
    /// Records the exception of a faulted task that was never awaited or read. The process goes on (the event is not
    /// marked observed: what that does is the app's choice), so the record is exported as any other.
    /// </summary>
    internal void OnUnobserved(AggregateException exception)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        // A task's exceptions always come wrapped; one alone says more about itself - its type is what groups it.
        var flattened = exception.Flatten();
        Record(flattened.InnerExceptions.Count == 1 ? flattened.InnerExceptions[0] : flattened, BelemetrySeverity.Error, "Error",
               "A faulted task's exception was never observed.");
    }

    /// <summary>
    /// Queues the <see cref="BelemetryFeatures.Errors"/> event of <paramref name="exception"/>, under the span current
    /// where it is reported (<see cref="Activity.Current"/>), if any.
    /// </summary>
    private void Record(Exception exception, BelemetrySeverity severity, string severityText, string body)
    {
        try
        {
            var now = BelemetryTime.NowUnixNano();
            var record = new BelemetryLogRecord
            {
                TimeUnixNano = now,
                ObservedTimeUnixNano = now,
                Severity = severity,
                SeverityText = severityText,
                Body = body,
                // An event, as TrackException's: it reaches every exporter, whatever its minimum log level.
                EventName = BelemetrySemanticConventions.ExceptionEvent,
                Scope = _scope,
                Feature = BelemetryFeatures.Errors,
                Exception = exception,
            };

            record.Attributes.Set(BelemetrySemanticConventions.ExceptionType, exception.GetType().FullName);
            record.Attributes.Set(BelemetrySemanticConventions.ExceptionMessage, exception.Message);
            record.Attributes.Set(BelemetrySemanticConventions.ExceptionStacktrace, exception.ToString());

            // AppDomain.UnhandledException is raised on the thread that threw: the span it threw in, if any. A task's
            // exception is reported by the finalizer thread, which carries none of the task's context.
            if (Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity)
            {
                record.TraceId = activity.TraceId.ToHexString();
                record.SpanId = activity.SpanId.ToHexString();
                record.Flags = (int)activity.ActivityTraceFlags;
            }

            pipeline.Enqueue(record, resource: null, relayed: false, export: pipeline.IsCollecting);
        }
        catch
        {
            // Telemetry never adds a failure of its own to the one it reports.
        }
    }

    /// <summary>Stops listening to the process-wide events and recording; idempotent.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1 || Volatile.Read(ref _subscribed) == 0) return;

        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }
}
