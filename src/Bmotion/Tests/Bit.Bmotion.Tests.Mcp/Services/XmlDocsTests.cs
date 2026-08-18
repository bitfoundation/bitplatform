namespace Bit.Bmotion.Tests.Mcp.Services;

/// <summary>
/// The XML documentation reader. It is the only source of prose in the API reference, and it is
/// deliberately forgiving - a missing or malformed file degrades to no documentation rather than
/// taking the tools down. These tests hold it to both halves of that: it finds the documentation
/// when it is there, and it flattens the markup into something an agent can read as a sentence.
/// </summary>
[TestClass]
public class XmlDocsTests
{
    [TestMethod]
    public void GetSummary_FindsTheDocumentationOfAKnownType()
    {
        var summary = BmotionXmlDocs.GetSummary($"T:{typeof(BmSpring).FullName}");

        Assert.IsFalse(string.IsNullOrWhiteSpace(summary), "BmSpring has no summary, so the XML file was not loaded.");
    }

    [TestMethod]
    public void GetSummary_FindsTheDocumentationOfAMember()
    {
        var summary = BmotionXmlDocs.GetSummary($"P:{typeof(BmSpring).FullName}.{nameof(BmSpring.Stiffness)}");

        Assert.IsFalse(string.IsNullOrWhiteSpace(summary));
    }

    [TestMethod]
    [DataRow("T:Bit.Bmotion.NoSuchType")]
    [DataRow("P:Bit.Bmotion.BmSpring.NoSuchMember")]
    [DataRow("")]
    [DataRow("nonsense")]
    public void GetSummary_AnUnknownId_IsNullRatherThanAThrow(string id)
    {
        Assert.IsNull(BmotionXmlDocs.GetSummary(id));
        Assert.IsNull(BmotionXmlDocs.GetRemarks(id));
    }

    /// <summary>
    /// A cref is a fully qualified id in the file and a name in the sentence: "BmSpring", not
    /// "T:Bit.Bmotion.BmSpring".
    /// </summary>
    [TestMethod]
    public void GetSummary_CrefReferences_ReadAsNames()
    {
        var texts = BmotionApiCatalog.Types
            .Select(type => BmotionApiCatalog.GetTypeDetails(type.Name)!)
            .SelectMany(details => details.Members.Select(member => member.Summary).Append(details.Summary))
            .Where(text => text is not null)
            .ToArray();

        Assert.AreNotEqual(0, texts.Length);

        foreach (var text in texts)
        {
            foreach (var prefix in new[] { "T:Bit.Bmotion", "P:Bit.Bmotion", "M:Bit.Bmotion", "F:Bit.Bmotion" })
            {
                Assert.IsFalse(text!.Contains(prefix, StringComparison.Ordinal),
                               $"A documentation id leaked into prose: {text}");
            }
        }
    }

    /// <summary>
    /// While the prose is unwrapped, a code sample is parked under a U+0001 placeholder so its own
    /// line breaks survive the whitespace passes. The placeholder itself must always be put back.
    /// </summary>
    [TestMethod]
    public void GetSummary_TheCodeSamplePlaceholder_IsNeverLeftInTheText()
    {
        var texts = BmotionApiCatalog.Types
            .Select(type => BmotionApiCatalog.GetTypeDetails(type.Name)!)
            .SelectMany(details => details.Members.SelectMany(member => new[] { member.Summary, member.Remarks })
                                                  .Concat([details.Summary, details.Remarks]))
            .Where(text => text is not null)
            .ToArray();

        Assert.AreNotEqual(0, texts.Length, "Nothing in the library is documented, so the XML file was not loaded.");

        foreach (var text in texts)
        {
            Assert.IsFalse(text!.Contains((char)1), $"A code-sample placeholder was left in the text: {text}");
        }
    }

    [TestMethod]
    public void GetSummary_TheProseIsUnwrapped_NotLeftAtTheSourcesLineWidth()
    {
        var summary = BmotionXmlDocs.GetSummary($"T:{typeof(BmSpring).FullName}")!;

        // Source comments wrap at around a hundred columns; a summary that still has single newlines
        // in the middle of sentences was not unwrapped.
        var brokenSentences = summary.Split('\n')
            .Where((line, index) => index > 0 && line.Length > 0 && char.IsLower(line[0]))
            .ToArray();

        Assert.AreEqual(0, brokenSentences.Length, $"The summary is still wrapped:\n{summary}");
    }
}
