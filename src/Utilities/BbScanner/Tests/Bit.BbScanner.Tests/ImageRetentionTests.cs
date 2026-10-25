using System.Collections;
using System.Reflection;

namespace Bit.BbScanner.Tests;

/// <summary>
/// The decoder reuses its buffers from frame to frame, so a picture lingers in them after the camera that took it has
/// closed - and dropping a buffer does not erase it, since freed memory keeps its contents until it is reused. These
/// check that what the decoder keeps of an image is zeroed when it is told to forget it.
/// </summary>
[TestClass]
public class ImageRetentionTests
{
    /// <summary>The types that keep something derived from the last image in their own arrays.</summary>
    private static readonly HashSet<Type> ImageHolders =
    [
        typeof(BbBarcodeDecoder), typeof(BbHybridBinarizer), typeof(BbAdaptiveRowBinarizer), typeof(BbLuminanceBuffer),
        typeof(BbBitRow), typeof(BbDarkRegionFinder), typeof(BbCode93Reader), typeof(BbCodabarReader), typeof(BbDataBarReader),
        typeof(BbBarcodeDecoder).Assembly.GetType("Bit.BbScanner.BbPdf417Locator", throwOnError: true)!,
        typeof(BbBarcodeDecoder).Assembly.GetType("Bit.BbScanner.BbPdf417Locator+RunBuffer", throwOnError: true)!,
    ];

    [TestMethod]
    public void ForgettingTheImageZeroesEverythingTheDecoderKeptOfIt()
    {
        using var decoder = new BbBarcodeDecoder(new BbScannerOptions { Formats = BbBarcodeFormat.All, DuplicateSuppressionWindow = TimeSpan.Zero });

        // Every stage gets something to chew on: a matrix symbol, a linear one, a noisy background, and a still
        // image's exhaustive pass on top.
        using var qr = SyntheticImage.FromMatrix(QrEncoder.Encode("RETAINED", BbQrErrorCorrectionLevel.M, 2), 4);
        using var qrFrame = ImageTransforms.Frame(qr, 640, 480);
        SyntheticImage.AddNoise(qrFrame, 20, seed: 3);
        Assert.IsNotNull(decoder.Decode(qrFrame.View, suppressDuplicates: false));

        using var linear = SyntheticImage.FromLinear(LinearEncoders.DataBar("09521234543213"), 3, 99);
        using var turned = ImageTransforms.Rotate(linear, 12);
        using var linearFrame = ImageTransforms.Frame(turned, 640, 480);
        decoder.DecodeAll(linearFrame.View, 4, suppressDuplicates: false);

        var before = NonZeroArrays(decoder);
        Assert.IsNotEmpty(before, "Nothing held any image data, so this test no longer tests.");

        // Internal: the component calls it whenever its camera closes, and Dispose calls it too.
        typeof(BbBarcodeDecoder).GetMethod("ForgetImage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(decoder, null);

        var after = NonZeroArrays(decoder);
        Assert.IsEmpty(after, $"Still holding image data: {string.Join(", ", after)}");
    }

    [TestMethod]
    public void BuffersGoBackToTheSharedPoolCleared()
    {
        // The pool may hand the same array out again to anything in the process, so it must come back empty. Renting
        // straight after a return gets the same array back on this thread.
        var buffer = BbLuminanceBuffer.Rent(64, 64);
        buffer.Pixels.Fill(200);
        buffer.Dispose();
        var again = System.Buffers.ArrayPool<byte>.Shared.Rent(64 * 64);
        try
        {
            Assert.IsTrue(again.All(b => b == 0), "A luminance buffer went back to the pool with its picture in it.");
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(again);
        }

        var matrix = new BbBitMatrix(64, 64);
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                matrix[x, y] = true;
            }
        }

        matrix.Dispose();
        var bits = System.Buffers.ArrayPool<uint>.Shared.Rent(2 * 64);
        try
        {
            Assert.IsTrue(bits.All(b => b == 0), "A bit matrix went back to the pool with its picture in it.");
        }
        finally
        {
            System.Buffers.ArrayPool<uint>.Shared.Return(bits);
        }
    }

    /// <summary>
    /// Every array of numbers reachable from <paramref name="root"/> through the fields of the image holding types -
    /// a list's whole backing array included, not just its count - that still has a non-zero element.
    /// </summary>
    private static List<string> NonZeroArrays(object root)
    {
        var found = new List<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Walk(root, root.GetType().Name);
        return found;

        void Walk(object node, string path)
        {
            if (!seen.Add(node))
            {
                return;
            }

            if (node is IEnumerable sequence and not Array && node.GetType().IsGenericType &&
                node.GetType().GetGenericTypeDefinition() == typeof(List<>))
            {
                // A list of decoders: look inside each one.
                var index = 0;
                foreach (var item in sequence)
                {
                    if (item is not null && item.GetType().Assembly == typeof(BbBarcodeDecoder).Assembly)
                    {
                        Walk(item, $"{path}[{index}]");
                    }

                    index++;
                }

                return;
            }

            var holds = ImageHolders.Contains(node.GetType());
            for (var type = node.GetType(); type is not null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    var value = field.GetValue(node);
                    var name = $"{path}.{field.Name}";
                    switch (value)
                    {
                        case null:
                            break;
                        case Array array when holds && array.GetType().GetElementType()!.IsPrimitive:
                            if (!IsZero(array))
                            {
                                found.Add(name);
                            }

                            break;
                        case List<int> list when holds:
                            var items = (int[])typeof(List<int>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(list)!;
                            if (!IsZero(items))
                            {
                                found.Add(name);
                            }

                            break;
                        default:
                            if (value.GetType().Assembly == typeof(BbBarcodeDecoder).Assembly || value is IList)
                            {
                                Walk(value, name);
                            }

                            break;
                    }
                }
            }
        }

        static bool IsZero(Array array)
        {
            foreach (var item in array)
            {
                if (!item.Equals(Activator.CreateInstance(item.GetType())))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
