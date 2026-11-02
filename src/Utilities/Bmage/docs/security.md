# Security

Image files are attacker-controlled input. A user upload, a pasted screenshot and a remote URL are all untrusted, and a
decoder is one of the more attractive things to attack. This page covers what the library does about that and what it
leaves to you.

## Limits, applied before allocation

```csharp
public sealed record ImageLimits
{
    public int MaxWidth { get; init; } = 16384;
    public int MaxHeight { get; init; } = 16384;
    public long MaxPixels { get; init; } = 64L * 1024 * 1024;      // 64 megapixels
    public long MaxEncodedBytes { get; init; } = 256L * 1024 * 1024;
}
```

Limits are checked against the **header**, before any pixel memory is allocated. A decompression bomb declaring
50000 × 50000 is rejected at 26 bytes in, not after it has tried to allocate 10 GB.

Tighten them for user uploads:

```csharp
builder.Services.AddBitBmageServices(options =>
{
    options.Limits = ImageLimits.Default with
    {
        MaxPixels = 30_000_000,          // 30 megapixels is generous for a phone photo
        MaxEncodedBytes = 25 * 1024 * 1024,
    };
    options.MaxSourceBytes = 25 * 1024 * 1024;
});
```

Rejection is an `ImageLimitExceededException` with the actual numbers in the message, so you can tell the user what went
wrong rather than showing a generic failure.

`ImageProcessor.Identify` lets you check before committing to a decode at all:

```csharp
var info = processor.Identify(bytes);
if (info is null) return Error("Not a supported image format.");
if ((long)info.Width * info.Height > 30_000_000) return Error("That image is too large.");
```

## Malformed input

Every decoder treats its input as hostile. Truncated files, bad chunk lengths, invalid Huffman tables, corrupt LZW
streams, impossible palettes and out-of-range offsets all produce an `ImageDecodeException` rather than an out-of-bounds
read or an unbounded allocation.

EXIF parsing is deliberately more forgiving, because metadata is not worth failing an image over: bad offsets, circular
IFD references and absurd component counts are ignored, and parsing continues. Tests truncate valid EXIF at forty
different lengths and corrupt its offsets, asserting only that nothing escapes. The values copied out of one EXIF block,
its thumbnail included, share a 4 MB budget: a PNG or WebP can carry a far larger block than a JPEG's 64 KB segment, and
hundreds of entries pointing at one large value would otherwise each copy it. A PNG's colour profile is inflated only up
to 4 MB and dropped past that, so a small compressed profile cannot unpack into gigabytes. Only the first profile is
read, as the PNG standard allows only one, so a file repeating it cannot have each copy inflated in turn.

The GIF LZW decoder bounds its code table and dictionary explicitly, because the classic GIF attack is a stream that
drives the decoder past the end of its tables. A frame overhanging the logical screen is clipped to it, its own size is
checked against `ImageLimits` before it is decoded, and it is decoded a row at a time, so the part outside the screen is
never stored.

The JPEG decoder walks the whole image once per scan, however little the scan holds, so a few bytes repeating a scan
header would each cost a full pass. It decodes no more scans than a baseline image has components, as the standard
requires, and no more than 500 of a progressive one; any scans past those are ignored, and the image decoded so far is
kept.

The WebP decoder checks the canvas and every frame against `ImageLimits` before allocating, validates every prefix code
it builds as complete, bounds every back reference by the pixels already decoded, and rejects a lossless stream that
declares more prefix codes than its remaining data could describe, so a few bytes cannot ask for a large table. A lossy
stream that ends before its last macroblock is truncated, not padded with zeros.

The AVIF decoder checks the encoded size, every item's declared size, every AV1 frame header and a grid's output size
against `ImageLimits` before it allocates a plane, and bounds every offset and length in the container by the file
itself. Its sample planes come from a shared pool, so the one tool that copies samples within a frame, intra block copy,
is held to the vectors the AV1 specification allows, which point only at samples already decoded, and a frame that
enables it starts from cleared planes besides: no earlier image, another user's included, can show through. A failure
anywhere, a grid tile decoded on another thread included, is an `ImageDecodeException`.

Project files (`BmageProject`) are ZIP archives, which brings the archive attacks with it. A project is read from
memory, never extracted to disk, so path traversal cannot write anything; even so only the files the manifest names
are read, and only names under `images/` made of letters, digits, `-` and `_`. The files in the archive's directory are
counted before .NET opens it, which builds an object for each, and ZIP64 archives, which no project needs, are
refused. Each file's unpacked size and the total are bounded by `BmageProjectOptions` and counted as the bytes unpack,
because the sizes an archive declares are the attacker's to choose: a zip bomb stops at the budget, not at the end of
the bomb. Only the images the document uses are decoded. Each is checked against `ImageLimits`, and all of them
together against `MaxTotalPixels` (four times `ImageLimits.MaxPixels` unless set), before it is decoded: a PNG of one
colour compresses to almost nothing, so a small file could otherwise ask for many large images. The document is
validated as any JSON document is, anything malformed, a damaged archive included, is an
`ImageDocumentFormatException`, and a project that fails to load returns every buffer it had decoded.

A document's operations are as untrusted as its images. A blur, sharpen or high-pass costs the same whatever its radius,
a radius or amount that is infinite or not a number is refused, a redaction's strength is clamped to a finite one, a
resize fused with a crop is checked against `ImageLimits` at the full resized size, as the two steps apart would be,
and shaping text with a long run of combining marks takes time in proportion to its length.

## SVG is refused

```csharp
// throws ImageDecodeException
await processor.DecodeAsync(svgBytes);
```

> SVG input is not decoded by Bmage because rasterising untrusted SVG can execute embedded scripts and fetch
> remote resources. Rasterise it yourself and pass the pixels instead.

An SVG is a document, not an image: it can contain `<script>`, reference external resources, and pull in fonts and other
SVGs. Rendering one safely means sandboxing it, which is the caller's decision and not something a library can make on
their behalf. Refusing with an explanation is more honest than accepting and hoping.

## Remote URLs

```csharp
await loader.LoadAsync(ImageSource.FromUrl("https://example.com/photo.jpg"));
```

The fetch happens in the browser and is subject to the same-origin policy and CORS. A server that does not permit
cross-origin reads produces a clear error naming CORS as the likely cause, rather than a tainted canvas.

Only `http`, `https`, `blob` and `data` schemes are accepted; anything else is rejected before the request is made.
Credentials are not sent unless you opt in with `ImageSource.FromUrl(url, withCredentials: true)`.

The download is held to `BitBmageOptions.MaxSourceBytes`, as an upload is. A response declaring more is refused before
its body is read, and one sending more is cut off at the limit; either way the load fails with an
`ImageLimitExceededException`. An image read from the clipboard is held to the same limit, and refused by its size
before the browser reads it.

If your application accepts a URL from a user, validate it against your own allowlist first. The library cannot know
which hosts you consider acceptable, and a URL fetched by the browser can be used to probe your users' network.

## Memory exhaustion

A browser tab has a hard memory ceiling and exceeding it kills the page rather than throwing something catchable. The
defences are structural:

- Pixel buffers are pooled and disposed, not left to the collector.
- The resampler works per row; the JPEG encoder per MCU row; the PNG encoder emits incremental chunks.
- The editor edits through a proxy and touches full resolution once, on export.
- Batch concurrency is bounded and defaults to 2.
- `ImageCapabilities.SuggestedMaxWorkingDimension` reports a device-appropriate cap based on reported memory.
- `MaxCanvasDimension` is probed, because exceeding a browser's canvas limit silently produces a blank image.
- The browser decodes an image at full size before it can be shrunk to `ImageLimits.MaxPixels`, so an image it would
  decode at more than four times that is refused before the browser sees the file. The size is the one the browser
  decodes at, which can be larger than the header's: a GIF's first frame grows the canvas past its logical screen, and
  an AVIF is decoded whole before its clean aperture crops it. With `DecodeOptions.MaxSize` the browser decodes straight
  to the smaller size, so that is the size checked; an image allowed only on the strength of that resize is refused,
  rather than retried at full size, when the browser rejects the resize. A file whose size cannot be read is refused too.
- `BrowserTextRasterizer` refuses text whose bitmap would exceed its `Limits`, which the services take from
  `BitBmageOptions.Limits`, before the browser allocates a canvas for it. Its cache holds at most 64 MB of bitmaps
  besides the newest, and is emptied when the limits change.

`ImageLimits` refuses absurd dimensions; a `BudgetedPixelAllocator` refuses absurd totals. Give each request on a server,
or each editor session in a browser, its own budget, and an image that would need more than its share fails with
`ImageMemoryBudgetExceededException` before anything is allocated, instead of taking memory from everyone else:

```csharp
var budget = new BudgetedPixelAllocator(256L * 1024 * 1024);
var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault(), budget, ImageLimits.Default);
```

The services take theirs from `BitBmageOptions`: the processors, the loader and every `BmageEditor` session allocate
through `Allocator` and are bound by `Limits`. Those options are one instance for the application, which in a
WebAssembly app means one browser tab, so `options.Allocator = new BudgetedPixelAllocator(...)` gives the tab's editor
and loads one budget between them. On Blazor Server the same instance serves every circuit; give each its own session
there instead, with an `ImageEditSession` built from `ImageEditSessionOptions { Allocator = ... }`.

A batch can be admitted by memory too: with `BatchOptions.MaxMemoryBytes` an image only starts when its estimated need,
read from its header before anything is decoded, fits beside the ones already running.

## Redaction

The redaction annotation replaces the pixels underneath it. The hidden content is genuinely absent from the exported
file, not covered by a layer that can be moved or an overlay that survives in a separate channel.

Prefer `RedactionMode.Solid`:

```csharp
new RedactionAnnotation
{
    Rectangle = sensitiveArea,
    Mode = RedactionMode.Solid,
}
```

A strong blur or a coarse pixelation is irreversible in practice, but "in practice" is doing real work in that sentence:
published research has recovered text from pixelated and blurred images when the possible inputs are constrained, which
is exactly the case for account numbers and short strings. A solid rectangle removes the doubt.

Redactions always render before other annotations, so an arrow placed on top of one is not blurred away, and the
redaction cannot be defeated by drawing over it afterwards.

Remember that redaction covers pixels. Metadata is a separate leak, handled by the metadata policy.

## Metadata leakage

The default is `MetadataPolicy.Remove`. You have to ask for metadata to be kept, which means the failure mode of not
thinking about it is safe rather than a location disclosure. See [metadata-and-privacy.md](metadata-and-privacy.md).

## The JavaScript boundary

One module, loaded from the library's own static web assets, and one codec worker beside it. Neither executes
caller-supplied code, builds HTML from input, or uses `eval` or `innerHTML`. Bytes cross as streams with a size cap
(`BrowserImageInterop.MaxStreamBytes`, 512 MB by default) so a hostile or buggy page cannot stream unbounded data into
the .NET heap.

The library adds no `<script>` tag to your page, and its module and worker are served from your origin. Your Content
Security Policy needs `blob:` in two places. In `img-src`, because an image that `createImageBitmap` can't decode, such
as an SVG, is decoded through an `<img>` element with a `blob:` URL. In `connect-src`, when you hand the editor a `blob:`
URL, such as the preview URL of a file the user picked, since the library fetches it. The codec worker needs
`worker-src 'self'`, or a `script-src 'self'` it falls back to; where the policy blocks it, codec work simply stays on the
page's thread.

## Server-side use

The headless API has no browser and no JavaScript. It is the right choice for processing uploads on a server, but the
same rules apply: set limits appropriate to your service, check `Identify` before decoding, and remember that the
managed decoders are the only ones available there.

Processing untrusted images on a server means an attacker chooses the input to your decoder. Keep limits tight, run the
work with a timeout, and treat a decode failure as an expected outcome rather than an exceptional one.

## Reporting a vulnerability

Open a security advisory on the repository rather than a public issue.
