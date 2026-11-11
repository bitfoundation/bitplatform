using System.Collections.Concurrent;

namespace Bit.Blow;

/// <summary>
/// What a node's handler is given: the node, the values that reached it, the run it belongs to, and the means to say
/// how it is going. One context per attempt.
/// </summary>
public sealed class BlowNodeRunContext
{
    private readonly BlowRunner.Execution _execution;
    private volatile bool _closed;
    private int _subRuns;

    internal BlowNodeRunContext(BlowRunner.Execution execution, BlowNode node, IReadOnlyList<BlowRunInput> inputs,
        int attempt, CancellationToken cancellationToken)
    {
        _execution = execution;
        Node = node;
        Inputs = inputs;
        Attempt = attempt;
        CancellationToken = cancellationToken;
    }

    /// <summary>The node being run. Read its <see cref="BlowNode.Data"/> for its settings.</summary>
    public BlowNode Node { get; }

    /// <summary>The graph being run: the nodes taking part and the edges among them.</summary>
    public BlowGraphIndex Graph => _execution.Graph;

    /// <summary>The run's id.</summary>
    public string RunId => _execution.RunId;

    /// <summary>
    /// The same for every attempt of this node in this run, and in a run resumed from its checkpoint: a key for an
    /// external service to recognise a request it has already carried out.
    /// </summary>
    public string IdempotencyKey => $"{RunId}:{Node.Id}";

    /// <summary>The attempt, starting at 1.</summary>
    public int Attempt { get; }

    /// <summary>How deep the run is nested: 0 for a run started by the application, 1 for one a node started, and so on.</summary>
    public int Depth => _execution.Depth;

    /// <summary>Cancelled when the run is, or when the attempt takes longer than its timeout. Pass it to everything that waits.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>The value the run was started with (<see cref="BlowRunRequest.Input"/>).</summary>
    public object? RunInput => _execution.Request.Input;

    /// <summary>Services the run was given (<see cref="BlowRunRequest.Services"/>), or null.</summary>
    public IServiceProvider? Services => _execution.Request.Services;

    /// <summary>Values shared by every node of the run - counters, a session, what a trigger set up. Safe to use from nodes running at once.</summary>
    public ConcurrentDictionary<string, object?> Variables => _execution.Variables;

    /// <summary>Every value that reached the node, in the order of its incoming edges.</summary>
    public IReadOnlyList<BlowRunInput> Inputs { get; }

    /// <summary>The first value that reached the node at any handle, or null.</summary>
    public object? Input => Inputs.Count > 0 ? Inputs[0].Value : null;

    /// <summary>True when a value reached the given target handle (null or empty for the unnamed one).</summary>
    public bool HasInput(string? handleId = null) => Inputs.Any(i => BlowGraphIndex.SameHandle(i.TargetHandle, handleId));

    /// <summary>The first value that reached the given target handle, as a <typeparamref name="T"/>; the default when none did or it is not one.</summary>
    public T? GetInput<T>(string? handleId = null)
    {
        foreach (var input in Inputs)
        {
            if (BlowGraphIndex.SameHandle(input.TargetHandle, handleId) && input.Value is T value) return value;
        }
        return default;
    }

    /// <summary>Every value that reached the given target handle that is a <typeparamref name="T"/> - what several edges into one input carried.</summary>
    public IReadOnlyList<T> GetInputs<T>(string? handleId = null)
        => [.. Inputs.Where(i => BlowGraphIndex.SameHandle(i.TargetHandle, handleId) && i.Value is T).Select(i => (T)i.Value!)];

    /// <summary>
    /// Says how the node is going: the message becomes its run state's (shown and read out with the node), the progress
    /// goes to the event. Neither should carry data that must not be shown.
    /// </summary>
    /// <param name="message">What to say, or null to keep the last message.</param>
    /// <param name="progress">How far along, from 0 to 1, or null.</param>
    /// <remarks>Ignored once the attempt is over - a handler abandoned after a timeout cannot change what it left behind.</remarks>
    public void Report(string? message, double? progress = null)
    {
        if (!_closed) _execution.Report(Node.Id, Attempt, message, progress);
    }

    /// <summary>Ends the attempt: what the handler reports from now on is ignored.</summary>
    internal void Close() => _closed = true;

    private void Waiting(string? message)
    {
        if (!_closed) _execution.Waiting(Node.Id, Attempt, message);
    }

    private void Resumed()
    {
        if (!_closed) _execution.Resumed(Node.Id, Attempt);
    }

    /// <summary>
    /// Waits for something outside the run - an approval, a webhook, a person - showing the node as
    /// <see cref="BlowRunStatus.Waiting"/> meanwhile. Cancelled with the attempt.
    /// </summary>
    public async Task<T> WaitAsync<T>(Task<T> task, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        Waiting(message);
        try
        {
            return await task.WaitAsync(CancellationToken);
        }
        finally
        {
            Resumed();
        }
    }

    /// <summary>
    /// Waits for something outside the run, showing the node as <see cref="BlowRunStatus.Waiting"/> meanwhile.
    /// </summary>
    public async Task WaitAsync(Task task, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        Waiting(message);
        try
        {
            await task.WaitAsync(CancellationToken);
        }
        finally
        {
            Resumed();
        }
    }

    /// <summary>
    /// Runs a graph of its own with the same handlers - a sub-flow, one iteration of a loop - and hands back its result.
    /// It shares this run's variables, services and limits, and is cancelled with this attempt. Its events are not part
    /// of this run's; pass a request with <see cref="BlowRunRequest.OnEvent"/> to watch them - with a
    /// <see cref="BlowRunStates"/> of their own, as each run starts by clearing the states it is given. The inner run's
    /// id is this run's, the node's and the call's number within the attempt, so a retried attempt or a resumed run
    /// gives the inner nodes the same idempotency keys.
    /// </summary>
    /// <param name="nodes">The nodes of the inner graph.</param>
    /// <param name="edges">Its edges.</param>
    /// <param name="input">What its nodes read as <see cref="RunInput"/>.</param>
    /// <param name="request">More about the inner run; its <see cref="BlowRunRequest.Input"/> is replaced by <paramref name="input"/>.</param>
    public Task<BlowRunResult> RunSubgraphAsync(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
        object? input = null, BlowRunRequest? request = null)
        => _execution.RunSubgraphAsync(Node.Id, Interlocked.Increment(ref _subRuns), nodes, edges, input, request, CancellationToken);
}
