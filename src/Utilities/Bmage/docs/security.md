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
different lengths and corrupt its offsets, asserting only that nothing escapes.

The GIF LZW decoder bounds its code table and dictionary explicitly, because the classic GIF attack is a stream that
drives the decoder past the end of its tables.

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

One module, loaded from the library's own static web assets. It executes no caller-supplied code, builds no HTML from
input, and does not use `eval` or `innerHTML`. Bytes cross as streams with a size cap
(`BrowserImageInterop.MaxStreamBytes`, 512 MB by default) so a hostile or buggy page cannot stream unbounded data into
the .NET heap.

The library adds no `<script>` tag to your page and requires no relaxation of your Content Security Policy beyond
allowing your own scripts, since the module is served from your origin.

## Server-side use

The headless API has no browser and no JavaScript. It is the right choice for processing uploads on a server, but the
same rules apply: set limits appropriate to your service, check `Identify` before decoding, and remember that the
managed decoders are the only ones available there.

Processing untrusted images on a server means an attacker chooses the input to your decoder. Keep limits tight, run the
work with a timeout, and treat a decode failure as an expected outcome rather than an exceptional one.

## Reporting a vulnerability

Open a security advisory on the repository rather than a public issue.
