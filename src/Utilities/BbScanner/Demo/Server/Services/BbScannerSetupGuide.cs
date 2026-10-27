using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BbScanner.Demo.Server.Services;

/// <summary>
/// Assembles the wiring for one Blazor render mode: the checklist, the guide's own section on that
/// hosting model, and - where the demo is that render mode - the demo's real hosting files.
/// <para>
/// BbScanner needs less wiring than most libraries - no services, and a stylesheet that injects itself -
/// which is exactly why an agent gets it wrong: it registers services that do not exist, or links the
/// stylesheet a second time. What does differ per render mode is where .NET runs, and so which project
/// the package belongs in, how camera frames reach the decoder and what the host has to allow - the
/// camera permission of each BlazorWebView platform, say; the guide's hosting section, quoted verbatim,
/// says so in the library's own words.
/// </para>
/// </summary>
public static partial class BbScannerSetupGuide
{
    public static readonly string[] RenderModes = ["wasm", "standalone-wasm", "auto", "server", "hybrid"];

    /// <summary>The demo files that make up its wiring - the demo is a Blazor Web App using InteractiveWebAssembly.</summary>
    private static readonly string[] _hostingFiles =
    [
        "Demo/Client/Extensions/IServiceCollectionExtensions.cs",
        "Demo/Server/Program.cs",
        "Demo/Client/Program.cs",
        "Demo/Server/Components/App.razor",
        "Demo/Client/Routes.razor",
        "Demo/Client/_Imports.razor",
    ];

    private sealed record Mode(string Title, string Summary, string[] GuideSections, string? Files, bool IncludeDemo);

    /// <summary>The wiring for one render mode as Markdown, or null when the mode is not one of <see cref="RenderModes"/>.</summary>
    public static string? Get(string? renderMode) => Describe(renderMode) is { } mode ? Compose(mode) : null;

    /// <summary>What sets one render mode apart, in a paragraph - the line the search index ranks a setup guide by.</summary>
    public static string Summary(string renderMode) => Describe(renderMode) is { } mode ? $"{mode.Title}. {mode.Summary.ReplaceLineEndings(" ")}" : string.Empty;

    private static Mode? Describe(string? renderMode)
    {
        var mode = (renderMode ?? string.Empty).Trim().ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal);

        mode = mode switch
        {
            "interactiveserver" or "blazor-server" => "server",
            "webassembly" or "interactivewebassembly" or "blazor-wasm" => "wasm",
            "interactiveauto" => "auto",
            "standalone" or "standalone-webassembly" or "standalone-wasm-app" => "standalone-wasm",
            "maui" or "blazor-hybrid" or "wpf" or "winforms" => "hybrid",
            _ => mode
        };

        return mode switch
        {
            "wasm" => new Mode(
                "Blazor Web App - InteractiveWebAssembly",
                """
                The fastest of the render modes: the component, the frame loop and the decoder all run in the browser,
                and every camera frame crosses from JavaScript into managed memory exactly once, through a `[JSImport]`
                memory view. The component must live in the .Client project so its assembly is downloaded to the browser;
                the server prerenders the same page, where the scanner renders its markup and starts the camera once
                WebAssembly takes over. Reference `Bit.BbScanner` from the .Client project (the server gets it
                transitively). This is exactly how the demo is built, so the files below are its own.
                """,
                ["Blazor Web App", "Blazor WebAssembly"],
                null,
                IncludeDemo: true),

            "standalone-wasm" => new Mode(
                "standalone Blazor WebAssembly",
                """
                One project and no prerendering: the scanner runs in the browser from the first render and opens the
                camera as soon as it is on screen (unless `AutoStart` is false).
                """,
                ["Blazor WebAssembly"],
                StandaloneFiles,
                IncludeDemo: false),

            "auto" => new Mode(
                "Blazor Web App - InteractiveAuto",
                """
                The first visit runs on a circuit while the WebAssembly runtime downloads, later visits in the browser,
                and the scanner works on both: on the circuit its frames are decoded on the server, as under
                InteractiveServer, and once the runtime is cached the same page decodes them in the browser, with
                nothing in the page changing. Put the package and the scanning pages in the .Client project, which the
                server project references; no page needs `@rendermode InteractiveWebAssembly` of its own, though one
                that has it keeps every frame in the browser. The server registers `AddInteractiveServerComponents()`
                and `AddInteractiveWebAssemblyComponents()`, and maps both `AddInteractiveServerRenderMode()` and
                `AddInteractiveWebAssemblyRenderMode()`; the rest of the wiring is the WebAssembly setup below - the
                demo uses InteractiveWebAssembly throughout.
                """,
                ["Blazor Web App", "Blazor Server"],
                null,
                IncludeDemo: true),

            "server" => new Mode(
                "Blazor Web App - InteractiveServer",
                """
                The component, its frame loop and the decoder run on the server, and the page does the part only a page
                can: it opens the camera, cuts each frame down to the luminance - one byte a pixel - of the searched
                `Region`, and the component reads that back in byte array slices of 30 KB, several at once. Each slice
                fits in one message, so SignalR's 32 KB `MaximumReceiveMessageSize` can stay as it is. A 640x360 frame
                searched through `BbScanRegion.CenteredSquare(0.7)` is about 63 KB, against about 900 KB of RGBA. Each
                tick costs one round trip to poll plus the slices of a frame, and every active scanner costs server CPU
                to decode. Reference `Bit.BbScanner` from the server project, where the pages render.
                """,
                ["Blazor Server", "Using the decoder without Blazor"],
                ServerFiles,
                IncludeDemo: false),

            "hybrid" => new Mode(
                "Blazor Hybrid (MAUI, WPF, Windows Forms)",
                """
                .NET runs natively in the app's process and the BlazorWebView is the page: the WebView opens the camera,
                cuts each frame down to the luminance of the searched `Region` and sends it to the component, whose
                frame loop and decoder run in-process at native speed - no network is involved. Nothing changes in the
                Razor; what each platform adds is permission to use the camera, below. A BlazorWebView is a secure
                context already, and a WebView that exposes no camera API at all (`getUserMedia` in WKWebView needs
                iOS 14.3 or later) makes the scanner report `NotSupported`.
                """,
                ["Blazor Hybrid"],
                HybridFiles,
                IncludeDemo: false),

            _ => null
        };
    }

    private static string Compose(Mode mode)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# Setting BbScanner up - {mode.Title}").AppendLine();
        builder.AppendLine(mode.Summary).AppendLine();
        builder.AppendLine(Checklist).AppendLine();

        if (mode.Files is not null) builder.AppendLine(mode.Files).AppendLine();

        if (mode.IncludeDemo)
        {
            builder.AppendLine("## The files, from this demo").AppendLine();
            builder.AppendLine("Everything below compiles as-is; rename the projects and namespaces to your own.").AppendLine();

            foreach (var path in _hostingFiles)
            {
                var content = BbScannerSourceCatalog.GetSourceFile(path);
                if (content is null) continue;

                AppendFile(builder, path, content);
            }
        }

        // The guide's own words on the hosting model, so the caveats are the library's rather than a
        // paraphrase of them that can drift.
        builder.AppendLine("## From the guide").AppendLine();

        foreach (var heading in mode.GuideSections)
        {
            var section = BbScannerSourceCatalog.GetGuideSection(heading);
            if (section is null) continue;

            builder.AppendLine(Demote(section)).AppendLine();
        }

        builder.AppendLine(Next);

        return builder.ToString();
    }

    private const string Checklist = """
        ## Checklist

        1. `dotnet add package Bit.BbScanner` - one package, everything under the `Bit.BbScanner` namespace. In a Blazor
           Web App, add it to the project the scanning pages render in: the server project for InteractiveServer, the
           .Client project for InteractiveWebAssembly or InteractiveAuto (the server project references it).
        2. Add `@using Bit.BbScanner` to `_Imports.razor`.
        3. There are NO services to register - do not look for an `AddBbScanner...` method, there is none.
        4. There is no stylesheet to add: `_content/Bit.BbScanner/bit-bbscanner.css` is injected by the package's Blazor
           initializer. Linking it yourself also works (the injection then stands down); never link it twice under
           another path.
        5. Render the scanner in an interactive render mode - WebAssembly, Server, Auto and a BlazorWebView all open the
           camera. On a statically rendered page `<BbScanner>` is inert markup; during prerendering it is inert until
           the page turns interactive, and then starts by itself.
        6. Serve the app over HTTPS or from `localhost`: browsers only expose the camera to a secure context, and the
           scanner reports `NotSupported` with that reason when the page is not one. A BlazorWebView is one already.
        7. Narrow `Formats` to the symbologies the app needs, handle `OnScan`, and handle `OnError` - a denied
           permission arrives there as `BbScannerErrorKind.PermissionDenied`, a missing camera as `NoCamera`.
        8. On WebAssembly, publish the client compiled ahead of time: `<RunAOTCompilation>true</RunAOTCompilation>` in
           the project that runs in the browser (the .Client project of a Blazor Web App), for Release, with the
           `wasm-tools` workload installed. On Blazor's default IL interpreter the decoder is about a hundred times
           slower - a camera frame takes around 100 ms instead of 1 to 2, and blocks the page while it does - so an app
           that scans with the camera there, above all on phones, should not ship without it. Under Server and Hybrid
           the decoder runs on the server's or the app's own .NET runtime, where this does not apply. See the guide's
           "Performance" section.
        """;

    private const string ServerFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Nothing to register for BbScanner.

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseAntiforgery();

        // .NET 9 and later; on .NET 8, app.UseStaticFiles() before UseAntiforgery.
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
        ```

        ### `Components/Pages/Scan.razor`

        ```razor
        @page "/scan"
        @rendermode InteractiveServer

        <BbScanner Formats="BbBarcodeFormat.QrCode | BbBarcodeFormat.Ean13"
                   Region="BbScanRegion.CenteredSquare(0.7)"
                   OnScan="HandleScan" />

        @code {
            private void HandleScan(BbBarcodeResult result) => Console.WriteLine($"{result.Format}: {result.Text}");
        }
        ```

        The `Region` is what keeps the stream small: only the pixels inside it cross the network. On a slow link lower
        `TargetFrameRate` (to 8, say) and `MaxProcessingSize` (to 480, say) as well. The frame rate limits itself
        anyway - a tick waits for the previous frame - so a slow link gets fewer frames, never a queue of them.

        ### When the circuit drops

        The page keeps a watchdog on the camera it opened: if the server stops polling for 20 seconds - the circuit
        dropped for good, or the server is gone - the page closes the camera. A circuit that reconnects within that time
        carries on; if the watchdog closed the camera meanwhile, the scanner reopens it as soon as the page answers
        again, as an interruption rather than an error. A frame call with no answer after 10 seconds is given up and
        tried again on the next tick.

        ### Still images

        `ScanImageAsync`, the built-in *Scan image* button and dropping a file on the scanner work here too, and the
        page always decodes the image, with the browser's image decoders and EXIF orientation. A stream the server
        holds - an `InputFile` upload, say - is sent to the browser and comes back as luminance, so an upload crosses
        the network twice; the built-in picker sends the file once. That button opens the picker in the page's own
        click handler, while `OpenFilePickerAsync()` called from your own button reaches the page a round trip after
        the click, which a strict browser may not treat as a user gesture.

        ### Pixels the server already has

        `BbBarcodeDecoder` has no dependency on Blazor or JavaScript, so an image your own code has turned into pixels
        decodes in-process, with no page involved:

        ```csharp
        // rgba: the image as packed RGBA pixels, produced by whichever image library the app already uses.
        var options = BbScannerOptions.ForStillImages();
        options.Formats = BbBarcodeFormat.QrCode;

        using var decoder = new BbBarcodeDecoder(options);
        var result = decoder.Decode(rgba, width, height, BbPixelFormat.Rgba32);
        ```
        """;

    private const string StandaloneFiles = """
        ## The files

        ### `Program.cs`

        ```csharp
        using Microsoft.AspNetCore.Components.Web;
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Nothing to register for BbScanner.

        await builder.Build().RunAsync();
        ```

        ### `_Imports.razor`

        ```razor
        @using Bit.BbScanner
        ```

        ### `Pages/Scan.razor`

        ```razor
        @page "/scan"

        <BbScanner Formats="BbBarcodeFormat.QrCode" OnScan="r => _last = r" OnError="e => _error = e" />

        <p>@(_last?.Text ?? _error?.Message ?? "Point the camera at a QR code.")</p>

        @code {
            private BbBarcodeResult? _last;
            private BbScannerException? _error;
        }
        ```

        There is no script tag or stylesheet link to add to `wwwroot/index.html`: the package's JavaScript is a module
        it imports itself, and its initializer injects the stylesheet.
        """;

    private const string HybridFiles = """
        ## The files

        `@using Bit.BbScanner` in `_Imports.razor` and `<BbScanner>` on a page, as anywhere else; there is nothing to
        register beside `AddMauiBlazorWebView()` (or its WPF or Windows Forms counterpart). The rest is the camera
        permission, platform by platform.

        ### Android - `Platforms/Android/AndroidManifest.xml`

        ```xml
        <uses-permission android:name="android.permission.CAMERA" />
        ```

        ### Android - `Platforms/Android/CameraWebChromeClient.cs`

        Android hands every `getUserMedia` call to the app as `WebChromeClient.OnPermissionRequest`, and BlazorWebView's
        own client refuses it, which the scanner reports as `PermissionDenied`. This client grants the page the camera
        once the user has granted the app the CAMERA runtime permission, and wraps BlazorWebView's client rather than
        replacing it, so the file chooser every `<input type="file">` goes through keeps working.

        ```csharp
        using Android.Webkit;
        using AndroidWebView = Android.Webkit.WebView;
        using Message = Android.OS.Message;

        public class CameraWebChromeClient(WebChromeClient? blazorWebChromeClient) : WebChromeClient
        {
            public override void OnPermissionRequest(PermissionRequest? request)
            {
                if (request is null) return;

                var resources = request.GetResources() ?? [];
                if (resources is not [PermissionRequest.ResourceVideoCapture])
                {
                    request.Deny();
                    return;
                }

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.Camera>();
                    }

                    if (status == PermissionStatus.Granted) request.Grant(resources);
                    else request.Deny();
                });
            }

            public override bool OnShowFileChooser(AndroidWebView? view, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
                => blazorWebChromeClient?.OnShowFileChooser(view, filePathCallback, fileChooserParams)
                    ?? base.OnShowFileChooser(view, filePathCallback, fileChooserParams);

            public override bool OnCreateWindow(AndroidWebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
                => blazorWebChromeClient?.OnCreateWindow(view, isDialog, isUserGesture, resultMsg)
                    ?? base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
        }
        ```

        ### Android - installing it in `MainPage.xaml.cs`

        With `BlazorWebViewInitialized="OnBlazorWebViewInitialized"` on the `<BlazorWebView>` in `MainPage.xaml`, and
        the body below compiled for Android only (inside `#if ANDROID` in a MAUI project):

        ```csharp
        private void OnBlazorWebViewInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
        {
            // BlazorWebView's own client can only be read back from Android 8 (API 26).
            if (OperatingSystem.IsAndroidVersionAtLeast(26) && e.WebView.WebChromeClient is not CameraWebChromeClient)
            {
                e.WebView.SetWebChromeClient(new CameraWebChromeClient(e.WebView.WebChromeClient));
            }
        }
        ```

        ### iOS and Mac Catalyst - `Info.plist`

        ```xml
        <key>NSCameraUsageDescription</key>
        <string>Point the camera at a barcode to scan it.</string>
        ```

        A Mac Catalyst app in the App Sandbox also needs the camera entitlement, in `Entitlements.plist`:

        ```xml
        <key>com.apple.security.device.camera</key>
        <true/>
        ```

        MAUI's BlazorWebView already sets `AllowsInlineMediaPlayback`. `getUserMedia` in WKWebView needs iOS 14.3 or
        later.

        ### Windows - `Platforms/Windows/Package.appxmanifest`

        WebView2 asks the user with a permission prompt of its own, in WPF and Windows Forms as in MAUI. A packaged app
        also declares the webcam:

        ```xml
        <Capabilities>
            <DeviceCapability Name="webcam" />
        </Capabilities>
        ```

        All of it, working, is the MAUI sample in `src/Utilities/BbScanner/Samples/Maui` of the bitplatform repository.
        """;

    private const string Next = """
        The scanning itself is markup and plain objects - call `GetBbScannerGuideSection(heading: "Using the component")`
        for the parameters, events and methods, `GetBbScannerFormats()` for the symbologies and the options that steer
        them, `InspectBbScannerSymbol()` to see a real `BbBarcodeResult`, or `GetBbScannerSourceFile()` for the index of
        the demo's working pages.
        """;

    /// <summary>
    /// Pushes a quoted guide section two heading levels down, so it nests under "From the guide"
    /// instead of competing with the headings of this document.
    /// </summary>
    private static string Demote(string section)
    {
        var lines = section.Split('\n');
        var fenced = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();

            if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
            {
                fenced = fenced is false;
                continue;
            }

            if (fenced is false && lines[i].StartsWith('#')) lines[i] = $"#{lines[i]}";
        }

        return string.Join('\n', lines);
    }

    private static void AppendFile(StringBuilder builder, string path, string content)
    {
        content = content.TrimEnd();
        var fence = Fence(content);

        builder.AppendLine($"### `{path}`").AppendLine()
               .AppendLine($"{fence}{Language(path)}")
               .AppendLine(content)
               .AppendLine(fence)
               .AppendLine();
    }

    /// <summary>
    /// The fence for a file's code block. It has to outrun the longest run of backticks in the file
    /// itself - a file that shows Markdown would otherwise end the block in the middle of its own
    /// content, and the rest of the file would render as prose.
    /// </summary>
    private static string Fence(string content)
    {
        var longest = BacktickRunRegex().Matches(content).Select(match => match.Length).DefaultIfEmpty(0).Max();

        return new string('`', Math.Max(3, longest + 1));
    }

    private static string Language(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".razor" => "razor",
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".css" => "css",
        ".js" => "javascript",
        _ => string.Empty
    };

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
