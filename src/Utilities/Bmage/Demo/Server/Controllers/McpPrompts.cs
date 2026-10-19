using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Bit.Bmage.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the things people actually ask an image library for: build an image
/// feature, add the library to an app, choose output settings, work out why something fails, and
/// make sure nothing private leaves the device.
/// <para>
/// Each prompt spends its words on the order to call the tools in, because the failure mode of an
/// agent holding a shelf of them is not ignorance - it is calling them in a sequence that skips the
/// check which would have caught the bug. For Bmage that check is nearly always one of two: the
/// code review, which catches what compiles and then breaks at prerender or leaks memory, and the
/// engine run, which replaces "quality 0.8 should be fine" with a byte count. So every workflow that
/// writes code ends by running it back through them rather than by declaring victory when it builds.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "build-with-bmage")]
    [Description("Builds an image feature with Bit.Bmage - an upload, a thumbnail, an avatar, an annotation tool, a server endpoint - using its real API, and verifies the result before finishing.")]
    public static string BuildWithBmage(
        [Description("What the feature should do, in your own words - e.g. 'let users upload an avatar, crop it to a circle and keep it under 100 KB'.")] string request,
        [Description("The app's hosting model: wasm, server, auto, standalone-wasm or headless. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown")
    {
        return $"""
            Build this with Bit.Bmage: {request}

            The app's hosting model is: {renderMode}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project before writing anything: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, a separate .Client
               project, or no Blazor at all (an API, a console app). It decides which registration to use and whether
               WebP/AVIF exist at all.
            2. Call `GetBmageRecipes` and look for a recipe that already covers the request. If one does, fetch it with
               `GetBmageRecipe` and follow its shape - including its Notes, which carry the caveat the code cannot show.
               If none fits, call `SearchBmage` with the request as written.
            3. Call `GetBmageSetupGuide` with the hosting model if the app does not reference Bit.Bmage yet.
            4. Call `GetBmageApiDetails` for every Bmage type you are about to use, and match its namespace, package,
               parameter names and defaults exactly. Do not carry over APIs from ImageSharp, SkiaSharp or a JavaScript
               library - several names are close enough to look right.
            5. Choose the numbers with evidence: call `RunBmagePipeline` with the pipeline you intend, the formats you
               may export and two or three qualities (and maxFileSize if there is a byte budget), on the source kind
               that matches the content. Pick the dimensions, format and quality from the measured bytes.
            6. Write the code. Browser work goes in event handlers or OnAfterRenderAsync, every ImageBuffer you are
               handed is disposed, WebP/AVIF go through EncodeWithFallbackAsync, and user files keep the default
               MetadataPolicy.Remove unless there is a reason.
            7. Call `ReviewBmageCode` on everything you wrote - components, Program.cs, the .csproj, App.razor - and fix
               every Error and Warning it reports.
            8. Build the app and fix what the compiler says.

            Show me the final code, state the format, quality and dimensions you chose with the bytes RunBmagePipeline
            measured for them, and say plainly which hosting models the code works in.
            """;
    }

    [McpServerPrompt(Name = "add-bmage-to-app")]
    [Description("Walks through adding Bit.Bmage to an existing app, in the right order for its hosting model - both DI containers of a Web App, the editor's stylesheet, AOT for WebAssembly, or the headless registration for a server.")]
    public static string AddBmageToApp(
        [Description("The app's hosting model: wasm, server, auto, standalone-wasm or headless. Pass 'unknown' to have it determined from the project first.")] string renderMode = "unknown",
        [Description("Whether the app needs the BmageEditor component: 'yes', 'no' or 'unknown'.")] string needsEditor = "unknown")
    {
        return $"""
            Add Bit.Bmage to this app. Its hosting model is: {renderMode}. It needs the editor component: {needsEditor}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, a separate .Client
               project, or no Blazor at all. Continue with what you find.
            2. Call `GetBmageSetupGuide` with that hosting model and follow it: the package (Bit.Bmage.Editor when the
               editor is needed, Bit.Bmage otherwise, in the project that runs in the browser), the registration, and the
               namespaces for _Imports.razor.
            3. Register in the right containers. A Blazor Web App registers AddBitBmageServices in BOTH the server and the
               client project, through one shared extension method - the prerender pass needs it on the server. A server,
               console app or worker registers AddBitBmageHeadlessServices instead.
            4. If the editor is used, link `_content/Bit.Bmage.Editor/bit-bmage-editor.css` from App.razor or index.html,
               and do not add a script tag for bit-bmage.js.
            5. If any project runs on WebAssembly, enable RunAOTCompilation for publishing in that project.
            6. Add the `thumbnail` recipe from `GetBmageRecipe` (or `headless-upload-endpoint` for a server) to prove the
               wiring end to end.
            7. Call `ReviewBmageCode` on every file you changed - both Program.cs files, the .csproj, App.razor and the
               sample page - and fix what it reports. Then build.

            Show me the diff for each file you change, and say explicitly which DI containers you registered the services
            in and why.
            """;
    }

    [McpServerPrompt(Name = "tune-bmage-output")]
    [Description("Chooses the dimensions, format, quality and byte budget for a Bit.Bmage export by measuring the alternatives on the real engine instead of guessing at numbers.")]
    public static string TuneBmageOutput(
        [Description("The pipeline as it is written today, e.g. 'ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920)'.")] string pipeline,
        [Description("What the output has to achieve - e.g. 'under 150 KB', 'smallest file that still looks good', 'fastest on a phone'.")] string goal,
        [Description("What the images are: 'photo', 'screenshot' or 'graphic' (transparent).")] string content = "photo")
    {
        return $"""
            Tune the output of this Bit.Bmage pipeline: {pipeline}

            The goal is: {goal}. The images are: {content}.

            Work in this order:

            1. Call `RunBmagePipeline` with the pipeline as it is, source '{content}', formats 'jpeg, png, webp' and
               qualities '0.6, 0.75, 0.85, 0.95'. Read the output size, the bytes per format and quality, the optimiser
               notes and the timings.
            2. If the goal is a byte budget, call `RunBmagePipeline` again with maxFileSize set to it and read the quality
               and dimensions the search settles on - or the reason it cannot be met.
            3. Propose two or three changes that move the number the goal is about: a smaller MaxSize, a different
               format (PNG for flat graphics and transparency, JPEG or WebP for photographs), a lower quality, or a
               reordered pipeline (cap the dimensions before the adjustments; group adjustments so they fuse). Measure
               each with `RunBmagePipeline`.
            4. WebP cannot be measured on the server. If WebP is the choice, plan it as
               `EncodeWithFallbackAsync([WebP, Jpeg])` and use the JPEG numbers as the upper bound.
            5. Check the export options you settle on with `GetBmageApiDetails` (typeName 'ImageExportOptions'), then
               call `ReviewBmageCode` on the final code - an unhandled ImageEncodeException from a budget, or WebP on a
               server, are the usual findings.

            Give me the replacement pipeline and ImageExportOptions, the before-and-after bytes and timings, and one
            sentence on what a user would notice.
            """;
    }

    [McpServerPrompt(Name = "debug-bmage-problem")]
    [Description("Diagnoses a Bit.Bmage problem - an exception at prerender, a missing service, a sideways photo, an unstyled or invisible editor, a crashing tab, a slow page, a format that will not export.")]
    public static string DebugBmageProblem(
        [Description("What goes wrong, with the exception message and the code involved if you have them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Bmage problem: {symptom}

            Most Bmage failures are mistakes that compile cleanly, so work through them in order of how often they are
            the answer:

            1. Call `ReviewBmageCode` on the component involved, and on Program.cs (both of them in a Blazor Web App),
               the .csproj and App.razor/index.html. Its rules check exactly the silent causes: browser calls in
               OnInitialized during prerendering, the wrong registration for the host, @inject ImageProcessor in a
               browser app, the missing stylesheet, undisposed or double-disposed buffers, WebP without a fallback.
            2. Call `GetBmageDocument` with name 'troubleshooting' and find the symptom: it lists the exact exception
               messages ("Could not load the Bmage browser module", "This browser cannot encode Avif", "Cannot reach N
               bytes") with their causes.
            3. If the problem depends on where the code runs - works locally, fails in production; works on the second
               visit - call `GetBmageSetupGuide` for the hosting model and compare the wiring line by line.
            4. If it is about output - size, format, quality, orientation - reproduce it with `RunBmagePipeline`; if it
               is about metadata, with `CompareBmageMetadataPolicies`.
            5. Confirm the API you rely on with `GetBmageApiDetails` before concluding it is a bug - check the default
               value of the option involved (DecodeOptions.AutoOrient, ImageExportOptions.Metadata, Background).
            6. If it is slow, check RunAOTCompilation in the WebAssembly project and measure the pipeline with
               `RunBmagePipeline`: the optimiser notes say what was fused and what was not.

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }

    [McpServerPrompt(Name = "audit-bmage-privacy")]
    [Description("Audits how an app handles users' images with Bit.Bmage for what can leak - GPS and identity in metadata, redactions that can be reversed, originals sent to a server - and fixes it.")]
    public static string AuditBmagePrivacy(
        [Description("Where users' images go in this app - e.g. 'avatars are uploaded to our API and shown publicly', 'bug reports with screenshots go to a ticket system'.")] string flow)
    {
        return $"""
            Audit the privacy of this image flow built with Bit.Bmage: {flow}

            Work in this order:

            1. Find every export: ImageExportOptions, ImageExporter calls, BmageEditor's ExportOptions, ProcessAsync and
               ProcessBatchAsync on the server. Call `ReviewBmageCode` on each file - MetadataPolicy.Preserve,
               RedactionMode.Blur/Pixelate and SVG inputs are reported there.
            2. Call `CompareBmageMetadataPolicies` and use what it measured to decide the policy per export: Remove for
               anything published or uploaded (the default), StripSensitive only where camera details are a feature.
            3. Read `GetBmageDocument` with name 'metadata-and-privacy' and section 'A checklist for user uploads', and
               `GetBmageDocument` with name 'security', and check the flow against both.
            4. Check whether originals leave the device at all. If they do only to be shrunk, move that into the browser
               with the `upload-optimise` recipe from `GetBmageRecipe`, so the server never receives the full-size file.
            5. Where users are told nothing, add the warning from the `strip-location` recipe: tell them what their
               file contains before it is published.

            Report each leak you found with the file and line, the fix you made, and what remains a decision for me.
            """;
    }
}
