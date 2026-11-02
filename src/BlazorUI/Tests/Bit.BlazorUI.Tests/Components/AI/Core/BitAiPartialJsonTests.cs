using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Core;

[TestClass]
public class BitAiPartialJsonTests
{
    [TestMethod]
    public void CompleteJsonParsesUnchanged()
    {
        var value = BitAiPartialJson.TryParse("""{"city":"Seattle","days":3}""");

        Assert.IsNotNull(value);
        Assert.AreEqual("Seattle", value!.Value.GetProperty("city").GetString());
        Assert.AreEqual(3, value.Value.GetProperty("days").GetInt32());
    }

    [TestMethod]
    public void OpenObjectIsClosed()
    {
        var value = BitAiPartialJson.TryParse("""{"city":"Seattle","days":3""");

        Assert.IsNotNull(value);
        Assert.AreEqual(3, value!.Value.GetProperty("days").GetInt32());
    }

    [TestMethod]
    public void PartialStringValueIsKept()
    {
        var value = BitAiPartialJson.TryParse("""{"city":"Sea""");

        Assert.IsNotNull(value);
        Assert.AreEqual("Sea", value!.Value.GetProperty("city").GetString());
    }

    [TestMethod]
    public void PartialKeyIsDropped()
    {
        var value = BitAiPartialJson.TryParse("""{"city":"Seattle","da""");

        Assert.IsNotNull(value);
        Assert.AreEqual("Seattle", value!.Value.GetProperty("city").GetString());
        Assert.IsFalse(value.Value.TryGetProperty("da", out _));
    }

    [TestMethod]
    public void KeyWithoutValueIsDropped()
    {
        var value = BitAiPartialJson.TryParse("""{"city":"Seattle","days":""");

        Assert.IsNotNull(value);
        Assert.ContainsSingle(value!.Value.EnumerateObject());
    }

    [TestMethod]
    public void TrailingCommaIsRemoved()
    {
        var value = BitAiPartialJson.TryParse("""{"a":1,""");

        Assert.IsNotNull(value);
        Assert.AreEqual(1, value!.Value.GetProperty("a").GetInt32());
    }

    [TestMethod]
    public void NestedContainersAreClosedInOrder()
    {
        var value = BitAiPartialJson.TryParse("""{"rows":[{"id":1},{"id":2""");

        Assert.IsNotNull(value);
        var rows = value!.Value.GetProperty("rows");
        Assert.AreEqual(2, rows.GetArrayLength());
        Assert.AreEqual(2, rows[1].GetProperty("id").GetInt32());
    }

    [TestMethod]
    public void ArrayOfPartialStringsIsUsable()
    {
        var value = BitAiPartialJson.TryParse("""["alpha","be""");

        Assert.IsNotNull(value);
        Assert.AreEqual(2, value!.Value.GetArrayLength());
        Assert.AreEqual("be", value.Value[1].GetString());
    }

    [TestMethod]
    public void IncompleteLiteralIsDropped()
    {
        var value = BitAiPartialJson.TryParse("""{"ok":tru""");

        Assert.IsNotNull(value);
        Assert.IsFalse(value!.Value.TryGetProperty("ok", out _));
    }

    [TestMethod]
    public void CompleteNumberAtEndIsKept()
    {
        var value = BitAiPartialJson.TryParse("""{"count":42""");

        Assert.IsNotNull(value);
        Assert.AreEqual(42, value!.Value.GetProperty("count").GetInt32());
    }

    [TestMethod]
    public void EscapedQuoteInsideStringDoesNotTerminateIt()
    {
        var value = BitAiPartialJson.TryParse("""{"text":"he said \"hi""");

        Assert.IsNotNull(value);
        Assert.AreEqual("he said \"hi", value!.Value.GetProperty("text").GetString());
    }

    [TestMethod]
    public void DanglingEscapeIsTrimmed()
    {
        var value = BitAiPartialJson.TryParse("""{"text":"line\""");

        Assert.IsNotNull(value);
        Assert.AreEqual("line", value!.Value.GetProperty("text").GetString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    public void EmptyInputYieldsNothing(string? input) => Assert.IsNull(BitAiPartialJson.TryParse(input));

    [TestMethod]
    public void GrowingPayloadNeverThrowsAndEndsCorrect()
    {
        const string complete = """{"items":[{"name":"a","score":1.5},{"name":"b","score":2}],"done":true}""";

        for (var length = 1; length < complete.Length; length++)
        {
            var partial = BitAiPartialJson.TryParse(complete[..length]);
            if (partial is { } value) Assert.AreNotEqual(JsonValueKind.Undefined, value.ValueKind);
        }

        var final = BitAiPartialJson.TryParse(complete);
        Assert.IsNotNull(final);
        Assert.IsTrue(final!.Value.GetProperty("done").GetBoolean());
    }

    [TestMethod]
    [DataRow("""{"a":"x","b":1.""")]
    [DataRow("""{"a":"x","b":-""")]
    [DataRow("""{"a":"x","b":1e""")]
    [DataRow("""{"a":"x","b":1e+""")]
    [DataRow("""{"a":"x","b":NaN""")]
    public void AnUnfinishedNumberIsDroppedWithoutLosingTheRestOfTheDocument(string input)
    {
        // double.TryParse accepts "1." and "NaN", which used to be kept and made the repair invalid.
        var value = BitAiPartialJson.TryParse(input);

        Assert.IsNotNull(value);
        Assert.AreEqual("x", value!.Value.GetProperty("a").GetString());
        Assert.IsFalse(value.Value.TryGetProperty("b", out _));
    }

    [TestMethod]
    public void AnEscapedBackslashFollowedByUIsNotMistakenForAUnicodeEscape()
    {
        var value = BitAiPartialJson.TryParse("""{"t":"a\\u12""");

        Assert.IsNotNull(value);
        Assert.AreEqual("a\\u12", value!.Value.GetProperty("t").GetString());
    }

    [TestMethod]
    [DataRow("""{"t":"x\uD83D""")]
    [DataRow("""{"t":"x\uD83D\uDE""")]
    public void HalfAnEscapedSurrogatePairIsTrimmedSoTheStringStaysReadable(string input)
    {
        var value = BitAiPartialJson.TryParse(input);

        Assert.IsNotNull(value);
        Assert.AreEqual("x", value!.Value.GetProperty("t").GetString());
    }

    [TestMethod]
    public void HalfARawSurrogatePairIsTrimmedRatherThanThrown()
    {
        // Kept out of InlineData: attribute strings are stored as UTF-8 and cannot hold a lone surrogate.
        var value = BitAiPartialJson.TryParse("{\"t\":\"x\uD83D");

        Assert.IsNotNull(value);
        Assert.AreEqual("x", value!.Value.GetProperty("t").GetString());
    }

    [TestMethod]
    public void TextThatCannotBeTranscodedIsReportedAsInvalidRatherThanThrown()
    {
        Assert.IsNull(BitAiPartialJson.TryParseComplete("{\"t\":\"x\uD83D\"}"));
    }

    [TestMethod]
    public void GrowingEscapedEmojiNeverYieldsAnUnreadableString()
    {
        const string complete = """{"t":"ok 😀 \\u done"}""";

        for (var length = 1; length <= complete.Length; length++)
        {
            if (BitAiPartialJson.TryParse(complete[..length]) is { ValueKind: JsonValueKind.Object } value
                && value.TryGetProperty("t", out var text))
            {
                _ = text.GetString();
            }
        }
    }
}
