using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The output limit of <c>Compression.Decompress</c>, driven from JS: what is under test is where the
/// stream stops, and a payload big enough to show that is not worth carrying across .NET interop.
/// </summary>
[TestClass]
public class CompressionTests : ButilPageTest
{
    [TestMethod]
    public async Task Decompressing_Stops_At_The_Output_Limit()
    {
        // A megabyte of zeros packs into about a kilobyte - the shape of a decompression bomb. Exactly
        // the limit comes back whole; one byte less is refused rather than cut short.
        var result = await Page.EvaluateAsync<int[]>(
            """
            async () => {
                if (typeof DecompressionStream !== 'function') return [];
                const zeros = new Uint8Array(1024 * 1024);
                const packed = await BitButil.compression.compress(zeros, 'gzip');
                const exact = await BitButil.compression.decompress(packed, 'gzip', zeros.length);
                const over = await BitButil.compression.decompress(packed, 'gzip', zeros.length - 1);
                return [packed.length, exact?.length ?? -1, over === null ? 1 : 0];
            }
            """);

        if (result.Length == 0) Assert.Inconclusive("This browser has no DecompressionStream.");

        Assert.IsLessThan(64 * 1024, result[0], "the payload under test has to be far smaller than what it unpacks to");
        Assert.AreEqual(1024 * 1024, result[1], "output of exactly the limit must come back whole");
        Assert.AreEqual(1, result[2], "output past the limit must come back as null");
    }
}
