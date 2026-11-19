using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Bit.Bcrdt.Tests;

/// <summary>A synchronization context backed by one dedicated thread, like a UI thread.</summary>
/// <remarks>
/// <para>
/// A <see cref="BcrdtDocument"/> is not thread-safe. A provider built with no context applies what
/// its transport delivers on whatever thread delivered it - a socket's, a timer's - so a test that
/// reads or edits the document from its own thread meanwhile races it. A read is not harmless either:
/// <see cref="BcrdtDocument.GetText"/> on a root whose content arrived before anyone asked for it
/// re-parents that content, and a part integrated halfway through is lost to the replica for good.
/// </para>
/// <para>
/// A test that keeps a replica connected therefore starts with <c>await thread.SwitchTo()</c>: the
/// providers it builds from then on capture this context, the way a Blazor component's do, and
/// everything that touches their documents - what the transports deliver, the timers, and the test's
/// own reads and edits after each <c>await</c> - runs here, one thing at a time.
/// </para>
/// </remarks>
internal sealed class SingleThreadContext : SynchronizationContext, IDisposable
{
    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State, ExecutionContext? Flow)> _queue = [];
    private readonly Thread _thread;

    public SingleThreadContext()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "single-thread-context" };
        _thread.Start();
    }

    public int ThreadId => _thread.ManagedThreadId;

    /// <remarks>
    /// The work runs in the execution context it was posted from, as it does on Blazor's renderer and on a UI
    /// thread: the ambient span a reconnection started is still the ambient one when the handshake it posts begins.
    /// </remarks>
    public override void Post(SendOrPostCallback d, object? state)
    {
        try
        {
            _queue.Add((d, state, ExecutionContext.Capture()));
        }
        catch (InvalidOperationException)
        {
            // Shut down; like a closed window, it no longer runs work.
        }
    }

    public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

    public Task<T> InvokeAsync<T>(Func<T> work)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(_ =>
        {
            try
            {
                completion.SetResult(work());
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }, null);
        return completion.Task;
    }

    public Task InvokeAsync(Func<Task> work) => InvokeAsync<Task>(work).Unwrap();

    /// <summary>
    /// Moves the awaiting method onto this context's thread. Every later <c>await</c> in it captures the
    /// context and resumes there too, unless it says otherwise.
    /// </summary>
    public SwitchAwaitable SwitchTo() => new(this);

    public void Dispose() => _queue.CompleteAdding();

    private void Run()
    {
        SetSynchronizationContext(this);
        foreach ((SendOrPostCallback callback, object? state, ExecutionContext? flow) in _queue.GetConsumingEnumerable())
        {
            if (flow is null) callback(state);
            else ExecutionContext.Run(flow, new ContextCallback(callback), state);
        }
    }

    internal readonly struct SwitchAwaitable(SingleThreadContext context)
    {
        public Awaiter GetAwaiter() => new(context);

        internal readonly struct Awaiter(SingleThreadContext context) : INotifyCompletion
        {
            public bool IsCompleted => Current == context;

            public void OnCompleted(Action continuation) => context.Post(static state => ((Action)state!)(), continuation);

            public void GetResult()
            {
            }
        }
    }
}
