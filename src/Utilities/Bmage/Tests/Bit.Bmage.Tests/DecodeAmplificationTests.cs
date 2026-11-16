using System.Buffers.Binary;
using Bit.Bmage.Codecs;
using Bit.Bmage.Codecs.Jpeg;
using Bit.Bmage.Codecs.WebP;

namespace Bit.Bmage.Tests;

/// <summary>
/// A file of a kilobyte whose header declares an enormous image is refused before the decoder allocates for it: the
/// header is all a decoder needs to allocate hundreds of megabytes, an amplification an upload to a server, or a page on
/// a phone, cannot afford. PNG already compared its compressed data with the size it declares.
/// </summary>
[TestClass]
public sealed class DecodeAmplificationTests
{
    // A frame of 8192 x 8192 is within ImageLimits.Default; what it costs is the point.
    private const int Side = 8192;

    // Far less than the planes of an 8192 x 8192 frame, which are 200 MB and more; far more than an 8 x 8 decode needs.
    private const long Allowance = 32L * 1024 * 1024;

    [TestMethod]
    public void ABaselineJpegWhoseDataIsFarTooShortForItsFrameIsRefusedBeforeItsPlanesAreAllocated()
    {
        using var small = TestImages.Gradient(8, 8);
        var jpeg = WithFrameSize(JpegEncoder.Encode(small).Data, Side, Side);

        var allocated = Allocated(() => Assert.ThrowsExactly<ImageDecodeException>(() => JpegDecoder.Decode(jpeg)));

        Assert.IsLessThan(Allowance, allocated, $"Allocated {allocated:N0} bytes.");
    }

    [TestMethod]
    public void AProgressiveJpegWhoseDataIsFarTooShortForItsFrameIsRefusedBeforeItsCoefficientsAreAllocated()
    {
        // The same file marked progressive (SOF2): its scan, a sequential one, is then invalid, but the coefficients
        // were allocated from the frame header before any scan was read.
        using var small = TestImages.Gradient(8, 8);
        var jpeg = WithFrameSize(JpegEncoder.Encode(small).Data, Side, Side);
        jpeg[FrameMarker(jpeg) + 1] = 0xC2;

        var allocated = Allocated(() => Assert.ThrowsExactly<ImageDecodeException>(() => JpegDecoder.Decode(jpeg)));

        Assert.IsLessThan(Allowance, allocated, $"Allocated {allocated:N0} bytes.");
    }

    [TestMethod]
    public void AJpegPaddedAfterItsScanIsRefusedBeforeItsPlanesAreAllocated()
    {
        // Comment segments after the scan make the file long enough for its frame, but none of them is image data.
        using var small = TestImages.Gradient(8, 8);
        var jpeg = WithFrameSize(JpegEncoder.Encode(small).Data, Side, Side);
        var comment = new byte[4 + 65533];
        comment[0] = 0xFF;
        comment[1] = 0xFE;
        BinaryPrimitives.WriteUInt16BigEndian(comment.AsSpan(2), 65535);
        var padded = jpeg[..^2].Concat(Enumerable.Repeat(comment, 32).SelectMany(c => c)).Concat(jpeg[^2..]).ToArray();

        var allocated = Allocated(() => Assert.ThrowsExactly<ImageDecodeException>(() => JpegDecoder.Decode(padded)));

        Assert.IsLessThan(Allowance, allocated, $"Allocated {allocated:N0} bytes.");
    }

    [TestMethod]
    public void AProgressiveJpegWhoseFirstScanIsAnAcScanIsRefusedBeforeItsCoefficientsAreAllocated()
    {
        // An AC scan codes a run of blocks in a few bits, so its length says nothing about the frame it covers.
        byte[] jpeg =
        [
            0xFF, 0xD8,
            0xFF, 0xDB, 0x00, 0x43, 0x00, .. Enumerable.Repeat((byte)1, 64),
            0xFF, 0xC2, 0x00, 0x0B, 0x08, 0x20, 0x00, 0x20, 0x00, 0x01, 0x01, 0x11, 0x00,
            // One AC code, a single bit, for an end of block.
            0xFF, 0xC4, 0x00, 0x14, 0x10, 0x01, .. new byte[15], 0x00,
            // One component, spectral selection 1 to 1, no successive approximation.
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x01, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0xFF, 0xD9,
        ];

        var allocated = Allocated(() => Assert.ThrowsExactly<ImageDecodeException>(() => JpegDecoder.Decode(jpeg)));

        Assert.IsLessThan(Allowance, allocated, $"Allocated {allocated:N0} bytes.");
    }

    [TestMethod]
    public void AJpegThatIsWhole_EvenTheSmallest_StillDecodes()
    {
        foreach (var size in new[] { 1, 8, 17, 640 })
        {
            using var source = TestImages.Gradient(size, size);
            using var decoded = JpegDecoder.Decode(JpegEncoder.Encode(source).Data);
            Assert.AreEqual(source.Size, decoded.Size);
        }
    }

    [TestMethod]
    public void ALossyWebPWhoseFirstPartitionIsFarTooShortForItsFrameIsRefusedBeforeItIsAllocated()
    {
        using var small = TestImages.Gradient(16, 16);
        var webp = WebPEncoder.Encode(small, ImageExportOptions.WebP).Data;
        var payload = webp.AsSpan().IndexOf("VP8 "u8) + 8;
        Assert.IsGreaterThan(7, payload, "Not a simple lossy WebP.");
        BinaryPrimitives.WriteUInt16LittleEndian(webp.AsSpan(payload + 6), 8909);
        BinaryPrimitives.WriteUInt16LittleEndian(webp.AsSpan(payload + 8), 4302);

        var allocated = Allocated(() => Assert.ThrowsExactly<ImageDecodeException>(() => WebPDecoder.Decode(webp)));

        Assert.IsLessThan(Allowance, allocated, $"Allocated {allocated:N0} bytes.");
    }

    private static long Allocated(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    /// <summary>The offset of the file's SOF0 marker.</summary>
    private static int FrameMarker(byte[] jpeg)
    {
        for (var i = 2; i + 1 < jpeg.Length; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xC0) return i;
        }

        throw new AssertFailedException("The encoder wrote no SOF0.");
    }

    private static byte[] WithFrameSize(byte[] jpeg, int width, int height)
    {
        var frame = FrameMarker(jpeg);
        BinaryPrimitives.WriteUInt16BigEndian(jpeg.AsSpan(frame + 5), (ushort)height);
        BinaryPrimitives.WriteUInt16BigEndian(jpeg.AsSpan(frame + 7), (ushort)width);
        return jpeg;
    }
}
