using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Png;
using Bit.Bmage.Editing;
using Bit.Bmage.Memory;
using Bit.Bmage.Metadata;

namespace Bit.Bmage.Serialization;

/// <summary>How a <see cref="BmageProject"/> is written and read.</summary>
public sealed record BmageProjectOptions
{
    /// <summary>The defaults: no custom codecs, the default image limits, metadata removed, at most 1 GB unpacked.</summary>
    public static BmageProjectOptions Default { get; } = new();

    /// <summary>
    /// Codecs for custom operations, annotations, layers and masks. Their image handling is not used: a project stores
    /// its images itself. Null for the built-in types only.
    /// </summary>
    public BmageJsonOptions? Json { get; init; }

    /// <summary>The largest image a project may hold, checked before an image is decoded.</summary>
    public ImageLimits Limits { get; init; } = ImageLimits.Default;

    /// <summary>
    /// The most pixels a project read may decode, its original and the images its document uses together, checked
    /// before each is decoded; null for four times <see cref="ImageLimits.MaxPixels"/>. A PNG of one colour compresses
    /// to almost nothing, so without this a small file could ask for many large images.
    /// </summary>
    public long? MaxTotalPixels
    {
        get;
        init => field = value is null or > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxTotalPixels));
    }

    internal long TotalPixelLimit => MaxTotalPixels ?? (Limits.MaxPixels > long.MaxValue / 4 ? long.MaxValue : Limits.MaxPixels * 4);

    /// <summary>Where the decoded images' pixels come from, or null for the pooled allocator.</summary>
    public IPixelAllocator? Allocator { get; init; }

    /// <summary>
    /// What of the original's metadata the project keeps: removed by default, as an export removes it, so a project
    /// passed on does not carry the photo's location.
    /// </summary>
    public MetadataPolicy OriginalMetadata { get; init; } = MetadataPolicy.Remove;

    /// <summary>The most files a project read may hold.</summary>
    public int MaxEntries
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxEntries));
    } = 1024;

    /// <summary>The most bytes the files a project read holds may unpack to, together.</summary>
    public long MaxUnpackedBytes
    {
        get;
        init => field = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(MaxUnpackedBytes));
    } = 1L << 30;

    /// <summary>The PNG compression level the images are written with, 0 to 9.</summary>
    public int PngCompressionLevel
    {
        get;
        init => field = value is >= 0 and <= 9 ? value : throw new ArgumentOutOfRangeException(nameof(PngCompressionLevel));
    } = 6;
}

/// <summary>
/// A whole edit in one file: the original image, the non-destructive document, and every image the document refers
/// to, so an edit can be closed and reopened, or moved to another machine, and edited further. Its layers, masks,
/// adjustments, annotations and painted strokes all stay editable.
/// </summary>
/// <remarks>
/// <para>
/// A project is a ZIP archive of <c>manifest.json</c>, <c>document.json</c> (as <see cref="BmageJson"/> writes a document)
/// and lossless PNG images: the original and each image a layer, an annotation or a mask refers to, stored once however
/// often it is used. PNG keeps the pixels exactly, at the cost of size: a 12 megapixel photo takes tens of megabytes.
/// </para>
/// <para>
/// Reading one is safe on untrusted input. Only the files the manifest names are read, and only under
/// <c>images/</c>; the archive's file count is counted before the archive is opened, and each file's unpacked size and
/// their total are enforced while reading rather than trusted from the archive's own directory; only the images the
/// document uses are decoded, each checked against <see cref="BmageProjectOptions.Limits"/> and all of them against
/// <see cref="BmageProjectOptions.MaxTotalPixels"/> before decoding; and the document is validated as
/// <see cref="BmageJson"/> validates any document. Anything wrong is reported as an
/// <see cref="ImageDocumentFormatException"/>, and a project that fails to load leaves nothing allocated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Save the editor's work.
/// await using var file = File.Create("holiday.bmage");
/// await BmageProject.SaveAsync(file, session.Original, session.Document);
///
/// // Later, or elsewhere: everything comes back, ready to edit or render.
/// using var project = await BmageProject.LoadAsync(File.OpenRead("holiday.bmage"));
/// using var rendered = new DocumentRenderer().Render(project.Document, project.Original);
/// </code>
/// </example>
public sealed class BmageProject : IDisposable
{
    /// <summary>The version of the format this library writes, and the newest it reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>The extension project files are given by convention.</summary>
    public const string FileExtension = ".bmage";

    private const string Format = "bmage-project";
    private const string ManifestName = "manifest.json";
    private const string DocumentName = "document.json";
    private const string OriginalName = "images/original.png";

    /// <summary>A document is a description; no real one comes near this.</summary>
    private const long MaxJsonBytes = 64L * 1024 * 1024;

    private BmageProject(ImageBuffer original, ImageDocument document, IReadOnlyList<ImageBuffer> images)
    {
        Original = original;
        Document = document;
        Images = images;
    }

    /// <summary>The original image. Owned by the project until <see cref="Dispose"/>, or handed on with <see cref="DetachOriginal"/>.</summary>
    public ImageBuffer Original { get; private set; }

    /// <summary>The edit.</summary>
    public ImageDocument Document { get; }

    /// <summary>
    /// The images the document's layers, annotations and masks refer to, owned by the project: keep it alive while the
    /// document is used, or hand them to whoever keeps the document, such as an <see cref="ImageEditSession"/>.
    /// </summary>
    public IReadOnlyList<ImageBuffer> Images { get; private set; }

    /// <summary>Hands the original to the caller, who disposes it from then on.</summary>
    public ImageBuffer DetachOriginal()
    {
        var original = Original;
        Original = null!;
        return original;
    }

    /// <summary>Hands the document's images to the caller, who disposes them from then on.</summary>
    public IReadOnlyList<ImageBuffer> DetachImages()
    {
        var images = Images;
        Images = [];
        return images;
    }

    public void Dispose()
    {
        Original?.Dispose();
        Original = null!;
        foreach (var image in Images) image.Dispose();
        Images = [];
    }

    /// <summary>Writes a project of <paramref name="original"/> and the edit <paramref name="document"/> made to it.</summary>
    /// <exception cref="ArgumentException">The document was made for an image of another size.</exception>
    public static async Task SaveAsync(Stream destination, ImageBuffer original, ImageDocument document, BmageProjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(document);
        if (document.SourceSize != original.Size)
            throw new ArgumentException($"The document was made for a {document.SourceSize.Width}x{document.SourceSize.Height} image, not this {original.Width}x{original.Height} one.", nameof(document));
        options ??= BmageProjectOptions.Default;

        // Every image the document refers to, named in the order it is first met and stored once.
        var names = new Dictionary<ImageBuffer, string>(ReferenceEqualityComparer.Instance);
        var json = BmageJson.Serialize(document, (options.Json ?? BmageJsonOptions.Default).WithImages(image =>
        {
            if (!names.TryGetValue(image, out var name)) names[image] = name = $"images/{names.Count + 1}.png";
            return name;
        }, null));

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteText(zip, ManifestName, Manifest([.. names.Values]));
            WriteText(zip, DocumentName, json);
            WritePng(zip, OriginalName, original, options, options.OriginalMetadata, cancellationToken);
            foreach (var (image, name) in names)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WritePng(zip, name, image, options, MetadataPolicy.Remove, cancellationToken);
            }
        }
        buffer.Position = 0;
        await buffer.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static string Manifest(IReadOnlyList<string> images)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", Format);
            writer.WriteNumber("version", FormatVersion);
            writer.WriteString("document", DocumentName);
            writer.WriteString("original", OriginalName);
            writer.WriteStartArray("images");
            foreach (var image in images) writer.WriteStringValue(image);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteText(ZipArchive zip, string name, string text)
    {
        using var entry = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
        entry.Write(Encoding.UTF8.GetBytes(text));
    }

    private static void WritePng(ZipArchive zip, string name, ImageBuffer image, BmageProjectOptions options, MetadataPolicy metadata, CancellationToken cancellationToken)
    {
        var encoded = PngEncoder.Encode(image, new ImageExportOptions
        {
            Format = ImageFormat.Png,
            Metadata = metadata,
            PngCompressionLevel = options.PngCompressionLevel,
        }, cancellationToken);
        // PNG is compressed already; deflating it again would cost time and save nothing.
        using var entry = zip.CreateEntry(name, CompressionLevel.NoCompression).Open();
        entry.Write(encoded.Data);
    }

    /// <summary>Reads a project written by <see cref="SaveAsync"/>. The caller owns, and disposes, the result.</summary>
    /// <exception cref="ImageDocumentFormatException">The data is not a project this version can read, or exceeds the options' limits.</exception>
    public static async Task<BmageProject> LoadAsync(Stream source, BmageProjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        options ??= BmageProjectOptions.Default;

        // The archive is read from memory: ZIP needs to seek, and an upload's stream may not.
        using var buffer = new MemoryStream();
        await CopyBoundedAsync(source, buffer, Math.Min(options.MaxUnpackedBytes, Array.MaxLength), "The project file", cancellationToken).ConfigureAwait(false);
        // .NET builds an object for every file in the archive's directory as it opens it, so they are counted first.
        CountEntries(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), options.MaxEntries);
        buffer.Position = 0;

        ZipArchive zip;
        try
        {
            zip = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new ImageDocumentFormatException("The file is not a Bmage project: it is not a ZIP archive.", ex);
        }

        var decoded = new Dictionary<string, ImageBuffer>(StringComparer.Ordinal);
        ImageBuffer? original = null;
        try
        {
            using (zip)
            {
                var budget = new UnpackBudget(options.MaxUnpackedBytes, options.TotalPixelLimit);

                using var manifest = ParseManifest(ReadEntry(zip, ManifestName, budget, MaxJsonBytes));
                var root = manifest.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != Format)
                    throw new ImageDocumentFormatException("The file is not a Bmage project: its manifest does not say it is one.");
                var version = root.TryGetProperty("version", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var number) ? number : 0;
                if (version is < 1 or > FormatVersion)
                    throw new ImageDocumentFormatException($"The project is format version {version}; this version of Bmage reads versions 1 to {FormatVersion}.");

                var documentName = ImageOrFileName(root, "document", allowDocument: true);
                var originalName = ImageOrFileName(root, "original", allowDocument: false);
                var imageNames = new HashSet<string>(StringComparer.Ordinal);
                if (root.TryGetProperty("images", out var images))
                {
                    if (images.ValueKind != JsonValueKind.Array) throw new ImageDocumentFormatException("$.images in the manifest must be an array.");
                    foreach (var image in images.EnumerateArray())
                        imageNames.Add(image.ValueKind == JsonValueKind.String ? CheckImageName(image.GetString()!) : throw new ImageDocumentFormatException("$.images may only hold file names."));
                }

                original = DecodePng(zip, originalName, budget, options, cancellationToken);

                // An image is decoded when the document first uses it, so one the manifest lists but nothing uses costs nothing.
                var json = Encoding.UTF8.GetString(ReadEntry(zip, documentName, budget, MaxJsonBytes));
                var document = BmageJson.DeserializeDocument(json, (options.Json ?? BmageJsonOptions.Default).WithImages(null, name =>
                {
                    if (decoded.TryGetValue(name, out var image)) return image;
                    if (!imageNames.Contains(name)) return null;
                    cancellationToken.ThrowIfCancellationRequested();
                    return decoded[name] = DecodePng(zip, name, budget, options, cancellationToken);
                }));
                if (document.SourceSize != original.Size)
                    throw new ImageDocumentFormatException($"The project's document was made for a {document.SourceSize.Width}x{document.SourceSize.Height} image, but its original is {original.Width}x{original.Height}.");
                return new BmageProject(original, document, [.. decoded.Values]);
            }
        }
        catch (Exception ex)
        {
            original?.Dispose();
            foreach (var image in decoded.Values) image.Dispose();
            // A damaged archive is reported as any other malformed project is.
            if (ex is InvalidDataException or NotSupportedException)
                throw new ImageDocumentFormatException($"The project file is damaged: {ex.Message}", ex);
            throw;
        }
    }

    private static JsonDocument ParseManifest(byte[] json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ImageDocumentFormatException($"The project's manifest is not valid JSON: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Counts the files in a ZIP archive's central directory, refusing the archive once there are more than
    /// <paramref name="max"/>. Anything this does not recognise is left for <see cref="ZipArchive"/> to refuse.
    /// </summary>
    private static void CountEntries(ReadOnlySpan<byte> zip, int max)
    {
        // The end of central directory record: 22 bytes, then a comment of up to 65535, at the very end.
        var end = -1;
        for (var i = zip.Length - 22; i >= 0 && i >= zip.Length - 22 - ushort.MaxValue; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(zip[i..]) == 0x06054b50) { end = i; break; }
        }
        if (end < 0) return;
        var declared = BinaryPrimitives.ReadUInt16LittleEndian(zip[(end + 10)..]);
        long offset = BinaryPrimitives.ReadUInt32LittleEndian(zip[(end + 16)..]);
        // ZIP64 is only needed past 65535 files or 4 GB, which no project this reads comes near; it is not followed.
        if (declared == ushort.MaxValue || offset == uint.MaxValue)
            throw new ImageDocumentFormatException("The project file is a ZIP64 archive, which projects never need and are not read as.");
        if (declared > max) throw new ImageDocumentFormatException($"The project holds {declared} files; at most {max} are read.");
        // The directory itself is walked too, as ZipArchive walks it, rather than trusting the count it declares.
        var count = 0;
        while (offset <= zip.Length - 46 && BinaryPrimitives.ReadUInt32LittleEndian(zip[(int)offset..]) == 0x02014b50)
        {
            if (++count > max) throw new ImageDocumentFormatException($"The project holds more than {max} files; at most {max} are read.");
            var header = zip[(int)offset..];
            offset += 46L + BinaryPrimitives.ReadUInt16LittleEndian(header[28..]) + BinaryPrimitives.ReadUInt16LittleEndian(header[30..]) + BinaryPrimitives.ReadUInt16LittleEndian(header[32..]);
        }
    }

    /// <summary>A name the manifest gives, which must be one of the project's own files.</summary>
    private static string ImageOrFileName(JsonElement root, string property, bool allowDocument)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
            throw new ImageDocumentFormatException($"The project's manifest names no {property}.");
        var name = value.GetString()!;
        if (allowDocument) return name == DocumentName ? name : throw new ImageDocumentFormatException($"The project's manifest names '{name}' as its document; only '{DocumentName}' is read.");
        return CheckImageName(name);
    }

    /// <summary>Only plain names under <c>images/</c> are read: nothing outside the folder, nothing but PNG.</summary>
    private static string CheckImageName(string name)
    {
        if (name.Length is 0 or > 128 || !name.StartsWith("images/", StringComparison.Ordinal) || !name.EndsWith(".png", StringComparison.Ordinal))
            throw new ImageDocumentFormatException($"The project names an image '{Shorten(name)}'; images must be PNG files under images/.");
        foreach (var c in name.AsSpan("images/".Length, name.Length - "images/".Length - ".png".Length))
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                throw new ImageDocumentFormatException($"The project names an image '{Shorten(name)}'; image names may hold letters, digits, '-' and '_' only.");
        return name;
    }

    private static string Shorten(string text) => text.Length <= 64 ? text : text[..64] + "...";

    private static ImageBuffer DecodePng(ZipArchive zip, string name, UnpackBudget budget, BmageProjectOptions options, CancellationToken cancellationToken)
    {
        var bytes = ReadEntry(zip, name, budget, budget.Remaining);
        // The size a PNG declares is in its first chunk; what fails to parse here, the decoder refuses below. PNG sizes
        // stop at 2^31 - 1, so a larger one is counted as that, which keeps the product in range.
        if (bytes.Length >= 24 && BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(12)) == 0x49484452) // "IHDR"
        {
            long width = Math.Min(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16)), int.MaxValue);
            long height = Math.Min(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20)), int.MaxValue);
            budget.SpendPixels(width * height, name);
        }
        try
        {
            return PngDecoder.Decode(bytes, new DecodeOptions { Limits = options.Limits, AutoOrient = false }, options.Allocator ?? PooledPixelAllocator.Shared, cancellationToken);
        }
        catch (ImageException ex)
        {
            throw new ImageDocumentFormatException($"The project's image '{name}' could not be read: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// The bytes of one file, refused once they pass <paramref name="max"/> or the archive's budget: what the
    /// archive's directory says a file holds is not trusted, the bytes are counted as they unpack.
    /// </summary>
    private static byte[] ReadEntry(ZipArchive zip, string name, UnpackBudget budget, long max)
    {
        var entry = zip.GetEntry(name) ?? throw new ImageDocumentFormatException($"The project has no '{name}'.");
        using var stream = entry.Open();
        using var bytes = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (bytes.Length + read > max)
                throw new ImageDocumentFormatException($"The project's '{name}' unpacks to more than {max} bytes.");
            budget.Spend(read, name);
            bytes.Write(chunk, 0, read);
        }
        return bytes.ToArray();
    }

    private static async Task CopyBoundedAsync(Stream source, Stream destination, long max, string what, CancellationToken cancellationToken)
    {
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > max) throw new ImageDocumentFormatException($"{what} is larger than {max} bytes.");
            destination.Write(chunk, 0, read);
        }
    }

    /// <summary>The bytes the files of one archive may still unpack to, and the pixels its images may still decode to.</summary>
    private sealed class UnpackBudget(long bytes, long pixels)
    {
        public long Remaining { get; private set; } = bytes;

        private long _pixels = pixels;

        public void Spend(int count, string name)
        {
            Remaining -= count;
            if (Remaining < 0) throw new ImageDocumentFormatException($"The project unpacks to more than the bytes allowed; it passed the limit at '{name}'.");
        }

        public void SpendPixels(long count, string name)
        {
            _pixels -= count;
            if (_pixels < 0) throw new ImageDocumentFormatException($"The project's images hold more pixels than allowed together; they passed the limit at '{name}'.");
        }
    }
}
