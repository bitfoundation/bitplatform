// Spelled out rather than left to the implicit usings, because this file is also compile-linked into the
// test project, which has none: the guard only ever runs in the browser, so what it decides can be reached
// only from a copy of its own source (see the csproj of Bit.BlazorUI.Tests).
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bit.BlazorUI;

/// <summary>
/// The DEBUG-only developer aid behind <c>FastInvoke</c>'s synchronous path.
/// </summary>
/// <remarks>
/// <para>
/// On a multithreaded WebAssembly runtime (<c>&lt;WasmEnableThreads&gt;</c>) <see cref="IJSInProcessRuntime"/>
/// may only be used on the main thread, so running it from a background thread - the usual outcome of a
/// preceding <c>ConfigureAwait(false)</c> - throws. This reports that ahead of the framework's own
/// lower-level exception, with a message that says what to do about it.
/// </para>
/// <para>
/// It <em>reports</em> and does not fail. The test it can afford to make - "is this a thread-pool thread?" -
/// is a heuristic, and on the single-threaded WebAssembly runtime that nearly every Blazor WASM app is built
/// with, a queued work item runs on the main thread, where synchronous interop is perfectly legal. Failing
/// fast on that would abort a debug build over something that is not a problem. Where it IS a problem, the
/// framework throws its own precise exception a moment later, so nothing is lost by making this advisory.
/// Each identifier is reported once, so a call in a render loop does not bury the console.
/// </para>
/// <para>
/// The whole thing is called from a <c>[Conditional("DEBUG")]</c> method, so neither the call nor its
/// argument survives into a shipping build and the fast path stays branch-free.
/// </para>
/// </remarks>
internal static class BitInProcessInteropThreadGuard
{
    // One report per identifier. A ConcurrentDictionary because the very situation being reported is a call
    // arriving from somewhere other than the renderer's synchronization context.
    private static readonly ConcurrentDictionary<string, byte> ReportedIdentifiers = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether a call made from a thread with these properties is worth reporting. Pure, so that what the
    /// guard decides can be checked without being in a browser - which is the one place it ever runs.
    /// </summary>
    /// <param name="isBrowser">Whether the code is running in the browser (WebAssembly).</param>
    /// <param name="isThreadPoolThread">Whether the calling thread is a thread-pool thread.</param>
    internal static bool ShouldReport(bool isBrowser, bool isThreadPoolThread)
    {
        // Outside the browser there is no in-process runtime to misuse: Server and Hybrid take the
        // asynchronous path, and a test double is not a runtime with thread affinity. Reporting there would
        // be noise about a rule that does not apply.
        return isBrowser && isThreadPoolThread;
    }

    /// <summary>The message a report carries. It names the call and says what to do instead.</summary>
    internal static string BuildMessage(string identifier)
    {
        return $"FastInvoke('{identifier}') ran synchronous in-process JS interop on a thread-pool thread. " +
               "On multithreaded WebAssembly (<WasmEnableThreads>) this is only valid on the main thread and will throw. " +
               "Invoke it on the renderer's synchronization context (component lifecycle/event callbacks) without a " +
               "preceding ConfigureAwait(false), or use the regular asynchronous invocation instead.";
    }

    /// <summary>
    /// Reports the call if it is worth reporting, at most once per identifier. Never throws: it is a
    /// diagnostic, and one that cannot be certain it is right.
    /// </summary>
    internal static void Report(string identifier)
    {
        if (ShouldReport(OperatingSystem.IsBrowser(), Thread.CurrentThread.IsThreadPoolThread) is false) return;

        // The message is built only when there is something to say - these are hot paths, and in a DEBUG
        // build every FastInvoke passes through here.
        if (ReportedIdentifiers.TryAdd(identifier, 0) is false) return;

        var message = BuildMessage(identifier);

        Debug.WriteLine(message);
        Console.Error.WriteLine(message);
    }

    /// <summary>Forgets what has been reported. For tests, which need each case reported on its own.</summary>
    internal static void ResetReported() => ReportedIdentifiers.Clear();
}
