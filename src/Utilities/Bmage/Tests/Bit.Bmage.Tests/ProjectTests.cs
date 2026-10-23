using System.Buffers.Binary;
using System.Drawing;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using Bit.Bmage.Annotations;
using Bit.Bmage.Drawing;
using Bit.Bmage.Editing;
using Bit.Bmage.Layers;
using Bit.Bmage.Masks;
using Bit.Bmage.Memory;
using Bit.Bmage.Operations.Transforms;
using Bit.Bmage.Pipeline;
using Bit.Bmage.Serialization;

namespace Bit.Bmage.Tests;

[TestClass]
public sealed class ProjectTests
{
    private static ImageDocument RichDocument(ImageBuffer source, ImageBuffer logo, ImageBuffer matte) => ImageDocument.Create(source)
        .AddOperation(new CropOperation(5, 5, 150, 100))
        .AddOperation(new StraightenOperation(3))
        .AddLayer(new ImageLayer { Image = logo, Bounds = new RectangleF(10, 10, 30, 20), BlendMode = BlendMode.Overlay })
        .AddLayer(new ImageLayer { Image = logo, Bounds = new RectangleF(80, 40, 15, 10), Opacity = 0.5f })
        .AddLayer(new PaintLayer([new PaintStroke([new(20, 60), new(90, 70)], 6, new Rgba32(200, 30, 30)) { Pressures = [0.4f, 1f] }]))
        .AddLayer(new AdjustmentLayer(ImagePipeline.Create().Hsl(Operations.Adjustments.HueBand.Reds, saturation: 0.5f)) { Mask = new BitmapMask(matte) })
        .AddAnnotation(new ImageAnnotation { Image = logo, Rectangle = new RectangleF(100, 70, 20, 12) })
        .AddAnnotation(new ArrowAnnotation { Start = new Vector2(10, 90), End = new Vector2(60, 40) });

    private static async Task<byte[]> SaveAsync(ImageBuffer original, ImageDocument document, BmageProjectOptions? options = null)
    {
        using var stream = new MemoryStream();
        await BmageProject.SaveAsync(stream, original, document, options);
        return stream.ToArray();
    }

    private static Task<BmageProject> LoadAsync(byte[] bytes, BmageProjectOptions? options = null) => BmageProject.LoadAsync(new MemoryStream(bytes), options);

    [TestMethod]
    public async Task AProjectReopensAsTheSameEditWithTheSamePixels()
    {
        using var source = ParallelProcessingTests.Noise(160, 110, seed: 2);
        using var logo = ParallelProcessingTests.Noise(24, 16, seed: 3);
        using var matte = TestImages.Gradient(40, 30);
        var document = RichDocument(source, logo, matte);

        using var project = await LoadAsync(await SaveAsync(source, document));

        Assert.AreEqual(0, TestImages.MaxChannelDifference(source, project.Original), "the original comes back exactly");
        using var expected = new DocumentRenderer().Render(document, source);
        using var actual = new DocumentRenderer().Render(project.Document, project.Original);
        Assert.AreEqual(0, TestImages.MaxChannelDifference(expected, actual), "and renders the same edit");
        Assert.AreEqual(document.Pipeline.Count, project.Document.Pipeline.Count);
        Assert.AreEqual(document.Layers.Count, project.Document.Layers.Count);
        Assert.AreEqual(document.Annotations[1].Id, project.Document.Annotations[1].Id, "ids survive, so the editor can select what it selected");
    }

    [TestMethod]
    public async Task AnImageUsedSeveralTimesIsStoredOnce()
    {
        using var source = TestImages.Gradient(64, 48);
        using var logo = ParallelProcessingTests.Noise(12, 12, seed: 1);
        using var matte = TestImages.Gradient(8, 8);
        var bytes = await SaveAsync(source, RichDocument(source, logo, matte));
        using var zip = new ZipArchive(new MemoryStream(bytes));
        var images = zip.Entries.Where(e => e.FullName.StartsWith("images/", StringComparison.Ordinal)).Select(e => e.FullName).ToArray();
        CollectionAssert.AreEquivalent(new[] { "images/original.png", "images/1.png", "images/2.png" }, images, "the logo is used three times, the matte once");

        using var project = await LoadAsync(bytes);
        Assert.HasCount(2, project.Images);
        var first = ((ImageLayer)project.Document.Layers[0]).Image;
        Assert.AreSame(first, ((ImageLayer)project.Document.Layers[1]).Image, "and comes back as one buffer");
        Assert.AreSame(first, ((ImageAnnotation)project.Document.Annotations[0]).Image);
    }

    [TestMethod]
    public async Task TheProjectOwnsItsImagesUntilTheyAreHandedOn()
    {
        var allocator = new BudgetedPixelAllocator(64L * 1024 * 1024);
        using var source = TestImages.Gradient(64, 48);
        using var logo = ParallelProcessingTests.Noise(12, 12, seed: 1);
        using var matte = TestImages.Gradient(8, 8);
        var bytes = await SaveAsync(source, RichDocument(source, logo, matte));

        var project = await LoadAsync(bytes, new BmageProjectOptions { Allocator = allocator });
        Assert.IsGreaterThan(0, allocator.CurrentBytes);
        var original = project.DetachOriginal();
        project.Dispose();
        Assert.AreEqual((long)original.Width * original.Height * 4, allocator.CurrentBytes, "disposing releases all but what was handed on");
        original.Dispose();
        Assert.AreEqual(0, allocator.CurrentBytes);
    }

    [TestMethod]
    public async Task AProjectFromAnotherImageIsRefusedWhenSaving()
    {
        using var source = TestImages.Gradient(64, 48);
        using var other = TestImages.Gradient(10, 10);
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => SaveAsync(other, ImageDocument.Create(source)));
    }

    // ---- Untrusted input ----

    private static byte[] Zip(params (string Name, byte[] Bytes)[] entries)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, bytes) in entries)
            {
                using var entry = zip.CreateEntry(name).Open();
                entry.Write(bytes);
            }
        return stream.ToArray();
    }

    private static byte[] Text(string text) => Encoding.UTF8.GetBytes(text);

    private static byte[] Png(int width, int height) => Codecs.Png.PngEncoder.Encode(TestImages.Gradient(width, height)).Data;

    private static string Manifest(string original = "images/original.png", string images = "[]", string format = "bmage-project", int version = 1)
        => $$"""{"format":"{{format}}","version":{{version}},"document":"document.json","original":"{{original}}","images":{{images}}}""";

    private static string Document(int width, int height) => BmageJson.Serialize(ImageDocument.Create(new Size(width, height)));

    private static async Task AssertRefusedAsync(byte[] bytes, string expected, BmageProjectOptions? options = null)
    {
        var error = await Assert.ThrowsExactlyAsync<ImageDocumentFormatException>(() => LoadAsync(bytes, options));
        StringAssert.Contains(error.Message, expected);
    }

    [TestMethod]
    public async Task AFileThatIsNotAProjectIsRefusedClearly()
    {
        await AssertRefusedAsync(Text("not a zip at all"), "not a ZIP archive");
        await AssertRefusedAsync(Zip(("readme.txt", Text("hello"))), "has no 'manifest.json'");
        await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest(format: "something-else")))), "not a Bmage project");
        await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest(version: 99)))), "format version 99");
    }

    /// <summary>Overwrites 16-bit fields, at offsets from its start, of every ZIP header with a given signature.</summary>
    private static byte[] Patch(byte[] zip, uint signature, params (int Offset, ushort Value)[] fields)
    {
        var bytes = zip.ToArray();
        for (var i = 0; i + 4 <= bytes.Length; i++)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i)) != signature) continue;
            foreach (var (offset, value) in fields) BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(i + offset), value);
        }
        return bytes;
    }

    [TestMethod]
    public async Task AMalformedProjectIsReportedAsOneWhateverIsWrongWithIt()
    {
        var png = Png(8, 6);
        await AssertRefusedAsync(Zip(("manifest.json", Text("{ not json"))), "manifest is not valid JSON");
        await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest().Replace("\"version\":1", "\"version\":\"1\"", StringComparison.Ordinal)))), "format version 0");

        // Every file marked as compressed by a method nothing reads: the archive opens, its files do not.
        var project = Zip(("manifest.json", Text(Manifest())), ("images/original.png", png), ("document.json", Text(Document(8, 6))));
        var unreadable = Patch(Patch(project, 0x04034b50, (8, 99)), 0x02014b50, (10, 99));
        await AssertRefusedAsync(unreadable, "damaged");
    }

    [TestMethod]
    public async Task TheFilesAreCountedFromTheDirectoryNotFromWhatTheArchiveDeclares()
    {
        var many = Zip([.. Enumerable.Range(0, 20).Select(i => ($"x{i}.txt", Text("x"))).Prepend(("manifest.json", Text(Manifest())))]);
        // The end record says one file; the directory holds twenty-one.
        var understated = Patch(many, 0x06054b50, (8, 1), (10, 1));
        await AssertRefusedAsync(understated, "more than 10 files", new BmageProjectOptions { MaxEntries = 10 });

        var zip64 = Patch(many, 0x06054b50, (10, ushort.MaxValue));
        await AssertRefusedAsync(zip64, "ZIP64");
    }

    /// <summary>A project whose document puts each image on the original once, as an image annotation.</summary>
    private static byte[] ProjectUsing(int size, params int[] imageSizes)
    {
        var images = imageSizes.Select(s => TestImages.Gradient(s, s)).ToArray();
        try
        {
            var document = ImageDocument.Create(new Size(size, size));
            foreach (var image in images) document = document.AddAnnotation(new ImageAnnotation { Image = image, Rectangle = new RectangleF(0, 0, 4, 4) });
            var json = BmageJson.Serialize(document, new BmageJsonOptions { ImageReferences = image => $"images/{Array.IndexOf(images, image) + 1}.png" });
            var names = string.Join(",", images.Select((_, i) => $"\"images/{i + 1}.png\""));
            return Zip([("manifest.json", Text(Manifest(images: $"[{names}]"))), ("images/original.png", Png(size, size)), ("document.json", Text(json)),
                .. images.Select((image, i) => ($"images/{i + 1}.png", Codecs.Png.PngEncoder.Encode(image).Data))]);
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }

    [TestMethod]
    public async Task TheImagesTogetherAreBoundedBeforeTheyAreDecoded()
    {
        var allocator = new BudgetedPixelAllocator(64L * 1024 * 1024);
        // 64² for the original and 64² for each image: the second image passes 10,000 pixels.
        var project = ProjectUsing(64, 64, 64, 64);
        await AssertRefusedAsync(project, "more pixels than allowed together", new BmageProjectOptions { MaxTotalPixels = 10_000, Allocator = allocator });
        Assert.AreEqual(0, allocator.CurrentBytes, "and what was decoded before is returned");

        // By default the images may hold four times the largest one allowed.
        var limits = ImageLimits.Default with { MaxPixels = 64 * 64 };
        using (var fits = await LoadAsync(ProjectUsing(64, 64, 64, 64), new BmageProjectOptions { Limits = limits })) Assert.HasCount(3, fits.Images);
        await AssertRefusedAsync(ProjectUsing(64, 64, 64, 64, 64), "more pixels than allowed together", new BmageProjectOptions { Limits = limits });
    }

    [TestMethod]
    public async Task AnImageNothingUsesIsNeverDecoded()
    {
        var allocator = new BudgetedPixelAllocator(64L * 1024 * 1024);
        var project = Zip(("manifest.json", Text(Manifest(images: """["images/1.png"]"""))), ("images/original.png", Png(32, 32)), ("images/1.png", Png(500, 500)),
            ("document.json", Text(Document(32, 32))));
        using var loaded = await LoadAsync(project, new BmageProjectOptions { Allocator = allocator, MaxTotalPixels = 32 * 32 });
        Assert.IsEmpty(loaded.Images);
        Assert.AreEqual(32L * 32 * 4, allocator.CurrentBytes, "only the original was decoded");
    }

    [TestMethod]
    public async Task OnlyPngFilesUnderImagesAreRead()
    {
        foreach (var name in new[] { "../outside.png", "images/../../x.png", "images/sub/x.png", "images/x.jpg", "/etc/passwd", "images/a b.png" })
            await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest(original: name))), ("document.json", Text(Document(4, 4)))), "images");
    }

    [TestMethod]
    public async Task TheDocumentMustFitTheOriginalAndOnlyUseImagesTheManifestNames()
    {
        await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest())), ("images/original.png", Png(8, 6)), ("document.json", Text(Document(9, 9)))), "made for a 9x9 image");

        using var stamp = TestImages.Gradient(2, 2);
        var withImage = BmageJson.Serialize(ImageDocument.Create(new Size(8, 6)).AddAnnotation(new ImageAnnotation { Image = stamp, Rectangle = new RectangleF(0, 0, 2, 2) }),
            new BmageJsonOptions { ImageReferences = _ => "images/elsewhere.png" });
        await AssertRefusedAsync(Zip(("manifest.json", Text(Manifest())), ("images/original.png", Png(8, 6)), ("document.json", Text(withImage))), "images/elsewhere.png");
    }

    [TestMethod]
    public async Task LimitsAreEnforcedBeforeTheyAreTrusted()
    {
        var project = Zip(("manifest.json", Text(Manifest())), ("images/original.png", Png(64, 64)), ("document.json", Text(Document(64, 64))));
        await AssertRefusedAsync(project, "larger than 256 bytes", new BmageProjectOptions { MaxUnpackedBytes = 256 });
        await AssertRefusedAsync(project, "could not be read", new BmageProjectOptions { Limits = ImageLimits.Default with { MaxWidth = 32 } });

        var many = Enumerable.Range(0, 20).Select(i => ($"x{i}.txt", Text("x"))).Prepend(("manifest.json", Text(Manifest()))).ToArray();
        await AssertRefusedAsync(Zip(many), "21 files", new BmageProjectOptions { MaxEntries = 10 });

        // A zip bomb: a few kilobytes of zeros that unpack far past the budget.
        var bomb = Zip(("manifest.json", Text(Manifest())), ("images/original.png", new byte[64 * 1024 * 1024]), ("document.json", Text(Document(1, 1))));
        Assert.IsLessThan(1024 * 1024, bomb.Length, "the archive is small");
        await AssertRefusedAsync(bomb, "unpack", new BmageProjectOptions { MaxUnpackedBytes = 8 * 1024 * 1024 });
    }

    [TestMethod]
    public async Task AFailedLoadLeavesNothingAllocated()
    {
        var allocator = new BudgetedPixelAllocator(64L * 1024 * 1024);
        // Everything decodes, then the document turns out not to fit: the decoded images must all be returned.
        var project = ProjectUsing(32, 8, 8);
        var mismatched = Zip([.. ReadAll(project).Select(e => e.Name == "document.json" ? (e.Name, Text(Encoding.UTF8.GetString(e.Bytes).Replace("32", "33", StringComparison.Ordinal))) : e)]);
        await AssertRefusedAsync(mismatched, "made for a 33x33 image", new BmageProjectOptions { Allocator = allocator });
        Assert.AreEqual(0, allocator.CurrentBytes);
    }

    private static (string Name, byte[] Bytes)[] ReadAll(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip));
        return [.. archive.Entries.Select(e =>
        {
            using var stream = e.Open();
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            return (e.FullName, bytes.ToArray());
        })];
    }
}
