using Bit.Bmage.Demo.Server.Dtos;
using System.Text.RegularExpressions;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// Reviews Bmage code for the mistakes that compile cleanly and then fail at runtime, leak, or
/// quietly produce the wrong file.
/// <para>
/// Image code goes wrong in ways no compiler sees. A capability probe in <c>OnInitializedAsync</c>
/// compiles, runs during prerendering, and concludes that no browser can write WebP. An
/// <c>ImageBuffer</c> nobody disposes is 48 MB of pooled memory that stays rented until the
/// collector gets round to it - on a phone, the tab dies first. <c>MetadataPolicy.Preserve</c>
/// publishes the user's home address in the GPS block. <c>@inject ImageProcessor</c> in a browser
/// app is a missing-service exception at the first prerender, because the browser registration
/// deliberately does not register it. Every rule below is one of those, and each one is grounded in
/// the library's own documented behaviour: the guide it comes from is named in <see cref="Catalog"/>.
/// </para>
/// <para>
/// The rules are deliberately textual. The input is a fragment an agent just wrote - often not a
/// whole file, never a compilable project - so each rule looks for one shape and stays quiet when it
/// cannot tell; a false positive teaches an agent to stop reading the review.
/// </para>
/// </summary>
public static partial class BmageCodeReview
{
    /// <summary>Every rule, with the mistake it catches and the guide that documents the behaviour behind it.</summary>
    public static readonly BmageReviewRuleDto[] Catalog =
    [
        new() { Id = "browser-call-during-prerender", Severity = "Error", Source = "getting-started",
                Catches = "Browser features called from OnInitialized/OnParametersSet, which also run during prerendering: the JS calls throw, and the capability probe answers with the managed set." },
        new() { Id = "image-buffer-not-disposed", Severity = "Warning", Source = "getting-started",
                Catches = "An ImageBuffer (or an ImageCanvas that owns one) from LoadAsync, ExecuteAsync, DecodeAsync, Render, Snapshot or Clone that is never disposed." },
        new() { Id = "borrowed-buffer-disposed", Severity = "Error", Source = "troubleshooting",
                Catches = "Disposing ImageEditSession.Preview or Original, which the session owns - an ObjectDisposedException on the next render." },
        new() { Id = "editor-owns-loaded-buffer", Severity = "Error", Source = "troubleshooting",
                Catches = "A buffer handed to BmageEditor.LoadImageAsync (which takes ownership by default) that is also disposed by the caller." },
        new() { Id = "editor-stylesheet-missing", Severity = "Warning", Source = "getting-started",
                Catches = "A page head with the editor in use but no _content/Bit.Bmage.Editor/bit-bmage-editor.css link: the editor renders unstyled." },
        new() { Id = "editor-stylesheet-wrong-path", Severity = "Error", Source = "getting-started",
                Catches = "A Bmage stylesheet link whose path is not _content/Bit.Bmage.Editor/bit-bmage-editor.css - a 404 and an unstyled editor." },
        new() { Id = "bridge-script-tag", Severity = "Warning", Source = "getting-started",
                Catches = "A <script> tag for bit-bmage.js: the library imports its ES module on demand, and as a classic script it is a syntax error." },
        new() { Id = "editor-without-height", Severity = "Suggestion", Source = "troubleshooting",
                Catches = "A BmageEditor with no height anywhere around it: it fills its container, and a container with no height collapses." },
        new() { Id = "processor-injected-in-browser", Severity = "Error", Source = "getting-started",
                Catches = "@inject ImageProcessor in a browser app: AddBitBmageServices registers ImageProcessorFactory, not ImageProcessor." },
        new() { Id = "browser-services-with-headless-registration", Severity = "Error", Source = "headless",
                Catches = "ImageLoader, ImageExporter, ImageProcessorFactory or the editor used where only AddBitBmageHeadlessServices is registered." },
        new() { Id = "headless-services-in-browser", Severity = "Error", Source = "getting-started",
                Catches = "AddBitBmageHeadlessServices in a WebAssembly host: no browser codecs, no ImageLoader, no ImageExporter." },
        new() { Id = "services-not-registered", Severity = "Warning", Source = "browser-support",
                Catches = "A Blazor host Program.cs that never registers Bmage - in a Web App the prerender pass needs it in the server container too." },
        new() { Id = "aot-not-enabled", Severity = "Warning", Source = "performance",
                Catches = "A Blazor WebAssembly project using Bmage without RunAOTCompilation: pixel loops run 10 to 35 times slower." },
        new() { Id = "format-without-capability-check", Severity = "Warning", Source = "formats",
                Catches = "Exporting WebP or AVIF in a browser with no capability check or fallback: browsers that cannot write it throw ImageCapabilityException." },
        new() { Id = "browser-format-on-server", Severity = "Error", Source = "headless",
                Catches = "WebP or AVIF with the headless/managed codecs, which cannot encode either - ImageCapabilityException at the first request." },
        new() { Id = "metadata-preserve-keeps-location", Severity = "Warning", Source = "metadata-and-privacy",
                Catches = "MetadataPolicy.Preserve, which keeps GPS, serial numbers and owner names in the exported file." },
        new() { Id = "autoorient-off-without-rotation", Severity = "Warning", Source = "metadata-and-privacy",
                Catches = "DecodeOptions.AutoOrient = false with no AutoOrient() step: the default export removes the orientation tag and the image is saved sideways." },
        new() { Id = "resize-late-in-pipeline", Severity = "Suggestion", Source = "performance",
                Catches = "Per-pixel work (adjustments, filters, rotation) placed before the downscale, so it runs on every source pixel." },
        new() { Id = "size-budget-unhandled", Severity = "Suggestion", Source = "formats",
                Catches = "MaxFileSize with no ImageEncodeException handling: an impossible budget throws rather than returning an oversized file." },
        new() { Id = "data-url-preview", Severity = "Suggestion", Source = "getting-started",
                Catches = "ToDataUrl() for showing an image: base64 inflates the bytes by a third and pushes them through the DOM as a string." },
        new() { Id = "object-url-not-released", Severity = "Suggestion", Source = "getting-started",
                Catches = "CreateObjectUrlAsync with no ReleaseObjectUrlAsync: every preview keeps its bytes alive in the browser until the exporter's scope ends." },
        new() { Id = "slider-onchange-with-oninput", Severity = "Warning", Source = "troubleshooting",
                Catches = "A range input handling both @onchange and @oninput, where onchange is not reliable - the slider stops updating the image." },
        new() { Id = "browser-file-default-limit", Severity = "Warning", Source = "troubleshooting",
                Catches = "IBrowserFile.OpenReadStream() without maxAllowedSize: it refuses anything over 500 KB, which is most photos." },
        new() { Id = "redaction-not-solid", Severity = "Suggestion", Source = "security",
                Catches = "RedactionMode.Blur or Pixelate for hiding text: recoverable when the possible inputs are constrained. Solid removes the doubt." },
        new() { Id = "svg-input", Severity = "Warning", Source = "security",
                Catches = "Accepting SVG files: Bmage refuses SVG with ImageDecodeException, because untrusted SVG can run scripts." },
        new() { Id = "batch-concurrency-high", Severity = "Suggestion", Source = "pipelines",
                Catches = "A browser batch with MaxConcurrency of 4 or more: each worker holds a decoded image, and phones run out of memory." },
        new() { Id = "identify-before-decode", Severity = "Suggestion", Source = "security",
                Catches = "A server endpoint decoding an upload without ImageProcessor.Identify first, so dimensions are checked only after allocation is attempted." },
    ];

    /// <summary>The rules this review applies, so an empty result is not mistaken for an unchecked one.</summary>
    public static readonly string[] Rules = [.. Catalog.Select(rule => rule.Id)];

    /// <summary>The Bmage calls whose argument is only read, never kept: passing a buffer to one is not handing it over.</summary>
    private static readonly HashSet<string> _borrowingCalls = new(StringComparer.Ordinal)
    {
        "ExecuteAsync", "Execute", "EncodeAsync", "EncodeWithFallbackAsync", "DownloadAsync", "ProcessAsync", "DrawImage",
        "GetDominantColors", "Measure", "Create", "FindCrop", "HasTransparency", "RenderAsync", "Render", "CopyTo",
        "CopyRegion", "Clone", "FromBuffer", "FromImage", "ApplyPipeline", "GetRow", "Fill", "ToArray", "WriteLine",
    };

    /// <summary>Reviews Razor, C#, a csproj or a host page and reports what will not work as written.</summary>
    public static BmageReviewDto Review(string? code)
    {
        var text = BlankRawStrings((code ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal));
        var findings = new List<BmageReviewFindingDto>();

        if (text.Trim().Length == 0)
        {
            return new BmageReviewDto { Passed = true, Findings = [], RulesApplied = Rules };
        }

        var lines = text.Split('\n');

        CheckPrerenderCalls(text, findings);
        CheckUndisposedBuffers(text, lines, findings);
        CheckBorrowedBuffers(lines, findings);
        CheckEditorOwnership(text, findings);
        CheckStylesheet(text, lines, findings);
        CheckBridgeScript(lines, findings);
        CheckEditorHeight(text, lines, findings);
        CheckProcessorInjection(text, lines, findings);
        CheckHeadlessRegistration(text, lines, findings);
        CheckServicesRegistered(text, lines, findings);
        CheckAot(text, lines, findings);
        CheckFormats(text, lines, findings);
        CheckMetadataPolicy(text, lines, findings);
        CheckAutoOrient(text, lines, findings);
        CheckPipelineOrder(text, findings);
        CheckSizeBudget(text, lines, findings);
        CheckObjectUrls(text, lines, findings);
        CheckSliders(text, findings);
        CheckBrowserFileLimit(text, lines, findings);
        CheckRedaction(lines, findings);
        CheckSvg(lines, findings);
        CheckBatchConcurrency(text, lines, findings);
        CheckIdentify(text, lines, findings);

        var ordered = findings
            .OrderBy(finding => SeverityOrder(finding.Severity))
            .ThenBy(finding => finding.Line ?? int.MaxValue)
            .ToArray();

        return new BmageReviewDto
        {
            Passed = ordered.Any(finding => finding.Severity != "Suggestion") is false,
            Findings = ordered,
            RulesApplied = Rules
        };
    }

    /// <summary>
    /// Browser work in a lifecycle method the prerender pass also runs. There is no browser then:
    /// the JS bridge calls throw ImageCapabilityException, and the capability probe - deliberately
    /// forgiving - answers with the managed set, so a decision made from it is wrong on every browser.
    /// </summary>
    private static void CheckPrerenderCalls(string text, List<BmageReviewFindingDto> findings)
    {
        foreach (var (method, start, end) in LifecycleBodies(text))
        {
            var body = text[start..end];

            // A body that checks where it is running has already handled the case this rule is about.
            if (body.Contains("RendererInfo.IsInteractive", StringComparison.Ordinal) || body.Contains("OperatingSystem.IsBrowser", StringComparison.Ordinal)) continue;

            // OnInitialized runs exactly once, during the prerender. OnParametersSet runs then too, but
            // also on every interactive parameter change, which is a legitimate place to react to a new
            // image - so there it is a warning about the first render rather than an error.
            var initialization = method.StartsWith("OnInitialized", StringComparison.Ordinal);

            foreach (Match match in BridgeCallRegex().Matches(body))
            {
                findings.Add(new BmageReviewFindingDto
                {
                    Severity = initialization ? "Error" : "Warning",
                    Rule = "browser-call-during-prerender",
                    Line = LineOf(text, start + match.Index),
                    Message = $"{match.Groups["call"].Value} needs a live browser, and {method} also runs during prerendering, " +
                              "when there is none - it throws ImageCapabilityException (\"Could not load the Bmage browser " +
                              "module\") on the server, before the page is ever interactive.",
                    Fix = initialization
                        ? "Move it to OnAfterRenderAsync(bool firstRender) behind `if (!firstRender || !RendererInfo.IsInteractive) return;`, " +
                          "or into the event handler that needs it."
                        : "Return early while `!RendererInfo.IsInteractive`, so the first, prerendered pass does not reach it - or move " +
                          "it to OnAfterRenderAsync."
                });
            }

            foreach (Match match in ProbeCallRegex().Matches(body))
            {
                findings.Add(new BmageReviewFindingDto
                {
                    Severity = "Warning",
                    Rule = "browser-call-during-prerender",
                    Line = LineOf(text, start + match.Index),
                    Message = $"{match.Groups["call"].Value} in {method} runs during prerendering too. It does not throw there: " +
                              "IImageCapabilityProvider answers with the managed capability set (Environment = Prerendering), " +
                              "and ImageProcessorFactory/ImageExporter fall back to the managed codecs - so a format chosen here " +
                              "is JPEG or PNG on every browser, and WebP is reported unsupported.",
                    Fix = "Read capabilities and encode in OnAfterRenderAsync(firstRender) when RendererInfo.IsInteractive, or in an " +
                          "event handler; render a 'probing' state until then."
                });
            }
        }
    }

    /// <summary>A buffer the caller owns that is neither disposed nor handed to anything that keeps it.</summary>
    private static void CheckUndisposedBuffers(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        var checkedBuffers = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var match = OwnedBufferRegex().Match(lines[i]);
            if (match.Success is false) continue;
            if (++checkedBuffers > MaxBuffersChecked) break;

            var name = match.Groups["name"].Value;
            var api = match.Groups["api"].Value;

            if (IsReleased(text, name)) continue;

            var canvas = api.StartsWith("ImageCanvas", StringComparison.Ordinal);

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "image-buffer-not-disposed",
                Line = i + 1,
                Message = canvas
                    ? $"'{name}' is an ImageCanvas that owns its pixel buffer, and it is never disposed. The pixels are pooled " +
                      "memory that stays rented until a garbage collection happens to finalise it."
                    : $"'{name}' is an ImageBuffer you own ({api} returns a new buffer to the caller), and it is never disposed. " +
                      "Buffers are pooled memory - a 4000 x 3000 image is 48 MB - and an undisposed one stays rented until the " +
                      "collector gets round to it. On a phone the tab is killed first.",
                Fix = $"Declare it with `using var {name} = ...`, or dispose it when it is replaced and in the component's Dispose. " +
                      "Buffers you read from a property (ImageEditSession.Preview) are not yours to dispose."
            });
        }
    }

    /// <summary>
    /// The patterns built per variable name run over the caller's whole text, and a few of them backtrack
    /// quadratically on text with no ';'. The review is public and anonymous, so each one gets a deadline,
    /// and a pattern that runs out of time answers <paramref name="whenTooSlow"/> - the answer that reports nothing.
    /// </summary>
    private static readonly TimeSpan _dynamicRegexTimeout = TimeSpan.FromMilliseconds(200);

    /// <summary>The most buffer variables one review checks for disposal.</summary>
    private const int MaxBuffersChecked = 64;

    private static bool Has(string text, bool whenTooSlow, string pattern)
    {
        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.None, _dynamicRegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return whenTooSlow;
        }
    }

    private static IEnumerable<Match> Find(string text, string pattern)
    {
        try
        {
            // Materialised here, so a timeout is caught here rather than in the caller's loop.
            return Regex.Matches(text, pattern, RegexOptions.None, _dynamicRegexTimeout).ToArray();
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }
    }

    /// <summary>Whether a local is disposed, returned, stored or passed to something that keeps it.</summary>
    private static bool IsReleased(string text, string name)
    {
        var escaped = Regex.Escape(name);

        if (Has(text, true, $@"\b{escaped}\s*\??\.\s*Dispose(Async)?\s*\(")) return true;
        if (Has(text, true, $@"\busing\s*\(\s*{escaped}\b")) return true;
        if (Has(text, true, $@"\breturn\s+{escaped}\b")) return true;
        if (Has(text, true, $@"=>\s*{escaped}\s*[;,)]")) return true;
        if (Has(text, true, $@"[\w\]\.]+\s*=\s*{escaped}\s*[;,)]")) return true;
        if (Has(text, true, $@"\b\w+\s*=\s*{escaped}\s*\}}")) return true;
        if (Has(text, true, $@"Image\s*=\s*""@?{escaped}""")) return true;

        // Passed to a call that may keep it: anything but the Bmage calls known only to read their argument.
        foreach (Match call in Find(text, $@"\b(?<method>\w+)\s*\((?:[^()]*,\s*)?{escaped}\s*[,)]"))
        {
            if (_borrowingCalls.Contains(call.Groups["method"].Value) is false) return true;
        }

        // Inside a collection or object initializer, it now belongs to the thing built around it.
        return Has(text, true, $@"\bnew\b[\w.<>\s]*[({{\[][^;]*\b{escaped}\b") || Has(text, true, $@"\[[^\];]*\b{escaped}\b[^\];]*\]\s*;");
    }

    /// <summary>Disposing a buffer the edit session owns and keeps using.</summary>
    private static void CheckBorrowedBuffers(string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (BorrowedDisposeRegex().IsMatch(lines[i]) is false && BorrowedUsingRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Error",
                Rule = "borrowed-buffer-disposed",
                Line = i + 1,
                Message = "ImageEditSession.Preview and ImageEditSession.Original belong to the session, which keeps rendering from " +
                          "them. Disposing one returns its memory to the pool while the editor still reads it: the next render " +
                          "throws ObjectDisposedException, or worse, paints whatever reused that memory.",
                Fix = "Do not dispose buffers you read from a property. If you need pixels of your own, take a copy with .Clone() " +
                      "and dispose that, or render the document with RenderFullAsync, which returns a buffer you own."
            });
        }
    }

    /// <summary>LoadImageAsync takes ownership by default - so a caller that also disposes the buffer frees the editor's image.</summary>
    private static void CheckEditorOwnership(string text, List<BmageReviewFindingDto> findings)
    {
        foreach (Match match in LoadImageRegex().Matches(text))
        {
            var name = Regex.Escape(match.Groups["name"].Value);

            var disposed = Has(text, false, $@"\busing\s+(var|ImageBuffer\??)\s+{name}\b")
                        || Has(text, false, $@"\busing\s*\(\s*(var\s+)?{name}\b")
                        || Has(text, false, $@"\b{name}\s*\??\.\s*Dispose\s*\(");

            if (disposed is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Error",
                Rule = "editor-owns-loaded-buffer",
                Line = LineOf(text, match.Index),
                Message = $"'{match.Groups["name"].Value}' is handed to LoadImageAsync, which takes ownership of the buffer by default " +
                          "(takeOwnership: true) - and it is also disposed here. When the using scope ends the editor's session is " +
                          "left editing freed memory: ObjectDisposedException on the next render.",
                Fix = "Either drop the using/Dispose and let the editor own it, or call LoadImageAsync(image, takeOwnership: false) " +
                      "so the editor works on its own copy. Binding Image=\"...\" also copies."
            });
        }
    }

    /// <summary>The editor's stylesheet: missing from a head that hosts it, or linked at a path that does not exist.</summary>
    private static void CheckStylesheet(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            foreach (Match match in BmageStylesheetRegex().Matches(lines[i]))
            {
                var href = match.Groups["href"].Value.TrimStart('~', '/');
                if (href.StartsWith("./", StringComparison.Ordinal)) href = href[2..];

                if (href == "_content/Bit.Bmage.Editor/bit-bmage-editor.css") continue;

                findings.Add(new BmageReviewFindingDto
                {
                    Severity = "Error",
                    Rule = "editor-stylesheet-wrong-path",
                    Line = i + 1,
                    Message = $"'{match.Groups["href"].Value}' is not where the editor's stylesheet is served. The request 404s, and " +
                              "the editor renders unstyled with nothing in the build to say so.",
                    Fix = "<link rel=\"stylesheet\" href=\"_content/Bit.Bmage.Editor/bit-bmage-editor.css\" /> - the package is " +
                          "Bit.Bmage.Editor and the file is bit-bmage-editor.css."
                });
            }
        }

        var usesEditor = text.Contains("<BmageEditor", StringComparison.Ordinal) || text.Contains("<BmageDropZone", StringComparison.Ordinal)
                      || text.Contains("Bit.Bmage.Editor", StringComparison.Ordinal);

        if (usesEditor is false || text.Contains("bit-bmage-editor.css", StringComparison.Ordinal)) return;

        var head = Array.FindIndex(lines, line => HeadRegex().IsMatch(line));
        if (head < 0) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Warning",
            Rule = "editor-stylesheet-missing",
            Line = head + 1,
            Message = "The editor is used, and this page head does not link its stylesheet. The component renders anyway - as a " +
                      "toolbar of bare buttons with no canvas frame - and nothing in the build or the console says why.",
            Fix = "Add <link rel=\"stylesheet\" href=\"_content/Bit.Bmage.Editor/bit-bmage-editor.css\" /> to the head of App.razor " +
                  "(Blazor Web App) or wwwroot/index.html (standalone WebAssembly)."
        });
    }

    /// <summary>A script tag for the bridge, which the library imports itself as an ES module.</summary>
    private static void CheckBridgeScript(string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (BridgeScriptRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "bridge-script-tag",
                Line = i + 1,
                Message = "bit-bmage.js is an ES module that the library imports on demand. Loaded with a plain <script> tag it is " +
                          "parsed as a classic script and fails with a syntax error on its first export, and it is not needed.",
                Fix = "Remove the tag. The only thing the packages ask of the host page is the editor's stylesheet."
            });
        }
    }

    /// <summary>The editor fills its container; with no height anywhere it collapses.</summary>
    private static void CheckEditorHeight(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (text.Contains("height", StringComparison.OrdinalIgnoreCase)) return;

        var editor = Array.FindIndex(lines, line => line.Contains("<BmageEditor", StringComparison.Ordinal));
        if (editor < 0) return;

        // A class may carry the height in a stylesheet this review cannot see.
        if (EditorClassRegex().IsMatch(lines[editor])) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Suggestion",
            Rule = "editor-without-height",
            Line = editor + 1,
            Message = "BmageEditor fills its container, and nothing here gives that container a height. A container with no height " +
                      "collapses, and the editor is invisible or one pixel tall.",
            Fix = "Wrap it: <div style=\"height: 70vh\"><BmageEditor /></div>, or give the container a height in CSS."
        });
    }

    /// <summary>ImageProcessor injected into a component, where the browser registration does not provide it.</summary>
    private static void CheckProcessorInjection(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (HeadlessCallRegex().IsMatch(text)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (ProcessorInjectRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Error",
                Rule = "processor-injected-in-browser",
                Line = i + 1,
                Message = "AddBitBmageServices does not register ImageProcessor: in a browser the processor can only be built once " +
                          "the browser's codecs have been probed, which is asynchronous. Injecting it fails with \"There is no " +
                          "registered service of type 'ImageProcessor'\" - at prerender in a Web App, not at compile time.",
                Fix = "@inject ImageProcessorFactory Processors, then `var processor = await Processors.GetAsync();` in an event " +
                      "handler or OnAfterRenderAsync. Or use ImageLoader/ImageExporter, which do this for you."
            });
        }
    }

    /// <summary>Browser services against the headless registration, and the headless registration in a browser.</summary>
    private static void CheckHeadlessRegistration(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        // A call, not a mention: a page that explains the headless registration in its prose has not made it.
        var headless = Array.FindIndex(lines, line => HeadlessCallRegex().IsMatch(line));
        if (headless < 0) return;

        if (text.Contains("WebAssemblyHostBuilder", StringComparison.Ordinal))
        {
            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Error",
                Rule = "headless-services-in-browser",
                Line = headless + 1,
                Message = "AddBitBmageHeadlessServices in a WebAssembly host registers only the managed engine: no ImageLoader, no " +
                          "ImageExporter, no ImageProcessorFactory, no browser codecs - so no WebP or AVIF, no clipboard, no " +
                          "downloads, and the editor cannot be resolved.",
                Fix = "builder.Services.AddBitBmageServices() in the browser project. The headless registration is for servers, " +
                      "console apps and workers."
            });

            return;
        }

        if (BrowserCallRegex().IsMatch(text)) return;

        var browserUse = BrowserServiceUseRegex().Match(text);
        if (browserUse.Success is false) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Error",
            Rule = "browser-services-with-headless-registration",
            Line = LineOf(text, browserUse.Index),
            Message = $"{browserUse.Groups["service"].Value} is a browser service, and the only registration here is " +
                      "AddBitBmageHeadlessServices, which registers ImageProcessor, a managed capability provider and " +
                      "FontTextRasterizer - nothing else. Resolving it throws a missing-service exception.",
            Fix = "Components that talk to a browser need AddBitBmageServices (in every container they run in). On the server, " +
                  "inject ImageProcessor and work with bytes instead."
        });
    }

    /// <summary>A Blazor host that never registers Bmage at all.</summary>
    private static void CheckServicesRegistered(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (text.Contains("AddBitBmage", StringComparison.Ordinal)) return;

        var webApp = text.Contains("AddRazorComponents", StringComparison.Ordinal)
                  && (text.Contains("AddInteractiveWebAssemblyComponents", StringComparison.Ordinal) || text.Contains("AddInteractiveServerComponents", StringComparison.Ordinal));
        var wasm = text.Contains("WebAssemblyHostBuilder", StringComparison.Ordinal);

        if (webApp is false && wasm is false) return;

        // A Web App that never prerenders does not instantiate the client's components on the server.
        if (webApp && wasm is false && text.Contains("prerender: false", StringComparison.Ordinal)) return;

        // A call this review does not recognise may be the app's own method that registers Bmage inside.
        foreach (Match call in ServiceAddRegex().Matches(text))
        {
            if (_frameworkRegistrations.Contains(call.Groups["name"].Value) is false) return;
        }

        var line = Array.FindIndex(lines, line => line.Contains(wasm ? "WebAssemblyHostBuilder" : "AddRazorComponents", StringComparison.Ordinal));

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Warning",
            Rule = "services-not-registered",
            Line = line + 1,
            Message = wasm
                ? "This WebAssembly host never registers Bmage, so every @inject ImageLoader / ImageExporter / " +
                  "IImageCapabilityProvider in its components throws a missing-service exception when the page renders."
                : "This Blazor Web App host never registers Bmage. Its prerender pass instantiates the client's components in " +
                  "this container, so any component that injects a Bmage service fails at prerender - in production, not at build.",
            Fix = "Call builder.Services.AddBitBmageServices() here - in a Web App, through one shared extension method that both " +
                  "the server's and the client's Program.cs call, so the two containers cannot drift. GetBmageSetupGuide has the files."
        });
    }

    private static readonly HashSet<string> _frameworkRegistrations = new(StringComparer.Ordinal)
    {
        "AddRazorComponents", "AddInteractiveWebAssemblyComponents", "AddInteractiveServerComponents", "AddControllers",
        "AddControllersWithViews", "AddMcpServer", "AddHttpClient", "AddScoped", "AddSingleton", "AddTransient",
        "AddAuthentication", "AddAuthorization", "AddAuthorizationCore", "AddCascadingAuthenticationState", "AddAntiforgery",
        "AddLogging", "AddOptions", "AddCors", "AddResponseCompression", "AddHttpContextAccessor", "AddEndpointsApiExplorer",
        "AddSwaggerGen", "AddOpenApi", "AddProblemDetails", "AddHealthChecks", "AddMemoryCache", "AddDistributedMemoryCache",
        "AddRazorPages", "AddServerSideBlazor", "AddLocalization", "AddDataProtection", "AddOutputCache", "AddRateLimiter",
        "AddJsonOptions", "AddHostedService", "AddSignalR",
    };

    /// <summary>A WebAssembly project using Bmage with the interpreter.</summary>
    private static void CheckAot(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (text.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.Ordinal) is false) return;
        if (BmageReferenceRegex().IsMatch(text) is false) return;

        var disabled = AotDisabledRegex().Match(text);

        if (text.Contains("RunAOTCompilation", StringComparison.Ordinal) && disabled.Success is false) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Warning",
            Rule = "aot-not-enabled",
            Line = disabled.Success ? LineOf(text, disabled.Index) : Array.FindIndex(lines, line => line.Contains("Sdk=", StringComparison.Ordinal)) + 1,
            Message = "This browser project uses Bmage and does not compile ahead of time. Every pixel loop in Bmage is C#, and on " +
                      "Blazor's IL interpreter it runs 10 to 35 times slower than compiled code (a 1920x1080 Lanczos resize: " +
                      "505 ms interpreted, 56 ms with AOT).",
            Fix = "<RunAOTCompilation>true</RunAOTCompilation> in this project (needs `dotnet workload install wasm-tools`; only " +
                  "applies to dotnet publish). To keep `dotnet build -c Release` working without the workload, condition it on " +
                  "publishing as the Bmage demo's client project does."
        });
    }

    /// <summary>WebP/AVIF without a capability check in a browser, or at all on a server.</summary>
    private static void CheckFormats(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        var line = Array.FindIndex(lines, line => BrowserFormatRegex().IsMatch(line));
        if (line < 0) return;

        var format = BrowserFormatRegex().Match(lines[line]).Groups["format"].Value;

        var serverSide = ServerMarkerRegex().IsMatch(text) && BrowserMarkerRegex().IsMatch(text) is false;

        if (serverSide)
        {
            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Error",
                Rule = "browser-format-on-server",
                Line = line + 1,
                Message = $"{format} is a browser codec. With no browser - AddBitBmageHeadlessServices, new ImageProcessor(), a " +
                          "server endpoint - only the managed codecs exist (PNG, JPEG, BMP encode), and asking for " +
                          $"{format} throws ImageCapabilityException: \"No encoder is registered for {format} in this environment\".",
                Fix = "Encode JPEG (photos) or PNG (graphics, transparency) on the server. If the file must be WebP, encode it in " +
                      "the browser before upload with ImageExporter.EncodeWithFallbackAsync."
            });

            return;
        }

        if (CapabilityCheckRegex().IsMatch(text)) return;
        if (BrowserMarkerRegex().IsMatch(text) is false) return;

        // Code that catches the refusal has decided what to do on a browser that cannot write the format.
        if (text.Contains("ImageCapabilityException", StringComparison.Ordinal)) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Warning",
            Rule = "format-without-capability-check",
            Line = line + 1,
            Message = $"{format} is exported with no capability check and no fallback. Encoding support varies by browser - AVIF " +
                      "encoding is rarely available, older Safari cannot write WebP - and Bmage refuses the PNG a browser silently " +
                      "substitutes: EncodeAsync throws ImageCapabilityException on those browsers.",
            Fix = "Exporter.EncodeWithFallbackAsync(image, [ImageFormat.WebP, ImageFormat.Jpeg], options) - it picks the first " +
                  "format this browser can really encode and reports it on encoded.Format. Or check " +
                  "capabilities.CanEncode(format) first."
        });
    }

    /// <summary>Preserve keeps the location and the person.</summary>
    private static void CheckMetadataPolicy(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (text.Contains("WithoutLocation", StringComparison.Ordinal) || text.Contains("StripSensitive()", StringComparison.Ordinal)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("MetadataPolicy.Preserve", StringComparison.Ordinal) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "metadata-preserve-keeps-location",
                Line = i + 1,
                Message = "MetadataPolicy.Preserve keeps everything the format can carry: the GPS position the phone recorded, the " +
                          "camera's serial number, the owner's name, unique ids and XMP. Published, that is where the user lives.",
                Fix = "Use MetadataPolicy.Remove (the default) for anything users upload or publish, or StripSensitive to keep camera " +
                      "and exposure data without the location and identity. Keep Preserve for files you produced yourself."
            });

            return;
        }
    }

    /// <summary>Orientation reported rather than applied, then exported with the default policy that removes the tag.</summary>
    private static void CheckAutoOrient(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (AutoOrientStepRegex().IsMatch(text)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (AutoOrientOffRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "autoorient-off-without-rotation",
                Line = i + 1,
                Message = "With AutoOrient = false the pixels stay as the camera stored them, and only the EXIF tag says how to turn " +
                          "them. Every metadata policy resets that tag on export (and Remove, the default, drops it), so a phone " +
                          "photo is saved sideways with nothing left to correct it.",
                Fix = "Leave DecodeOptions.AutoOrient on, or add .AutoOrient() as the first step of the pipeline before exporting."
            });

            return;
        }
    }

    /// <summary>Per-pixel work ahead of the downscale, in a fluent pipeline.</summary>
    private static void CheckPipelineOrder(string text, List<BmageReviewFindingDto> findings)
    {
        foreach (Match chain in PipelineChainRegex().Matches(text))
        {
            var steps = StepRegex().Matches(chain.Value).Select(step => (Name: step.Groups["name"].Value, Index: chain.Index + step.Index)).ToArray();

            var resize = Array.FindIndex(steps, step => step.Name is "MaxSize" or "Cover" or "Resize");
            if (resize <= 0) continue;

            var expensive = steps.Take(resize).FirstOrDefault(step => _perPixelSteps.Contains(step.Name));
            if (expensive.Name is null) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "resize-late-in-pipeline",
                Line = LineOf(text, expensive.Index),
                Message = $"{expensive.Name} runs before {steps[resize].Name}, so it processes every source pixel - for a 24 MP photo " +
                          "shrunk to 1920px that is around six times the work - and a sharpen before a downscale is mostly undone by it.",
                Fix = $"Cap the dimensions first: .AutoOrient().{steps[resize].Name}(...) and then the adjustments and filters, " +
                      "unless the order is intentional (a vignette meant to frame the uncropped image). RunBmagePipeline measures both orders."
            });
        }
    }

    private static readonly HashSet<string> _perPixelSteps = new(StringComparer.Ordinal)
    {
        "Brightness", "Contrast", "Saturation", "Hue", "Exposure", "Gamma", "Temperature", "Tint", "Vibrance", "ShadowsHighlights",
        "Opacity", "Grayscale", "Sepia", "Invert", "Posterize", "Threshold", "Blur", "Sharpen", "Pixelate", "Noise", "DenoiseMedian",
        "Vignette", "AutoLevels", "AutoContrast", "ColorMatrix", "Rotate", "Skew",
    };

    /// <summary>A size budget whose failure is not handled.</summary>
    private static void CheckSizeBudget(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (text.Contains("ImageEncodeException", StringComparison.Ordinal) || GeneralCatchRegex().IsMatch(text)) return;

        var line = Array.FindIndex(lines, line => MaxFileSizeRegex().IsMatch(line));
        if (line < 0) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Suggestion",
            Rule = "size-budget-unhandled",
            Line = line + 1,
            Message = "MaxFileSize makes the encoder search for settings that fit, and when the budget is impossible it throws " +
                      "ImageEncodeException (\"Cannot reach N bytes for this image\") rather than returning an oversized file. " +
                      "Unhandled, that is an unhandled exception for whichever user uploads a detailed photo.",
            Fix = "catch (ImageEncodeException) and tell the user, or raise the budget, lower MinQuality, or allow " +
                  "MaxFileSizeStrategy.QualityThenDimensions. RunBmagePipeline with maxFileSize shows whether a budget is reachable."
        });
    }

    /// <summary>Data URLs for previews, and object URLs never released.</summary>
    private static void CheckObjectUrls(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(".ToDataUrl()", StringComparison.Ordinal) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "data-url-preview",
                Line = i + 1,
                Message = "ToDataUrl() base64-encodes the image: a third larger, built as one string in .NET, then pushed through the " +
                          "DOM as an attribute. For anything bigger than an icon that is the slow way to show it.",
                Fix = "var url = await Exporter.CreateObjectUrlAsync(encoded); hands the browser the bytes directly. Release it " +
                      "with ReleaseObjectUrlAsync when it is replaced."
            });

            break;
        }

        if (text.Contains("ReleaseObjectUrlAsync", StringComparison.Ordinal)) return;

        var create = Array.FindIndex(lines, line => line.Contains("CreateObjectUrlAsync", StringComparison.Ordinal));
        if (create < 0) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Suggestion",
            Rule = "object-url-not-released",
            Line = create + 1,
            Message = "Every object URL keeps its bytes alive in the browser until it is released. ImageExporter releases the ones " +
                      "it created when it is disposed - but it is scoped, and in WebAssembly that scope is the whole app, so a " +
                      "page that previews many images keeps every one of them.",
            Fix = "await Exporter.ReleaseObjectUrlAsync(previousUrl) before replacing a preview, and in the component's DisposeAsync."
        });
    }

    /// <summary>A range input with both onchange and oninput.</summary>
    private static void CheckSliders(string text, List<BmageReviewFindingDto> findings)
    {
        foreach (Match input in InputTagRegex().Matches(text))
        {
            var tag = input.Value;

            if (tag.Contains("@onchange", StringComparison.Ordinal) is false || tag.Contains("@oninput", StringComparison.Ordinal) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "slider-onchange-with-oninput",
                Line = LineOf(text, input.Index),
                Message = "This input handles both @onchange and @oninput. In Blazor, onchange on a range input is not reliable when " +
                          "oninput is also handled on the same element, so the commit that should re-run the pipeline does not fire.",
                Fix = "Handle oninput only, and debounce it so a drag queues one run rather than one per pixel of travel. For an editor " +
                      "adjustment, pass transient: true while dragging and commit once."
            });
        }
    }

    /// <summary>IBrowserFile's 500 KB default.</summary>
    private static void CheckBrowserFileLimit(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (BrowserFileMarkerRegex().IsMatch(text) is false) return;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(".OpenReadStream()", StringComparison.Ordinal) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "browser-file-default-limit",
                Line = i + 1,
                Message = "IBrowserFile.OpenReadStream() with no maxAllowedSize refuses anything over 512,000 bytes - an IOException " +
                          "for nearly every photo a phone takes.",
                Fix = "Pass the limit: file.OpenReadStream(maxAllowedSize: 32 * 1024 * 1024). Or let ImageLoader.LoadAsync(file) read " +
                      "it, which applies BitBmageOptions.MaxSourceBytes."
            });

            return;
        }
    }

    /// <summary>Blur and pixelate as redaction.</summary>
    private static void CheckRedaction(string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            var match = WeakRedactionRegex().Match(lines[i]);
            if (match.Success is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "redaction-not-solid",
                Line = i + 1,
                Message = $"RedactionMode.{match.Groups["mode"].Value} replaces the pixels, but published research has recovered text from " +
                          "blurred and pixelated images when the possible inputs are constrained - exactly the case for account " +
                          "numbers and short strings.",
                Fix = "Use RedactionMode.Solid for anything that must not be recovered."
            });
        }
    }

    /// <summary>SVG offered to a decoder that refuses it.</summary>
    private static void CheckSvg(string[] lines, List<BmageReviewFindingDto> findings)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (SvgAcceptRegex().IsMatch(lines[i]) is false) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Warning",
                Rule = "svg-input",
                Line = i + 1,
                Message = "This input offers SVG files, and Bmage refuses SVG with ImageDecodeException - rasterising untrusted SVG " +
                          "can run embedded scripts and fetch remote resources, and a library cannot make that safe for you.",
                Fix = "Accept image/png, image/jpeg, image/webp, image/avif and image/gif (or image/*). If you control the SVG, " +
                      "rasterise it yourself and pass the pixels."
            });
        }
    }

    /// <summary>A browser batch wide enough to run a phone out of memory.</summary>
    private static void CheckBatchConcurrency(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (ServerMarkerRegex().IsMatch(text)) return;

        for (int i = 0; i < lines.Length; i++)
        {
            var match = ConcurrencyRegex().Match(lines[i]);
            if (match.Success is false || int.Parse(match.Groups["n"].Value) < 4) continue;

            findings.Add(new BmageReviewFindingDto
            {
                Severity = "Suggestion",
                Rule = "batch-concurrency-high",
                Line = i + 1,
                Message = $"MaxConcurrency = {match.Groups["n"].Value}: each worker holds a decoded image, so four workers on 24 MP " +
                          "photos is about 400 MB of live pixels. A browser tab that exceeds its memory ceiling is killed, not thrown at.",
                Fix = "Keep it at 2 in a browser (the default), 1 on phones with large photos. Concurrency buys little in WebAssembly " +
                      "anyway, which runs the pixel loops on one thread."
            });
        }
    }

    /// <summary>A server decode without a header check first.</summary>
    private static void CheckIdentify(string text, string[] lines, List<BmageReviewFindingDto> findings)
    {
        if (UploadMarkerRegex().IsMatch(text) is false) return;
        if (text.Contains("Identify(", StringComparison.Ordinal)) return;

        var line = Array.FindIndex(lines, line => ServerDecodeRegex().IsMatch(line));
        if (line < 0) return;

        findings.Add(new BmageReviewFindingDto
        {
            Severity = "Suggestion",
            Rule = "identify-before-decode",
            Line = line + 1,
            Message = "An uploaded file is decoded with no look at its header first. The decoder's ImageLimits still refuse a " +
                      "decompression bomb, but the endpoint has no say in what it accepts, and the user gets an exception rather " +
                      "than a reason.",
            Fix = "var info = processor.Identify(bytes); reject null (not an image) and anything whose Width * Height is over " +
                  "your limit, before ProcessAsync. Treat ImageDecodeException as an expected outcome."
        });
    }

    /// <summary>
    /// The bodies of the lifecycle methods the prerender pass runs, as (method, start, end) offsets.
    /// Expression-bodied ones end at their semicolon.
    /// </summary>
    private static IEnumerable<(string Method, int Start, int End)> LifecycleBodies(string text)
    {
        foreach (Match match in LifecycleRegex().Matches(text))
        {
            var method = match.Groups["method"].Value;
            var position = match.Index + match.Length;

            // Past the parameter list.
            var close = text.IndexOf(')', position);
            if (close < 0) continue;

            var arrow = text.IndexOf("=>", close, StringComparison.Ordinal);
            var brace = text.IndexOf('{', close);

            if (arrow >= 0 && (brace < 0 || arrow < brace))
            {
                var end = text.IndexOf(';', arrow);

                yield return (method, arrow, end < 0 ? text.Length : end);
                continue;
            }

            if (brace < 0) continue;

            var depth = 0;

            for (var i = brace; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0)
                {
                    yield return (method, brace, i);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Blanks the contents of C# raw string literals, keeping their line breaks so every finding still
    /// points at the right line. A page that shows a code sample - a server endpoint quoted in a
    /// browser page, say - holds that sample in one, and it is text the page displays, not code it runs.
    /// </summary>
    private static string BlankRawStrings(string text)
        => RawStringRegex().Replace(text, match => string.Create(match.Length, match.Value, (span, value) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = value[i] == '\n' ? '\n' : ' ';
        }));

    private static int LineOf(string text, int index) => text.AsSpan(0, Math.Clamp(index, 0, text.Length)).Count('\n') + 1;

    private static int SeverityOrder(string severity) => severity switch
    {
        "Error" => 0,
        "Warning" => 1,
        _ => 2
    };

    [GeneratedRegex(@"\b(?:protected\s+)?(?:override\s+)?(?:async\s+)?(?:Task|void|ValueTask)\s+(?<method>OnInitialized(?:Async)?|OnParametersSet(?:Async)?)\s*\(")]
    private static partial Regex LifecycleRegex();

    // The calls that need the JavaScript bridge, and throw without it.
    [GeneratedRegex(@"\.(?<call>DownloadAsync|SaveAsAsync|CopyToClipboardAsync|CreateObjectUrlAsync|ReleaseObjectUrlAsync|LoadFromClipboardAsync|ListenForPasteAsync|DrawTextAsync|MeasureAsync)\s*\(|(?<call>ImageSource\.FromUrl)\s*\(")]
    private static partial Regex BridgeCallRegex();

    // The calls that degrade instead of throwing: they read capabilities, which answer "managed" during prerendering.
    [GeneratedRegex(@"\b(?<call>\w*(?:[Cc]apabilit|[Pp]rovider|[Ff]actor|[Pp]rocessors)\w*\.GetAsync|\w*[Ee]xporter\w*\.Encode(?:WithFallback)?Async)\s*\(")]
    private static partial Regex ProbeCallRegex();

    [GeneratedRegex(@"^\s*(?:var|ImageBuffer\??|ImageCanvas)\s+(?<name>\w+)\s*=\s*(?:await\s+)?[^;]*?(?<api>\bLoadAsync|\bLoadFromClipboardAsync|\bExecuteAsync|\bExecute|\bDecodeAsync|\bRenderAsync|\bRenderFullAsync|\bRender|\bSnapshot|\bClone|\bCopyRegion|ImageBuffer\.FromRgba|ImageBuffer\.Create|ImageCanvas\.Create|ImageCanvas\.FromImage)\s*\(")]
    private static partial Regex OwnedBufferRegex();

    [GeneratedRegex(@"\.(?:Preview|Original)\s*\??\.\s*Dispose\s*\(")]
    private static partial Regex BorrowedDisposeRegex();

    [GeneratedRegex(@"\busing\s+(?:var\s+\w+\s*=\s*|\(\s*)[^;]*\.(?:Preview|Original)\s*[;)]")]
    private static partial Regex BorrowedUsingRegex();

    [GeneratedRegex(@"\bLoadImageAsync\s*\(\s*(?<name>\w+)\s*(?:\)|,\s*(?:takeOwnership\s*:\s*)?true\s*\))")]
    private static partial Regex LoadImageRegex();

    [GeneratedRegex(@"href\s*=\s*""(?<href>[^""]*(?:_content/Bit\.Bmage[^""]*|bit-bmage[^""]*)\.css)""", RegexOptions.IgnoreCase)]
    private static partial Regex BmageStylesheetRegex();

    [GeneratedRegex(@"<head\b", RegexOptions.IgnoreCase)]
    private static partial Regex HeadRegex();

    [GeneratedRegex(@"<script\b[^>]*\bsrc\s*=\s*""[^""]*bit-bmage[^""]*\.js""", RegexOptions.IgnoreCase)]
    private static partial Regex BridgeScriptRegex();

    [GeneratedRegex(@"<BmageEditor\b[^>]*\b(?:Class|Style)\s*=")]
    private static partial Regex EditorClassRegex();

    // A Razor directive starts its line; "@inject ImageProcessor" in a sentence is prose about one.
    [GeneratedRegex(@"(?:^\s*@inject\s+(?:Bit\.Bmage\.Pipeline\.)?ImageProcessor\s+\w+|\[Inject\][^;{]*\bImageProcessor\s+\w+\s*\{)")]
    private static partial Regex ProcessorInjectRegex();

    [GeneratedRegex(@"(?:^\s*@inject\s+|\[Inject\][^;{]*\b|\bGetRequiredService<)(?<service>ImageLoader|ImageExporter|ImageProcessorFactory|BrowserTextRasterizer|BrowserCapabilityProvider)\b|<(?<service>BmageEditor)\b", RegexOptions.Multiline)]
    private static partial Regex BrowserServiceUseRegex();

    // Only calls on the service collection: the render-mode calls chained on MapRazorComponents are not registrations.
    [GeneratedRegex(@"\b[Ss]ervices\s*\.\s*(?<name>Add\w+)\s*\(")]
    private static partial Regex ServiceAddRegex();

    [GeneratedRegex(@"Include\s*=\s*""Bit\.Bmage(?:\.Editor|\.Core)?""|Bit\.Bmage(?:\.Editor|\.Core)?\.csproj")]
    private static partial Regex BmageReferenceRegex();

    [GeneratedRegex(@"<RunAOTCompilation>\s*false\s*</RunAOTCompilation>", RegexOptions.IgnoreCase)]
    private static partial Regex AotDisabledRegex();

    [GeneratedRegex(@"\b(?:ImageFormat|ImageExportOptions)\.(?<format>WebP|Avif)\b")]
    private static partial Regex BrowserFormatRegex();

    [GeneratedRegex(@"EncodeWithFallbackAsync|\.CanEncode\s*\(|SupportsWebP|SupportsAvif|ChooseEncodableFormat")]
    private static partial Regex CapabilityCheckRegex();

    [GeneratedRegex(@"AddBitBmageHeadlessServices|new\s+ImageProcessor\s*\(|ImageCodecRegistry\.CreateDefault|\bIFormFile\b|\bMapPost\s*\(|:\s*ControllerBase\b|static\s+async\s+Task\s+Main\s*\(")]
    private static partial Regex ServerMarkerRegex();

    [GeneratedRegex(@"\bImageExporter\b|\bExporter\.|ImageProcessorFactory|\bIBrowserFile\b|InputFileChangeEventArgs|<BmageEditor\b|\bAddBitBmageServices\b|@inject\b")]
    private static partial Regex BrowserMarkerRegex();

    [GeneratedRegex(@"\bAutoOrient\s*=\s*false\b")]
    private static partial Regex AutoOrientOffRegex();

    [GeneratedRegex(@"\.AutoOrient\s*\(\s*\)")]
    private static partial Regex AutoOrientStepRegex();

    // A fluent pipeline: from where it is created to the end of the statement or the call that runs it.
    [GeneratedRegex(@"ImagePipeline\s*\.\s*(?:Create\s*\(\s*\)|Empty)[^;]*", RegexOptions.Singleline)]
    private static partial Regex PipelineChainRegex();

    [GeneratedRegex(@"\.\s*(?<name>[A-Z]\w*)\s*\(")]
    private static partial Regex StepRegex();

    [GeneratedRegex(@"catch\s*\(\s*(?:System\.)?(?:Image)?Exception\b|catch\s*\{|catch\s*$", RegexOptions.Multiline)]
    private static partial Regex GeneralCatchRegex();

    [GeneratedRegex(@"\bMaxFileSize\s*=\s*(?!null\b)\S")]
    private static partial Regex MaxFileSizeRegex();

    [GeneratedRegex(@"<input\b[^>]*>", RegexOptions.Singleline)]
    private static partial Regex InputTagRegex();

    [GeneratedRegex(@"\bIBrowserFile\b|InputFileChangeEventArgs|GetMultipleFiles|\be\.File\b|<InputFile\b|OnFilesSelected")]
    private static partial Regex BrowserFileMarkerRegex();

    [GeneratedRegex(@"RedactionMode\.(?<mode>Blur|Pixelate)\b")]
    private static partial Regex WeakRedactionRegex();

    [GeneratedRegex(@"accept\s*=\s*""[^""]*(?:svg|\.svg)[^""]*""", RegexOptions.IgnoreCase)]
    private static partial Regex SvgAcceptRegex();

    [GeneratedRegex(@"(""{3,})[\s\S]*?\1")]
    private static partial Regex RawStringRegex();

    [GeneratedRegex(@"\bAddBitBmageHeadlessServices\s*\(")]
    private static partial Regex HeadlessCallRegex();

    [GeneratedRegex(@"\bAddBitBmageServices\s*\(")]
    private static partial Regex BrowserCallRegex();

    [GeneratedRegex(@"\bMaxConcurrency\s*=\s*(?<n>\d+)")]
    private static partial Regex ConcurrencyRegex();

    [GeneratedRegex(@"\bIFormFile\b|\bMapPost\s*\(|Request\.Body|\[FromForm\]")]
    private static partial Regex UploadMarkerRegex();

    [GeneratedRegex(@"\.(?:ProcessAsync|DecodeAsync)\s*\(")]
    private static partial Regex ServerDecodeRegex();
}
