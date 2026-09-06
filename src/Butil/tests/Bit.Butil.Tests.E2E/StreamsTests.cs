using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Drives the harness against <c>/data/stream-sample.txt</c> in the sample host's wwwroot - exactly
/// 1024 bytes, so every assertion is an exact number rather than a shape.
/// </summary>
[TestClass]
public class StreamsTests : ButilPageTest
{
    [TestMethod]
    public async Task A_Response_Body_Reads_Through_To_Its_Content_Length()
    {
        // status / decoded bytes read / whether a Content-Length was present. The decoded count is
        // what can be asserted: Content-Length describes the transfer, so a dev server that
        // compresses the response reports the encoded size instead of 1024.
        await ClickAndExpectAsync("stream-read", "stream:read:200/1024/True");
    }

    [TestMethod]
    public async Task Reading_Locks_The_Stream_And_A_Locked_Stream_Cannot_Be_Teed()
    {
        // unlocked before the first read, locked after, and Tee refuses rather than half-working.
        await ClickAndExpectAsync("stream-locked", "stream:locked:False/True/True");
    }

    [TestMethod]
    public async Task Tee_Gives_Both_Branches_Every_Chunk()
    {
        await ClickAndExpectAsync("stream-tee", "stream:tee:1024/1024");
    }

    [TestMethod]
    public async Task A_Pipeline_Through_Both_Codecs_Returns_The_Original_Bytes()
    {
        // Compress, decompress, and land in a C# sink: 1024 bytes in, 1024 bytes out.
        await ClickAndExpectAsync("stream-roundtrip", "stream:roundtrip:1024");
    }

    [TestMethod]
    public async Task Writing_By_Hand_Reaches_The_Sink_And_Closes_Cleanly()
    {
        // 3 chunks, 10 + 20 + 30 bytes, and a close that reports no reason.
        await ClickAndExpectAsync("stream-write", "stream:write:3/60/True");
    }

    [TestMethod]
    public async Task A_Failed_Request_Reports_Its_Status_Rather_Than_Throwing()
    {
        await ClickAndExpectAsync("stream-missing", "stream:missing:404");
    }
}
