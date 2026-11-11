using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bit.Bcrdt;

/// <summary>
/// The metrics and traces bit Bcrdt publishes, for watching synchronization and storage in production.
/// </summary>
/// <remarks>
/// <para>
/// Everything is published on one <see cref="Meter"/> named <see cref="MeterName"/>, through
/// <c>System.Diagnostics.Metrics</c>: subscribe with OpenTelemetry (<c>.AddMeter(BcrdtDiagnostics.MeterName)</c>),
/// <c>dotnet-counters monitor --counters Bit.Bcrdt</c>, or a <see cref="MeterListener"/>. With nobody
/// listening an instrument costs a single check, so the metrics are always on.
/// </para>
/// <para>
/// No measurement carries a document identifier, a client identifier, a user or any content - only
/// counts, sizes, durations and the kind of thing measured. Identifiers routinely come from URLs and say
/// more than they should; correlate per document in your own code, where you know what is safe to record.
/// </para>
/// <list type="table">
/// <listheader><term>Instrument</term><description>What it counts</description></listheader>
/// <item><term><c>bcrdt.sync.messages.sent</c>, <c>bcrdt.sync.bytes.sent</c></term><description>Protocol messages a provider handed to its transport, by <c>bcrdt.message.kind</c>.</description></item>
/// <item><term><c>bcrdt.sync.messages.received</c>, <c>bcrdt.sync.bytes.received</c></term><description>Protocol messages a provider received, by <c>bcrdt.message.kind</c>.</description></item>
/// <item><term><c>bcrdt.sync.handshake.duration</c></term><description>Milliseconds from connecting to synced.</description></item>
/// <item><term><c>bcrdt.sync.reconnects</c></term><description>Reconnection attempts scheduled.</description></item>
/// <item><term><c>bcrdt.sync.errors</c></term><description>Failures reported through a provider's <c>Error</c> event, by <c>error.type</c>.</description></item>
/// <item><term><c>bcrdt.persistence.writes</c>, <c>bcrdt.persistence.bytes</c></term><description>Records written to a store.</description></item>
/// <item><term><c>bcrdt.persistence.failures</c></term><description>Store writes and compactions that failed.</description></item>
/// <item><term><c>bcrdt.persistence.compactions</c></term><description>Store compactions that completed.</description></item>
/// <item><term><c>bcrdt.host.documents</c>, <c>bcrdt.host.connections</c></term><description>Documents a document host has loaded, and connections to them.</description></item>
/// <item><term><c>bcrdt.host.messages.dropped</c></term><description>Messages a document host refused to apply or relay, by <c>bcrdt.reason</c>.</description></item>
/// </list>
/// <para>
/// Spans are started on one <see cref="System.Diagnostics.ActivitySource"/> named <see cref="ActivitySourceName"/>,
/// the meter's name: subscribe with OpenTelemetry (<c>.AddSource(BcrdtDiagnostics.ActivitySourceName)</c>) or an
/// <see cref="ActivityListener"/>. With nobody listening, starting one is a single check and nothing is
/// allocated. They follow the rule above - their tags are kinds, sizes, counts, outcomes and reasons, and a
/// failure is recorded by its exception type alone, because exception messages name documents.
/// </para>
/// <list type="table">
/// <listheader><term>Span</term><description>What it covers</description></listheader>
/// <item><term><c>bcrdt.sync.handshake</c></term><description>A provider's handshake, from its announcement until it is synced or abandoned, with <c>bcrdt.outcome</c> and <c>bcrdt.handshake.retries</c>.</description></item>
/// <item><term><c>bcrdt.update.apply</c></term><description>A provider applying a change a peer sent, by <c>bcrdt.message.kind</c> and <c>bcrdt.message.size</c>.</description></item>
/// <item><term><c>bcrdt.sync.reconnect</c></term><description>One reconnection attempt, with <c>bcrdt.reconnect.attempt</c> and <c>bcrdt.outcome</c>.</description></item>
/// <item><term><c>bcrdt.persistence.load</c>, <c>bcrdt.persistence.append</c>, <c>bcrdt.persistence.compact</c></term><description>A document persistence binding loading, writing a record and compacting, with <c>bcrdt.persistence.size</c>, <c>bcrdt.persistence.records</c> and <c>bcrdt.persistence.updates</c>.</description></item>
/// <item><term><c>bcrdt.host.load</c>, <c>bcrdt.host.unload</c></term><description>A document host loading a document and unloading it.</description></item>
/// <item><term><c>bcrdt.host.apply</c></term><description>A document host taking a client's change, with <c>bcrdt.outcome</c>, <c>bcrdt.validation</c> and, when dropped, <c>bcrdt.reason</c>.</description></item>
/// <item><term><c>bcrdt.host.handshake</c></term><description>A document host answering a client's handshake, with <c>bcrdt.reply.size</c>.</description></item>
/// </list>
/// </remarks>
public static class BcrdtDiagnostics
{
    /// <summary>The name of the meter every bit Bcrdt metric is published on.</summary>
    public const string MeterName = "Bit.Bcrdt";

    /// <summary>The name of the activity source every bit Bcrdt span is started on: the same as <see cref="MeterName"/>.</summary>
    public const string ActivitySourceName = MeterName;

    private static readonly string? AssemblyVersion = typeof(BcrdtDiagnostics).Assembly.GetName().Version?.ToString();

    internal static readonly Meter Meter = new(MeterName, AssemblyVersion);

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, AssemblyVersion);

    internal static readonly Counter<long> MessagesSent = Meter.CreateCounter<long>(
        "bcrdt.sync.messages.sent", "{message}", "Protocol messages handed to a transport.");

    internal static readonly Counter<long> BytesSent = Meter.CreateCounter<long>(
        "bcrdt.sync.bytes.sent", "By", "Bytes of protocol messages handed to a transport.");

    internal static readonly Counter<long> MessagesReceived = Meter.CreateCounter<long>(
        "bcrdt.sync.messages.received", "{message}", "Protocol messages received from a transport.");

    internal static readonly Counter<long> BytesReceived = Meter.CreateCounter<long>(
        "bcrdt.sync.bytes.received", "By", "Bytes of protocol messages received from a transport.");

    internal static readonly Histogram<double> HandshakeDuration = Meter.CreateHistogram<double>(
        "bcrdt.sync.handshake.duration", "ms", "How long a connection took to become synced.");

    internal static readonly Counter<long> Reconnects = Meter.CreateCounter<long>(
        "bcrdt.sync.reconnects", "{attempt}", "Reconnection attempts scheduled.");

    internal static readonly Counter<long> SyncErrors = Meter.CreateCounter<long>(
        "bcrdt.sync.errors", "{error}", "Failures a synchronization provider reported.");

    internal static readonly Counter<long> PersistenceWrites = Meter.CreateCounter<long>(
        "bcrdt.persistence.writes", "{record}", "Records written to a document store.");

    internal static readonly Counter<long> PersistenceBytes = Meter.CreateCounter<long>(
        "bcrdt.persistence.bytes", "By", "Bytes written to a document store.");

    internal static readonly Counter<long> PersistenceFailures = Meter.CreateCounter<long>(
        "bcrdt.persistence.failures", "{failure}", "Document store writes and compactions that failed.");

    internal static readonly Counter<long> PersistenceCompactions = Meter.CreateCounter<long>(
        "bcrdt.persistence.compactions", "{compaction}", "Document store compactions that completed.");

    internal static readonly Counter<long> PendingDiscards = Meter.CreateCounter<long>(
        "bcrdt.document.pending.discards", "{discard}", "Times a document discarded changes it was holding for missing dependencies because they outgrew its bound.");

    internal static KeyValuePair<string, object?> Kind(BcrdtSyncMessageKind kind) => new("bcrdt.message.kind", KindName(kind));

    internal static KeyValuePair<string, object?> Kind(ReadOnlySpan<byte> message) =>
        message.Length >= 2 && Enum.IsDefined((BcrdtSyncMessageKind)message[1])
            ? Kind((BcrdtSyncMessageKind)message[1])
            : new("bcrdt.message.kind", "unknown");

    // Fixed strings rather than ToString, so a tag never allocates on the hot path.
    private static string KindName(BcrdtSyncMessageKind kind) => kind switch
    {
        BcrdtSyncMessageKind.SyncStep1 => "sync_step1",
        BcrdtSyncMessageKind.SyncStep2 => "sync_step2",
        BcrdtSyncMessageKind.Update => "update",
        BcrdtSyncMessageKind.AwarenessUpdate => "awareness_update",
        BcrdtSyncMessageKind.AwarenessQuery => "awareness_query",
        BcrdtSyncMessageKind.Auth => "auth",
        BcrdtSyncMessageKind.Ping => "ping",
        BcrdtSyncMessageKind.Pong => "pong",
        BcrdtSyncMessageKind.SyncStep1Reply => "sync_step1_reply",
        _ => "unknown",
    };

    // ---------------------------------------------------------------------------------------------
    // Tracing
    // ---------------------------------------------------------------------------------------------

    internal const string HandshakeSpan = "bcrdt.sync.handshake";
    internal const string ReconnectSpan = "bcrdt.sync.reconnect";
    internal const string UpdateApplySpan = "bcrdt.update.apply";
    internal const string PersistenceLoadSpan = "bcrdt.persistence.load";
    internal const string PersistenceAppendSpan = "bcrdt.persistence.append";
    internal const string PersistenceCompactSpan = "bcrdt.persistence.compact";
    internal const string HostLoadSpan = "bcrdt.host.load";
    internal const string HostUnloadSpan = "bcrdt.host.unload";
    internal const string HostApplySpan = "bcrdt.host.apply";
    internal const string HostHandshakeSpan = "bcrdt.host.handshake";

    private const string OutcomeTag = "bcrdt.outcome";
    private const string ReasonTag = "bcrdt.reason";
    private const string ValidationTag = "bcrdt.validation";

    /// <summary>Starts a span, or returns <see langword="null"/> when nobody listens.</summary>
    /// <remarks>The span becomes <see cref="Activity.Current"/>: start it in the method that stops it.</remarks>
    internal static Activity? Start(string name) => ActivitySource.StartActivity(name);

    /// <summary>
    /// Starts a span that outlives the call starting it - or that belongs to an explicit
    /// <paramref name="parent"/> - without making it <see cref="Activity.Current"/>.
    /// </summary>
    /// <remarks>
    /// Starting a span makes it the ambient one for whatever the calling code does next: a handshake begun
    /// inside a transport callback would otherwise become the parent of everything the application traced
    /// on that flow until the handshake ended. Stop it with <see cref="Stop"/>.
    /// </remarks>
    internal static Activity? StartDetached(string name, Activity? parent = null)
    {
        if (!ActivitySource.HasListeners()) return null;

        Activity? previous = Activity.Current;
        Activity? activity = parent is null
            ? ActivitySource.StartActivity(name)
            : ActivitySource.StartActivity(name, ActivityKind.Internal, parent.Context);
        if (activity is not null) Activity.Current = previous;
        return activity;
    }

    /// <summary>Stops a span from <see cref="StartDetached"/>, leaving <see cref="Activity.Current"/> as it was.</summary>
    /// <remarks>Stopping a span makes its parent current, which on an unrelated flow is somebody else's span.</remarks>
    internal static void Stop(Activity? activity)
    {
        if (activity is null) return;

        Activity? current = Activity.Current;
        activity.Stop();
        if (!ReferenceEquals(current, activity)) Activity.Current = current;
    }

    /// <summary>Marks a span failed, by the exception's type alone: exception messages name documents.</summary>
    internal static void Fail(Activity? activity, Exception error)
    {
        if (activity is null) return;

        string type = error.GetType().Name;
        activity.SetTag("error.type", type);
        activity.SetStatus(ActivityStatusCode.Error, type);
    }

    /// <summary>Records how the operation a span covers ended.</summary>
    internal static void SetOutcome(Activity? activity, string outcome) => activity?.SetTag(OutcomeTag, outcome);

    /// <summary>Starts the span of a provider applying a change a peer sent; none for a message that carries no change.</summary>
    /// <remarks>A change that arrives during a handshake is part of it, so it is the handshake's child.</remarks>
    internal static Activity? StartUpdateApply(BcrdtSyncMessage message, Activity? handshake)
    {
        if (message.Kind is not (BcrdtSyncMessageKind.Update or BcrdtSyncMessageKind.SyncStep2) || !ActivitySource.HasListeners()) return null;

        Activity? activity = StartDetached(UpdateApplySpan, handshake);
        activity?.SetTag("bcrdt.message.kind", KindName(message.Kind)).SetTag("bcrdt.message.size", message.Payload.Length);
        return activity;
    }

    /// <summary>Starts the span of one reconnection attempt.</summary>
    internal static Activity? StartReconnect(int attempt) => Start(ReconnectSpan)?.SetTag("bcrdt.reconnect.attempt", attempt);

    /// <summary>Ends a provider's handshake span, if it has one. Safe on every path: the span ends once.</summary>
    /// <remarks>
    /// A handshake refused, or lost to a connection that failed, failed; one abandoned on purpose - a
    /// disconnection, disposal, a newer handshake - or concluded alone did not.
    /// </remarks>
    internal static void EndHandshake(ref Activity? handshake, string outcome, int retries, Exception? error = null)
    {
        Activity? activity = handshake;
        if (activity is null) return;
        handshake = null;

        activity.SetTag(OutcomeTag, outcome).SetTag("bcrdt.handshake.retries", retries);
        if (error is not null)
        {
            Fail(activity, error);
        }
        else if (outcome is "refused" or "faulted")
        {
            activity.SetTag("error.type", outcome);
            activity.SetStatus(ActivityStatusCode.Error, outcome);
        }
        Stop(activity);
    }

    /// <summary>How a handshake still under way ended when its connection's status moved on.</summary>
    internal static string HandshakeOutcome(BcrdtConnectionStatus status) => status switch
    {
        BcrdtConnectionStatus.Synced => "synced",
        BcrdtConnectionStatus.Faulted => "faulted",
        _ => "disconnected",
    };

    /// <summary>Records how many bytes a persistence span read or wrote, and how many records or updates they were.</summary>
    internal static void TagPersistence(Activity? activity, long? size = null, int? records = null, int? updates = null)
    {
        if (activity is null) return;

        if (size is { } bytes) activity.SetTag("bcrdt.persistence.size", bytes);
        if (records is { } recordCount) activity.SetTag("bcrdt.persistence.records", recordCount);
        if (updates is { } updateCount) activity.SetTag("bcrdt.persistence.updates", updateCount);
    }

    /// <summary>
    /// Starts the span of a document host handling a client's message: a change, or a handshake to answer.
    /// Presence and the rest are too frequent and too cheap to be worth one.
    /// </summary>
    internal static Activity? StartHostMessage(ReadOnlySpan<byte> message)
    {
        if (message.Length < 2 || !ActivitySource.HasListeners()) return null;

        BcrdtSyncMessageKind kind = (BcrdtSyncMessageKind)message[1];
        string? name = kind switch
        {
            BcrdtSyncMessageKind.Update or BcrdtSyncMessageKind.SyncStep2 => HostApplySpan,
            BcrdtSyncMessageKind.SyncStep1 or BcrdtSyncMessageKind.SyncStep1Reply => HostHandshakeSpan,
            _ => null,
        };
        if (name is null) return null;

        Activity? activity = Start(name);
        activity?.SetTag("bcrdt.message.kind", KindName(kind)).SetTag("bcrdt.message.size", message.Length);
        return activity;
    }

    /// <summary>Records how a host message span ended: dropped, its reason already recorded, or handled.</summary>
    internal static void EndHostMessage(Activity? activity)
    {
        if (activity is null || activity.GetTagItem(OutcomeTag) is not null) return;

        string outcome = activity.GetTagItem(ReasonTag) is not null ? "dropped"
            : activity.OperationName == HostApplySpan ? "applied"
            : "answered";
        activity.SetTag(OutcomeTag, outcome);
    }

    /// <summary>
    /// The host message span the calling code runs under, if any: a drop or a validator's verdict is
    /// recorded on it without threading it through every check that may make one.
    /// </summary>
    private static Activity? CurrentHostMessage =>
        Activity.Current is { } current &&
        ReferenceEquals(current.Source, ActivitySource) &&
        current.OperationName is HostApplySpan or HostHandshakeSpan
            ? current
            : null;

    /// <summary>Records why a document host dropped the message it is handling.</summary>
    internal static void HostDropped(string reason) => CurrentHostMessage?.SetTag(ReasonTag, reason);

    /// <summary>Records what a document host's validator said of the change it is handling.</summary>
    internal static void HostValidated(bool accepted)
    {
        Activity? activity = CurrentHostMessage;
        if (activity is null || activity.GetTagItem(ValidationTag) is not null) return;
        activity.SetTag(ValidationTag, accepted ? "accepted" : "rejected");
    }

    /// <summary>Records that a document host's validator threw, which refuses the change.</summary>
    internal static void HostValidationFailed(Exception error)
    {
        Activity? activity = CurrentHostMessage;
        activity?.SetTag(ValidationTag, "failed");
        Fail(activity, error);
    }

    /// <summary>Records how many bytes a document host's answer to a handshake came to.</summary>
    internal static void HostAnswered(List<byte[]> replies)
    {
        Activity? activity = CurrentHostMessage;
        if (activity is null) return;

        long size = 0;
        foreach (byte[] reply in replies) size += reply.Length;
        activity.SetTag("bcrdt.reply.size", size);
    }
}
