using System.Collections.Frozen;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// The complete wiring for adding Bit.Bmage to an app, one hosting model at a time.
/// <para>
/// Where Bmage runs decides three things an agent otherwise gets wrong: which DI containers have to
/// register the services (a Blazor Web App has two, and the prerender pass instantiates every
/// component in the server's), which registration to use (<c>AddBitBmageServices</c> in a browser,
/// <c>AddBitBmageHeadlessServices</c> without one - and the two register different services), and
/// which codecs exist at all (WebP and AVIF are browser-only). None of those mistakes fails to
/// compile. A missing registration throws at prerender; a headless registration in a browser app
/// leaves <c>ImageLoader</c> unresolvable; a WebP export on a server throws at the first upload.
/// </para>
/// <para>
/// The guides are composed from plain (uninterpolated) raw string literals: every one of them is
/// mostly C# and Razor, and an interpolated literal would mean escaping every brace in every code
/// sample.
/// </para>
/// </summary>
public static class BmageSetupGuide
{
    /// <summary>The hosting models a guide exists for.</summary>
    public static readonly string[] RenderModes = ["wasm", "server", "auto", "standalone-wasm", "headless"];

    private static readonly FrozenDictionary<string, string> _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["wasm"] = "wasm",
        ["webassembly"] = "wasm",
        ["interactivewebassembly"] = "wasm",
        ["web app"] = "wasm",
        ["webapp"] = "wasm",
        ["server"] = "server",
        ["blazor server"] = "server",
        ["interactiveserver"] = "server",
        ["auto"] = "auto",
        ["interactiveauto"] = "auto",
        ["standalone-wasm"] = "standalone-wasm",
        ["standalone"] = "standalone-wasm",
        ["standalone wasm"] = "standalone-wasm",
        ["blazorwasm"] = "standalone-wasm",
        ["headless"] = "headless",
        ["console"] = "headless",
        ["aspnetcore"] = "headless",
        ["asp.net core"] = "headless",
        ["api"] = "headless",
        ["minimal api"] = "headless",
        ["worker"] = "headless",
        ["no browser"] = "headless",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>The canonical name of a hosting model, or null when it is not one of <see cref="RenderModes"/>.</summary>
    public static string? Resolve(string? renderMode)
        => _aliases.TryGetValue((renderMode ?? string.Empty).Trim(), out var mode) ? mode : null;

    /// <summary>The guide for a hosting model, or null when it is not one of <see cref="RenderModes"/>.</summary>
    public static string? Get(string? renderMode)
    {
        return Resolve(renderMode) switch
        {
            "server" => Compose(ServerHeader, Install, ServerRegistration, Stylesheet, ServerWhen, FirstImage, ServerCapabilities),
            "auto" => Compose(AutoHeader, Install, AutoRegistration, Stylesheet, Aot, PrerenderRule, FirstImage, AutoGuidance),
            "standalone-wasm" => Compose(StandaloneHeader, Install, StandaloneRegistration, StandaloneStylesheet, Aot, FirstImage, StandaloneCapabilities),
            "headless" => Compose(HeadlessHeader, HeadlessInstall, HeadlessRegistration, HeadlessEndpoint, HeadlessConsole, HeadlessCapabilities),
            "wasm" => Compose(WasmHeader, Install, WasmRegistration, Stylesheet, Aot, PrerenderRule, FirstImage, WasmCapabilities),
            _ => null
        };
    }

    private static string Compose(params string[] parts) => string.Join("\n\n", parts.Select(part => part.Trim()));

    // -- Shared sections -------------------------------------------------------

    private const string Install = """
        ## 1. Install

        Pick the package for what you need; each one brings the ones below it.

        | Package | Use it when | Namespace roots |
        |---|---|---|
        | `Bit.Bmage.Editor` | You want the `BmageEditor` / `BmageDropZone` components | `Bit.Bmage.Editor.Components`, `Bit.Bmage.Editor.Tools` |
        | `Bit.Bmage` | Browser codecs, loading and export, your own UI | `Bit.Bmage`, `Bit.Bmage.Input`, `Bit.Bmage.Output` |
        | `Bit.Bmage.Core` | The engine with no Blazor dependency at all | `Bit.Bmage.Pipeline`, `Bit.Bmage.Codecs`, ... |

        ```bash
        dotnet add package Bit.Bmage.Editor
        ```

        Install it in the project that runs in the browser - in a Blazor Web App, the **.Client** project.
        Targets .NET 8, 9 and 10.

        Then add the namespaces to `_Imports.razor` of that project. Bmage's types are spread over several
        namespaces, and the ones a page normally needs are these:

        ```razor
        @using Bit.Bmage
        @using Bit.Bmage.Input
        @using Bit.Bmage.Output
        @using Bit.Bmage.Pipeline
        @using Bit.Bmage.Codecs
        @using Bit.Bmage.Capabilities
        @using Bit.Bmage.Metadata
        @using Bit.Bmage.Geometry
        @using Bit.Bmage.Editor.Components
        @using Bit.Bmage.Editor.Tools
        ```

        The browser bridge (`_content/Bit.Bmage/bit-bmage.js`) is imported by the library on demand. Do not add
        a `<script>` tag for it: it is an ES module, and loaded as a classic script it fails with a syntax error.
        """;

    private const string Stylesheet = """
        ## 3. Link the editor's stylesheet

        Only for `Bit.Bmage.Editor`. In `Components/App.razor` of the server project:

        ```razor
        <head>
            ...
            <link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" />
            <HeadOutlet @rendermode="InteractiveWebAssembly" />
        </head>
        ```

        Without it the editor renders unstyled - a toolbar of bare buttons and no canvas frame - with no error
        anywhere. The editor also fills its container, so give that container a height
        (`<div style="height: 70vh"><BmageEditor /></div>`): a container with no height collapses to nothing.
        """;

    private const string StandaloneStylesheet = """
        ## 3. Link the editor's stylesheet

        Only for `Bit.Bmage.Editor`. In `wwwroot/index.html`:

        ```html
        <head>
            ...
            <link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" />
        </head>
        ```

        Without it the editor renders unstyled, with no error anywhere. Give the editor's container a height:
        it fills its container, and one with no height collapses to nothing.
        """;

    private const string Aot = """
        ## 4. Compile the browser project ahead of time

        Every pixel loop in Bmage is C#. On Blazor's default IL interpreter it runs **10 to 35 times slower** than
        compiled code; with AOT it lands within 1.2 to 2.4 times of desktop .NET. In the project that runs in the
        browser (the .Client project of a Web App):

        ```xml
        <PropertyGroup>
          <RunAOTCompilation>true</RunAOTCompilation>
        </PropertyGroup>
        ```

        - It needs the `wasm-tools` workload: `dotnet workload install wasm-tools`.
        - It only applies to `dotnet publish`. `dotnet run` and the debugger always interpret, so a page that feels
          slow in development may be fine once published - judge performance on a published build.
        - The download grows (`dotnet.native.wasm` went from 2.9 MB to 13 MB uncompressed) and publishing takes
          minutes. To keep `dotnet build -c Release` working on a machine without the workload, condition it on
          publishing, as this demo's client project does (`GetBmageSourceFile(path: "Demo/Client/Bit.Bmage.Demo.Client.csproj")`).
        """;

    private const string PrerenderRule = """
        ## 5. Do browser work after the first render, never during prerendering

        The server prerenders every page before the WebAssembly runtime has arrived, and there is no browser
        during that pass. Anything that needs one has to run from an event handler or from
        `OnAfterRenderAsync` - never from `OnInitialized(Async)` or `OnParametersSet(Async)`, which the
        prerender also runs:

        ```razor
        @inject IImageCapabilityProvider Capabilities

        @code {
            private ImageCapabilities? _capabilities;

            protected override async Task OnAfterRenderAsync(bool firstRender)
            {
                // OnAfterRenderAsync never runs during prerendering; RendererInfo says whether
                // this render is the interactive one.
                if (!firstRender || !RendererInfo.IsInteractive) return;

                _capabilities = await Capabilities.GetAsync();
                StateHasChanged();
            }
        }
        ```

        What goes wrong otherwise is quiet. `IImageCapabilityProvider.GetAsync` does not throw during prerendering:
        it returns the **managed** capability set with `Environment = Prerendering` - so a page that decides its
        export format in `OnInitializedAsync` concludes that WebP is unsupported on every browser. The calls that
        need the JavaScript bridge - downloads, object URLs, the clipboard, paste listeners, remote URLs - throw
        `ImageCapabilityException` ("Could not load the Bmage browser module") instead.
        """;

    private const string FirstImage = """
        ## 6. Check it works

        ```razor
        @page "/upload"
        @implements IAsyncDisposable
        @inject ImageLoader Loader
        @inject ImageExporter Exporter

        <InputFile OnChange="OnChange" accept="image/*" />

        @if (_preview is not null)
        {
            <img src="@_preview" alt="Processed preview" />
            <p>@_size bytes</p>
        }

        @code {
            private string? _preview;
            private long _size;

            private async Task OnChange(InputFileChangeEventArgs e)
            {
                // Decoded by the browser, upright (EXIF rotation applied), and yours to dispose.
                using var image = await Loader.LoadAsync(e.File);

                using var processed = await ImagePipeline.Create()
                    .MaxSize(800, 800)
                    .Sharpen(0.3f)
                    .ExecuteAsync(image);

                // WebP where this browser can write it, JPEG where it cannot.
                var encoded = await Exporter.EncodeWithFallbackAsync(
                    processed, [ImageFormat.WebP, ImageFormat.Jpeg],
                    new ImageExportOptions { Quality = 0.85 });

                if (_preview is not null) await Exporter.ReleaseObjectUrlAsync(_preview);

                _size = encoded.Length;
                _preview = await Exporter.CreateObjectUrlAsync(encoded);
            }

            public async ValueTask DisposeAsync()
            {
                if (_preview is not null) await Exporter.ReleaseObjectUrlAsync(_preview);
            }
        }
        ```

        Two things in it are easy to miss: `using` on every `ImageBuffer` (a 4000 x 3000 image is 48 MB of pooled
        memory, and you own what `LoadAsync` and `ExecuteAsync` return), and an object URL rather than
        `ToDataUrl()` (base64 inflates the bytes by a third and pushes them through the DOM as a string).
        """;

    // -- InteractiveWebAssembly ------------------------------------------------

    private const string WasmHeader = """
        # Bit.Bmage in a Blazor Web App - InteractiveWebAssembly

        The intended home: the image work runs on the user's device, with no pixels crossing a network. Every
        feature works once the component is interactive. What needs care is the prerender pass, which runs every
        component on the server first - with no browser.
        """;

    private const string WasmRegistration = """
        ## 2. Register the services - in BOTH containers

        A Blazor Web App has two DI containers: the client's, in the browser, and the server's, which prerenders
        every page before the runtime has downloaded. Every component is instantiated in both, so anything it
        injects (`ImageLoader`, `ImageExporter`, `IImageCapabilityProvider`, `ImageProcessorFactory`) has to
        resolve in both - or the prerender fails with a missing-service exception, not the build. Keep the
        registration in one method in the client project and call it from both `Program.cs` files:

        ```csharp
        // Client/Extensions/IServiceCollectionExtensions.cs
        using Bit.Bmage;

        namespace Microsoft.Extensions.DependencyInjection;

        public static class IServiceCollectionExtensions
        {
            public static IServiceCollection AddAppServices(this IServiceCollection services)
            {
                services.AddBitBmageServices(options =>
                {
                    // Refuse oversized uploads before anything is decoded.
                    options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };
                    options.MaxSourceBytes = 32 * 1024 * 1024;
                });

                return services;
            }
        }
        ```

        ```csharp
        // Client/Program.cs
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddAppServices();

        await builder.Build().RunAsync();
        ```

        ```csharp
        // Server/Program.cs
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        // The prerender pass instantiates the client's components in THIS container.
        builder.Services.AddAppServices();

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

        app.Run();
        ```

        `AddBitBmageServices` registers `ImageLoader`, `ImageExporter`, `IImageCapabilityProvider` and
        `ImageProcessorFactory` - **not** `ImageProcessor`, because in a browser the processor can only be built
        once the browser's codecs have been probed, which is asynchronous. Inject the factory and call
        `await Factory.GetAsync()` in a handler. Do not use `AddBitBmageHeadlessServices` here: it registers none
        of the browser services.

        This demo is exactly this setup: `GetBmageSourceFile(path: "Demo/Server/Program.cs")` and
        `GetBmageSourceFile(path: "Demo/Client/Extensions/IServiceCollectionExtensions.cs")`.
        """;

    private const string WasmCapabilities = """
        ## What works in this mode

        | Feature | Status |
        |---|---|
        | Loading from files, drag and drop, clipboard, camera, URLs | Full, once interactive |
        | PNG, JPEG, WebP, AVIF, GIF decode | The browser's codecs, probed at runtime |
        | WebP encode | Most browsers - check `CanEncode` or use `EncodeWithFallbackAsync` |
        | AVIF encode | Rarely - always with a fallback |
        | Pipelines, drawing, annotations, editor | Full, in C# on the user's device |
        | During prerendering | Managed codecs only, `Environment = Prerendering`, no JS bridge |
        """;

    // -- InteractiveServer -----------------------------------------------------

    private const string ServerHeader = """
        # Bit.Bmage in a Blazor Web App - InteractiveServer

        Everything works, because JavaScript interop works over the circuit - but the pixels travel. The browser
        decodes an image and the decoded pixels are streamed to the server for processing, then streamed back to be
        encoded: a 4000 x 3000 photo is 48 MB in each direction. On a local network that is unremarkable; over a
        poor mobile connection it is not, and every user's pixels share the server's memory.
        """;

    private const string ServerRegistration = """
        ## 2. Register the services

        One container, because the components only ever run on the server:

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddBitBmageServices(options =>
        {
            // Work small: every pixel crosses the circuit twice.
            options.MaxInteractiveDimension = 1200;
            options.Limits = ImageLimits.Default with { MaxPixels = 30_000_000 };
            options.MaxSourceBytes = 20 * 1024 * 1024;
        });

        var app = builder.Build();

        app.UseAntiforgery();
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        `AddBitBmageServices`, not `AddBitBmageHeadlessServices`: the components still talk to a browser, through
        the circuit. Services are scoped, so there is one set per circuit and no browser state is shared between
        users.
        """;

    private const string ServerWhen = """
        ## 4. Do browser work after the first render

        Interactive Server prerenders too. Call browser features from `OnAfterRenderAsync(firstRender)` or from an
        event handler, never from `OnInitialized(Async)`: during the prerender there is no circuit yet, so
        `IImageCapabilityProvider.GetAsync` answers with the managed set (`Environment = Prerendering`) and the JS
        calls throw `ImageCapabilityException`.

        There is no WebAssembly here, so `RunAOTCompilation` does not apply - the pipelines run on the server's JIT.
        """;

    private const string ServerCapabilities = """
        ## Writing for this mode

        1. Shrink before the pixels travel: `Loader.LoadAsync(file, new DecodeOptions { MaxSize = new Size(1600, 1600) })`
           lets the browser downscale while decoding, so the full-size pixels never cross the circuit.
        2. Keep `MaxInteractiveDimension` low (1200) - the editor's proxy is what crosses the circuit on every edit.
        3. Prefer the headless API in a controller or minimal API for bulk work (`GetBmageSetupGuide("headless")`),
           and use the browser only for what needs one.
        4. `IImageCapabilityProvider` reports `ImageEnvironment.BrowserServer`, so code can adapt.
        5. If images are central to the app, consider rendering the image pages with `InteractiveWebAssembly`
           instead - `GetBmageSetupGuide("wasm")`.
        """;

    // -- InteractiveAuto -------------------------------------------------------

    private const string AutoHeader = """
        # Bit.Bmage in a Blazor Web App - InteractiveAuto

        The same component runs in both modes: on the server over a circuit on the first visit, and on WebAssembly
        once the runtime has been downloaded and cached. Bmage works in both, so nothing breaks - but the first
        visit has the Interactive Server cost (decoded pixels crossing the circuit) and the later ones have the
        WebAssembly one (the interpreter, unless the client is compiled ahead of time).
        """;

    private const string AutoRegistration = """
        ## 2. Register the services - in BOTH containers

        Identical to the WebAssembly guide, and doubly required here: in Auto the server container is not only
        prerendering, it runs the components interactively until the runtime arrives.

        ```csharp
        // Server/Program.cs
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddAppServices();   // the shared method that calls AddBitBmageServices - see the wasm guide

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
        ```

        ```csharp
        // Client/Program.cs
        builder.Services.AddAppServices();
        ```

        Set `MaxInteractiveDimension` with the Server path in mind (1200 to 1600): it is the one that pays for it.
        """;

    private const string AutoGuidance = """
        ## Writing for this mode

        - Never decide anything from capabilities read during prerendering; read them in `OnAfterRenderAsync`, and
          expect `Environment` to be `BrowserServer` on a first visit and `BrowserWebAssembly` afterwards.
        - Use `DecodeOptions.MaxSize` when loading, so a first visit does not stream full-size pixels to the server.
        - Use `EncodeWithFallbackAsync` for WebP/AVIF: the browser does the encoding in both modes, and not every
          browser can write them.
        """;

    // -- Standalone WebAssembly ------------------------------------------------

    private const string StandaloneHeader = """
        # Bit.Bmage in a standalone Blazor WebAssembly app

        The simplest setup: one project, one DI container, no prerendering, and every feature available.
        """;

    private const string StandaloneRegistration = """
        ## 2. Register the services

        ```csharp
        using Bit.Bmage;
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddBitBmageServices(options =>
        {
            options.Limits = ImageLimits.Default with { MaxPixels = 40_000_000 };
        });

        await builder.Build().RunAsync();
        ```

        There is no second container and no prerender pass, so the registration mistake that catches out Blazor
        Web Apps cannot happen here. `ImageProcessor` is still not registered directly - inject
        `ImageProcessorFactory` and `await Factory.GetAsync()` for the low-level API.
        """;

    private const string StandaloneCapabilities = """
        ## What works in this mode

        Everything: the browser's codecs (WebP and usually AVIF decode, WebP encode on most browsers), the
        clipboard, the camera, downloads and the editor. Probe rather than assume - `IImageCapabilityProvider`
        encodes a real pixel in each format, because browsers asked for a format they cannot write silently
        hand back a PNG.
        """;

    // -- Headless --------------------------------------------------------------

    private const string HeadlessHeader = """
        # Bit.Bmage without a browser - ASP.NET Core, console, worker

        The same engine with no JavaScript at all. Only the managed codecs exist: PNG, JPEG, BMP and GIF (first
        frame) for decoding; PNG, JPEG and BMP for encoding. **WebP and AVIF are browser codecs**: asking for them
        here throws `ImageCapabilityException` ("No encoder is registered for WebP in this environment").
        """;

    private const string HeadlessInstall = """
        ## 1. Install

        ```bash
        dotnet add package Bit.Bmage        # for AddBitBmageHeadlessServices
        # or, with no ASP.NET Core dependency at all:
        dotnet add package Bit.Bmage.Core   # then construct ImageProcessor yourself
        ```
        """;

    private const string HeadlessRegistration = """
        ## 2. Register the services

        ```csharp
        using Bit.Bmage;   // AddBitBmageHeadlessServices lives here

        builder.Services.AddBitBmageHeadlessServices(options =>
        {
            options.Limits = ImageLimits.Default with { MaxPixels = 30_000_000 };
        });
        ```

        This registers `ImageProcessor` directly (the managed codecs need no probing), a managed
        `IImageCapabilityProvider` and a `FontTextRasterizer` - and none of the browser services: no
        `ImageLoader`, no `ImageExporter`, no `ImageProcessorFactory`, and so no `BmageEditor`.
        """;

    private const string HeadlessEndpoint = """
        ## 3. Process an upload

        ```csharp
        using Bit.Bmage.Codecs;
        using Bit.Bmage.Pipeline;

        app.MapPost("/thumbnail", async (IFormFile file, ImageProcessor processor) =>
        {
            if (file.Length > 20 * 1024 * 1024) return Results.BadRequest("Too large.");

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer);
            var bytes = buffer.ToArray();

            // Reject before decoding anything: Identify reads the header only.
            var info = processor.Identify(bytes);
            if (info is null) return Results.BadRequest("Not a supported image.");
            if ((long)info.Width * info.Height > 30_000_000) return Results.BadRequest("Too large.");

            var thumbnail = await processor.ProcessAsync(
                bytes,
                ImagePipeline.Create().AutoOrient().MaxSize(400, 400),
                new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 });

            return Results.File(thumbnail.Data, thumbnail.MimeType);
        }).DisableAntiforgery();
        ```

        An upload is attacker-chosen input to your decoder. Keep the limits tight, check `Identify` before decoding,
        run the work with a timeout, and treat `ImageDecodeException` as an expected outcome.
        """;

    private const string HeadlessConsole = """
        ## A console app, with no DI at all

        ```csharp
        using Bit.Bmage;
        using Bit.Bmage.Codecs;
        using Bit.Bmage.Pipeline;

        var processor = new ImageProcessor();   // the managed codecs

        var result = await processor.ProcessBatchAsync(
            Directory.GetFiles("photos", "*.jpg").Select(path => new BatchItem(File.ReadAllBytes(path), Path.GetFileName(path))),
            ImagePipeline.Create().AutoOrient().MaxSize(1600, 1600).Sharpen(0.3f),
            new BatchOptions
            {
                MaxConcurrency = Environment.ProcessorCount / 2,
                ExportOptions = new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.82 },
            });

        foreach (var item in result.Succeeded) File.WriteAllBytes(Path.Combine("out", item.FileName!), item.Output!.Data);
        foreach (var item in result.Failed) Console.Error.WriteLine($"{item.FileName}: {item.Error!.Message}");
        ```

        Each worker holds a decoded image, so size `MaxConcurrency` against memory, not cores alone.
        """;

    private const string HeadlessCapabilities = """
        ## What works here

        | Feature | Status |
        |---|---|
        | PNG, JPEG, BMP, GIF (first frame) decode | Managed |
        | PNG, JPEG, BMP encode | Managed |
        | WebP, AVIF | **Not available** - browser codecs only; throws `ImageCapabilityException` |
        | SVG | Refused everywhere with `ImageDecodeException` (untrusted SVG can run scripts) |
        | Pipelines, `ImageCanvas` drawing, annotations, `DocumentRenderer`, metadata, analysis | Full |
        | Text | `FontTextRasterizer` with a registered TrueType font (`glyf` outlines; CFF fonts throw) |

        `RunBmagePipeline` runs exactly this engine with these codecs, so its byte counts are what this host would
        produce.
        """;
}
