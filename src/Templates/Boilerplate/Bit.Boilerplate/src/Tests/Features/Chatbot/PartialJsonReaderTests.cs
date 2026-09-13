using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// <see cref="PartialJsonReader{T}"/> is what shows an answer while the model is still writing it, by reading the
/// <c>answer</c> property out of every prefix of the document (See <c>AppAiChatPanel.RunChannel</c>). No parser
/// accepts a prefix - an unclosed string, a trailing backslash, an open array - so those are what is asserted here.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class PartialJsonReaderTests
{
    private static PartialJsonReader<AssistantReply> Reader() => new(AppJsonContext.Default.Options.GetTypeInfo<AssistantReply>());

    /// <summary>The whole point: every prefix reads as an answer, and what it says only ever grows.</summary>
    [TestMethod]
    public void EveryPrefixOfADocument_Should_ReadAsWhatHasArrivedSoFar()
    {
        const string answer = "**Amsterdam** to Berlin is not something I help with.\nI can help with \"this app\" \\ its cars ☕.";

        var document = JsonSerializer.Serialize(new AssistantReply
        {
            Answer = answer,
            FollowUpSuggestions = ["What cars do you sell?", "How do I install the app?", "Show me SUVs"]
        }, AppJsonContext.Default.AssistantReply);

        var reader = Reader();
        var longestSoFar = string.Empty;

        // One character at a time is the worst case the wire can produce, and it visits every state the scanner has.
        foreach (var character in document)
        {
            var readSoFar = reader.Append(character.ToString())?.Answer ?? string.Empty;

            Assert.StartsWith(readSoFar, answer,
                $"'{readSoFar}' is not the beginning of the answer being streamed, so the panel would show text the model never wrote.");

            Assert.IsGreaterThanOrEqualTo(longestSoFar.Length, readSoFar.Length,
                $"The answer went backwards, from '{longestSoFar}' to '{readSoFar}', which the user sees as text disappearing.");

            longestSoFar = readSoFar;
        }

        var completed = reader.Append(null);

        Assert.IsNotNull(completed);
        Assert.AreEqual(answer, completed.Answer);
        Assert.IsNotNull(completed.FollowUpSuggestions);
        Assert.HasCount(3, completed.FollowUpSuggestions);
        Assert.AreEqual("Show me SUVs", completed.FollowUpSuggestions[^1]);
    }

    /// <summary>
    /// Each of these is a prefix a parser rejects outright, and each is where an answer spends part of its life.
    /// </summary>
    [TestMethod]
    [DataRow("""{"answer":"Hel""", "Hel", DisplayName = "An unclosed string")]
    [DataRow("""{"answer":"Line one\n""", "Line one\n", DisplayName = "A completed escape")]
    [DataRow("""{"answer":"Line one\""", "Line one", DisplayName = "A backslash with nothing after it")]
    [DataRow("""{"answer":"Ready \u26""", "Ready ", DisplayName = "Half of a unicode escape")]
    [DataRow("""{"answer":"Ready ☕""", "Ready ☕", DisplayName = "A character that was not escaped")]
    [DataRow("""{"answer":"Quote \" inside""", "Quote \" inside", DisplayName = "An escaped quote inside the string")]
    [DataRow("""{"answer":"Done","followUpSuggestions":["Fir""", "Done", DisplayName = "An open array after the answer")]
    [DataRow("""{"answer":"Done","followUpSuggestions":["First",""", "Done", DisplayName = "A comma waiting on the next suggestion")]
    [DataRow("""{"answer":"Done","fol""", "Done", DisplayName = "A property name being written")]
    public void APrefix_Should_ReadAsTheAnswerItAlreadyContains(string prefix, string expected)
    {
        var partial = Reader().Append(prefix);

        Assert.IsNotNull(partial);
        Assert.AreEqual(expected, partial.Answer);
    }

    /// <summary>Nothing has been said yet, so the panel must be handed nothing rather than a guess.</summary>
    [TestMethod]
    [DataRow("", DisplayName = "Nothing at all")]
    [DataRow("{", DisplayName = "An object that has said nothing")]
    [DataRow("""{"answer":""", DisplayName = "A value that has not started")]
    [DataRow("""{"an""", DisplayName = "A property name that has not finished")]
    public void APrefixWithNoAnswerInIt_Should_ReadAsNothing(string prefix)
    {
        Assert.IsNull(Reader().Append(prefix)?.Answer);
    }

    /// <summary>
    /// A chunk that cannot be finished into a document must not blank what the previous one already said.
    /// </summary>
    [TestMethod]
    public void AChunkThatCannotBeCompleted_Should_LeaveTheLastReadableAnswerStanding()
    {
        var reader = Reader();

        // '{"answer":"Hi","' finishes as neither a property name nor a value, and neither does what follows it until
        // the array opens.
        foreach (var chunk in new[] { """{"answer":"Hi""", "\",\"", "followUpSuggestions\":[" })
        {
            var partial = reader.Append(chunk);

            Assert.IsNotNull(partial);
            Assert.AreEqual("Hi", partial.Answer, $"'{chunk}' blanked what was already on screen.");
        }
    }

    /// <summary>Each suggestion is readable the moment it is whole, not only when the array closes.</summary>
    [TestMethod]
    public void SuggestionsThatAreWhole_Should_BeReadBeforeTheRestArrive()
    {
        var partial = Reader().Append("""{"answer":"Done","followUpSuggestions":["First","Second","Thi""");

        Assert.IsNotNull(partial?.FollowUpSuggestions);
        Assert.HasCount(3, partial.FollowUpSuggestions);
        Assert.AreEqual("First", partial.FollowUpSuggestions[0]);
        Assert.AreEqual("Thi", partial.FollowUpSuggestions[2]);
    }

    /// <summary>
    /// A cancelled turn leaves the reply cut off mid word, and the server has to close it before splicing the rest of
    /// the turn on (See <c>AppChatbot.CloseTurn</c>) - by appending only, since what it sent it sent.
    /// </summary>
    [TestMethod]
    [DataRow("{", DisplayName = "An object that has said nothing")]
    [DataRow("""{"answer":""", DisplayName = "A value that has not started")]
    [DataRow("""{"answer":"Hel""", DisplayName = "An unclosed string")]
    [DataRow("""{"answer":"Line one\""", DisplayName = "A backslash with nothing after it")]
    [DataRow("""{"answer":"Ready \u26""", DisplayName = "Half of a unicode escape")]
    [DataRow("""{"answer":"Done","fol""", DisplayName = "A property name being written")]
    [DataRow("""{"answer":"Done","followUpSuggestions":["Fir""", DisplayName = "An open array")]
    [DataRow("""{"answer":"Done","followUpSuggestions":["First",""", DisplayName = "A comma waiting on the next suggestion")]
    public void ACutOffDocument_Should_BeFinishedByAppendingItsCompletion(string cutOff)
    {
        var reader = Reader();

        reader.Append(cutOff);

        var whole = cutOff + reader.Completion();

        var wholeReader = Reader();
        var completed = wholeReader.Append(whole);

        Assert.IsTrue(wholeReader.IsComplete, $"'{whole}' is still not a whole document.");
        Assert.IsNotNull(completed, $"'{whole}' does not read as a reply.");
    }

    /// <summary>And what the reply already said has to survive being finished off.</summary>
    [TestMethod]
    public void ACutOffAnswer_Should_SurviveItsCompletion()
    {
        var reader = Reader();

        const string cutOff = """{"answer":"The GLC starts at""";

        reader.Append(cutOff);

        var completed = Reader().Append(cutOff + reader.Completion());

        Assert.IsNotNull(completed);
        Assert.AreEqual("The GLC starts at", completed.Answer);
    }

    /// <summary>Nothing to finish once it has finished itself, which is the ordinary case.</summary>
    [TestMethod]
    public void AWholeDocument_Should_NeedNoCompletion()
    {
        var reader = Reader();

        reader.Append("""{"answer":"Done","followUpSuggestions":["One"]}""");

        Assert.IsTrue(reader.IsComplete);
        Assert.IsEmpty(reader.Completion());
    }

    /// <summary>Where the chunks are split is the provider's business, so the result must not depend on it.</summary>
    [TestMethod]
    public void WhereTheChunksAreSplit_Should_NotChangeWhatIsRead()
    {
        const string document = """{"answer":"A \"quoted\" word, a ☕ and a\nnewline.","followUpSuggestions":["One","Two"]}""";

        for (var size = 1; size <= document.Length; size++)
        {
            var reader = Reader();
            AssistantReply? partial = null;

            for (var at = 0; at < document.Length; at += size)
            {
                partial = reader.Append(document[at..Math.Min(at + size, document.Length)]);
            }

            Assert.IsNotNull(partial?.FollowUpSuggestions, $"Chunks of {size} characters read differently.");
            Assert.AreEqual("A \"quoted\" word, a ☕ and a\nnewline.", partial.Answer, $"Chunks of {size} characters read differently.");
            Assert.HasCount(2, partial.FollowUpSuggestions, $"Chunks of {size} characters read differently.");
        }
    }
}
