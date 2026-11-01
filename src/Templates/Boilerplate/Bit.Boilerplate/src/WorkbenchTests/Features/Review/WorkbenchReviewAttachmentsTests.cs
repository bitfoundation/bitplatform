namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// The page state kept with a comment is a file named after an id that comes from the review document, which a browser
/// can write, so the id must never lead out of the folder.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchReviewAttachmentsTests
{
    private readonly WorkbenchReviewAttachments attachments = new();

    [TestMethod]
    public async Task AState_Should_BeReadBackAndDeleted()
    {
        var id = Guid.NewGuid().ToString("N")[..16];

        Assert.IsTrue(await attachments.SaveState(id, """{"pos-dashboard":{"isEditing":true}}"""));
        Assert.AreEqual("""{"pos-dashboard":{"isEditing":true}}""", await attachments.ReadState(id));

        attachments.DeleteState(id);

        Assert.IsNull(await attachments.ReadState(id));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not json")]
    [DataRow("[]")]
    public async Task AStateThatIsNotAJsonObject_Should_NotBeSaved(string? state)
    {
        Assert.IsFalse(await attachments.SaveState(Guid.NewGuid().ToString("N")[..16], state));
    }

    [TestMethod]
    [DataRow("../../escape")]
    [DataRow("..")]
    [DataRow("a/b")]
    [DataRow("a\\b")]
    [DataRow("C:")]
    [DataRow("")]
    [DataRow("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task AnIdThatIsNotLettersAndDigits_Should_BeRefused(string id)
    {
        Assert.IsFalse(await attachments.SaveState(id, "{}"));
        Assert.IsNull(await attachments.ReadState(id));

        attachments.DeleteState(id);
    }
}
