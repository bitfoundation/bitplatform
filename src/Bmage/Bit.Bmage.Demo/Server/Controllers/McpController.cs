using System.Text;
using System.Reflection;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Bit.Bmage.Demo.Server.Dtos;
using Bit.Bmage.Demo.Server.Services;

namespace Bit.Bmage.Demo.Server.Controllers;

/// <summary>
/// The Bmage MCP server: the tools an AI agent calls to write image code with Bit.Bmage without
/// guessing at its API - or at what its output will weigh.
/// <para>
/// Documentation tools alone are not enough for an image library. An agent can read every property
/// of <c>ImageExportOptions</c> and still not know whether a 1200 x 1200 avatar fits in 80 KB at
/// quality 0.8, whether PNG or JPEG is smaller for a screenshot, or whether an <c>AutoOrient()</c> in
/// front of a resize stops the optimiser fusing it with the crop after it. Those facts live in the
/// engine, not in prose. So the tools here fall into two halves: the ones that answer from this
/// build's own text - the guides, the XML documentation compiled into the three assemblies, the
/// demo's sources - and the ones that answer by <b>running the real engine on this server</b> and
/// reporting what it did.
/// </para>
/// <para>
/// Every method is also a plain HTTP GET endpoint under <c>/api/mcp/...</c>, which makes each of
/// them inspectable from a browser - and is what the demo's own MCP page calls to show the tools
/// working live.
/// </para>
/// </summary>
[ApiController]
[McpServerToolType]
[Route("api/[controller]/[action]")]
public class McpController : ControllerBase
{
    // Long enough for the largest guide whole (performance.md, around 10,700 characters), short
    // enough that a couple of tool calls cannot crowd out a client's context window. McpResources
    // reads the same bound: the resources hand out the same documents, so a client that pins one
    // instead of calling the tool gets the same text rather than an unbounded one.
    public const int MaxDocumentLength = 15_000;

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageOverview), Title = "What Bit.Bmage is and how to use it",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Explains what Bit.Bmage is, which of its three packages to install, how to register it, and the rules that are not visible in any signature - prerendering, disposal, codecs per host, metadata. Worth reading once per session; for a specific question, call SearchBmage instead.")]
    public string GetBmageOverview()
    {
        var builder = new StringBuilder();

        var readme = BmageSourceCatalog.Readme;
        var firstSection = readme.IndexOf("\n## ", StringComparison.Ordinal);

        builder.AppendLine((firstSection > 0 ? readme[..firstSection] : readme).Trim().TrimEnd('-').Trim()).AppendLine();

        // Which build the answers come from: every tool below reflects THIS assembly, not a remembered version.
        builder.AppendLine($"_These tools answer from Bit.Bmage {BmageServerInstructions.BmageVersion}, loaded in this server._").AppendLine();

        AppendSection(builder, BmageSourceCatalog.ReadmeName, "Packages");
        AppendSection(builder, BmageSourceCatalog.ReadmeName, "Installation");
        AppendSection(builder, BmageSourceCatalog.ReadmeName, "Where it runs");

        // Deliberately no tool directory here. The client already holds every tool's description
        // from tools/list; restating them would be paid on every call to say what the caller could
        // already read. What is left is the part that is nowhere else: the order to work in, and
        // the rules no signature shows.
        builder.AppendLine("""
            ---

            ## How to work

            1. `SearchBmage` with the request in the words it arrived in. Every hit carries the exact follow-up call.
            2. `GetBmageRecipe` if the search turned up a recipe: they are complete, correct against this build, and
               carry the caveat that is not visible in the code.
            3. `GetBmageSetupGuide` for the app's hosting model before writing any wiring.
            4. `GetBmageApiDetails` for every type you use - it names the namespace and the package, and the defaults.
            5. `RunBmagePipeline` to choose sizes, formats, qualities and byte budgets on measurements, not habit.
            6. `ReviewBmageCode` on what you wrote, before calling it done.

            ## Rules of thumb when writing Bmage code

            - **Register for the host.** `AddBitBmageServices` in a browser app - in BOTH the server and the client
              container of a Blazor Web App, because the prerender pass instantiates every component on the server.
              `AddBitBmageHeadlessServices` on a server, in a console app or a worker. They register different things.
            - **No `@inject ImageProcessor` in a browser app.** Get it from `ImageProcessorFactory.GetAsync()`, or use
              `ImageLoader` and `ImageExporter`, which do that for you.
            - **Browser work after the first render.** Capabilities, downloads, object URLs, the clipboard and paste
              listeners need a browser, and `OnInitialized(Async)` also runs during prerendering, when there is none. Use
              `OnAfterRenderAsync(firstRender)` or an event handler.
            - **Dispose what you are handed.** `LoadAsync`, `ExecuteAsync`, `DecodeAsync`, `Render`, `Snapshot` and
              `Clone` return buffers you own: `using var`. Buffers you read from a property (`Session.Preview`) are not
              yours. `BmageEditor.LoadImageAsync(buffer)` takes ownership.
            - **WebP and AVIF are browser codecs.** Export them with `EncodeWithFallbackAsync([WebP, Jpeg])`; never on a
              server, where only PNG, JPEG and BMP encode.
            - **Privacy is the default.** Export metadata is `Remove` unless you ask; `StripSensitive` keeps camera data
              without GPS; `Preserve` keeps the location. Redact with `RedactionMode.Solid`.
            - **Cap dimensions first.** `.AutoOrient().MaxSize(...)` before adjustments and filters; group adjustments
              together so the optimiser fuses them into one pass.
            - **AOT the browser project.** `<RunAOTCompilation>true</RunAOTCompilation>`: the interpreter runs the pixel
              loops 10 to 35 times slower.
            - **The editor** needs `_content/Bit.Bmage.Editor/bit-bmage-editor.css` in the page head and a container
              with a height.
            """);

        return builder.ToString();
    }

    [HttpGet]
    [McpServerTool(Name = nameof(SearchBmage), Title = "Search everything about Bit.Bmage",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Start here. Searches everything known about Bit.Bmage at once - every guide section, every public type and member of the three packages, the setup guides, the ready-made recipes, the code-review rules and the demo's source files - and returns the best matches, each with the exact follow-up tool call that returns its full text. Call it whenever you do not already know which guide, type or recipe holds the answer. Example queries: 'avatar under 100 KB', 'strip gps from uploads', 'paste a screenshot and redact it', 'why is the editor unstyled', 'thumbnail on the server'.")]
    public BmageSearchHitDto[] SearchBmage(
        [Description("What you are trying to do or what went wrong, in your own words, e.g. 'crop an avatar to a circle' or 'WebP export fails on Safari'.")] string query,
        [Description("How many hits to return, 1 to 50. Default 12.")] int limit = 12)
    {
        return BmageSearchIndex.Search(query, limit);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageSetupGuide), Title = "Setup guide for one render mode",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets the complete wiring for adding Bit.Bmage to an app in one hosting model, as the real files: 'wasm' (Blazor Web App, InteractiveWebAssembly), 'server' (InteractiveServer), 'auto' (InteractiveAuto), 'standalone-wasm', or 'headless' (ASP.NET Core API, console app, worker - no browser). Call this before writing any setup code: which DI containers register the services, which registration to use, and which codecs exist at all all differ per host.")]
    public string GetBmageSetupGuide(
        [Description("The hosting model: wasm, server, auto, standalone-wasm or headless. Aliases such as 'InteractiveServer', 'console' or 'aspnetcore' are accepted.")] string renderMode)
    {
        return BmageSetupGuide.Get(renderMode)
            ?? $"'{renderMode}' is not a known hosting model. Use one of: {string.Join(", ", BmageSetupGuide.RenderModes)}.";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageRecipes), Title = "List the ready-made recipes",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists every ready-made Bit.Bmage pattern this server can hand out complete, with its id, its intent, where it runs (browser, server or anywhere) and the terms it covers - thumbnails, upload optimisation, avatars with a size budget, stripping GPS, screenshot annotation and redaction, batch processing, the editor, server endpoints and more. Use it to pick the id to pass to GetBmageRecipe; a recipe is usually the fastest route from a request to correct code.")]
    public BmageRecipeDto[] GetBmageRecipes()
    {
        return BmageRecipeCatalog.Summaries;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageRecipe), Title = "One recipe in full",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets one Bit.Bmage recipe in full: the Razor or C#, and the caveat that is not visible in the code - the buffer to dispose, the format that needs a fallback, the metadata that would leak, the call that cannot run during prerendering. Pass an id from GetBmageRecipes, e.g. 'avatar-crop', 'upload-optimise', 'strip-location', 'annotate-redact-screenshot', 'batch-processing', 'headless-upload-endpoint'.")]
    public BmageRecipeResultDto GetBmageRecipe(string id)
    {
        var recipe = BmageRecipeCatalog.Find(id);

        if (recipe is not null) return new BmageRecipeResultDto { Recipe = recipe };

        var ids = string.Join(", ", BmageRecipeCatalog.All.Select(entry => entry.Id));

        return new BmageRecipeResultDto
        {
            Message = $"There is no recipe called '{id}'. Available recipes: {ids}. " +
                      "Call SearchBmage with what you are trying to build if none of them fit."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageDocuments), Title = "List the guides",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists the Bit.Bmage guides - the README and every document under docs/ (getting started, architecture, pipelines, the editor, headless processing, formats, metadata and privacy, performance, security, browser support, extending, troubleshooting) - with a summary and the sections of each. Use it to pick the name and section to pass to GetBmageDocument.")]
    public BmageDocumentDto[] GetBmageDocuments()
    {
        return BmageSourceCatalog.Documents;
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageDocument), Title = "Read one guide or section",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets one Bit.Bmage guide as Markdown, or one section of it, with its code samples - e.g. name 'pipelines', 'formats', 'metadata-and-privacy', 'troubleshooting' or 'readme'; section 'The optimiser' or 'Quality and size budgets'. Sub-sections are included. Name and section matching ignore case and punctuation.")]
    public string GetBmageDocument(
        [Description("The guide's name from GetBmageDocuments, e.g. 'pipelines', 'headless' or 'readme'.")] string name,
        [Description("Optional. One heading of that guide, e.g. 'The six resize modes'. Omitted, the whole guide is returned.")] string? section = null)
    {
        var resolved = BmageSourceCatalog.ResolveDocumentName(name);

        if (resolved is null)
        {
            var names = string.Join(", ", BmageSourceCatalog.Documents.Select(document => $"'{document.Name}'"));

            return $"There is no Bit.Bmage guide called '{name}'. The guides are: {names}.";
        }

        if (string.IsNullOrWhiteSpace(section)) return Truncate(BmageSourceCatalog.GetDocument(resolved)!, document: resolved);

        var text = BmageSourceCatalog.GetDocumentSection(resolved, section);

        if (text is null)
        {
            var headings = string.Join(", ", BmageSourceCatalog.GetHeadings(resolved).Select(heading => $"'{heading.Heading}'"));

            return $"The '{resolved}' guide has no section called '{section}'. Its sections are: {headings}.";
        }

        return Truncate(text, document: resolved);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageApiList), Title = "List the public types",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists the public types of the three Bit.Bmage packages - pipelines, operations, codecs, options, metadata, drawing, annotations, editing, the browser services and the editor components - with the namespace to import, the package that ships each, its kind and a one-line summary. Narrow it with 'filter' (a package, a namespace, a kind or any part of a name) rather than reading all of it, then pass a name to GetBmageApiDetails.")]
    public BmageApiTypeDto[] GetBmageApiList(
        [Description("Optional. A package ('Bit.Bmage.Core', 'Bit.Bmage', 'Bit.Bmage.Editor'), a namespace ('Bit.Bmage.Annotations'), a kind ('Enum', 'Component', 'Record', 'Exception') or any substring of a type name ('Export'). Omitted, every type is returned.")] string? filter = null)
    {
        var types = BmageApiCatalog.TypeSummaries;

        if (string.IsNullOrWhiteSpace(filter)) return types;

        var term = filter.Trim();

        // A package or a kind is matched exactly: "Bit.Bmage" as a substring is every type there is,
        // and "Class" would pull in every "...Class..." name as well.
        var exact = types.Where(type => type.Package.Equals(term, StringComparison.OrdinalIgnoreCase)
                                     || type.Kind.Equals(term, StringComparison.OrdinalIgnoreCase)
                                     || type.Namespace.Equals(term, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (exact.Length > 0) return exact;

        // A filter that matches nothing returns nothing: falling back to the whole list would read as
        // a match.
        return [.. types.Where(type => type.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                                    || type.Namespace.Contains(term, StringComparison.OrdinalIgnoreCase))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageApiDetails), Title = "Full reference of one type",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets the full reference of one Bit.Bmage type - or of one member, written 'Type.Member' - read straight out of the shipped assemblies: its namespace and package, constructors, Blazor parameters and properties with their default values, methods with their full parameter lists, enum values, each with its XML documentation. Call it before using a type you are unsure about, e.g. 'ImagePipeline', 'ImageExportOptions', 'ImageLoader', 'ImageExporter', 'BmageEditor', 'MetadataPolicy', 'RedactionAnnotation', or 'ImagePipeline.Cover'. In an image library the defaults (quality 0.85, metadata Remove, PNG level 6) are the behaviour.")]
    public BmageApiDetailsResultDto GetBmageApiDetails(
        [Description("A type name such as 'ImagePipeline', a full name such as 'Bit.Bmage.Output.ImageExporter', or 'Type.Member' such as 'ImageExportOptions.MaxFileSize'.")] string typeName)
    {
        var details = BmageApiCatalog.GetTypeDetails(typeName) ?? BmageApiCatalog.GetMemberDetails(typeName);

        if (details is not null) return new BmageApiDetailsResultDto { Details = details };

        var probe = (typeName ?? string.Empty).Trim();
        var dot = probe.LastIndexOf('.');
        var typePart = dot > 0 ? probe[..dot] : probe;

        // "ImagePipeline.Teleport": the type exists, the member does not - say which, and list what it has.
        if (dot > 0 && BmageApiCatalog.GetTypeDetails(typePart) is { } owner)
        {
            return new BmageApiDetailsResultDto
            {
                Message = $"{owner.Name} has no member called '{probe[(dot + 1)..]}'. Its members are: " +
                          $"{string.Join(", ", owner.Members.Select(member => member.Name).Distinct(StringComparer.Ordinal))}."
            };
        }

        var candidates = BmageApiCatalog.Types
            .Where(type => type.Name.Contains(typePart, StringComparison.OrdinalIgnoreCase))
            .Select(type => type.Name)
            .Take(20)
            .ToArray();

        return new BmageApiDetailsResultDto
        {
            Message = candidates.Length > 0
                ? $"Bit.Bmage has no public type called '{typeName}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"Bit.Bmage has no public type called '{typeName}'. Call SearchBmage with what you need, or GetBmageApiList for the full list."
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageSourceFiles), Title = "List the demo source files",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Lists the working Bit.Bmage source files this server can hand out: every page of the demo site - one per feature, each a complete working example (avatar, upload optimisation, screenshot annotation, batch, the editor, headless use, metadata, formats, pipelines) - plus the layout, the shared components and the host wiring. Demo pages carry the route they are served at and the terms they cover. Use it to pick the path to pass to GetBmageSourceFile.")]
    public BmageSourceFileDto[] GetBmageSourceFiles(
        [Description("Optional. 'Demo page' for only the feature pages, 'Host' for the server wiring, 'Demo' for the layout and shared components, or any substring of a path, a title or a page's keywords. Omitted, every file is returned.")] string? filter = null)
    {
        var files = BmageSourceCatalog.SourceFiles;

        if (string.IsNullOrWhiteSpace(filter)) return files;

        var term = filter.Trim();

        return [.. files.Where(file => file.Kind.Equals(term, StringComparison.OrdinalIgnoreCase)
                                    || file.Path.Contains(term, StringComparison.OrdinalIgnoreCase)
                                    || (file.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                    || (file.Keywords?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }

    [HttpGet]
    [McpServerTool(Name = nameof(GetBmageSourceFile), Title = "Read one demo source file",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Gets one source file listed by GetBmageSourceFiles, verbatim - e.g. 'Demo/Client/Pages/Avatar.razor' for a complete, working avatar editor, or 'Demo/Server/Program.cs' for the host wiring. Long files run past what one answer carries; pass fromLine and toLine to read them a part at a time, using the line count GetBmageSourceFiles reports.")]
    public string GetBmageSourceFile(
        [Description("The path from GetBmageSourceFiles, e.g. 'Demo/Client/Pages/Avatar.razor'.")] string path,
        [Description("Optional 1-based first line to return. Omitted, the file is read from its start.")] int? fromLine = null,
        [Description("Optional 1-based last line to return, inclusive. Omitted, the file is read to its end.")] int? toLine = null)
    {
        var content = BmageSourceCatalog.GetSourceFile(path);

        if (content is null)
        {
            var candidates = BmageSourceCatalog.SourceFiles
                .Where(file => file.Path.Contains(path ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                            || Path.GetFileName(file.Path).Equals(Path.GetFileName(path ?? string.Empty), StringComparison.OrdinalIgnoreCase))
                .Select(file => file.Path)
                .Take(10)
                .ToArray();

            return candidates.Length > 0
                ? $"No source file at '{path}'. Did you mean: {string.Join(", ", candidates)}?"
                : $"No source file at '{path}'. Call GetBmageSourceFiles for the full list.";
        }

        if (fromLine is null && toLine is null) return Truncate(content, path);

        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        // Both ends are clamped rather than rejected: asking for lines 400-600 of a 380-line file is an
        // off-by-something, not a request worth refusing, and the header says what was returned.
        var first = Math.Clamp(fromLine ?? 1, 1, lines.Length);
        var last = Math.Clamp(toLine ?? lines.Length, first, lines.Length);

        var slice = Truncate(string.Join('\n', lines[(first - 1)..last]), path, first);

        return $"// {path}, lines {first}-{last} of {lines.Length}\n\n{slice}";
    }

    [HttpGet]
    [McpServerTool(Name = nameof(ReviewBmageCode), Title = "Review code that uses Bit.Bmage",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Reviews Razor, C#, a .csproj or a host page that uses Bit.Bmage and reports the mistakes that compile cleanly and then fail at runtime, leak or produce the wrong file: browser calls in OnInitialized that break prerendering, ImageBuffers never disposed, @inject ImageProcessor in a browser app, the wrong registration for the host, the editor's stylesheet missing or mislinked, no RunAOTCompilation, WebP/AVIF with no fallback or on a server, MetadataPolicy.Preserve leaking GPS, orientation lost, unhandled size budgets, the 500 KB IBrowserFile limit. Every finding names the line and the correction. Run it before calling Bmage code done.")]
    public BmageReviewDto ReviewBmageCode(
        [Description("The code to review: a component, a Program.cs, a .csproj, App.razor/index.html, or a fragment of any of them. Several files can be pasted together.")] string code)
    {
        var text = code ?? string.Empty;

        // Every rule is a pass over every line, so the reviewed body is bounded by the same limit as the
        // documents this server hands out. What was cut is reported rather than dropped: a review that
        // silently covered the first half of a file would read as a clean bill of health.
        if (text.Length <= MaxDocumentLength) return BmageCodeReview.Review(text);

        var review = BmageCodeReview.Review(text[..MaxDocumentLength]);

        return review with
        {
            Passed = false,
            Findings =
            [
                new BmageReviewFindingDto
                {
                    Severity = "Warning",
                    Rule = "code-too-long",
                    Message = $"Only the first {MaxDocumentLength} characters were reviewed; the code passed in is " +
                              $"{text.Length} characters long, so anything after that was not checked.",
                    Fix = "Review one component or file at a time, and call this tool once per piece."
                },
                .. review.Findings
            ]
        };
    }

    [HttpGet]
    [McpServerTool(Name = nameof(RunBmagePipeline), Title = "Run a pipeline on the real engine",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Runs a pipeline on the real Bit.Bmage engine on this server, against a generated test image (a photo with sensor grain, a flat-colour screenshot, or a transparent graphic), and reports what it really does: the output size, the encoded bytes per format and quality with the managed codecs, whether a MaxFileSize budget is reachable and at what quality, the operation list the optimiser actually runs, and how long it took with and without the optimiser. Use it to choose dimensions, formats, qualities and byte budgets on evidence before writing them into code. Example pipeline: 'ImagePipeline.Create().AutoOrient().MaxSize(1920, 1920).Sharpen(0.3f)'.")]
    public Task<BmagePipelineRunDto> RunBmagePipeline(
        [Description("The pipeline as the C# method chain, e.g. 'ImagePipeline.Create().AutoOrient().Cover(512, 512, Anchor.Top).Sharpen(0.3f)' - copied out of code verbatim, or without the 'ImagePipeline.Create()' prefix. Every ImagePipeline builder method is accepted with its real parameters, named arguments included; Rgba32 and Rectangle are written 'new Rgba32(24, 26, 31)' / '\"#18181f\"' and 'new Rectangle(0, 0, 200, 80)'.")] string pipeline,
        [Description("The generated input: 'photo' (default - a landscape with sensor grain, compresses like a photograph), 'screenshot' (flat colour, hard edges, text-like runs) or 'graphic' (a logo on a transparent background).")] string source = "photo",
        [Description("Width of the generated input in pixels. Default 2400; at most 4096, and 12 megapixels in all.")] int width = BmagePipelineLab.DefaultWidth,
        [Description("Height of the generated input in pixels. Default 1600.")] int height = BmagePipelineLab.DefaultHeight,
        [Description("Optional. Formats to encode the output in, comma-separated: jpeg, png, bmp - measured; webp, avif - browser codecs, reported with guidance rather than measured. Default 'jpeg, png'.")] string? formats = null,
        [Description("Optional. Qualities for the lossy formats, comma-separated in [0, 1], e.g. '0.6, 0.75, 0.85'. Default '0.7, 0.8, 0.9'.")] string? qualities = null,
        [Description("Optional. A file-size budget in bytes, e.g. 102400: runs ImageExportOptions.MaxFileSize's search for jpeg and png and reports the quality and dimensions it settles on, or why it cannot be met.")] long? maxFileSize = null,
        CancellationToken cancellationToken = default)
    {
        return BmagePipelineLab.RunAsync(pipeline, source, width, height, formats, qualities, maxFileSize, cancellationToken);
    }

    [HttpGet]
    [McpServerTool(Name = nameof(CompareBmageMetadataPolicies), Title = "Compare metadata export policies",
                   ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Encodes a camera photo carrying a GPS position, a serial number, the owner's name, camera, lens, ISO, a date and a copyright through the real Bit.Bmage encoder under each MetadataPolicy (Remove, StripSensitive, Preserve), decodes every file again, and reports which fields survived - so a privacy decision rests on what the exported file contains rather than on what a policy name suggests. Pass format 'jpeg' (default), 'png' or 'bmp'.")]
    public Task<BmageMetadataComparisonDto> CompareBmageMetadataPolicies(
        [Description("The export format: 'jpeg' (default), 'png' or 'bmp'. WebP behaves as JPEG does; it is encoded in the browser, which this server does not have.")] string format = "jpeg",
        CancellationToken cancellationToken = default)
    {
        return BmagePipelineLab.CompareMetadataPoliciesAsync(format, cancellationToken);
    }

    /// <summary>
    /// Everything this server exposes over MCP - its tools, prompts and resources - read off the
    /// attributes that declare them.
    /// <para>
    /// Deliberately not an MCP tool: an agent already gets this from the protocol's own
    /// <c>tools/list</c>. It exists for the /mcp-server demo page, which documents the server to a
    /// human reader. Reflecting over the registrations rather than restating them means the page
    /// cannot describe a tool that no longer exists, or miss one that was just added.
    /// </para>
    /// </summary>
    [HttpGet]
    public BmageMcpCatalogDto GetMcpCatalog()
    {
        return new BmageMcpCatalogDto
        {
            Tools = [.. typeof(McpController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method,
                                   Tool: method.GetCustomAttribute<McpServerToolAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Tool is not null)
                .Select(entry => new BmageMcpMemberDto
                {
                    Name = entry.Tool!.Name ?? entry.Method.Name,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Where(IsArgument).Select(FormatParameter)]
                })
                .OrderBy(tool => tool.Name, StringComparer.Ordinal)],

            Prompts = [.. typeof(McpPrompts)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Method: method,
                                   Prompt: method.GetCustomAttribute<McpServerPromptAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Prompt is not null)
                .Select(entry => new BmageMcpMemberDto
                {
                    Name = entry.Prompt!.Name ?? entry.Method.Name,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = [.. entry.Method.GetParameters().Where(IsArgument).Select(FormatParameter)]
                })
                .OrderBy(prompt => prompt.Name, StringComparer.Ordinal)],

            Resources = [.. typeof(McpResources)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Select(method => (Resource: method.GetCustomAttribute<McpServerResourceAttribute>(),
                                   Description: method.GetCustomAttribute<DescriptionAttribute>()))
                .Where(entry => entry.Resource is not null)
                .Select(entry => new BmageMcpMemberDto
                {
                    Name = entry.Resource!.UriTemplate ?? entry.Resource.Name ?? string.Empty,
                    Description = entry.Description?.Description ?? string.Empty,
                    Parameters = []
                })
                .OrderBy(resource => resource.Name, StringComparer.Ordinal)]
        };
    }

    /// <summary>A parameter the caller supplies - not the cancellation token the transport supplies.</summary>
    private static bool IsArgument(ParameterInfo parameter) => parameter.ParameterType != typeof(CancellationToken);

    /// <summary>A tool or prompt parameter, as "name: type" with "?" for the optional ones.</summary>
    private static string FormatParameter(ParameterInfo parameter)
    {
        var type = BmageApiCatalog.FriendlyName(parameter.ParameterType);

        return parameter.HasDefaultValue ? $"{parameter.Name}?: {type}" : $"{parameter.Name}: {type}";
    }

    /// <summary>
    /// A renamed README heading must not silently leave a blank gap in the overview - the agent is
    /// told where the text went instead.
    /// </summary>
    private static void AppendSection(StringBuilder builder, string document, string heading)
    {
        builder.AppendLine(BmageSourceCatalog.GetDocumentSection(document, heading)
                           ?? $"_The \"{heading}\" section of the {document} guide was not found in this build. " +
                              "Call GetBmageDocuments for the sections it does have._")
               .AppendLine();
    }

    /// <summary>
    /// Bounds one document. A cut that says only that it happened leaves the caller to guess whether
    /// the rest matters and how to reach it, so the notice carries the call that reads on.
    /// </summary>
    /// <param name="continuation">
    /// What to name instead of the default follow-up call. A resource is read by clients that need
    /// not have this server's tools available, so what one of them names has to be another resource.
    /// </param>
    internal static string Truncate(string text, string? sourcePath = null, int firstLine = 1, string? continuation = null, string? document = null)
    {
        if (text.Length <= MaxDocumentLength) return text;

        var kept = text[..MaxDocumentLength];

        // Where the reader got to, in the units the follow-up call is written in.
        var line = firstLine + kept.Count(character => character == '\n');

        var howToContinue = continuation
            ?? (sourcePath is not null
                ? $"Call GetBmageSourceFile(path: \"{sourcePath}\", fromLine: {line}) to read on from there."
                : $"Call GetBmageDocument(name: \"{document ?? BmageSourceCatalog.ReadmeName}\", section: \"...\") with one of its headings to read a section at a time.");

        return $"{kept}\n\n[truncated at line {line} - the full text is longer than " +
               $"{MaxDocumentLength} characters. {howToContinue}]";
    }
}
