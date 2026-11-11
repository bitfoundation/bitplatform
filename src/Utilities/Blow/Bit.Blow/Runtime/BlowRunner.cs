using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Bit.Blow;

/// <summary>
/// Runs a graph with no UI: every node through the handler registered for its type, each after the nodes it depends on,
/// values passed along the edges. On a server, in a background worker or behind a <c>BlowCanvas</c> - the canvas only
/// shows a run, through <c>RunStates</c> fed by <see cref="BlowRunStates"/>.
/// </summary>
/// <remarks>
/// <para>
/// A node runs once everything before it has settled. A handler returns a <see cref="BlowNodeResult"/> saying which of
/// its outputs carry a value; edges from the others carry nothing, and a node no value reaches is skipped - that is
/// how branches work. A failure skips what depends on the node, unless the node has an
/// <see cref="BlowNodeRunOptions.ErrorOutput"/>. Independent nodes run at once up to
/// <see cref="BlowRunnerOptions.MaxConcurrency"/>.
/// </para>
/// <para>
/// The graph must be acyclic: a loop is a node that runs a graph of its own as many times as it needs
/// (<see cref="BlowNodeRunContext.RunSubgraphAsync"/>). Configure the runner before running it; one runner can then
/// run any number of graphs at once. Handlers and options are looked up once per run, when it starts.
/// </para>
/// <para>
/// The run continues on the context it was started on, and <see cref="BlowRunRequest.OnEvent"/> is always called on
/// that context - also for a <see cref="BlowNodeRunContext.Report"/> made from another thread - so in a Blazor
/// component its events can update state and re-render directly. Handlers that do heavy work off that context use
/// <c>Task.Run</c> themselves.
/// </para>
/// <para>
/// Timeouts and cancellation do not wait for a handler that ignores its token: the attempt is abandoned and counted
/// as timed out or cancelled, and whatever the handler does afterwards is ignored.
/// </para>
/// </remarks>
public sealed class BlowRunner
{
    /// <summary>The longest wait a .NET timer takes; anything longer is treated as no limit.</summary>
    private static readonly TimeSpan MaxTimer = TimeSpan.FromMilliseconds(uint.MaxValue - 1.0);

    private readonly Dictionary<string, (BlowNodeHandler Handler, BlowNodeRunOptions? Options)> _handlers = new(StringComparer.Ordinal);

    /// <summary>A runner with the given settings, or the defaults.</summary>
    public BlowRunner(BlowRunnerOptions? options = null) => Options = options ?? new BlowRunnerOptions();

    /// <summary>The runner's settings.</summary>
    public BlowRunnerOptions Options { get; }

    /// <summary>The node types with a handler.</summary>
    public IReadOnlyCollection<string> HandledTypes => _handlers.Keys;

    /// <summary>Registers the handler for nodes of a type, replacing any it had.</summary>
    /// <param name="nodeType">The <see cref="BlowNode.Type"/> it runs.</param>
    /// <param name="handler">What runs each node.</param>
    /// <param name="options">How nodes of the type run: timeout, retries, join. <see cref="BlowRunnerOptions.NodeOptions"/> can override them per node.</param>
    public BlowRunner Handle(string nodeType, BlowNodeHandler handler, BlowNodeRunOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(nodeType);
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[nodeType] = (handler, options);
        return this;
    }

    /// <summary>
    /// Registers a handler that finishes at once. What it returns goes along every edge from the node, unless it is a
    /// <see cref="BlowNodeResult"/>, which is used as it is.
    /// </summary>
    public BlowRunner Handle(string nodeType, Func<BlowNodeRunContext, object?> handler, BlowNodeRunOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Handle(nodeType, context =>
        {
            var value = handler(context);
            return Task.FromResult(value as BlowNodeResult ?? BlowNodeResult.Done(value));
        }, options);
    }

    /// <summary>
    /// The node kinds whose <see cref="BlowNodeDefinition.Run"/> handles their nodes, after the handlers registered
    /// here and before <see cref="BlowRunnerOptions.ResolveHandler"/>. Definitions added to it later are picked up by
    /// the runs started after.
    /// </summary>
    public BlowNodeRegistry? Registry { get; private set; }

    /// <summary>Runs nodes through the handlers of their definitions in the registry (see <see cref="Registry"/>).</summary>
    public BlowRunner Use(BlowNodeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        Registry = registry;
        return this;
    }

    /// <summary>Removes the handler for a node type. True when there was one.</summary>
    public bool Remove(string nodeType) => _handlers.Remove(nodeType);

    /// <summary>True when the runner has a handler for the node.</summary>
    public bool CanRun(BlowNode node) => ResolveHandler(node) is not null;

    /// <summary>
    /// Runs the graph and hands back how it went. It does not throw for a node that fails, a graph it refuses, a
    /// cancellation, a timeout or a callback of the options that throws - those are the result's
    /// <see cref="BlowRunResult.Outcome"/> and its nodes' records - only for an <see cref="BlowRunRequest.OnEvent"/>
    /// that throws.
    /// </summary>
    /// <param name="nodes">The nodes; the list is read once, when the run starts.</param>
    /// <param name="edges">The edges.</param>
    /// <param name="request">What to run and how to watch it.</param>
    /// <param name="cancellationToken">Stops the run: no node starts after it, and the working ones are cancelled.</param>
    public Task<BlowRunResult> RunAsync(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
        BlowRunRequest? request = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        return new Execution(this, nodes, edges, request ?? new BlowRunRequest(), depth: 0, variables: null, counter: null)
            .RunAsync(cancellationToken);
    }

    /// <summary>
    /// What the runner would refuse the graph over - a cycle, a node with no handler, a start node that is not there -
    /// without running anything. Empty when it would run.
    /// </summary>
    public IReadOnlyList<BlowValidationIssue> Check(IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, BlowRunRequest? request = null)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentNullException.ThrowIfNull(edges);
        return new Execution(this, nodes, edges, request ?? new BlowRunRequest(), depth: 0, variables: null, counter: null).Prepare();
    }

    private (BlowNodeHandler Handler, BlowNodeRunOptions? Options)? ResolveHandler(BlowNode node)
    {
        if (_handlers.TryGetValue(node.Type, out var registered)) return registered;
        if (Registry?.HandlerOf(node) is { } defined) return defined;
        return Options.ResolveHandler?.Invoke(node) is { } resolved ? (resolved, null) : null;
    }

    /// <summary>A wait a timer can take: negative is none, longer than a timer takes is null (no limit).</summary>
    internal static TimeSpan? Timer(TimeSpan? wait) => wait switch
    {
        null => null,
        { } w when w > MaxTimer => null,
        { } w when w < TimeSpan.Zero => TimeSpan.Zero,
        var w => w,
    };

    /// <summary>One run: the graph as it was when the run started, and the state of every node in it.</summary>
    internal sealed class Execution
    {
        private readonly BlowRunner _runner;
        private readonly BlowRunnerOptions _options;
        private readonly TimeProvider _time;
        private readonly StrongBox<int> _counter;
        private readonly object _eventGate = new();
        private readonly object _dispatchGate = new();
        private readonly List<BlowRunEvent> _events = [];
        private readonly Queue<BlowRunEvent> _undelivered = new();
        private readonly Dictionary<string, BlowNodeRunRecord> _records = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Pending> _pending = new(StringComparer.Ordinal);
        private readonly HashSet<string> _activeEdges = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _rank = new(StringComparer.Ordinal);
        private readonly HashSet<string> _toRun = new(StringComparer.Ordinal);
        private readonly HashSet<string> _startAt = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BlowNodeResult> _restored = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Plan> _plans = new(StringComparer.Ordinal);
        private readonly PriorityQueue<BlowNode, int> _ready = new();
        private SynchronizationContext? _context;
        private ExceptionDispatchInfo? _observerError;
        private CancellationTokenSource? _stop;

        public Execution(BlowRunner runner, IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges, BlowRunRequest request,
            int depth, ConcurrentDictionary<string, object?>? variables, StrongBox<int>? counter)
        {
            _runner = runner;
            _options = runner.Options;
            _time = _options.TimeProvider ?? TimeProvider.System;
            Request = request;
            Depth = depth;
            Variables = variables ?? new ConcurrentDictionary<string, object?>(StringComparer.Ordinal);
            _counter = counter ?? new StrongBox<int>();
            RunId = request.RunId ?? request.Resume?.RunId ?? Guid.NewGuid().ToString("N");

            var include = request.Include;
            Graph = new BlowGraphIndex(include is null ? nodes : nodes.Where(include), edges);
        }

        public BlowRunRequest Request { get; }

        public BlowGraphIndex Graph { get; }

        public string RunId { get; }

        public int Depth { get; }

        public ConcurrentDictionary<string, object?> Variables { get; }

        private BlowRunMessages Messages => _options.Messages ?? new BlowRunMessages();

        /// <summary>
        /// Orders the graph, decides what runs and what is restored, looks up every node's handler and options once,
        /// and collects what would refuse the run.
        /// </summary>
        public List<BlowValidationIssue> Prepare()
        {
            var problems = new List<BlowValidationIssue>();
            if (Depth > _options.MaxDepth)
            {
                problems.Add(new BlowValidationIssue(BlowRunCodes.TooDeep, BlowValidationSeverity.Error,
                    $"Runs are nested deeper than {_options.MaxDepth}."));
                return problems;
            }

            var order = BlowGraphAlgorithms.TopologicalSort(Graph);
            if (!order.IsAcyclic)
            {
                foreach (var cycle in BlowGraphAlgorithms.FindCycles(Graph))
                {
                    problems.Add(new BlowValidationIssue(BlowValidationCodes.Cycle, BlowValidationSeverity.Error,
                        $"'{Name(cycle[0])}' is part of a cycle, which a run cannot order.") { NodeId = cycle[0].Id });
                }
                return problems;
            }
            for (var i = 0; i < order.Order.Count; i++) _rank[order.Order[i].Id] = i;

            var checkpoint = Request.Resume?.Results;
            if (Request.StartAt is { } startAt)
            {
                foreach (var id in startAt)
                {
                    if (!Graph.ContainsNode(id))
                    {
                        problems.Add(new BlowValidationIssue(BlowRunCodes.UnknownStart, BlowValidationSeverity.Error,
                            $"The start node '{id}' is not in the graph.") { NodeId = id });
                        continue;
                    }
                    _startAt.Add(id);
                    _toRun.Add(id);
                    _toRun.UnionWith(BlowGraphAlgorithms.GetDescendants(Graph, id).Select(n => n.Id));
                }
                foreach (var node in order.Order)
                {
                    if (!_toRun.Contains(node.Id) && checkpoint?.GetValueOrDefault(node.Id) is { } result) _restored[node.Id] = result;
                }
            }
            else
            {
                foreach (var node in order.Order)
                {
                    if (checkpoint?.GetValueOrDefault(node.Id) is { } result) _restored[node.Id] = result;
                    else _toRun.Add(node.Id);
                }
            }

            foreach (var node in order.Order)
            {
                if (!_toRun.Contains(node.Id)) continue;
                var plan = PlanFor(node);
                _plans[node.Id] = plan;
                if (plan.Handler is null && plan.Error is null && _options.MissingHandler == BlowMissingHandlerBehavior.Refuse)
                {
                    problems.Add(new BlowValidationIssue(BlowRunCodes.MissingHandler, BlowValidationSeverity.Error,
                        $"Nothing runs '{Name(node)}': no handler is registered for the type '{node.Type}'.") { NodeId = node.Id });
                }
            }

            if (Request.Validator is { } validator)
            {
                problems.AddRange(validator.Validate(Graph.Nodes, Graph.Edges).Issues.Where(i => i.Severity == BlowValidationSeverity.Error));
            }
            return problems;
        }

        /// <summary>A node's handler and options, looked up once. A lookup that throws fails the node when it runs.</summary>
        private Plan PlanFor(BlowNode node)
        {
            try
            {
                var resolved = _runner.ResolveHandler(node);
                var options = _options.NodeOptions?.Invoke(node) ?? resolved?.Options ?? BlowNodeRunOptions.Default;
                return new Plan(resolved?.Handler, options, null);
            }
            catch (Exception ex)
            {
                return new Plan(null, BlowNodeRunOptions.Default, ex);
            }
        }

        public async Task<BlowRunResult> RunAsync(CancellationToken cancellationToken)
        {
            _context = SynchronizationContext.Current;
            var startedAt = _time.GetUtcNow();
            var problems = Prepare();
            if (problems.Count > 0)
            {
                Emit(new BlowRunEvent(BlowRunEventKind.RunFinished, RunId, _time.GetUtcNow()) { Outcome = BlowRunOutcome.Refused });
                Deliver();
                _observerError?.Throw();
                return Result(BlowRunOutcome.Refused, startedAt, problems);
            }

            using var timeout = Timer(_options.RunTimeout) is { } limit ? new CancellationTokenSource(limit, _time) : null;
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout?.Token ?? CancellationToken.None);
            _stop = stop;
            var token = stop.Token;

            Emit(new BlowRunEvent(BlowRunEventKind.RunStarted, RunId, startedAt));
            foreach (var node in Graph.Nodes.OrderBy(n => _rank[n.Id]))
            {
                if (!_toRun.Contains(node.Id)) continue;
                _records[node.Id] = new BlowNodeRunRecord(node.Id);
                _pending[node.Id] = new Pending(Graph.Incoming(node.Id).Count);
                Emit(NodeEvent(BlowRunEventKind.NodeQueued, node.Id, BlowRunStatus.Queued));
            }

            // What does not run settles first: restored results pass their values on, nodes left out of a partial run
            // pass nothing. Then every node with nothing to wait for is ready.
            foreach (var node in Graph.Nodes.OrderBy(n => _rank[n.Id]))
            {
                if (_restored.TryGetValue(node.Id, out var result))
                {
                    var record = new BlowNodeRunRecord(node.Id) { Status = BlowRunStatus.Succeeded, Result = result, Restored = true, Message = result.Message };
                    _records[node.Id] = record;
                    Emit(NodeEvent(BlowRunEventKind.NodeRestored, node.Id, BlowRunStatus.Succeeded, result.Message));
                    Settle(node.Id);
                }
                else if (!_toRun.Contains(node.Id))
                {
                    Settle(node.Id);
                }
            }
            foreach (var (id, pending) in _pending.ToList())
            {
                if (pending.Remaining == 0 && _records[id].Status == BlowRunStatus.Queued && !pending.Decided && Decide(id)) Settle(id);
            }

            var running = new Dictionary<Task<Outcome>, BlowNode>();
            Task? resumed = null;
            var interrupted = false;
            try
            {
                while (true)
                {
                    var paused = Request.Control?.IsPaused == true;
                    while (!paused && !token.IsCancellationRequested && running.Count < Math.Max(1, _options.MaxConcurrency)
                           && _ready.TryDequeue(out var next, out _))
                    {
                        running[ExecuteAsync(next, token)] = next;
                    }
                    Deliver();

                    if (running.Count == 0)
                    {
                        if (token.IsCancellationRequested || _ready.Count == 0) break;
                        // Paused with nothing working: wait for the resume, one wait per pause.
                        resumed ??= Request.Control!.WhenResumedAsync(token);
                        try
                        {
                            await resumed;
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        resumed = null;
                        continue;
                    }

                    Task done;
                    if (paused && _ready.Count > 0 && !token.IsCancellationRequested)
                    {
                        resumed ??= Request.Control!.WhenResumedAsync(token);
                        done = await Task.WhenAny(running.Keys.Cast<Task>().Append(resumed));
                        if (resumed.IsCompleted) resumed = null;
                    }
                    else
                    {
                        done = await Task.WhenAny(running.Keys);
                    }

                    if (done is Task<Outcome> finished && running.Remove(finished, out var node))
                    {
                        Complete(node, await finished);
                    }
                }
            }
            catch
            {
                // A bug of the runner's own, not a node's: stop everything and let it surface, after the working nodes are done.
                stop.Cancel();
                await Task.WhenAll(running.Keys.Select(t => t.ContinueWith(static _ => { }, TaskScheduler.Default)));
                throw;
            }

            var messages = Messages;
            foreach (var node in Graph.Nodes.OrderBy(n => _rank[n.Id]))
            {
                if (_records.TryGetValue(node.Id, out var record) && record.Status == BlowRunStatus.Queued)
                {
                    interrupted = true;
                    record.Status = BlowRunStatus.Skipped;
                    record.Message = messages.Stopped;
                    record.FinishedAt = _time.GetUtcNow();
                    Emit(NodeEvent(BlowRunEventKind.NodeSkipped, node.Id, BlowRunStatus.Skipped, messages.Stopped));
                }
            }
            interrupted |= _records.Values.Any(r => r.Status == BlowRunStatus.Cancelled);

            // A cancellation that came after every node had settled stopped nothing: the run is judged by its nodes.
            var failed = _records.Values.Any(r => r.Status is BlowRunStatus.Failed or BlowRunStatus.Cancelled);
            var outcome = interrupted && cancellationToken.IsCancellationRequested ? BlowRunOutcome.Cancelled
                : interrupted && timeout?.IsCancellationRequested == true ? BlowRunOutcome.TimedOut
                : failed ? BlowRunOutcome.Failed
                : BlowRunOutcome.Succeeded;
            Emit(new BlowRunEvent(BlowRunEventKind.RunFinished, RunId, _time.GetUtcNow()) { Outcome = outcome });
            Deliver();
            _stop = null;
            _observerError?.Throw();
            return Result(outcome, startedAt, []);
        }

        private BlowRunResult Result(BlowRunOutcome outcome, DateTimeOffset startedAt, IReadOnlyList<BlowValidationIssue> problems)
        {
            BlowRunEvent[] events;
            lock (_eventGate) events = [.. _events];
            return new BlowRunResult(RunId, outcome, startedAt, _time.GetUtcNow(),
                new Dictionary<string, BlowNodeRunRecord>(_records, StringComparer.Ordinal), problems, events,
                new HashSet<string>(_activeEdges, StringComparer.Ordinal));
        }

        /// <summary>Records a node's outcome and passes it on to the nodes after it.</summary>
        private void Complete(BlowNode node, Outcome outcome)
        {
            var record = _records[node.Id];
            record.Status = outcome.Status;
            record.FinishedAt = _time.GetUtcNow();
            record.Error = outcome.Error;
            record.Result = outcome.Result;
            record.Message = outcome.Message;

            var kind = outcome.Status switch
            {
                BlowRunStatus.Succeeded => BlowRunEventKind.NodeSucceeded,
                BlowRunStatus.Failed => BlowRunEventKind.NodeFailed,
                BlowRunStatus.Cancelled => BlowRunEventKind.NodeCancelled,
                _ => BlowRunEventKind.NodeSkipped,
            };
            Emit(NodeEvent(kind, node.Id, outcome.Status, record.Message, record.Attempts));

            if (outcome.Status == BlowRunStatus.Failed && _options.StopOnFailure && outcome.ErrorOutput is null) _stop?.Cancel();
            Settle(node.Id, outcome.ErrorOutput);
        }

        /// <summary>
        /// Passes a settled node's outcome along its outgoing edges, and decides every node that no longer waits for
        /// anything. Iterative, so a skip running down a long chain does not recurse.
        /// </summary>
        private void Settle(string nodeId, BlowRunError? error = null)
        {
            var work = new Stack<(string Id, BlowRunError? Error)>();
            work.Push((nodeId, error));
            while (work.Count > 0)
            {
                var (id, failure) = work.Pop();
                var record = _records.GetValueOrDefault(id);
                var status = record?.Status;
                var errorOutput = failure is null ? null : _plans.GetValueOrDefault(id)?.Options.ErrorOutput;
                // A node skipped because something before it failed fails its own dependants the same way, so the
                // reason reaches the end of the chain rather than turning into "no input" one step on.
                var failedUpstream = status == BlowRunStatus.Skipped && _pending.TryGetValue(id, out var own) && own.Failed > 0;

                foreach (var edge in Graph.Outgoing(id))
                {
                    if (!_pending.TryGetValue(edge.Target, out var target) || target.Decided) continue;

                    if (status == BlowRunStatus.Succeeded && record!.Result!.TryGetOutput(edge.SourceHandle, out var value))
                    {
                        Activate(edge, target, value);
                    }
                    else if (status == BlowRunStatus.Failed && failure is not null && SameHandle(edge.SourceHandle, errorOutput))
                    {
                        Activate(edge, target, failure);
                    }
                    else if ((status == BlowRunStatus.Failed && failure is null) || failedUpstream)
                    {
                        target.Failed++;
                    }
                    else
                    {
                        target.Inactive++;
                    }

                    if (--target.Remaining == 0 && _records[edge.Target].Status == BlowRunStatus.Queued)
                    {
                        if (Decide(edge.Target)) work.Push((edge.Target, null));
                    }
                }
            }
        }

        private void Activate(BlowEdge edge, Pending target, object? value)
        {
            target.Inputs.Add(new BlowRunInput(edge, value));
            target.Active++;
            _activeEdges.Add(edge.Id);
            Emit(new BlowRunEvent(BlowRunEventKind.EdgeActivated, RunId, _time.GetUtcNow()) { EdgeId = edge.Id, NodeId = edge.Target });
        }

        /// <summary>Queues a node with nothing left to wait for, or skips it. True when it was skipped, so its own edges settle too.</summary>
        private bool Decide(string id)
        {
            var pending = _pending[id];
            pending.Decided = true;
            var node = Graph.GetNode(id)!;
            var incoming = pending.Active + pending.Inactive + pending.Failed;
            var messages = Messages;

            string? skip = null;
            if (pending.Failed > 0) skip = messages.UpstreamFailed;
            // A start node of a partial run runs with whatever reached it: what came before it is not part of the run.
            else if (incoming > 0 && !_startAt.Contains(id))
            {
                var join = _plans.GetValueOrDefault(id)?.Options.Join ?? BlowJoinMode.Any;
                if (join == BlowJoinMode.All && pending.Inactive > 0) skip = messages.MissingInput;
                else if (pending.Active == 0) skip = messages.NoInput;
            }

            if (skip is null)
            {
                _ready.Enqueue(node, _rank[id]);
                return false;
            }

            var record = _records[id];
            record.Status = BlowRunStatus.Skipped;
            record.Message = skip;
            record.FinishedAt = _time.GetUtcNow();
            Emit(NodeEvent(BlowRunEventKind.NodeSkipped, id, BlowRunStatus.Skipped, skip));
            return true;
        }

        /// <summary>Runs every attempt of one node. Never throws: everything that can go wrong is in the outcome.</summary>
        private async Task<Outcome> ExecuteAsync(BlowNode node, CancellationToken runToken)
        {
            // Yield first, so the scheduler starts every ready node before any of them runs its first synchronous part.
            await Task.Yield();

            var messages = Messages;
            var plan = _plans[node.Id];
            var options = plan.Options;
            try
            {
                var record = _records[node.Id];
                var inputs = _pending[node.Id].Inputs;
                record.Inputs = inputs;

                if (plan.Error is { } lookup) return Failed(node, options, lookup, Describe(lookup, messages));
                if (plan.Handler is not { } handler)
                {
                    return _options.MissingHandler == BlowMissingHandlerBehavior.PassThrough
                        ? new Outcome(BlowRunStatus.Succeeded, BlowNodeResult.Done(inputs.Count > 0 ? inputs[0].Value : null), null, null, null)
                        : new Outcome(BlowRunStatus.Skipped, null, null, messages.NoHandler, null);
                }

                var retry = options.Retry ?? BlowRetryPolicy.None;
                var attempts = Math.Max(1, retry.MaxAttempts);
                var limit = Timer(options.Timeout);

                for (var attempt = 1; ; attempt++)
                {
                    if (runToken.IsCancellationRequested) return Cancelled(messages);

                    if (_options.MaxExecutions is { } max && Interlocked.Increment(ref _counter.Value) > max)
                    {
                        var limitError = new BlowNodeException(messages.TooManyExecutions) { IsTransient = false };
                        return Failed(node, options, limitError, messages.TooManyExecutions);
                    }

                    record.Attempts = attempt;
                    record.StartedAt ??= _time.GetUtcNow();
                    Emit(NodeEvent(BlowRunEventKind.NodeStarted, node.Id, BlowRunStatus.Running, null, attempt));

                    using var attemptTimeout = limit is { } t ? new CancellationTokenSource(t, _time) : null;
                    using var attemptToken = CancellationTokenSource.CreateLinkedTokenSource(runToken, attemptTimeout?.Token ?? CancellationToken.None);
                    var context = new BlowNodeRunContext(this, node, inputs, attempt, attemptToken.Token);

                    Exception error;
                    string message;
                    try
                    {
                        // Waiting on the token as well as the handler: one that ignores its token is abandoned, not waited for.
                        var result = await handler(context).WaitAsync(attemptToken.Token) ?? BlowNodeResult.Done();
                        return new Outcome(BlowRunStatus.Succeeded, result, null, result.Message, null);
                    }
                    catch (OperationCanceledException) when (runToken.IsCancellationRequested)
                    {
                        return Cancelled(messages);
                    }
                    catch (OperationCanceledException ex) when (attemptTimeout?.IsCancellationRequested == true)
                    {
                        message = Text(() => messages.TimedOut(limit!.Value), "Timed out.");
                        error = new TimeoutException(message, ex);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                        message = Describe(ex, messages);
                    }
                    finally
                    {
                        context.Close();
                    }

                    if (attempt < attempts && Allows(retry, error))
                    {
                        var delay = Timer(retry.DelayBefore(attempt + 1)) ?? MaxTimer;
                        Emit(NodeEvent(BlowRunEventKind.NodeRetrying, node.Id, BlowRunStatus.Running,
                            Text(() => messages.Retrying(message, attempt + 1), message), attempt) with { RetryDelay = delay });
                        try
                        {
                            if (delay > TimeSpan.Zero) await Task.Delay(delay, _time, runToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return Cancelled(messages);
                        }
                        continue;
                    }

                    return Failed(node, options, error, message);
                }
            }
            catch (Exception ex)
            {
                // The options' own code - a retry policy, a message formatter - threw: the node fails, the run goes on.
                return runToken.IsCancellationRequested && ex is OperationCanceledException
                    ? Cancelled(messages)
                    : Failed(node, options, ex, Describe(ex, messages));
            }
        }

        private static bool Allows(BlowRetryPolicy retry, Exception error)
        {
            try
            {
                return retry.Allows(error);
            }
            catch
            {
                return false;
            }
        }

        private static string Text(Func<string> make, string fallback)
        {
            try
            {
                return make() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static Outcome Cancelled(BlowRunMessages messages) => new(BlowRunStatus.Cancelled, null, null, messages.Cancelled, null);

        /// <summary>
        /// A failure, carried along the node's error output when it has one with an edge from it. An error output with no
        /// edge would swallow the failure, so the failure goes on as one.
        /// </summary>
        private Outcome Failed(BlowNode node, BlowNodeRunOptions options, Exception error, string message)
        {
            var routed = options.ErrorOutput is { } output && Graph.Outgoing(node.Id).Any(e => SameHandle(e.SourceHandle, output));
            return new(BlowRunStatus.Failed, null, error, message, routed ? new BlowRunError(node.Id, message, error) : null);
        }

        private string Describe(Exception error, BlowRunMessages messages)
        {
            if (error is BlowNodeException) return error.Message;
            return Text(() => _options.DescribeError?.Invoke(error) ?? messages.UnexpectedError, messages.UnexpectedError);
        }

        public void Report(string nodeId, int attempt, string? message, double? progress)
        {
            if (_records.TryGetValue(nodeId, out var record) && message is not null) record.Message = message;
            Emit(NodeEvent(BlowRunEventKind.NodeProgress, nodeId, BlowRunStatus.Running, message, attempt) with { Progress = progress });
        }

        public void Waiting(string nodeId, int attempt, string? message)
        {
            if (_records.TryGetValue(nodeId, out var record) && message is not null) record.Message = message;
            Emit(NodeEvent(BlowRunEventKind.NodeWaiting, nodeId, BlowRunStatus.Waiting, message, attempt));
        }

        public void Resumed(string nodeId, int attempt)
            => Emit(NodeEvent(BlowRunEventKind.NodeStarted, nodeId, BlowRunStatus.Running, null, attempt));

        public async Task<BlowRunResult> RunSubgraphAsync(string nodeId, int number, IEnumerable<BlowNode> nodes, IEnumerable<BlowEdge> edges,
            object? input, BlowRunRequest? request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(nodes);
            ArgumentNullException.ThrowIfNull(edges);
            var inner = new BlowRunRequest
            {
                // Numbered within the attempt, so a retry or a resumed run gives the inner nodes the same idempotency keys.
                RunId = request?.RunId ?? request?.Resume?.RunId ?? $"{RunId}/{nodeId}/{number}",
                Input = input,
                Include = request?.Include,
                StartAt = request?.StartAt,
                Resume = request?.Resume,
                Validator = request?.Validator,
                OnEvent = request?.OnEvent,
                Control = request?.Control ?? Request.Control,
                Services = request?.Services ?? Request.Services,
            };
            return await new Execution(_runner, nodes, edges, inner, Depth + 1, Variables, _counter).RunAsync(cancellationToken);
        }

        private BlowRunEvent NodeEvent(BlowRunEventKind kind, string nodeId, BlowRunStatus status, string? message = null, int attempt = 0)
            => new(kind, RunId, _time.GetUtcNow()) { NodeId = nodeId, Status = status, Message = message, Attempt = attempt };

        /// <summary>
        /// Records an event and hands it to the observer on the run's context: at once when already on it, posted
        /// there otherwise. The queue keeps the order either way.
        /// </summary>
        private void Emit(BlowRunEvent runEvent)
        {
            lock (_eventGate)
            {
                _events.Add(runEvent);
                if (Request.OnEvent is null) return;
                _undelivered.Enqueue(runEvent);
            }
            if (_context is null || SynchronizationContext.Current == _context) Deliver();
            else _context.Post(static state => ((Execution)state!).Deliver(), this);
        }

        /// <summary>Hands the observer every event not yet handed to it, in order.</summary>
        private void Deliver()
        {
            if (Request.OnEvent is not { } observer) return;
            lock (_dispatchGate)
            {
                while (true)
                {
                    BlowRunEvent next;
                    lock (_eventGate)
                    {
                        if (!_undelivered.TryDequeue(out next!)) return;
                    }
                    if (_observerError is not null) continue;
                    try
                    {
                        observer(next);
                    }
                    catch (Exception ex)
                    {
                        _observerError = ExceptionDispatchInfo.Capture(ex);
                        _stop?.Cancel();
                    }
                }
            }
        }

        private string Name(BlowNode node) => string.IsNullOrWhiteSpace(node.Label) ? node.Id : node.Label!;

        private static bool SameHandle(string? a, string? b) => string.IsNullOrEmpty(a) ? string.IsNullOrEmpty(b) : a == b;

        private sealed class Pending(int remaining)
        {
            public int Remaining = remaining;
            public int Active;
            public int Inactive;
            public int Failed;
            public bool Decided;
            public readonly List<BlowRunInput> Inputs = [];
        }

        private sealed record Plan(BlowNodeHandler? Handler, BlowNodeRunOptions Options, Exception? Error);

        private sealed record Outcome(BlowRunStatus Status, BlowNodeResult? Result, Exception? Error, string? Message, BlowRunError? ErrorOutput);
    }
}
