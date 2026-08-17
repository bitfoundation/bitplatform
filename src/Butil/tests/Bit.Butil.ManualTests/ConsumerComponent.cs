using Bit.Butil;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ButilManualTests;

/// <summary>
/// Stands in for a consumer's Blazor component. It exists to create exactly the kind of reference
/// that <c>@inject</c> creates, because that reference is what the trimming behaviour hinges on.
/// </summary>
/// <remarks>
/// A razor <c>@inject LocalStorage LocalStorage</c> compiles down to an <see cref="InjectAttribute"/>
/// decorated property, and Blazor fills it through the non-generic
/// <see cref="IServiceProvider.GetService(Type)"/>. So the only static reference the trimmer sees is
/// the property's <b>type</b> - never its constructor. That is why this class resolves through the
/// non-generic overload too: calling <c>GetRequiredService&lt;LocalStorage&gt;()</c> would annotate the
/// type argument with <c>PublicConstructors</c> and preserve the constructor by itself, which would
/// quietly defeat the very thing this project is meant to verify.
/// </remarks>
internal sealed class ConsumerComponent
{
    [Inject] public LocalStorage LocalStorage { get; set; } = default!;

    [Inject] public Clipboard Clipboard { get; set; } = default!;

    [Inject] public Cookie Cookie { get; set; } = default!;

    // Window and Geolocation are here for the interop contract rather than for the registration check:
    // between them they exercise the paths that are resolved by name at runtime and would break silently
    // under trimming - a DOM event subscription (the internal DomEventsInterop and its [JSInvokable]
    // callbacks, plus a ButilMouseEventArgs payload) and APIs whose results are JSON DTOs.
    [Inject] public Window Window { get; set; } = default!;

    [Inject] public Geolocation Geolocation { get; set; } = default!;

    /// <summary>
    /// The Butil services this component injects. <c>typeof</c> references the type without preserving
    /// its constructors, matching what the property declarations above already do.
    /// </summary>
    public static Type[] InjectedTypes => [typeof(LocalStorage), typeof(Clipboard), typeof(Cookie), typeof(Window), typeof(Geolocation)];

    /// <summary>
    /// The DTOs the calls in <see cref="Use"/> actually round-trip through System.Text.Json. These, and
    /// the Butil types nested inside them, must keep their public constructors and properties in a trimmed
    /// build or interop would silently hand back nulls.
    /// </summary>
    /// <remarks>
    /// Naming them explicitly is deliberate. "Any Butil type in a surviving public signature" over-reports
    /// wildly: types like <c>ScrollOptions</c> or <c>WindowFeatures</c> belong to methods this project never
    /// calls, so the trimmer strips them to shells - correct behaviour that would otherwise look like a
    /// defect. Only a type on a code path that is genuinely exercised can be asserted on.
    /// <br/>
    /// <c>typeof</c> here keeps the types present but preserves none of their members, so it cannot mask a
    /// failure: the members have to survive on the strength of the library's own annotations.
    /// </remarks>
    public static Type[] ExercisedPayloadTypes => [typeof(ButilCookie), typeof(GeolocationPosition), typeof(BarProp), typeof(ButilMouseEventArgs)];

    public void Inject(IServiceProvider serviceProvider)
    {
        LocalStorage = (LocalStorage)serviceProvider.GetRequiredService(typeof(LocalStorage));
        Clipboard = (Clipboard)serviceProvider.GetRequiredService(typeof(Clipboard));
        Cookie = (Cookie)serviceProvider.GetRequiredService(typeof(Cookie));
        Window = (Window)serviceProvider.GetRequiredService(typeof(Window));
        Geolocation = (Geolocation)serviceProvider.GetRequiredService(typeof(Geolocation));
    }

    /// <summary>
    /// Calls into each injected service so the reference is a real usage rather than an unused field
    /// the compiler or the trimmer could reason away.
    /// </summary>
    /// <remarks>
    /// The JS runtime is a stub, so return values are meaningless and a call may well throw - only the
    /// fact that the instances were constructed and are callable matters here.
    /// </remarks>
    public async Task<(int Succeeded, int Threw)> Use()
    {
        // Each step runs independently: the stub returns null where a DTO is expected, so one step
        // throwing must not stop the rest from running. Which of them throw is irrelevant to trimming -
        // the trimmer works off the calls being present in IL, not off them being executed - but running
        // them all keeps the report honest about what was actually exercised.
        (string Name, Func<Task> Call)[] steps =
        [
            ("LocalStorage.SetItem", () => LocalStorage.SetItem("butil-manual-test", "1")),
            ("LocalStorage.GetItem", () => LocalStorage.GetItem("butil-manual-test")),
            ("Clipboard.IsSupported", () => Clipboard.IsSupported().AsTask()),

            // The next three return Bit.Butil DTOs that System.Text.Json has to reconstruct by reflection,
            // which is what puts the payload half of the interop contract under test.
            ("Cookie.GetAll", () => Cookie.GetAll()),
            ("Geolocation.GetCurrentPosition", () => Geolocation.GetCurrentPosition()),
            ("Window.GetLocationBar", () => Window.GetLocationBar()),

            // A DOM subscription: reaches the internal DomEventsInterop, whose [JSInvokable] callbacks JS
            // dispatches by name, and hands back a ButilMouseEventArgs payload.
            ("Window.SubscribeEvent", () => Window.SubscribeEvent<ButilMouseEventArgs>(ButilEvents.Click, _ => { })),
        ];

        var succeeded = 0;
        var threw = 0;
        foreach (var (_, call) in steps)
        {
            try
            {
                await call();
                succeeded++;
            }
            catch
            {
                threw++;
            }
        }

        return (succeeded, threw);
    }
}
