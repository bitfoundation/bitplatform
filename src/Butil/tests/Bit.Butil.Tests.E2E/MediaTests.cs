using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// The media APIs that can be exercised without a prompt, a device or a network fetch. Everything
/// here runs against the deterministic harness page, so the codecs asked for are the ones every
/// Chromium build has (VP8 in WebM) and nothing needs an autoplay gesture.
/// </summary>
[TestClass]
public class MediaTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task MediaCapabilities_DecodingInfo_Answers_For_A_Known_Codec()
    {
        // A configuration the engine can satisfy: an answer object (not null), and supported.
        await ClickAndExpectAsync("mediacaps-decoding", "mediacaps:decoding:True/True");
    }

    [TestMethod]
    public async Task MediaSource_Reports_Support_For_A_Known_Segment_Type()
    {
        await ClickAndExpectAsync("mediasource-types", "mediasource:types:True/True");
    }

    [TestMethod]
    public async Task MediaSource_Open_Attaches_And_Accepts_A_Source_Buffer()
    {
        // The handle is only handed out once the element has adopted the source, so the state is
        // Open by construction; the buffer is created, and nothing is buffered before an append.
        await ClickAndExpectAsync("mediasource-open", "mediasource:open:Open/True/0");
    }

    [TestMethod]
    public async Task WebCodecs_Encodes_A_Frame_And_Decodes_It_Back()
    {
        // At least one chunk out of the encoder, and at least one frame back out of the decoder.
        await ClickAndExpectAsync("webcodecs-roundtrip", "webcodecs:roundtrip:True/True");
    }

    [TestMethod]
    public async Task WebAudio_Graph_Connects_And_The_Analyser_Reports_Its_Buffers()
    {
        // fftSize 512 gives 512 time-domain samples and 256 frequency bins, and the context reports
        // a sample rate - all of which hold while the context is still suspended.
        await ClickAndExpectAsync("webaudio-graph", "webaudio:graph:512/256/True");
    }

    [TestMethod]
    public async Task WebAudio_DecodeAudioData_Reports_The_Decoded_Buffer()
    {
        // Mono, and half a second long.
        await ClickAndExpectAsync("webaudio-decode", "webaudio:decode:1/True");
    }

    [TestMethod]
    public async Task EncryptedMedia_Completes_A_Clear_Key_Licence_Exchange()
    {
        // Request generated, licence fed back, and the key reported usable - no server involved.
        await ClickAndExpectAsync("eme-clearkey", "eme:clearkey:True/True");
    }

    [TestMethod]
    public async Task Media_Support_Probes_All_Answer()
    {
        // Which way each probe goes is the engine's business; that every one of them completes and
        // reports a value is this library's.
        await ClickAndExpectAsync("media-support", "media:support:");
    }
}
