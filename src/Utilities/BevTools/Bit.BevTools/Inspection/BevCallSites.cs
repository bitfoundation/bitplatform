using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>
/// Where in the application a call came from, read from the stack: the component method that made it and, between them,
/// the method of the application's own it called through ("OrderRow.OnInitializedAsync" via
/// "ProductApi.GetOrderDetailsAsync"). An HTTP request reaches BevTools' handler synchronously from the code that sent it
/// - an async method's frames are its state machine's <c>MoveNext</c>, there again after every await - so the caller is on
/// the stack unless something in between went asynchronous first; then nothing is claimed, and <see cref="CallSite.Missing"/>
/// says why.
/// </summary>
internal static class BevCallSites
{
    // A request from twenty nested async calls deep is about a hundred frames; the stack is captured whole either way.
    internal const int MaxFrames = 256;

    // Per declaring type, what the walk needs to know about its frames; read once per type, not per frame and request
    // (Assembly.GetName allocates a new AssemblyName on every call). Weak, so a collectible assembly can still go.
    private static readonly ConditionalWeakTable<Type, TypeFacts> Facts = new();

    private sealed record TypeFacts(Type? Owner, bool Skip, bool IsComponent);

    /// <param name="Component">"OrderRow.OnInitializedAsync": the nearest component method on the stack.</param>
    /// <param name="Via">"ProductApi.GetOrderDetailsAsync": the application method the component called, when it is not the component itself.</param>
    /// <param name="Missing">Why there is no <paramref name="Component"/>, when there is none.</param>
    internal readonly record struct CallSite(string? Component, string? Via, string? Missing = null);

    internal const string NoComponent = "no component on the stack (a service, a timer, or code that went asynchronous before sending)";

    internal const string AfterCompletion = "no component on the stack: it was sent from a continuation that another task's completion ran, so the code below it is whoever completed that task, not the sender";

    internal const string TooDeep = "no component within the 256 innermost frames of the stack";

    internal const string Unavailable = "the stack could not be read here (a trimmed or AOT build without stack metadata)";

    public static CallSite Find(int skipFrames)
    {
        try
        {
            var trace = new StackTrace(skipFrames + 1, fNeedFileInfo: false);
            (MethodBase Method, Type Owner)? via = null;
            var frames = Math.Min(trace.FrameCount, MaxFrames);
            for (var i = 0; i < frames; i++)
            {
                if (trace.GetFrame(i)?.GetMethod() is not { DeclaringType: { } declaring } method)
                {
                    continue;
                }

                // Completing a task runs the continuations waiting on it right there, on the completer's stack, when
                // nothing tells them to go elsewhere (WebAssembly has no synchronization context): below this frame is
                // whoever completed the task - another component, a timer - not the code that sent the request.
                if (declaring == typeof(Task) && method.Name is "RunContinuations" or "FinishContinuations")
                {
                    return new CallSite(null, null, AfterCompletion);
                }

                if (Facts.GetValue(declaring, Read) is not { Skip: false, Owner: { } owner } facts)
                {
                    continue;
                }

                if (facts.IsComponent)
                {
                    return new CallSite(Name(owner, method), via is { } v ? Name(v.Owner, v.Method) : null);
                }

                // The application method nearest the component: what the component called. Named only if one is found.
                via = (method, owner);
            }

            return new CallSite(null, null, trace.FrameCount > MaxFrames ? TooDeep : NoComponent);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // A runtime without stack metadata (trimmed, AOT): no call site rather than a wrong one.
            return new CallSite(null, null, Unavailable);
        }
    }

    private static TypeFacts Read(Type declaring)
    {
        // The type a method was written in: the component for its async state machine, lambda or local function.
        var owner = declaring;
        while (owner.IsDefined(typeof(CompilerGeneratedAttribute), false) && owner.DeclaringType is { } outer)
        {
            owner = outer;
        }

        var skip = IsFramework(owner.Assembly);
        return new TypeFacts(owner, skip, !skip && typeof(IComponent).IsAssignableFrom(owner));
    }

    private static string Name(Type owner, MethodBase method) => TypeNames.Short(owner) + "." + MethodName(method);

    /// <summary>"OnInitializedAsync" for its state machine's MoveNext, "Load" for a local function, the name otherwise.</summary>
    private static string MethodName(MethodBase method)
    {
        // An async method or iterator runs in its state machine: "<OnInitializedAsync>d__5", or for an async local
        // function or lambda "<<OnInitializedAsync>g__Fetch|3_0>d" - the method's own (generated) name in the brackets.
        var name = method.Name;
        if (name == "MoveNext" && method.DeclaringType?.Name is { } stateMachine && stateMachine.StartsWith('<') && stateMachine.LastIndexOf('>') is > 1 and var end)
        {
            name = stateMachine[1..end];
        }

        if (!name.StartsWith('<'))
        {
            return name;
        }

        // A local function "<OnInitializedAsync>g__Load|5_0" is named by itself; a lambda by the method it is in.
        var local = name.IndexOf(">g__", StringComparison.Ordinal);
        if (local > 0 && name.IndexOf('|', local) is > 0 and var bar)
        {
            return name[(local + 4)..bar];
        }

        var close = name.IndexOf('>');
        return close > 1 ? name[1..close] + " (lambda)" : name;
    }

    private static bool IsFramework(Assembly assembly)
    {
        if (assembly == typeof(BevCallSites).Assembly)
        {
            return true;
        }

        // BevTools' own assemblies by exact name: an application may be called Bit.BevTools.Anything (the demo is).
        var name = assembly.GetName().Name ?? "";
        return name is "mscorlib" or "netstandard" or "System" or "Bit.BevTools.Loader" or "Bit.BevTools.Server"
            || name.StartsWith("System.", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.", StringComparison.Ordinal);
    }
}
