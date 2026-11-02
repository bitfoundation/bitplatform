using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>
/// Names the type in Bit.BevTools that implements <see cref="IBevToolsEntryPoint"/>. The loader reads it from the
/// assembly once that is loaded: the one place the two meet, since the loader must not reference Bit.BevTools.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class BevToolsEntryPointAttribute(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entryPointType) : Attribute
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type EntryPointType { get; } = entryPointType;
}

/// <summary>What the loader asks of Bit.BevTools, which implements it. Internal to both assemblies.</summary>
internal interface IBevToolsEntryPoint
{
    /// <summary>The <c>BevToolsPanel</c> component.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    Type PanelType { get; }

    /// <summary>
    /// Registers BevTools while the app starts when it should run from the first render, or reports why not and
    /// leaves <paramref name="services"/> untouched.
    /// </summary>
    bool TryAddServices(IServiceCollection services, BevToolsLoaderContext context, out string reason);

    /// <summary>Attaches BevTools to the running app in the scope of <paramref name="services"/>, or returns the session registered at startup.</summary>
    IBevToolsAttachment Attach(IServiceProvider services, BevToolsLoaderContext context);

    /// <summary>
    /// Marks what runs until it is disposed as BevTools' own work, which no session records: the
    /// <see cref="BitBevToolsLoaderOptions.Authorize"/> callback's requests, logs and activities are not the app's.
    /// </summary>
    IDisposable BeginOwnWork();
}

/// <summary>A BevTools session as the loader sees it. Disposing it ends a session the attach created, never one DI owns.</summary>
internal interface IBevToolsAttachment : IDisposable
{
    bool IsEnabled { get; }

    /// <summary>Why the session is or is not enabled.</summary>
    string Reason { get; }

    void Open();
}

/// <summary>Records the requests that reach the loader's HTTP hooks. Implemented by Bit.BevTools.</summary>
internal interface IBevToolsHttpRecorder
{
    /// <summary>
    /// Notes on the request which session issued it. Called at the outer end of a pipeline, where the caller's
    /// synchronization context is still current; a handler further in may have continued on the thread pool.
    /// </summary>
    void CaptureSession(HttpRequestMessage request);

    /// <summary>
    /// Records the request around <paramref name="next"/>. <paramref name="attachedSessionsOnly"/> is true in
    /// <c>IHttpClientFactory</c> pipelines, which a BevTools registered at startup records with handlers of its own:
    /// recording them here too would show every request twice.
    /// </summary>
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, bool attachedSessionsOnly, CancellationToken cancellationToken);
}

/// <summary>The process-wide hook points Bit.BevTools fills once it is registered or attached.</summary>
internal static class BevToolsHooks
{
    private static IBevToolsHttpRecorder? _http;

    public static IBevToolsHttpRecorder? Http
    {
        get => Volatile.Read(ref _http);
        set => Volatile.Write(ref _http, value);
    }
}
