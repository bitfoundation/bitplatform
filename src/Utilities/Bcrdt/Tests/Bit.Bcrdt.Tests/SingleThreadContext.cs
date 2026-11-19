using System.Collections.Concurrent;

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
/// A test that keeps a replica connected and reads or edits its document is therefore marked
/// <see cref="SingleThreadTestMethodAttribute"/>, which runs it on one of these: the providers it builds
/// capture this context, the way a Blazor component's do, and everything that touches their documents -
/// what the transports deliver, the timers, and the test's own reads and edits after each <c>await</c> -
/// runs here, one thing at a time.
/// </para>
/// <para>
/// A callback that throws does not take the thread down with it: the exception is kept in <see cref="Faults"/>,
/// which the attribute turns into a failure of the test that posted it. Work posted after <see cref="Dispose"/>
/// still runs - on the thread pool, so an <c>await</c> that resumes late completes rather than hangs - and is
/// recorded as a fault too.
/// </para>
/// </remarks>
internal sealed class SingleThreadContext : SynchronizationContext, IDisposable
{
    /// <summary>How long <see cref="Dispose"/> waits for the work already queued to run.</summary>
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(15);

    private readonly BlockingCollection<(SendOrPostCallback Callback, object? State, ExecutionContext? Flow)> _queue = [];
    private readonly ConcurrentQueue<Exception> _faults = new();
    private readonly Thread _thread;
    private volatile bool _disposed;

    public SingleThreadContext()
    {
        _thread = new Thread(Run) { IsBackground = true, Name = "single-thread-context" };
        _thread.Start();
    }

    public int ThreadId => _thread.ManagedThreadId;

    /// <summary>What the work posted here threw, and the work that arrived after shutdown.</summary>
    public IReadOnlyCollection<Exception> Faults => _faults;

    /// <remarks>
    /// The work runs in the execution context it was posted from, as it does on Blazor's renderer and on a UI
    /// thread: the ambient span a reconnection started is still the ambient one when the handshake it posts begins.
    /// </remarks>
    public override void Post(SendOrPostCallback d, object? state)
    {
        ExecutionContext? flow = ExecutionContext.Capture();
        try
        {
            _queue.Add((d, state, flow));
            return;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // Shut down. Dropping the work would leave whoever waits on it hanging, which tells nobody anything.
        }

        _faults.Enqueue(new InvalidOperationException($"Work was posted to the context after it was disposed and ran on the thread pool instead: {d.Method}."));
        ThreadPool.UnsafeQueueUserWorkItem(_ => Execute(d, state, flow), null);
    }

    public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

    public Task<T> InvokeAsync<T>(Func<T> work)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

    public Task<T> InvokeAsync<T>(Func<Task<T>> work) => InvokeAsync<Task<T>>(work).Unwrap();

    /// <summary>
    /// Throws unless the caller runs on a <see cref="SingleThreadContext"/>: a replica a test helper builds anywhere
    /// else applies what it receives on transport threads while the test reads its document, and the race that
    /// follows shows up only as an occasional timeout.
    /// </summary>
    public static void EnsureCurrent()
    {
        if (Current is not SingleThreadContext)
        {
            throw new InvalidOperationException(
                $"This replica must be built on a {nameof(SingleThreadContext)}: mark the test [SingleThreadTestMethod], " +
                "or build a provider that captures no context only in a test that never touches its document.");
        }
    }

    /// <summary>Stops taking work, runs what is already queued and waits for the thread to finish it.</summary>
    /// <remarks>
    /// Called from the context's own thread, it cannot wait for itself: the thread finishes the queue once the
    /// work calling this returns.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _queue.CompleteAdding();
        if (Environment.CurrentManagedThreadId == ThreadId) return;

        if (_thread.Join(DrainTimeout) is false)
        {
            _faults.Enqueue(new TimeoutException($"The context's thread did not finish the work queued on it within {DrainTimeout}."));
        }
    }

    private void Run()
    {
        SetSynchronizationContext(this);
        try
        {
            foreach ((SendOrPostCallback callback, object? state, ExecutionContext? flow) in _queue.GetConsumingEnumerable())
            {
                Execute(callback, state, flow);
            }
        }
        finally
        {
            _queue.Dispose();
        }
    }

    private void Execute(SendOrPostCallback callback, object? state, ExecutionContext? flow)
    {
        try
        {
            if (flow is null) callback(state);
            else ExecutionContext.Run(flow, new ContextCallback(callback), state);
        }
        catch (Exception ex)
        {
            // An unhandled exception on a thread of its own would end the whole test host; this one fails the test.
            _faults.Enqueue(ex);
        }
    }
}
