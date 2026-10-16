using Bit.Bmage.Demo.Server.Dtos;

namespace Bit.Bmage.Demo.Server.Services;

/// <summary>
/// The image features people actually ask for, written out in full.
/// <para>
/// The API reference answers "what does this parameter do"; a recipe answers "what do I write for
/// the thing I want", which is the question that arrives. Each one is short, complete and correct
/// against the current API - most are lifted from the demo page that runs them - and carries the
/// caveat that is not visible in the code: the buffer that has to be disposed, the format that
/// needs a fallback, the metadata that would otherwise leak, the call that cannot happen during
/// prerendering. Those caveats are where hand-written image code goes wrong, and none of them
/// shows up as a compiler error.
/// </para>
/// </summary>
public static class BmageRecipeCatalog
{
    /// <summary>Every recipe, code included.</summary>
    public static readonly BmageRecipeDto[] All =
    [
        new()
        {
            Id = "thumbnail",
            Title = "Thumbnail from an upload",
            Intent = "Make a small preview of a picked image and show it on the page.",
            Keywords = "thumbnail preview small resize maxsize upload inputfile object url show image",
            RunsIn = "Browser",
            SeeAlso = "/headless",
            Code = """
                @implements IAsyncDisposable
                @inject ImageLoader Loader
                @inject ImageExporter Exporter

                <InputFile OnChange="OnChange" accept="image/*" />

                @if (_url is not null)
                {
                    <img src="@_url" alt="Thumbnail" />
                }

                @code {
                    // A pipeline is an immutable value: build it once, reuse it for every image.
                    private static readonly ImagePipeline Thumbnail = ImagePipeline.Create()
                        .AutoOrient()
                        .MaxSize(320, 320)      // shrink to fit, never enlarge
                        .Sharpen(0.3f);         // recover the bite downscaling takes away

                    private string? _url;

                    private async Task OnChange(InputFileChangeEventArgs e)
                    {
                        using var image = await Loader.LoadAsync(e.File);
                        using var thumb = await Thumbnail.ExecuteAsync(image);

                        var encoded = await Exporter.EncodeWithFallbackAsync(
                            thumb, [ImageFormat.WebP, ImageFormat.Jpeg],
                            new ImageExportOptions { Quality = 0.8 });

                        if (_url is not null) await Exporter.ReleaseObjectUrlAsync(_url);
                        _url = await Exporter.CreateObjectUrlAsync(encoded);
                    }

                    public async ValueTask DisposeAsync()
                    {
                        if (_url is not null) await Exporter.ReleaseObjectUrlAsync(_url);
                    }
                }
                """,
            Notes = "Both buffers are yours: `using` returns their pooled memory at once (a 12 MP photo is 48 MB). " +
                    "Show the result through an object URL, not ToDataUrl() - base64 inflates it by a third and pushes it " +
                    "through the DOM as a string - and release the previous URL when you replace it. MaxSize never " +
                    "enlarges, so a small image passes through untouched. Call RunBmagePipeline with this pipeline to see " +
                    "what it costs and how large the result is in each format."
        },
        new()
        {
            Id = "upload-optimise",
            Title = "Shrink an upload before it is sent",
            Intent = "Resize and re-encode a photo on the user's device so the server never receives the full-size original.",
            Keywords = "upload optimise optimize compress shrink reencode before send httpclient multipart webp jpeg bandwidth maxsize strip metadata",
            RunsIn = "Browser",
            SeeAlso = "/optimize",
            Code = """
                @inject ImageLoader Loader
                @inject ImageExporter Exporter
                @inject HttpClient Http

                <InputFile OnChange="UploadAsync" accept="image/*" />

                @code {
                    private static readonly ImagePipeline Shrink = ImagePipeline.Create()
                        .AutoOrient()
                        .MaxSize(1920, 1920)
                        .Sharpen(0.3f);

                    private async Task UploadAsync(InputFileChangeEventArgs e)
                    {
                        // A hint to the browser decoder: it may downscale while decoding, so the
                        // full 48 MP pixels of a large photo never exist in memory at all.
                        using var image = await Loader.LoadAsync(e.File,
                            new DecodeOptions { MaxSize = new System.Drawing.Size(3840, 3840) });

                        using var shrunk = await Shrink.ExecuteAsync(image);

                        var encoded = await Exporter.EncodeWithFallbackAsync(
                            shrunk, [ImageFormat.WebP, ImageFormat.Jpeg],
                            new ImageExportOptions
                            {
                                Quality = 0.82,
                                Metadata = MetadataPolicy.Remove,   // the default, written out
                            });

                        using var content = new MultipartFormDataContent();
                        var file = new ByteArrayContent(encoded.Data);
                        file.Headers.ContentType = new(encoded.MimeType);
                        content.Add(file, "file", Path.GetFileNameWithoutExtension(e.File.Name) + encoded.Extension);

                        await Http.PostAsync("api/photos", content);
                    }
                }
                """,
            Notes = "The server has to accept whichever format came back: EncodeWithFallbackAsync reports the one it " +
                    "used on encoded.Format, and the file name above carries the matching extension. Leave metadata on " +
                    "Remove for uploads - the phone wrote GPS into the original. Use RunBmagePipeline with maxFileSize " +
                    "or a list of qualities to choose the quality on measured bytes rather than habit."
        },
        new()
        {
            Id = "avatar-crop",
            Title = "Avatar: smart square or circle crop under a byte budget",
            Intent = "Crop a profile picture around the person, resize it, and keep the file under a size limit.",
            Keywords = "avatar profile picture circle circular square smart crop face subject size budget maxfilesize webp png transparency mask",
            RunsIn = "Browser",
            SeeAlso = "/avatar",
            Code = """
                @inject ImageLoader Loader
                @inject ImageExporter Exporter

                @code {
                    private async Task<EncodedImage?> MakeAvatarAsync(IBrowserFile file, int size, bool circular, int budgetKb)
                    {
                        using var source = await Loader.LoadAsync(file);

                        // SmartCrop scores detail, colour and skin tones to find the subject, instead of
                        // assuming it is in the middle - centre-cropping is how avatars lose the top of a head.
                        using var avatar = await ImagePipeline.Create()
                            .AutoOrient()
                            .SmartCrop(AspectRatio.Square)
                            .Resize(size, size, ResizeMode.Stretch)
                            .Sharpen(0.25f)
                            .ExecuteAsync(source);

                        if (circular) ApplyCircularMask(avatar);

                        try
                        {
                            // A circle needs alpha: JPEG would show the corners, so fall back to PNG instead.
                            return await Exporter.EncodeWithFallbackAsync(
                                avatar,
                                circular ? [ImageFormat.WebP, ImageFormat.Png] : [ImageFormat.WebP, ImageFormat.Jpeg],
                                new ImageExportOptions
                                {
                                    Quality = 0.9,
                                    MaxFileSize = budgetKb * 1024L,   // bisects on quality, then shrinks if allowed
                                    MinQuality = 0.5,
                                    Metadata = MetadataPolicy.Remove,
                                });
                        }
                        catch (ImageEncodeException)
                        {
                            return null;   // the budget is impossible for this image even at MinQuality
                        }
                    }

                    // Clears everything outside the inscribed circle, with a one pixel feather.
                    private static void ApplyCircularMask(ImageBuffer image)
                    {
                        var cx = image.Width / 2f;
                        var cy = image.Height / 2f;
                        var radius = MathF.Min(cx, cy);
                        for (var y = 0; y < image.Height; y++)
                        {
                            var row = image.GetRow(y);
                            var dy = y + 0.5f - cy;
                            for (var x = 0; x < image.Width; x++)
                            {
                                var dx = x + 0.5f - cx;
                                var distance = MathF.Sqrt(dx * dx + dy * dy);
                                if (distance <= radius - 1) continue;
                                var coverage = Math.Clamp(radius - distance, 0f, 1f);
                                row[x] = row[x].WithAlpha((byte)(row[x].A * coverage));
                            }
                        }
                    }
                }
                """,
            Notes = "SmartCrop is a heuristic, not face recognition: when you have a detector, pass its rectangle to " +
                    "SmartCrop.FindCrop(image, AspectRatio.Square, mustInclude: face) and crop to the result's Rectangle. " +
                    "MaxFileSize throws ImageEncodeException rather than handing back an oversized file, and the quality " +
                    "it settled on is on encoded.Quality. PNG ignores Quality, so a circular avatar that falls back to PNG " +
                    "can only meet the budget by shrinking (the default QualityThenDimensions strategy does that). " +
                    "RunBmagePipeline with maxFileSize shows whether a budget is reachable before you ship it."
        },
        new()
        {
            Id = "strip-location",
            Title = "Keep camera details, remove the location",
            Intent = "Publish photos with their camera and exposure data but without GPS, serial numbers or the owner's name.",
            Keywords = "strip gps location exif metadata privacy stripsensitive remove preserve camera serial owner geotag warn user",
            RunsIn = "Anywhere",
            SeeAlso = "/metadata",
            Code = """
                @inject ImageLoader Loader
                @inject ImageExporter Exporter

                @if (_location is not null)
                {
                    <p class="warning">
                        This photo records where it was taken (@_location). It will be removed when you save.
                    </p>
                }

                @code {
                    private string? _location;

                    private async Task<EncodedImage> PrepareAsync(IBrowserFile file)
                    {
                        using var image = await Loader.LoadAsync(file);

                        // Tell the user what is in their file before it is published.
                        _location = image.Metadata?.Exif?.Location?.ToString();

                        return await Exporter.EncodeAsync(image, new ImageExportOptions
                        {
                            Format = ImageFormat.Jpeg,
                            Quality = 0.88,
                            // Keeps camera, lens, exposure, dates, copyright and the ICC profile; drops GPS,
                            // serial numbers, owner name, user comments, maker notes, unique ids, the
                            // embedded thumbnail and all of XMP.
                            Metadata = MetadataPolicy.StripSensitive,
                        });
                    }
                }
                """,
            Notes = "MetadataPolicy.Remove is the default and the right choice for anything public (avatars, uploads). " +
                    "StripSensitive is for galleries that want to show camera data. Preserve keeps EVERYTHING the format " +
                    "can carry, GPS included - never use it on user photos. All three reset the orientation tag, because " +
                    "the exported pixels are already upright. CompareBmageMetadataPolicies encodes a GPS-tagged photo " +
                    "under each policy with the real encoder and reads the file back, if you want to see it happen."
        },
        new()
        {
            Id = "annotate-redact-screenshot",
            Title = "Paste a screenshot, annotate it, redact it",
            Intent = "Let users paste a screenshot, point at the problem with arrows and numbered steps, and black out secrets.",
            Keywords = "screenshot annotate paste clipboard ctrl+v arrow step number redact redaction blackout bug report editor addannotationasync",
            RunsIn = "Browser",
            SeeAlso = "/annotate",
            Code = """
                @implements IAsyncDisposable
                @inject ImageLoader Loader

                <div style="height: 70vh">
                    <BmageEditor @ref="_editor" ExportOptions="ExportOptions" />
                </div>

                <button @onclick="MarkUpAsync">Add a worked example</button>

                @code {
                    private static readonly ImageExportOptions ExportOptions = new()
                    {
                        Format = ImageFormat.Png,              // screenshots are flat colour: PNG
                        Metadata = MetadataPolicy.Remove,
                    };

                    private BmageEditor? _editor;
                    private IAsyncDisposable? _paste;

                    protected override async Task OnAfterRenderAsync(bool firstRender)
                    {
                        if (!firstRender) return;

                        // Ctrl+V anywhere on the page. The paste event carries the image, so no clipboard
                        // permission prompt; the editor takes ownership of the buffer it is handed.
                        _paste = await Loader.ListenForPasteAsync(image => _editor!.LoadImageAsync(image));
                    }

                    private async Task MarkUpAsync()
                    {
                        if (_editor?.Session is not { } session) return;
                        var w = session.OutputSize.Width;
                        var h = session.OutputSize.Height;

                        // Solid replaces the pixels underneath; the secret is gone from the exported file.
                        await _editor.AddAnnotationAsync(new RedactionAnnotation
                        {
                            Rectangle = new System.Drawing.RectangleF(w * 0.06f, h * 0.10f, w * 0.34f, h * 0.06f),
                            Mode = RedactionMode.Solid,
                        }, "Redact");

                        await _editor.AddAnnotationAsync(new ArrowAnnotation
                        {
                            Start = new Vector2(w * 0.30f, h * 0.78f),
                            End = new Vector2(w * 0.56f, h * 0.46f),
                            Style = ShapeStyle.Default with { Stroke = new StrokeStyle { Width = MathF.Max(4, w / 220f) } },
                        }, "Arrow");

                        await _editor.AddAnnotationAsync(new StepAnnotation
                        {
                            Center = new Vector2(w * 0.26f, h * 0.82f),
                            Number = 1,
                        }, "Step");
                    }

                    public async ValueTask DisposeAsync()
                    {
                        if (_paste is not null) await _paste.DisposeAsync();
                    }
                }
                """,
            Notes = "Prefer RedactionMode.Solid: a strong blur or pixelation is irreversible in practice, but text has " +
                    "been recovered from both when the possible inputs are constrained (account numbers). Redactions always " +
                    "render before other annotations, so an arrow on top is never blurred away. Redaction is about pixels; " +
                    "metadata is a separate leak, which the export policy handles. The paste listener needs a browser, so it " +
                    "is attached in OnAfterRenderAsync and disposed with the page. Annotation coordinates are image pixels, " +
                    "not screen pixels. Step numbers and text annotations need a text rasterizer: set the editor's " +
                    "TextRasterizer, or register a font with the FontTextRasterizer service, or the editor says it has none."
        },
        new()
        {
            Id = "redact-on-server",
            Title = "Redact or annotate on the server",
            Intent = "Black out a region and draw an arrow on an image with no browser and no editor.",
            Keywords = "redact redaction server headless annotate arrow documentrenderer imagedocument pixelate region blackout",
            RunsIn = "Anywhere",
            SeeAlso = "/headless",
            Code = """
                using System.Drawing;
                using System.Numerics;
                using Bit.Bmage.Annotations;
                using Bit.Bmage.Codecs;
                using Bit.Bmage.Drawing;
                using Bit.Bmage.Editing;
                using Bit.Bmage.Metadata;
                using Bit.Bmage.Pipeline;

                public static async Task<EncodedImage> RedactAsync(ImageProcessor processor, byte[] bytes, RectangleF secret)
                {
                    using var source = await processor.DecodeAsync(bytes);

                    // An edit is a value: the pixel operations plus the annotations, applied on render.
                    var document = ImageDocument.Create(source)
                        .AddAnnotation(new RedactionAnnotation { Rectangle = secret, Mode = RedactionMode.Solid })
                        .AddAnnotation(new ArrowAnnotation
                        {
                            Start = new Vector2(secret.Right + 120, secret.Bottom + 80),
                            End = new Vector2(secret.Right + 8, secret.Bottom + 4),
                            Style = ShapeStyle.Default with { Stroke = new StrokeStyle { Width = 6 } },
                        });

                    using var rendered = await new DocumentRenderer().RenderAsync(document, source);

                    return await processor.EncodeAsync(rendered, new ImageExportOptions
                    {
                        Format = ImageFormat.Png,
                        Metadata = MetadataPolicy.Remove,
                    });
                }

                // Or, for a pixel-only redaction with no annotation model at all:
                // ImagePipeline.Create().Pixelate(14, new Rectangle(40, 120, 300, 40))
                """,
            Notes = "The source buffer is never modified - rendering returns a new buffer you own. ImageProcessor comes " +
                    "from AddBitBmageHeadlessServices on a server (or new ImageProcessor() in a console app); in a browser " +
                    "app get it from ImageProcessorFactory.GetAsync() instead. Text annotations need a text rasterizer: " +
                    "pass new DocumentRenderer(fontTextRasterizer) with a registered TrueType font. A document is " +
                    "serialisable, so the browser can send the edit and the server can render it against the original."
        },
        new()
        {
            Id = "batch-processing",
            Title = "Process many images with progress and cancellation",
            Intent = "Run one pipeline over a set of files with bounded concurrency, live progress, and a per-file outcome.",
            Keywords = "batch bulk many files multiple concurrency progress cancel cancellation errors processbatchasync batchoptions",
            RunsIn = "Browser",
            SeeAlso = "/batch",
            Code = """
                @implements IDisposable
                @inject ImageProcessorFactory Processors

                <InputFile OnChange="RunAsync" multiple accept="image/*" />
                <button @onclick="() => _cts?.Cancel()">Stop</button>
                <p>@_completed of @_total</p>

                @code {
                    private CancellationTokenSource? _cts;
                    private int _completed, _total;

                    private async Task RunAsync(InputFileChangeEventArgs e)
                    {
                        _cts = new CancellationTokenSource();
                        var files = e.GetMultipleFiles(maximumFileCount: 200);
                        _total = files.Count;

                        var items = new List<BatchItem>(files.Count);
                        foreach (var file in files)
                        {
                            // IBrowserFile refuses anything over 500 KB unless told otherwise.
                            await using var stream = file.OpenReadStream(maxAllowedSize: 64 * 1024 * 1024, _cts.Token);
                            using var memory = new MemoryStream();
                            await stream.CopyToAsync(memory, _cts.Token);
                            items.Add(new BatchItem(memory.ToArray(), file.Name));
                        }

                        // The browser's codecs are probed once, asynchronously, so the processor comes from a factory.
                        var processor = await Processors.GetAsync(_cts.Token);

                        var result = await processor.ProcessBatchAsync(items,
                            ImagePipeline.Create().AutoOrient().MaxSize(1600, 1600),
                            new BatchOptions
                            {
                                MaxConcurrency = 2,          // each worker holds a decoded image
                                StopOnFirstError = false,    // one bad file must not lose the rest
                                ExportOptions = new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.82 },
                                Progress = new Progress<BatchProgress>(p =>
                                {
                                    _completed = p.Completed;
                                    InvokeAsync(StateHasChanged);
                                }),
                            }, _cts.Token);

                        foreach (var item in result.Failed) Console.WriteLine($"{item.FileName}: {item.Error!.Message}");
                    }

                    public void Dispose()
                    {
                        _cts?.Cancel();
                        _cts?.Dispose();
                    }
                }
                """,
            Notes = "MaxConcurrency is a memory dial as much as a speed dial: four workers on 24 MP photos is about 400 MB " +
                    "of live pixels, which kills a phone's tab. Two is the safe default in a browser; on a server size it " +
                    "against memory, not cores alone. Every item comes back with its own outcome (result.Succeeded, " +
                    "result.Failed); StopOnFirstError is opt-in. JPEG is used here because it encodes everywhere - for WebP " +
                    "check the capabilities first, as the batch options take one fixed format."
        },
        new()
        {
            Id = "editor-basic",
            Title = "The editor component, minimal",
            Intent = "Drop the full image editor on a page and save what the user made.",
            Keywords = "editor bmageeditor component crop rotate adjust filter draw save download export onexported height stylesheet",
            RunsIn = "Browser",
            SeeAlso = "/editor",
            Code = """
                @* App.razor / index.html: <link rel="stylesheet" href="_content/Bit.Bmage.Editor/bit-bmage-editor.css" /> *@

                <div style="height: 70vh">
                    <BmageEditor @ref="_editor"
                                 ExportOptions="_export"
                                 OnImageLoaded="_ => StateHasChanged()"
                                 OnExported="OnExported" />
                </div>

                <button disabled="@(_editor?.Session is null)" @onclick="SaveAsync">Save</button>

                @code {
                    private BmageEditor? _editor;

                    private readonly ImageExportOptions _export = new()
                    {
                        Format = ImageFormat.Jpeg,
                        Quality = 0.9,
                        Metadata = MetadataPolicy.Remove,
                    };

                    private Task SaveAsync() => _editor!.DownloadAsync("edited");

                    private void OnExported(EncodedImage image)
                    {
                        // Also raised when the user exports from the editor's own panel.
                    }
                }
                """,
            Notes = "The editor fills its container, so the container needs a height - one with none collapses the editor " +
                    "to nothing. The stylesheet is the one thing the package asks of its host; without it the editor " +
                    "renders unstyled with no error. The editor can load an image through its own drop zone while your " +
                    "Image parameter is still null, so handle OnImageLoaded if page buttons depend on an image. Large " +
                    "images are edited through a proxy (2048px by default) and replayed at full resolution on export."
        },
        new()
        {
            Id = "editor-customise",
            Title = "Editor with a custom toolbar, theme, language and direction",
            Intent = "Replace the editor's toolbar with your own buttons, translate its labels, theme it and support right-to-left.",
            Keywords = "editor customise customize toolbar context bmageeditorcontext theme dark css variables localise localization strings translation rtl direction",
            RunsIn = "Browser",
            SeeAlso = "/editor",
            Code = """
                <div style="height: 70vh">
                    <BmageEditor Strings="_german" Direction="ltr" Class="my-editor"
                                 MaxProxyDimension="3000"
                                 OnDocumentChanged="doc => _document = doc">
                        <Toolbar Context="editor">
                            <div class="my-toolbar">
                                <button aria-pressed="@(editor.Tool == EditorToolKind.Crop ? "true" : "false")"
                                        @onclick="() => editor.SetTool(EditorToolKind.Crop)">@editor.Strings.Crop</button>
                                <button @onclick="() => editor.RotateAsync(90)">⟳</button>
                                <button disabled="@(!editor.CanUndo)" @onclick="editor.Undo">@editor.Strings.Undo</button>
                                <button disabled="@(!editor.CanRedo)" @onclick="editor.Redo">@editor.Strings.Redo</button>
                                <span>@editor.OutputSize.Width × @editor.OutputSize.Height</span>
                                <button @onclick='() => editor.DownloadAsync("bild")'>@editor.Strings.Download</button>
                            </div>
                        </Toolbar>
                    </BmageEditor>
                </div>

                @code {
                    private ImageDocument? _document;

                    // Every label is a property on a record: override only the words you care about.
                    private static readonly BmageEditorStrings _german = BmageEditorStrings.Default with
                    {
                        Crop = "Zuschneiden",
                        Undo = "Rückgängig",
                        Redo = "Wiederholen",
                        Download = "Herunterladen",
                        DropImageHere = "Bild hier ablegen oder klicken zum Auswählen",
                        FormatFileSize = bytes => $"{bytes / 1024.0:0.#} KB",
                    };
                }

                @* CSS: every colour and metric is a custom property on .bmg-editor
                .my-editor {
                    --bmg-accent: #7c3aed;
                    --bmg-bg: #0f0f12;
                    --bmg-text: #eeeef2;
                    --bmg-radius: 4px;
                    --bmg-panel-width: 320px;
                }
                *@
                """,
            Notes = "Toolbar, Panel and Footer each receive a BmageEditorContext with every action the built-in chrome has, " +
                    "so a custom toolbar loses nothing. For a slider of your own, call SetAdjustmentAsync(op, label, " +
                    "transient: true) while dragging and once without it on release, so history gets one entry rather than " +
                    "hundreds - and handle oninput (debounced), not onchange alongside oninput. Use bmg-theme-dark or " +
                    "bmg-theme-light in Class to force a theme; otherwise it follows prefers-color-scheme. Direction=\"rtl\" " +
                    "mirrors the panel, crop handles and toolbar. The document is a value (pipeline + annotations) you can " +
                    "store and replay against the original."
        },
        new()
        {
            Id = "headless-upload-endpoint",
            Title = "Server-side thumbnail endpoint (ASP.NET Core)",
            Intent = "Accept an uploaded image in an API, validate it, and return a JPEG thumbnail - no browser involved.",
            Keywords = "server headless aspnetcore asp.net minimal api endpoint upload iformfile thumbnail identify limits addbitbmageheadlessservices imageprocessor",
            RunsIn = "Server",
            SeeAlso = "/hosting",
            Code = """
                using Bit.Bmage;
                using Bit.Bmage.Codecs;
                using Bit.Bmage.Pipeline;

                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddBitBmageHeadlessServices(options =>
                {
                    options.Limits = ImageLimits.Default with { MaxPixels = 30_000_000 };
                });

                var app = builder.Build();

                app.MapPost("/thumbnail", async (IFormFile file, ImageProcessor processor, CancellationToken cancellationToken) =>
                {
                    if (file.Length > 20 * 1024 * 1024) return Results.BadRequest("Too large.");

                    using var buffer = new MemoryStream();
                    await file.CopyToAsync(buffer, cancellationToken);
                    var bytes = buffer.ToArray();

                    // Reject before decoding anything: Identify reads the header only.
                    var info = processor.Identify(bytes);
                    if (info is null) return Results.BadRequest("Not a supported image.");
                    if ((long)info.Width * info.Height > 30_000_000) return Results.BadRequest("Too large.");

                    try
                    {
                        var thumbnail = await processor.ProcessAsync(
                            bytes,
                            ImagePipeline.Create().AutoOrient().MaxSize(400, 400),
                            new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.8 },
                            cancellationToken: cancellationToken);

                        return Results.File(thumbnail.Data, thumbnail.MimeType);
                    }
                    catch (ImageDecodeException)
                    {
                        return Results.BadRequest("That file could not be read as an image.");
                    }
                }).DisableAntiforgery();

                app.Run();
                """,
            Notes = "Only the managed codecs exist on a server: PNG, JPEG, BMP and GIF decode; PNG, JPEG and BMP encode. " +
                    "WebP and AVIF are browser codecs and throw ImageCapabilityException here - if the client wants WebP, " +
                    "encode it in the browser before upload (recipe upload-optimise). An upload is attacker-chosen input to " +
                    "your decoder: keep limits tight, Identify first, and treat ImageDecodeException as an expected outcome. " +
                    "AddBitBmageHeadlessServices registers ImageProcessor directly; it registers none of the browser services."
        },
        new()
        {
            Id = "console-batch",
            Title = "Console app: resize a folder of photos",
            Intent = "Shrink and re-encode every image in a folder from a command-line tool or a background worker.",
            Keywords = "console worker background folder directory batch resize command line tool no di headless imageprocessor",
            RunsIn = "Server",
            Code = """
                using Bit.Bmage;
                using Bit.Bmage.Codecs;
                using Bit.Bmage.Pipeline;

                var processor = new ImageProcessor();   // the managed codecs, no DI needed
                Directory.CreateDirectory("out");

                var items = Directory.EnumerateFiles("photos")
                    .Where(path => path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    .Select(path => new BatchItem(File.ReadAllBytes(path), Path.GetFileName(path)));

                var result = await processor.ProcessBatchAsync(items,
                    ImagePipeline.Create().AutoOrient().MaxSize(1600, 1600).Sharpen(0.3f),
                    new BatchOptions
                    {
                        MaxConcurrency = Math.Max(1, Environment.ProcessorCount / 2),
                        ExportOptions = new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.82 },
                    });

                foreach (var item in result.Succeeded)
                    File.WriteAllBytes(Path.Combine("out", Path.ChangeExtension(item.FileName!, item.Output!.Extension)), item.Output.Data);

                foreach (var item in result.Failed)
                    Console.Error.WriteLine($"{item.FileName}: {item.Error!.Message}");

                Console.WriteLine($"{result.SuccessCount} of {result.Results.Count}: {result.TotalInputBytes:N0} -> {result.TotalOutputBytes:N0} bytes in {result.Duration.TotalSeconds:0.0}s");
                """,
            Notes = "The engine is the same one the browser runs, so a pipeline tested here behaves identically there. " +
                    "Default export metadata is Remove; GPS does not survive. GIF decodes its first frame only; SVG is " +
                    "refused. Reading every file up front is fine for a folder; for thousands of large files, process them " +
                    "in chunks so the inputs are not all held at once."
        },
        new()
        {
            Id = "product-images",
            Title = "Product images: every size, never cropped",
            Intent = "Generate catalogue images in several sizes on a consistent background without cutting off the product.",
            Keywords = "product ecommerce catalogue shop sizes variants background padding contain fit letterbox square thumbnails center",
            RunsIn = "Anywhere",
            SeeAlso = "/product",
            Code = """
                // Fit, never Cover: a product must never be cropped. It is scaled into the padded
                // area and centred on a canvas of the catalogue background.
                public static async Task<List<EncodedImage>> ProductVariantsAsync(
                    ImageBuffer decodedOnce, ImageExporter exporter, Rgba32 background, double padding = 0.06)
                {
                    var results = new List<EncodedImage>();

                    foreach (var size in new[] { 1024, 512, 256 })
                    {
                        var inner = (int)(size * (1 - padding * 2));

                        using var fitted = await ImagePipeline.Create()
                            .AutoOrient()
                            .Resize(inner, inner, ResizeMode.Fit)
                            .Sharpen(0.25f)
                            .ExecuteAsync(decodedOnce);

                        using var canvas = ImageCanvas.Create(size, size, background);
                        canvas.DrawImage(fitted, new System.Drawing.RectangleF(
                            (size - fitted.Width) / 2f, (size - fitted.Height) / 2f, fitted.Width, fitted.Height));

                        using var variant = canvas.Snapshot();
                        results.Add(await exporter.EncodeWithFallbackAsync(variant,
                            [ImageFormat.WebP, ImageFormat.Jpeg], new ImageExportOptions { Quality = 0.85 }));
                    }

                    return results;
                }

                // One call does the same when the padding can be the background colour:
                // ImagePipeline.Create().Resize(512, 512, ResizeMode.Contain, background: Rgba32.White)
                """,
            Notes = "Decode once and derive every size from the original, rather than shrinking the previous variant: each " +
                    "resample from already-downscaled pixels loses detail. ResizeMode.Contain pads to the exact canvas in " +
                    "one step; Cover would crop; Max never enlarges a small source. On a server swap ImageExporter for " +
                    "ImageProcessor.EncodeAsync with JPEG or PNG - WebP is browser-only."
        },
        new()
        {
            Id = "social-card",
            Title = "Social image with a caption",
            Intent = "Compose a photo to a platform's exact canvas (Open Graph, Instagram, story) and burn in a caption.",
            Keywords = "social open graph og image instagram story caption text overlay compose canvas cover scrim imagecanvas browsertextrasterizer drawtext",
            RunsIn = "Browser",
            SeeAlso = "/social",
            Code = """
                @inject ImageExporter Exporter
                @inject BrowserTextRasterizer Text

                @code {
                    private async Task<EncodedImage> ComposeAsync(ImageBuffer source, string caption)
                    {
                        // Cover fills the canvas exactly, cropping the overflow rather than distorting.
                        using var composed = await ImagePipeline.Create()
                            .AutoOrient()
                            .Cover(1200, 630)
                            .Saturation(0.12f)
                            .Vignette(0.25f)
                            .ExecuteAsync(source);

                        using var canvas = ImageCanvas.FromBuffer(composed);
                        var band = composed.Height * 0.28f;
                        canvas.FillRectangle(new System.Drawing.RectangleF(0, composed.Height - band, composed.Width, band), new Rgba32(0, 0, 0, 130));

                        // The browser's own text engine: system fonts, emoji, right-to-left and shaping.
                        await Text.DrawTextAsync(canvas, caption,
                            new Vector2(composed.Width / 2f, composed.Height - band / 2), new TextStyle
                            {
                                FontSize = composed.Height * 0.06f,
                                Bold = true,
                                FillColor = Rgba32.White,
                                Alignment = TextAlignment.Center,
                                Baseline = TextBaseline.Middle,
                                MaxWidth = composed.Width * 0.86f,
                            });

                        return await Exporter.EncodeAsync(composed, new ImageExportOptions { Format = ImageFormat.Jpeg, Quality = 0.88 });
                    }
                }
                """,
            Notes = "ImageCanvas.FromBuffer draws into the buffer itself, so the encode afterwards sees the caption. " +
                    "BrowserTextRasterizer needs a browser (call it from a handler, never during prerendering); on a server " +
                    "use FontTextRasterizer with a registered TrueType font and canvas.DrawText - it does no complex-script " +
                    "shaping or bidi. Open Graph scrapers want JPEG or PNG; do not serve them WebP."
        },
        new()
        {
            Id = "placeholder",
            Title = "Blurred placeholder while the image loads (LQIP)",
            Intent = "Store a tiny colour grid per image and show it, blurred, until the real image has loaded.",
            Keywords = "placeholder lqip blurhash progressive loading low quality image placeholder css gradient average colour aspect ratio",
            RunsIn = "Anywhere",
            SeeAlso = "/headless",
            Code = """
                // When the image is saved (browser or server):
                var placeholder = ImagePlaceholder.Create(image, columns: 4, rows: 4);
                var token = placeholder.ToCompactString();   // ~67 characters, store it beside the row
                var ratio = placeholder.AspectRatio;          // reserve the layout space

                // When the list is rendered - no image request, CSS gradients alone:
                var restored = ImagePlaceholder.Parse(token, width, height);
                var css = restored?.ToCssBackground();

                // <div style="aspect-ratio: @ratio; background: @css">
                //     <img src="@url" loading="lazy" style="width: 100%; height: 100%" />
                // </div>

                // Or as pixels, smooth and upscaled:
                // using var blurred = restored!.Render(400, 300);
                """,
            Notes = "The placeholder is small enough to return from a list endpoint without thinking about it. AverageColor " +
                    "is a single-colour fallback. Render returns a buffer you own - dispose it."
        },
        new()
        {
            Id = "format-fallback",
            Title = "Choose WebP or AVIF only where the browser can write it",
            Intent = "Export in the best format the current browser supports, and degrade predictably where it cannot.",
            Keywords = "webp avif format fallback capabilities probe canencode supportswebp chooseencodableformat transparency png jpeg safari",
            RunsIn = "Browser",
            SeeAlso = "/formats",
            Code = """
                @inject IImageCapabilityProvider Capabilities
                @inject ImageExporter Exporter

                @code {
                    private ImageCapabilities? _capabilities;

                    protected override async Task OnAfterRenderAsync(bool firstRender)
                    {
                        // Never during prerendering: there the probe answers with the managed set
                        // (Environment = Prerendering), which can write neither WebP nor AVIF.
                        if (!firstRender || !RendererInfo.IsInteractive) return;
                        _capabilities = await Capabilities.GetAsync();
                        StateHasChanged();
                    }

                    private async Task<EncodedImage> ExportAsync(ImageBuffer image)
                    {
                        // Transparent output needs a format with alpha; a JPEG would flatten it onto Background.
                        IReadOnlyList<ImageFormat> preferred = image.HasTransparency()
                            ? [ImageFormat.Avif, ImageFormat.WebP, ImageFormat.Png]
                            : [ImageFormat.Avif, ImageFormat.WebP, ImageFormat.Jpeg];

                        // Picks the first format this browser can actually encode; reported on encoded.Format.
                        return await Exporter.EncodeWithFallbackAsync(image, preferred, new ImageExportOptions { Quality = 0.82 });
                    }

                    private bool TooLargeForThisBrowser(int width, int height)
                        => _capabilities is { } caps && !caps.CanRenderCanvas(width, height);
                }
                """,
            Notes = "Browsers asked to encode a format they cannot write silently hand back a PNG; Bmage detects that and " +
                    "throws ImageCapabilityException from EncodeAsync rather than mislabelling the bytes - so a plain " +
                    "EncodeAsync with Format = WebP is a crash on a browser without WebP encoding. AVIF encoding is rarely " +
                    "available; always pair it with a fallback. On a server neither exists. Exceeding the browser's canvas " +
                    "limit (4096px on older iPhones) gives a silently blank canvas: cap at SuggestedMaxWorkingDimension."
        },
        new()
        {
            Id = "size-budget",
            Title = "Hit a file-size budget",
            Intent = "Encode an image so the file is no larger than a given number of bytes, at the best quality that fits.",
            Keywords = "size budget maxfilesize kb limit bytes quality bisect minquality strategy qualitythendimensions qualityonly dimensionsonly imageencodeexception",
            RunsIn = "Anywhere",
            SeeAlso = "/formats",
            Code = """
                try
                {
                    var encoded = await exporter.EncodeAsync(image, new ImageExportOptions
                    {
                        Format = ImageFormat.Jpeg,
                        Quality = 0.9,                 // where the search starts
                        MaxFileSize = 150 * 1024,
                        MinQuality = 0.5,              // the floor before dimensions start to shrink
                        MaxFileSizeStrategy = MaxFileSizeStrategy.QualityThenDimensions,
                    });

                    Console.WriteLine($"{encoded.Length} bytes at quality {encoded.Quality:0.00}, {encoded.Width}x{encoded.Height}");
                }
                catch (ImageEncodeException ex)
                {
                    // "Cannot reach 153600 bytes for this image (smallest attempt was ...)"
                    Console.WriteLine(ex.Message);
                }
                """,
            Notes = "The search bisects on quality first (down to MinQuality), then shrinks the dimensions if the strategy " +
                    "allows; QualityOnly throws instead of resizing, DimensionsOnly keeps the quality and only resizes. " +
                    "Only lossy formats have a quality to bisect, so a PNG budget is met by shrinking or not at all. " +
                    "Check encoded.Width/Height when the dimensions matter. RunBmagePipeline with maxFileSize runs the " +
                    "same search on a test image and reports the quality and size it lands on."
        },
        new()
        {
            Id = "paste-from-clipboard",
            Title = "Paste an image from the clipboard",
            Intent = "Let users press Ctrl+V on the page, or click a button, to bring in an image from the clipboard.",
            Keywords = "paste clipboard ctrl+v cmd+v listenforpasteasync loadfromclipboardasync permission screenshot",
            RunsIn = "Browser",
            SeeAlso = "/annotate",
            Code = """
                @implements IAsyncDisposable
                @inject ImageLoader Loader
                @inject IImageCapabilityProvider Capabilities

                <button disabled="@(!_canRead)" @onclick="PasteAsync">Paste</button>

                @code {
                    private IAsyncDisposable? _listener;
                    private ImageBuffer? _image;
                    private bool _canRead;

                    protected override async Task OnAfterRenderAsync(bool firstRender)
                    {
                        if (!firstRender) return;

                        // The paste event carries the image: no permission prompt.
                        _listener = await Loader.ListenForPasteAsync(image =>
                        {
                            Replace(image);
                            return InvokeAsync(StateHasChanged);
                        });

                        // Reading the clipboard directly needs the API and a permission.
                        _canRead = (await Capabilities.GetAsync()).SupportsClipboardRead;
                        StateHasChanged();
                    }

                    private async Task PasteAsync()
                    {
                        if (await Loader.LoadFromClipboardAsync() is { } image) Replace(image);
                    }

                    private void Replace(ImageBuffer image)
                    {
                        _image?.Dispose();
                        _image = image;
                    }

                    public async ValueTask DisposeAsync()
                    {
                        if (_listener is not null) await _listener.DisposeAsync();
                        _image?.Dispose();
                    }
                }
                """,
            Notes = "LoadFromClipboardAsync needs a user gesture and usually a permission prompt, and is missing in some " +
                    "browsers and in insecure contexts - check SupportsClipboardRead and keep a file picker as well. Writing " +
                    "to the clipboard (ImageExporter.CopyToClipboardAsync) is usually PNG only."
        },
    ];

    /// <summary>The listing: every recipe without its code and notes, for picking one.</summary>
    public static BmageRecipeDto[] Summaries { get; } = [.. All.Select(recipe => recipe with { Code = null, Notes = null })];

    /// <summary>A recipe by id, ignoring case, or null.</summary>
    public static BmageRecipeDto? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var key = id.Trim();

        return All.FirstOrDefault(recipe => recipe.Id.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
