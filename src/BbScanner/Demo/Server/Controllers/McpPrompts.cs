using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.BbScanner.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a barcode scanner for: add it to an
/// app, build a scanning feature with it, and work out why a scanner does not read.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a scanner that silently reports NotSupported, a camera
/// that never opens on the deployed site, or a misread that reaches the database, rather than a
/// compiler error - because the failure mode of an agent with a handful of tools is not ignorance, it
/// is calling them in a sequence that skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-bbscanner-to-app", Title = "Add Bit.BbScanner to a Blazor app")]
    [Description("Walks through adding Bit.BbScanner to an existing Blazor app, in the right order for its render mode.")]
    public static string AddBbScannerToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the mode is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "wasm", "standalone-wasm", "auto", "server", "hybrid")]
        [Description("The app's Blazor render mode: wasm, standalone-wasm, auto, server or hybrid. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Add Bit.BbScanner to this Blazor app. Its render mode is: {renderMode}.

            Work in this order:

            1. If the render mode is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, and for a MAUI/WPF/Windows Forms BlazorWebView host, then continue with what you find.
            2. Call `GetBbScannerSetupGuide` with that render mode and follow it. Camera scanning runs on WebAssembly
               only: in a Blazor Web App the package and the pages with a scanner belong in the .Client project and
               are rendered with InteractiveWebAssembly. Under Server or Hybrid rendering the component reports
               NotSupported - say so rather than wiring it up anyway.
            3. Do NOT register services or link a stylesheet: Bit.BbScanner has no services, and its Blazor initializer
               injects `_content/Bit.BbScanner/bit-bbscanner.css` itself. Inventing an `AddBbScanner...` call is the most
               common mistake.
            4. Add `@using Bit.BbScanner` to `_Imports.razor`.
            5. Set `Formats` to the symbologies the app needs (call `GetBbScannerFormats` for the names) and handle
               both `OnScan` and `OnError`.
            6. Remember the camera needs a secure context: HTTPS, or localhost while developing.
            7. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which project the scanner pages live in and why.
            """;
    }

    [McpServerPrompt(Name = "implement-bbscanner-feature", Title = "Implement a scanning feature")]
    [Description("Implements a barcode scanning feature with Bit.BbScanner - live camera scanning, a symbology, still images or uploads, server-side decoding, camera controls, scan regions and overlays, theming - using its real API rather than a guessed one.")]
    public static string ImplementBbScannerFeature(
        [Description("What the scanning feature should do, in your own words - e.g. 'scan a QR code with the phone's rear camera' or 'scan a barcode from an uploaded photo'.")] string feature)
    {
        return $"""
            Implement this with Bit.BbScanner: {feature}

            Work in this order:

            1. Call `SearchBbScanner` with the request above. It searches the guide, every public member, the
               symbologies, the setup guides, the stylesheet and the demo's sources at once, and every hit tells you
               the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code.
            3. Call `GetBbScannerApi` for every BbScanner type you are about to use, and match the member names, types
               and defaults exactly. There is one package and one namespace, `Bit.BbScanner`; the component is
               `BbScanner` and every other public type is prefixed `Bb`. Do not borrow names from another scanner library.
            4. Call `GetBbScannerFormats` for the symbologies involved: narrow `Formats` to them, and set the options
               that guard against misreads where they apply (`ItfLengths` for ITF, `Code39CheckDigit` for Code 39).
            5. Prefer the library's own mechanism over hand-rolling one: `EnableFileScanning`/`OpenFilePickerAsync` or
               `DecodeImage` over reading files yourself, `BbBarcodeDecoder` with `BbScannerOptions.ForStillImages()`
               for images on the server, `DuplicateSuppressionWindow` over filtering repeats yourself, `Region` with
               `BbScanRegion.CenteredSquare` over cropping, `SetTorchAsync`/`SetZoomAsync`/`GetCamerasAsync` over
               touching the media stream, `Overlay`/`MessageTemplate`/`ControlsContent` and the `--bit-bbscanner-*`
               custom properties over restyling with selectors.
            6. Check the `BbBarcodeResult` the code will receive with `InspectBbScannerSymbol` - its text, raw bytes,
               corners and metadata - so the code reads the members that are actually filled in.
            7. Write the code, then say which tool call each non-obvious choice came from.

            If the demo already contains a working example of this, fetch it with `GetBbScannerSourceFile` and follow
            its shape rather than inventing a new one.
            """;
    }

    [McpServerPrompt(Name = "debug-bbscanner", Title = "Debug a scanner")]
    [Description("Diagnoses a Bit.BbScanner problem - a scanner that reports NotSupported, a camera that never opens, a barcode that is never read, repeated or wrong results, a slow page, a camera left running.")]
    public static string DebugBbScanner(
        [Description("What goes wrong, with the page, the symbology or the device involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.BbScanner problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBbScannerSetupGuide` for this render mode first: the scanner on a page
               rendered with InteractiveWebAssembly (or in a standalone WebAssembly app), served over HTTPS or from
               localhost, with no invented service registration. Read `OnError`: the `BbScannerException.Kind` says
               whether the cause is NotSupported, PermissionDenied, NoCamera, CameraUnavailable or ConstraintsUnsatisfied.
            2. Call `SearchBbScanner` with the symptom, and read the guide section it points to - "Hosting models"
               covers NotSupported, "Errors and permissions" the camera failures, "What it reads, and what it does not"
               the symbols that are never read, "Performance" a slow page, "PDF417 status" PDF417.
            3. For a symbol that is not read or is read wrongly, reproduce it with `InspectBbScannerSymbol` - the same
               payload, module size, rotation and blur - and compare the live and the still image attempts; check the
               symbology's options with `GetBbScannerFormats`.
            4. Confirm the API you are relying on with `GetBbScannerApi` before concluding it is a bug - check the actual
               default value of the parameter or option involved.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}
