using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>The output limit of <c>Compression.Decompress</c>: where the stream stops.</summary>
[TestClass]
public class CompressionTests : ButilPageTest
{
    [TestMethod]
    public async Task Decompressing_Stops_At_The_Output_Limit()
    {
        await ClickAndExpectAsync("compression-limit", "compression:limit:");
        var status = await CurrentStatusAsync();

        if (status.EndsWith(":unsupported", StringComparison.Ordinal)) Assert.Inconclusive("This browser has no CompressionStream.");

        // The payload is far smaller than what it unpacks to; output of exactly the limit comes back whole, and
        // output past it comes back as null.
        Assert.AreEqual($"compression:limit:True/{16 * 1024}/True", status);
    }
}
