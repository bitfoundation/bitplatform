using Boilerplate.Server.Api.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// Every case here is something a synthesizer reads out loud when it is left in, and none of them is visible from a
/// build or audible as a failure: the reading simply becomes worse, one "star star" at a time.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class SpeakableTextTests
{
    [TestMethod]
    public void EmphasisAndHeadingMarkers_Should_NotBeSpoken()
    {
        Assert.AreEqual("Getting started\nbit platform is a set of tools.",
                        SpeakableText.FromMarkdown("## Getting started\n**bit platform** is a set of `tools`."),
                        "The markers of markdown are read out as words by a synthesizer - '## bold star star' - so none of them may survive.");
    }

    [TestMethod]
    public void ALink_Should_BeReducedToItsLabel()
    {
        Assert.AreEqual("Read the docs.",
                        SpeakableText.FromMarkdown("Read [the docs](https://bitplatform.dev/getting-started)."),
                        "A url spelled out character by character is the single worst thing a reading can contain, and the label already says what it is.");
    }

    [TestMethod]
    public void CodeBlocksImagesAndEmoji_Should_BeDropped()
    {
        var spoken = SpeakableText.FromMarkdown("""
            Install it:

            ```bash
            dotnet new install Bit.Boilerplate
            ```

            ![the logo](https://bitplatform.dev/logo.png)

            Done ✅
            """);

        Assert.AreEqual("Install it:\nDone", spoken,
                        "A command read letter by letter, an image that cannot be seen and a check mark read as 'check mark button' are all noise once heard.");
    }

    [TestMethod]
    public void ATableRow_Should_BeReadAsOneSentenceRatherThanAsPipes()
    {
        var spoken = SpeakableText.FromMarkdown("""
            | Name | Price |
            | ---- | ----- |
            | Mustang | 32000 |
            """);

        Assert.AreEqual("Name, Price\nMustang, 32000", spoken,
                        "The cell separators have to become pauses, and the dashes under the header carry nothing at all.");
    }

    /// <summary>
    /// A product arrives titled, pictured and linked with the same words, so its name would be read three times over.
    /// </summary>
    [TestMethod]
    public void ALineRepeatedByTheMarkup_Should_BeSaidOnce()
    {
        Assert.AreEqual("Ford Mustang",
                        SpeakableText.FromMarkdown("### Ford Mustang\n![Ford Mustang](https://example.com/mustang.png)\n[Ford Mustang](https://example.com/mustang)"),
                        "The picture is gone by now and the heading and the link are neighbouring lines of the same words.");
    }

    /// <summary>
    /// The one result the caller has to act on rather than pass along: nothing left to say is silence, and
    /// <c>SynthesizeSpeech</c> answers 204 instead of paying a provider for an empty utterance.
    /// </summary>
    [TestMethod]
    public void MarkdownThatIsNothingButMarkup_Should_LeaveNothingToSay()
    {
        Assert.AreEqual(string.Empty, SpeakableText.FromMarkdown("```\nvar x = 1;\n```\n\n---\n\n![](https://example.com/a.png)"));
    }

    /// <summary>
    /// An underscore is emphasis around a word and the joint inside an identifier both, and dropping it outright would
    /// hand the synthesizer one unpronounceable word.
    /// </summary>
    [TestMethod]
    public void AnUnderscore_Should_BecomeAWordBreakRatherThanDisappear()
    {
        Assert.AreEqual("Set AI OpenAI TextToSpeechApiKey", SpeakableText.FromMarkdown("Set AI_OpenAI_TextToSpeechApiKey"));
    }

    /// <summary>
    /// Nearly every answer is short enough for one request, and one request is one round trip and one charge - so an
    /// answer that did not need splitting must not be split.
    /// </summary>
    [TestMethod]
    public void AnAnswerAProviderWillTake_Should_BeSpokenInOnePiece()
    {
        Assert.AreSequenceEqual(new[] { "bit platform is a set of dotnet libraries." },
                                SpeakableText.Segment("bit platform is a set of dotnet libraries.").ToArray());
    }

    /// <summary>
    /// A long answer has to arrive in pieces, and a word lost or reordered on the way is not something a listener
    /// can tell apart from a bad reading.
    /// </summary>
    [TestMethod]
    public void ALongAnswer_Should_BeCutIntoPiecesThatPutItBackTogether()
    {
        var answer = string.Join('\n', Enumerable.Range(0, 300).Select(line => $"Line {line} of a long answer."));

        var pieces = SpeakableText.Segment(answer).ToArray();

        Assert.IsGreaterThan(1, pieces.Length, "An answer past what a provider takes has to be cut up.");
        Assert.IsTrue(pieces.All(piece => piece.Length <= 4096), "No piece may be longer than a provider will accept.");
        Assert.AreEqual(answer, string.Join('\n', pieces), "The pieces have to reconstruct the answer exactly.");
    }

    /// <summary>
    /// A piece ending mid-word is heard as two mispronounced ones either side of a pause, so the cut goes at the
    /// last boundary that fits rather than at the limit.
    /// </summary>
    [TestMethod]
    public void APiece_Should_EndAtAWordBoundary()
    {
        var answer = string.Join(' ', Enumerable.Repeat("word", 2000));

        var pieces = SpeakableText.Segment(answer).ToArray();

        Assert.IsGreaterThan(1, pieces.Length);
        Assert.IsTrue(pieces.All(piece => piece.EndsWith("word", StringComparison.Ordinal)),
                      "Cutting at the limit rather than at the last space would leave a piece ending in 'wo'.");
    }

    /// <summary>
    /// One unbroken run past the limit has no boundary to respect, and dropping it would lose the words outright.
    /// </summary>
    [TestMethod]
    public void AnUnbrokenRunPastTheLimit_Should_BeCutRatherThanDropped()
    {
        var answer = new string('a', 10_000);

        var pieces = SpeakableText.Segment(answer).ToArray();

        Assert.AreEqual(answer.Length, pieces.Sum(piece => piece.Length));
        Assert.IsTrue(pieces.All(piece => piece.Length <= 4096));
    }
}
