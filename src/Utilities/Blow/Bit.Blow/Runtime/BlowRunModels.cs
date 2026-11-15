namespace Bit.Blow;

/// <summary>
/// Runs one node: reads its inputs from <see cref="BlowNodeRunContext"/>, does the work, and says which of its outputs
/// carry a value on to the nodes after it. Throw to fail the node; throw <see cref="BlowNodeException"/> for a message
/// that may be shown to the user.
/// </summary>
public delegate Task<BlowNodeResult> BlowNodeHandler(BlowNodeRunContext context);

/// <summary>How a node with several incoming edges decides whether it runs, once everything before it has settled.</summary>
public enum BlowJoinMode
{
    /// <summary>
    /// Runs when at least one incoming edge carried a value; skipped when none did. The join after a branch: only one
    /// of the branches reaches it.
    /// </summary>
    Any,

    /// <summary>Runs only when every incoming edge carried a value; skipped otherwise. The join after a parallel split.</summary>
    All,
}

/// <summary>What the runner does with a node no handler is registered for.</summary>
public enum BlowMissingHandlerBehavior
{
    /// <summary>The run is refused before anything starts, with a <see cref="BlowRunCodes.MissingHandler"/> problem per node.</summary>
    Refuse,

    /// <summary>The node succeeds without doing anything, passing its first input value on (or nothing, when it has none).</summary>
    PassThrough,

    /// <summary>The node is skipped, and so is everything that only it reached.</summary>
    Skip,
}

/// <summary>How a run ended.</summary>
public enum BlowRunOutcome
{
    /// <summary>Every node that ran succeeded. Nodes a branch passed by are skipped, which is not a failure.</summary>
    Succeeded,

    /// <summary>At least one node failed, other than a failure its <see cref="BlowNodeRunOptions.ErrorOutput"/> carried on.</summary>
    Failed,

    /// <summary>The run was cancelled through its <see cref="CancellationToken"/> before it finished.</summary>
    Cancelled,

    /// <summary>The run took longer than <see cref="BlowRunnerOptions.RunTimeout"/> and was stopped.</summary>
    TimedOut,

    /// <summary>The run never started: the graph has a cycle, a node has no handler, or validation found an error. See <see cref="BlowRunResult.Problems"/>.</summary>
    Refused,
}

/// <summary>What happened, in a run's event stream (<see cref="BlowRunRequest.OnEvent"/>, <see cref="BlowRunResult.Events"/>).</summary>
public enum BlowRunEventKind
{
    /// <summary>The run began. Every node in it is queued.</summary>
    RunStarted,

    /// <summary>The node is waiting for its turn.</summary>
    NodeQueued,

    /// <summary>An attempt of the node started, or it went back to work after waiting.</summary>
    NodeStarted,

    /// <summary>The node said how far along it is (<see cref="BlowNodeRunContext.Report"/>).</summary>
    NodeProgress,

    /// <summary>The node is waiting for something outside the run (<see cref="BlowNodeRunContext.WaitAsync{T}"/>).</summary>
    NodeWaiting,

    /// <summary>An attempt failed and the node will be tried again after <see cref="BlowRunEvent.RetryDelay"/>.</summary>
    NodeRetrying,

    /// <summary>The node finished successfully.</summary>
    NodeSucceeded,

    /// <summary>The node failed, after every attempt its retry policy allowed.</summary>
    NodeFailed,

    /// <summary>The node was not run: a branch passed it by, something before it failed, or the run stopped first.</summary>
    NodeSkipped,

    /// <summary>The node was working when the run was cancelled.</summary>
    NodeCancelled,

    /// <summary>The node's result was taken from a checkpoint instead of running it again (<see cref="BlowRunRequest.Resume"/>).</summary>
    NodeRestored,

    /// <summary>A value went along the edge (<see cref="BlowRunEvent.EdgeId"/>) to the node after it.</summary>
    EdgeActivated,

    /// <summary>The run ended. <see cref="BlowRunEvent.Outcome"/> says how.</summary>
    RunFinished,
}

/// <summary>
/// One thing that happened in a run. Events carry what to show - statuses, messages, timings - and never a node's
/// input or output values, so a log of them cannot leak a payload.
/// </summary>
public sealed record BlowRunEvent(BlowRunEventKind Kind, string RunId, DateTimeOffset Timestamp)
{
    /// <summary>The node the event is about, or null for the run's own events.</summary>
    public string? NodeId { get; init; }

    /// <summary>The edge a value went along, for <see cref="BlowRunEventKind.EdgeActivated"/>.</summary>
    public string? EdgeId { get; init; }

    /// <summary>The node's status after the event, or null for the run's own events and edge events.</summary>
    public BlowRunStatus? Status { get; init; }

    /// <summary>What to say about it: a progress note, a failure that is safe to show, why a node was skipped.</summary>
    public string? Message { get; init; }

    /// <summary>The attempt the event belongs to, starting at 1; 0 when the node has not started.</summary>
    public int Attempt { get; init; }

    /// <summary>How far along the node is, from 0 to 1, when its handler said.</summary>
    public double? Progress { get; init; }

    /// <summary>How long the runner waits before the next attempt, for <see cref="BlowRunEventKind.NodeRetrying"/>.</summary>
    public TimeSpan? RetryDelay { get; init; }

    /// <summary>How the run ended, for <see cref="BlowRunEventKind.RunFinished"/>.</summary>
    public BlowRunOutcome? Outcome { get; init; }
}

/// <summary>
/// How often to try a node again after it fails. Attempts wait <see cref="Delay"/>, multiplied by
/// <see cref="BackoffFactor"/> after each one, up to <see cref="MaxDelay"/>.
/// </summary>
public sealed record BlowRetryPolicy
{
    /// <summary>No retries: one attempt.</summary>
    public static readonly BlowRetryPolicy None = new() { MaxAttempts = 1 };

    /// <summary>The number of attempts in all, the first included. At least 1.</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>The wait before the second attempt.</summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>What each wait is multiplied by for the next: 2 doubles it (exponential backoff), 1 keeps it constant.</summary>
    public double BackoffFactor { get; init; } = 2;

    /// <summary>The longest wait between two attempts.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether a failure is worth another attempt. Null retries every failure but a <see cref="BlowNodeException"/>
    /// whose <see cref="BlowNodeException.IsTransient"/> is false. A timeout counts as a failure.
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; init; }

    /// <summary>The wait before the given attempt (2 for the first retry).</summary>
    public TimeSpan DelayBefore(int attempt)
    {
        if (attempt <= 1 || Delay <= TimeSpan.Zero) return TimeSpan.Zero;
        var ms = Delay.TotalMilliseconds * Math.Pow(Math.Max(BackoffFactor, 1), attempt - 2);
        var max = MaxDelay.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(double.IsFinite(ms) ? Math.Min(ms, max) : max);
    }

    internal bool Allows(Exception error)
        => ShouldRetry?.Invoke(error) ?? error is not BlowNodeException { IsTransient: false };
}

/// <summary>
/// How a node runs: how long an attempt may take, how often it is tried again, and how it joins its inputs. Given per
/// node type when its handler is registered (<see cref="BlowRunner.Handle(string, BlowNodeHandler, BlowNodeRunOptions?)"/>)
/// and per node through <see cref="BlowRunnerOptions.NodeOptions"/>.
/// </summary>
public sealed record BlowNodeRunOptions
{
    /// <summary>The options a node has when nothing says otherwise: one attempt, no timeout, <see cref="BlowJoinMode.Any"/>.</summary>
    public static readonly BlowNodeRunOptions Default = new();

    /// <summary>
    /// The longest one attempt may take, or null for no limit. A slower attempt is cancelled and counts as a failure,
    /// whether or not its handler heeds the token. <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>, or longer
    /// than a timer can wait (about 49 days), is no limit.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>How often to try again after a failure. Null is <see cref="BlowRetryPolicy.None"/>.</summary>
    public BlowRetryPolicy? Retry { get; init; }

    /// <summary>Whether the node needs a value on every incoming edge or on any one of them.</summary>
    public BlowJoinMode Join { get; init; } = BlowJoinMode.Any;

    /// <summary>
    /// The id of a source handle that takes over when the node fails: its edges carry a <see cref="BlowRunError"/> to
    /// the nodes after it, which run as they would on a value, while the node's other edges carry nothing. Without
    /// one - or with one that no edge leaves from - a failure skips everything that depends on the node.
    /// </summary>
    public string? ErrorOutput { get; init; }
}

/// <summary>The settings of a <see cref="BlowRunner"/>.</summary>
public sealed class BlowRunnerOptions
{
    /// <summary>
    /// The most nodes working at once - in a run and the runs its nodes start together, a node waiting for its sub-run
    /// counting as none. 1 runs one node at a time in dependency order. At least 1; 4 by default.
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// The longest a run may take, or null for no limit. A slower run ends as <see cref="BlowRunOutcome.TimedOut"/>.
    /// <see cref="Timeout.InfiniteTimeSpan"/>, or longer than a timer can wait (about 49 days), is no limit.
    /// </summary>
    public TimeSpan? RunTimeout { get; set; }

    /// <summary>When true, the first failure cancels the rest of the run. When false (the default), independent branches go on.</summary>
    public bool StopOnFailure { get; set; }

    /// <summary>What happens to a node no handler is registered for.</summary>
    public BlowMissingHandlerBehavior MissingHandler { get; set; } = BlowMissingHandlerBehavior.Refuse;

    /// <summary>
    /// Options for one node, over those its type was registered with - a timeout read from the node's data, say. Return
    /// null to keep the type's.
    /// </summary>
    public Func<BlowNode, BlowNodeRunOptions?>? NodeOptions { get; set; }

    /// <summary>
    /// The handler for a node whose type has none registered - for handlers chosen by something other than the type.
    /// Return null for none.
    /// </summary>
    public Func<BlowNode, BlowNodeHandler?>? ResolveHandler { get; set; }

    /// <summary>
    /// What to say about a failure that is not a <see cref="BlowNodeException"/>. Exception messages can carry
    /// connection strings, paths and payloads, so by default they are not used: the message is a plain "failed".
    /// </summary>
    public Func<Exception, string?>? DescribeError { get; set; }

    /// <summary>How deep runs may nest through <see cref="BlowNodeRunContext.RunSubgraphAsync"/>. 16 by default.</summary>
    public int MaxDepth { get; set; } = 16;

    /// <summary>The most attempts, retries included, one run may make in all, or null for no limit. A run past it fails its next node.</summary>
    public int? MaxExecutions { get; set; }

    /// <summary>The clock for timestamps, delays and timeouts. <see cref="TimeProvider.System"/> by default; replace it in tests.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>What the runner says about the nodes it skips, stops and times out.</summary>
    public BlowRunMessages Messages { get; set; } = new();
}

/// <summary>What to run and how to watch it.</summary>
public sealed class BlowRunRequest
{
    /// <summary>The run's id, given to every event and to <see cref="BlowNodeRunContext.IdempotencyKey"/>. A new one when null, or the checkpoint's when resuming.</summary>
    public string? RunId { get; init; }

    /// <summary>A value for the run as a whole - what a trigger received - read by handlers through <see cref="BlowNodeRunContext.RunInput"/>.</summary>
    public object? Input { get; init; }

    /// <summary>
    /// Which nodes take part. Null takes every node; group frames, notes and other nodes that do nothing are left out
    /// with a filter. Edges to or from a node left out are ignored.
    /// </summary>
    public Func<BlowNode, bool>? Include { get; init; }

    /// <summary>
    /// Runs only these nodes and what depends on them; the nodes before them are not run, and their outputs are taken
    /// from <see cref="Resume"/> when it has them. The start nodes run with whatever reached them - nothing, without a
    /// checkpoint. Null runs everything. Pair it with the previous run's checkpoint to
    /// run again from a node.
    /// </summary>
    public IReadOnlyCollection<string>? StartAt { get; init; }

    /// <summary>
    /// A previous run's checkpoint (<see cref="BlowRunResult.ToCheckpoint"/>): nodes that succeeded there are not run
    /// again, their outputs are used as they were. The run keeps the checkpoint's id unless <see cref="RunId"/> is set.
    /// </summary>
    public BlowRunCheckpoint? Resume { get; init; }

    /// <summary>Checked before the run starts; an error refuses it. Null checks only what the runner needs: no cycles, a handler per node.</summary>
    public BlowGraphValidator? Validator { get; init; }

    /// <summary>
    /// Called for every event, in order, on the context the run was started on (the renderer's, in a Blazor component).
    /// It must not throw: an exception stops the run and is thrown from <see cref="BlowRunner.RunAsync"/>.
    /// </summary>
    public Action<BlowRunEvent>? OnEvent { get; init; }

    /// <summary>Pauses and resumes the run while it is going (<see cref="BlowRunControl"/>).</summary>
    public BlowRunControl? Control { get; init; }

    /// <summary>Services for handlers, through <see cref="BlowNodeRunContext.Services"/>.</summary>
    public IServiceProvider? Services { get; init; }
}

/// <summary>
/// Pauses a run between nodes: while paused, no node starts and the ones working finish. Hand one to
/// <see cref="BlowRunRequest.Control"/>; one control may serve several runs.
/// </summary>
public sealed class BlowRunControl
{
    private readonly object _gate = new();
    private TaskCompletionSource? _resumed;

    /// <summary>True while paused.</summary>
    public bool IsPaused
    {
        get { lock (_gate) return _resumed is not null; }
    }

    /// <summary>Stops nodes from starting until <see cref="Resume"/>.</summary>
    public void Pause()
    {
        lock (_gate) _resumed ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>Lets nodes start again.</summary>
    public void Resume()
    {
        TaskCompletionSource? resumed;
        lock (_gate)
        {
            resumed = _resumed;
            _resumed = null;
        }
        resumed?.TrySetResult();
    }

    internal Task WhenResumedAsync(CancellationToken cancellationToken)
    {
        Task? wait;
        lock (_gate) wait = _resumed?.Task;
        return wait is null ? Task.CompletedTask : wait.WaitAsync(cancellationToken);
    }
}

/// <summary>
/// A failure whose message is meant to be shown: it becomes the node's run message, where other exceptions' messages
/// are kept out (see <see cref="BlowRunnerOptions.DescribeError"/>).
/// </summary>
public class BlowNodeException : Exception
{
    /// <summary>A failure with a message to show.</summary>
    public BlowNodeException(string message) : base(message) { }

    /// <summary>A failure with a message to show, and the exception behind it, kept for the application but not shown.</summary>
    public BlowNodeException(string message, Exception? innerException) : base(message, innerException) { }

    /// <summary>False for a failure another attempt would not fix - bad input, a refusal - so a retry policy skips the retries.</summary>
    public bool IsTransient { get; init; } = true;
}

/// <summary>What a failed node sends along its <see cref="BlowNodeRunOptions.ErrorOutput"/>.</summary>
/// <param name="NodeId">The node that failed.</param>
/// <param name="Message">What to say about it, as safe to show as the node's run message.</param>
/// <param name="Exception">The exception, for the handler that deals with it. Not shown anywhere by the library.</param>
public sealed record BlowRunError(string NodeId, string Message, Exception? Exception);

/// <summary>
/// What a node's handler produced: the value each of its outputs carries on. An output that carries nothing leaves the
/// edges from it inactive - which is how a node branches.
/// </summary>
public sealed class BlowNodeResult
{
    private BlowNodeResult(object? value, IReadOnlyDictionary<string, object?>? outputs)
    {
        Value = value;
        _outputs = outputs;
    }

    /// <summary>Succeeds with nothing going on: no edge from the node carries a value.</summary>
    public static BlowNodeResult Stop() => new(null, EmptyOutputs);

    /// <summary>Succeeds, every edge from the node carrying <paramref name="value"/>.</summary>
    public static BlowNodeResult Done(object? value = null) => new(value, null);

    /// <summary>Succeeds with one output carrying the value: the edges from that source handle are active, the others not.</summary>
    /// <param name="handleId">The source handle; null or empty for the node's unnamed one.</param>
    /// <param name="value">What its edges carry.</param>
    public static BlowNodeResult Branch(string? handleId, object? value = null)
        => new(null, new Dictionary<string, object?>(StringComparer.Ordinal) { [handleId ?? ""] = value });

    /// <summary>Succeeds with the given outputs, by source handle id (an empty key for the unnamed handle).</summary>
    public static BlowNodeResult Outputs(IReadOnlyDictionary<string, object?> outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        return new(null, new Dictionary<string, object?>(outputs, StringComparer.Ordinal));
    }

    /// <summary>The value every edge carries, when the result is <see cref="Done"/>.</summary>
    public object? Value { get; }

    /// <summary>The values per source handle, or null when every edge carries <see cref="Value"/>.</summary>
    public IReadOnlyDictionary<string, object?>? OutputValues => _outputs;

    private readonly IReadOnlyDictionary<string, object?>? _outputs;

    /// <summary>A message for the node's run state - a result to show, a count.</summary>
    public string? Message { get; init; }

    /// <summary>Whether edges from the given source handle carry a value, and which.</summary>
    public bool TryGetOutput(string? handleId, out object? value)
    {
        if (_outputs is null)
        {
            value = Value;
            return true;
        }
        return _outputs.TryGetValue(handleId ?? "", out value);
    }

    private static readonly IReadOnlyDictionary<string, object?> EmptyOutputs = new Dictionary<string, object?>();
}

/// <summary>A value arriving at a node along one of its incoming edges.</summary>
/// <param name="Edge">The edge it came along, as it was when the run started (a copy, so a reconnect during the run changes nothing).</param>
/// <param name="Value">What it carries.</param>
public readonly record struct BlowRunInput(BlowEdge Edge, object? Value)
{
    /// <summary>The node it came from.</summary>
    public string SourceId => Edge.Source;

    /// <summary>The target handle it arrives at, or null for the unnamed one.</summary>
    public string? TargetHandle => Edge.TargetHandle;
}

/// <summary>How one node fared in a run.</summary>
public sealed class BlowNodeRunRecord
{
    internal BlowNodeRunRecord(string nodeId) => NodeId = nodeId;

    /// <summary>The node.</summary>
    public string NodeId { get; }

    /// <summary>Where it ended: succeeded, failed, skipped or cancelled; queued when the run stopped first.</summary>
    public BlowRunStatus Status { get; internal set; } = BlowRunStatus.Queued;

    /// <summary>The last message: a progress note, why it failed (when safe to show), why it was skipped.</summary>
    public string? Message { get; internal set; }

    /// <summary>The attempts made, retries included; 0 when it never started.</summary>
    public int Attempts { get; internal set; }

    /// <summary>When its first attempt started.</summary>
    public DateTimeOffset? StartedAt { get; internal set; }

    /// <summary>When it settled.</summary>
    public DateTimeOffset? FinishedAt { get; internal set; }

    /// <summary>From its first attempt to settling, retries and waits included.</summary>
    public TimeSpan? Duration => StartedAt is { } s && FinishedAt is { } f ? f - s : null;

    /// <summary>The exception it failed with. For the application to log as it sees fit; never shown by the library.</summary>
    public Exception? Error { get; internal set; }

    /// <summary>What its handler produced, when it succeeded.</summary>
    public BlowNodeResult? Result { get; internal set; }

    /// <summary>
    /// True when it failed and its <see cref="BlowNodeRunOptions.ErrorOutput"/> carried the failure on to the nodes
    /// after it. Such a failure was dealt with, so it does not make the run <see cref="BlowRunOutcome.Failed"/>.
    /// </summary>
    public bool Handled { get; internal set; }

    /// <summary>True when its result came from the checkpoint the run resumed from.</summary>
    public bool Restored { get; internal set; }

    /// <summary>The values that reached it, when it ran.</summary>
    public IReadOnlyList<BlowRunInput> Inputs { get; internal set; } = [];
}

/// <summary>
/// What a run left behind for a later one to continue from: the results of the nodes that succeeded. Their values are
/// whatever the handlers returned; saving a checkpoint is the application's, as only it knows their types.
/// </summary>
/// <param name="RunId">The run it was taken from.</param>
/// <param name="Results">The results of the nodes that succeeded, by node id.</param>
public sealed record BlowRunCheckpoint(string RunId, IReadOnlyDictionary<string, BlowNodeResult> Results);

/// <summary>Problem codes a run is refused with, in <see cref="BlowRunResult.Problems"/>.</summary>
public static class BlowRunCodes
{
    /// <summary>A node has no handler and <see cref="BlowRunnerOptions.MissingHandler"/> refuses.</summary>
    public const string MissingHandler = "missing-handler";

    /// <summary>A start node (<see cref="BlowRunRequest.StartAt"/>) is not in the graph.</summary>
    public const string UnknownStart = "unknown-start";

    /// <summary>Runs nested deeper than <see cref="BlowRunnerOptions.MaxDepth"/>.</summary>
    public const string TooDeep = "too-deep";
}
