using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Butil;

public static class BitButil
{
    /// <summary>
    /// Registers every Butil service - each class marked with <see cref="ButilServiceAttribute"/> - as scoped.
    /// </summary>
    /// <remarks>
    /// Scoped matches Blazor's "one circuit / one WASM app instance per user" model. Transient would create
    /// a fresh wrapper on every <c>@inject</c>, fragmenting per-instance listener bookkeeping and keeping
    /// captured component delegates alive longer than the component itself.
    /// <br/>
    /// The services are discovered by reflection rather than by a list of <c>AddScoped&lt;T&gt;()</c> calls,
    /// and that is a trimming decision, not a style one. <c>AddScoped&lt;T&gt;()</c> annotates <c>T</c> with
    /// <see cref="DynamicallyAccessedMemberTypes.PublicConstructors"/>, so naming all the Butil classes in
    /// one method roots all of them: a consumer that injects only <see cref="LocalStorage"/> still carried
    /// every other Butil class in their trimmed output. Reflecting over the assembly gives the trimmer
    /// nothing to follow, so a class nobody injects is removed from the consumer's app and is simply not
    /// there to be discovered here - <see cref="System.Reflection.Assembly.GetTypes"/> returns only the
    /// types that survived. Constructors of the classes that DO survive are preserved by the annotated
    /// type argument of <see cref="ButilServiceAttribute"/>; see that type for why it is shaped that way.
    /// <br/>
    /// Consequence worth knowing: in a trimmed app this registers a subset, so injecting a Butil class from
    /// code the trimmer removed (or purely through reflection) fails at runtime rather than at build time.
    /// Untrimmed apps - Blazor Server, and the prerendering host of a WebAssembly app - register everything.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enumerating this assembly's types is the point: types the trimmer removed are absent from the consumer's app, so skipping them is correct rather than a defect. Constructors of the surviving types are preserved by ButilServiceAttribute's annotated type argument.")]
    public static IServiceCollection AddBitButilServices(this IServiceCollection services)
    {
        foreach (var type in typeof(BitButil).Assembly.GetTypes())
        {
            if (type.GetCustomAttribute<ButilServiceAttribute>(inherit: false) is not { } butilService) continue;

            // Registering ServiceType rather than the scanned type is what keeps this call trim-clean:
            // the property carries the PublicConstructors annotation, so no suppression is needed here.
            // TryAdd so that a consumer registration made before this call wins, and so that calling this
            // method twice on the same collection cannot produce duplicate descriptors.
            services.TryAddScoped(butilService.ServiceType);
        }

        return services;
    }

    private static volatile bool _fastInvokeEnabled;

    internal static bool FastInvokeEnabled => _fastInvokeEnabled;

    /// <summary>
    /// Enables the synchronous in-process ("fast") invoke path for the APIs that opt into it.
    /// <br/>
    /// Only APIs backed by synchronous JavaScript functions (for example <see cref="LocalStorage"/>,
    /// <see cref="SessionStorage"/>, <see cref="Cookie"/>, <see cref="Console"/> and <see cref="Location"/>)
    /// use this path; everything that wraps an asynchronous (Promise-returning) browser API always runs
    /// asynchronously regardless of this setting, so enabling it can't break those calls.
    /// Only effective on Blazor WebAssembly (where an <see cref="Microsoft.JSInterop.IJSInProcessRuntime"/> is available).
    /// <br/>
    /// NOTE: this is a process-wide static toggle, not per-app/per-circuit. It is intended to be set
    /// once at startup. On Blazor Server it is effectively a no-op (the fast path always falls back to
    /// the async path because there is no in-process runtime), so sharing it across circuits is benign.
    /// </summary>
    public static void UseFastInvoke()
    {
        _fastInvokeEnabled = true;
    }

    /// <summary>
    /// Disables the synchronous in-process ("fast") invoke path; all calls run asynchronously.
    /// Process-wide static toggle - see <see cref="UseFastInvoke"/>.
    /// </summary>
    public static void UseNormalInvoke()
    {
        _fastInvokeEnabled = false;
    }
}
