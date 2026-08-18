using System.Collections.Frozen;

namespace Bit.Bmotion.Demo.Server.Services;

/// <summary>
/// The complete wiring for adding Bit.Bmotion to a Blazor app, one render mode at a time.
/// <para>
/// Render mode is the question an agent gets wrong most expensively here, and it fails silently
/// twice over. In a Blazor Web App the services have to be registered in <b>both</b> DI containers,
/// because the prerender pass instantiates the client's components on the server - miss one and the
/// app throws at prerender, not at compile time. And on Blazor Server half the library degrades to
/// instant state changes by design, so an animation an agent tested on WebAssembly can arrive
/// working-but-motionless with nothing in the build output to say why.
/// </para>
/// <para>
/// Each guide therefore states the container list, the capability matrix for that mode, and the
/// smallest working file set - so the answer is the same shape as the work.
/// </para>
/// <para>
/// The guides are composed from plain (uninterpolated) raw string literals: every one of them is
/// mostly C# and Razor, and an interpolated literal would mean escaping every brace in every code
/// sample - which is how a sample ends up shipped with a doubled brace in it.
/// </para>
/// </summary>
public static class BmotionSetupGuide
{
    /// <summary>The render modes a guide exists for.</summary>
    public static readonly string[] RenderModes = ["wasm", "server", "auto", "standalone-wasm"];

    private static readonly FrozenDictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["wasm"] = "wasm",
        ["webassembly"] = "wasm",
        ["interactivewebassembly"] = "wasm",
        ["web app"] = "wasm",
        ["server"] = "server",
        ["blazor server"] = "server",
        ["interactiveserver"] = "server",
        ["auto"] = "auto",
        ["interactiveauto"] = "auto",
        ["standalone-wasm"] = "standalone-wasm",
        ["standalone"] = "standalone-wasm",
        ["standalone wasm"] = "standalone-wasm",
        ["hosted wasm"] = "standalone-wasm",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>The guide for a render mode, or null when the mode is not one of <see cref="RenderModes"/>.</summary>
    public static string? Get(string renderMode)
    {
        if (_aliases.TryGetValue((renderMode ?? string.Empty).Trim(), out var mode) is false) return null;

        return mode switch
        {
            "server" => Compose(ServerHeader, Install, ServerRegistration, Imports, FirstAnimation, ServerCapabilities),
            "auto" => Compose(AutoHeader, Install, AutoRegistration, Imports, FirstAnimation, AutoGuidance),
            "standalone-wasm" => Compose(StandaloneHeader, Install, StandaloneRegistration, Imports, FirstAnimation, StandaloneCapabilities),
            _ => Compose(WasmHeader, Install, WasmRegistration, Imports, FirstAnimation, WasmCapabilities)
        };
    }

    private static string Compose(params string[] parts) => string.Join("\n\n", parts.Select(part => part.Trim()));

    // -- Shared sections -------------------------------------------------------

    private const string Install = """
        ## 1. Install

        ```bash
        dotnet add package Bit.Bmotion
        ```

        The browser bridge (`bit-bmotion.js`) ships as a static web asset of the package and is imported
        automatically the first time an animation runs. There is no `<script>` tag to add, and no
        JavaScript to write.

        Targets .NET 8, 9 and 10.
        """;

    private const string Imports = """
        ## 3. Make the components reachable

        Add one line to `_Imports.razor` in every project that renders animations:

        ```razor
        @using Bit.Bmotion
        ```

        Without it `<Bmotion>` is not a known component and the build fails with RZ10012 - which is the
        friendly failure, unlike the DI mistake above, because the compiler catches this one.
        """;

    private const string FirstAnimation = """
        ## 4. Check it works

        ```razor
        <Bmotion Initial="Bm.To(opacity: 0, y: 20)"
                 Animate="Bm.To(opacity: 1, y: 0)"
                 Transition="Bm.Spring(bounce: 0.3, duration: 0.5)">
            <div>Hello, Bmotion!</div>
        </Bmotion>
        ```

        The element fades in and slides up on first render. If it appears without moving, the page is
        still prerendered markup: Bmotion is inert during prerendering by design and starts once the
        runtime is interactive.
        """;

    // -- InteractiveWebAssembly ------------------------------------------------

    private const string WasmHeader = """
        # Bit.Bmotion in a Blazor Web App - InteractiveWebAssembly

        The fully supported configuration: every feature works, because the browser runs .NET and the
        engine can use the synchronous per-frame loop.
        """;

    private const string WasmRegistration = """
        ## 2. Register the services - in BOTH containers

        A Blazor Web App has two DI containers. The client's components are instantiated **twice**: once
        on the server for the prerender pass, and once in the browser. Anything they inject has to
        resolve in both places, so both `Program.cs` files call the same method.

        Put the registration in one place so it cannot drift:

        ```csharp
        // Client/Extensions/IServiceCollectionExtensions.cs
        using Bit.Bmotion;

        namespace Microsoft.Extensions.DependencyInjection;

        public static class IServiceCollectionExtensions
        {
            public static IServiceCollection AddDemoServices(this IServiceCollection services)
            {
                services.AddBitBmotionServices();

                return services;
            }
        }
        ```

        ```csharp
        // Client/Program.cs (WebAssembly)
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddDemoServices();

        await builder.Build().RunAsync();
        ```

        ```csharp
        // Server/Program.cs (the host)
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // The prerender pass instantiates the client's components in THIS container, so it has to
        // register the very same services the WebAssembly container does.
        builder.Services.AddDemoServices();

        var app = builder.Build();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
        ```

        > Registering in only one container is the single most common Bmotion setup bug. It fails at
        > prerender with a missing-service exception, not at compile time.
        """;

    private const string WasmCapabilities = """
        ## What works in this mode

        | Feature | Status |
        |---|---|
        | Tweens and springs on transform + opacity | Full - offloaded to the browser compositor |
        | Colour, dimension and keyframe-array animation | Full - C# per-frame loop |
        | Drag, inertia, motion values, gestures | Full |
        | Layout (FLIP) and shared-element transitions | Full |
        | Scroll timelines | Full |

        Nothing degrades. Call `GetBmotionSetupGuide("server")` before assuming the same of a Server app.
        """;

    // -- InteractiveServer -----------------------------------------------------

    private const string ServerHeader = """
        # Bit.Bmotion in a Blazor Web App - InteractiveServer

        **Read the capability table at the bottom before writing any animation for this mode.** Blazor
        Server has no synchronous JS interop, so the per-frame engine cannot run. Animations the browser
        compositor can own still play normally; everything else becomes an instant state change -
        visibly, silently, and only at runtime.
        """;

    private const string ServerRegistration = """
        ## 2. Register the services

        One container, because the components only ever run on the server:

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBitBmotionServices();

        var app = builder.Build();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```
        """;

    private const string ServerCapabilities = """
        ## What works in this mode

        | Feature | Status on Blazor Server |
        |---|---|
        | Tweens and zero-velocity springs on transform + opacity | **Full.** Pre-sampled in C# and handed to the Web Animations API over one async call |
        | Enter, exit, hover, tap and variant animations on those properties | **Full** |
        | Colour interpolation (backgroundColor, color, fill, ...) | **Jumps to the target** |
        | Dimension and layout properties (width, height, top, ...) | **Jumps to the target** |
        | Keyframe arrays, per-property transitions, arc paths | **Jumps to the target** |
        | Drag, inertia, motion values, `Transition.OnUpdate` | **Not available** - they need the per-frame loop |
        | Layout (FLIP) and shared-element transitions | **Jumps to the target** |

        ## Writing for this mode

        1. Animate `x`, `y`, `scale`, `rotate` and `opacity`. They cover most interface motion, and they
           are exactly the properties the compositor can own.
        2. Replace a `width`/`height` animation with `scale`, and a `top`/`left` animation with `x`/`y`.
        3. Check any animation you are unsure about with `AnalyzeBmotionAnimation` - it runs the real
           engine and reports which path it took, rather than guessing from this table.
        4. Make the degradation visible to the reader rather than mysterious, by injecting the
           capability flags:

        ```razor
        @inject BmotionCapabilities Caps

        @if (Caps.SupportsFrameLoop is false)
        {
            <p>Drag is disabled here: this page is served over Blazor Server.</p>
        }
        ```
        """;

    // -- InteractiveAuto -------------------------------------------------------

    private const string AutoHeader = """
        # Bit.Bmotion in a Blazor Web App - InteractiveAuto

        Auto is the hardest mode to write animations for, because **the same component runs in both
        modes**: Server on the first visit, WebAssembly once the runtime has been downloaded and cached.
        An animation that needs the per-frame loop therefore works on the second visit and not on the
        first - which is the worst possible way to find out about it.
        """;

    private const string AutoRegistration = """
        ## 2. Register the services - in BOTH containers

        Identical to the WebAssembly guide, and doubly required here: in Auto the server container is not
        only prerendering, it is running the components interactively until the runtime arrives.

        ```csharp
        // Server/Program.cs
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddDemoServices();   // the shared method - see the wasm guide

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
        ```

        ```csharp
        // Client/Program.cs
        builder.Services.AddDemoServices();
        ```
        """;

    private const string AutoGuidance = """
        ## Writing for this mode

        Design for the Server capability set, and treat everything beyond it as an enhancement:

        1. Build the interface out of `x`, `y`, `scale`, `rotate` and `opacity` animations. Those play in
           both modes, so the first visit and the second look the same.
        2. Gate anything that needs the frame loop - drag, inertia, motion values, colour animation - on
           the capability flag, and give the Server path a static equivalent:

        ```razor
        @inject BmotionCapabilities Caps

        @if (Caps.SupportsFrameLoop)
        {
            <Bmotion Drag="BmDrag.Both">...</Bmotion>
        }
        else
        {
            <div>...</div>   @* the same content, without the gesture *@
        }
        ```

        3. Verify with `AnalyzeBmotionAnimation` before shipping: an animation it reports as running on
           the C# frame loop is one that behaves differently on the user's first visit.
        """;

    // -- Standalone WebAssembly ------------------------------------------------

    private const string StandaloneHeader = """
        # Bit.Bmotion in a standalone Blazor WebAssembly app

        The simplest setup: one project, one DI container, and every feature available.
        """;

    private const string StandaloneRegistration = """
        ## 2. Register the services

        ```csharp
        using Bit.Bmotion;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddBitBmotionServices();

        await builder.Build().RunAsync();
        ```

        There is no second container and no prerender pass, so the registration mistake that catches out
        Blazor Web Apps cannot happen here.
        """;

    private const string StandaloneCapabilities = """
        ## What works in this mode

        Everything. Compositor-eligible animations offload to the Web Animations API and the rest run on
        the C# per-frame loop over synchronous interop, which a standalone WebAssembly app always has.

        ## Worth setting while you are here

        ```csharp
        builder.Services.AddBitBmotionServices(o => o.ReducedMotion = BmReducedMotionMode.User);
        ```

        The default is `IgnoreUnlessConfigured`, which is back-compatible rather than correct: it
        consults the operating system's reduced-motion preference only inside a `<BmotionConfig>`.
        `User` respects it everywhere, which is the web-platform default and what a new app should do.
        """;
}
