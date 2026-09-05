using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The base of every built-in AI session: an on-device model instance that holds real memory until
/// it is disposed.
/// </summary>
/// <remarks>
/// <b>Always dispose a session.</b> An on-device model's state is measured in hundreds of megabytes,
/// and the browser only releases it when <c>destroy()</c> is called - which is what disposal does.
/// Prefer <c>await using</c>, or keep the session in a field and dispose it in the component's
/// <c>DisposeAsync</c>.
/// <br/>
/// Sessions cannot be constructed directly; each service's <c>Create</c> makes one.
/// </remarks>
public abstract class AiSession : IAsyncDisposable
{
    private bool _disposed;

    private protected AiSession(IJSRuntime js, AiInterop interop, Guid id)
    {
        Js = js;
        Interop = interop;
        Id = id;
    }

    /// <summary>The interop runtime this session talks through.</summary>
    private protected IJSRuntime Js { get; }

    /// <summary>The owning service's callback relay - shared so a forked session dispatches through the same reference.</summary>
    private protected AiInterop Interop { get; }

    /// <summary>The id the JS side keeps this session's model instance under.</summary>
    public Guid Id { get; }

    /// <summary>
    /// How much of the session's input quota is spent, or null once the session has been disposed.
    /// </summary>
    /// <remarks>
    /// Only the language model reports a meaningful quota; the task-specific APIs report zeros.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AiUsage))]
    public ValueTask<AiUsage?> GetUsage() => Js.Invoke<AiUsage?>("BitButil.ai.getUsage", Id);

    /// <summary>Runs the session's own method and returns the whole result at once.</summary>
    private protected ValueTask<string?> RunCore(string input, object? options)
        => Js.Invoke<string?>("BitButil.ai.run", Id, input, options);

    /// <summary>
    /// Runs the session's streaming method, reporting each chunk as it arrives and completing with
    /// the whole text.
    /// </summary>
    private protected async Task<string> RunStreamingCore(string input, object? options, Action<string>? onChunk)
    {
        var (streamId, completion) = Interop.BeginStream(onChunk);

        try
        {
            await Js.InvokeVoid("BitButil.ai.runStreaming",
                Id, input, options, Interop.DotNetRef, streamId,
                AiInterop.ChunkMethodName, AiInterop.DoneMethodName);
        }
        catch (Exception ex)
        {
            // The run never started, so nothing will ever call back done for it: the registration
            // has to be undone here, and the task faulted, or awaiting it would hang.
            Interop.EndStream(streamId, ex);
            throw;
        }

        return await completion;
    }

    /// <summary>
    /// Destroys the model instance and frees what it holds. Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try { await Js.InvokeVoid("BitButil.ai.destroy", Id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
