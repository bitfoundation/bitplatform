using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>The output limit of <c>Compression.Decompress</c>: what comes back at the limit, and past it.</summary>
[TestClass]
public class CompressionTests : ButilPageTest
{
    [TestMethod]
    public async Task Decompressing_Returns_Output_Up_To_The_Limit_And_Null_Past_It()
    {
        await ClickAndExpectAsync("compression-limit", "compression:limit:");
        var status = await CurrentStatusAsync();

        if (status.EndsWith(":unsupported", StringComparison.Ordinal)) Assert.Inconclusive("This browser has no CompressionStream.");

        // The payload is far smaller than what it unpacks to; output of exactly the limit comes back whole and
        // byte for byte, and output past it comes back as null.
        Assert.AreEqual($"compression:limit:True/{16 * 1024}/True/True", status);
    }
}
