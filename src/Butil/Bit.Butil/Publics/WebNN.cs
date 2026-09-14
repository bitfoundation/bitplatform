using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the entry point of the
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/WebNN_API">Web Neural Network API</see>:
/// <c>navigator.ml</c>, its execution contexts, and what each backend can do.
/// </summary>
/// <remarks>
/// <b>Scope.</b> This wraps the part that answers "can this device run a model, and on what" -
/// creating a context and reading its operator support. Building and running a graph is not wrapped:
/// an <c>MLGraphBuilder</c> program is dozens of chained operator calls over tensor handles that
/// cannot cross interop, and marshalling each one through JSON would be slower than the inference it
/// is meant to accelerate. Build the graph in a JS module and call into it; use this service to
/// decide whether that module is worth loading at all.
/// <br/>
/// <b>Early.</b> Behind a flag in Chromium, absent everywhere else, and its shape is still changing.
/// Treat a supported result as an opportunity, never as a requirement.
/// </remarks>
[ButilService(typeof(WebNN))]
public class WebNN(IJSRuntime js) : IAsyncDisposable
{
    /// <summary>True when the runtime exposes <c>navigator.ml.createContext</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.webNN.isSupported");

    /// <summary>
    /// Creates an execution context on a device.
    /// </summary>
    /// <param name="deviceType">
    /// <c>"cpu"</c>, <c>"gpu"</c> or <c>"npu"</c>. A hint: the runtime may hand back another device,
    /// which is why <see cref="WebNNContext.Info"/> reports what was actually created. Empty leaves
    /// the choice to the browser.
    /// </param>
    /// <param name="powerPreference">
    /// <c>"default"</c>, <c>"high-performance"</c> or <c>"low-power"</c>. Empty leaves it to the browser.
    /// </param>
    /// <returns>
    /// The context, or null when the runtime has no WebNN or no backend for the request.
    /// <b>Dispose it</b> when you are done.
    /// </returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(WebNNContextInfo))]
    public async ValueTask<WebNNContext?> CreateContext(string deviceType = "", string powerPreference = "")
    {
        var id = Guid.NewGuid();
        var info = await js.Invoke<WebNNContextInfo?>("BitButil.webNN.createContext", id, deviceType, powerPreference);

        return info is null ? null : new WebNNContext(js, id, info);
    }

    /// <summary>Releases every context created through this instance that is still open.</summary>
    public async ValueTask DisposeAsync()
    {
        try { await js.InvokeVoid("BitButil.webNN.disposeAll"); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
