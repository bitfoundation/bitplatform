namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// A reviewer who isn't at this computer, like the user on their phone through a tunnel, reaches the agent only through an
/// invite link: its key becomes a cookie and leaves the address, and from then on their comments go into the review while
/// nothing else they do is saved.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ReviewInviteTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TheInvite_Should_GiveThisMachineTheKeysQueryString()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = server.CreateClient();

        var invite = await client.GetFromJsonAsync<JsonObject>("api/review/invite", TestContext.CancellationToken);

        Assert.IsNotNull(invite);

        var query = invite["query"];

        Assert.IsNotNull(query);
        Assert.AreEqual($"{WorkbenchReviewers.QueryName}={WorkbenchReviewers.Key}", query.GetValue<string>());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AnInviteLink_Should_LeaveTheAddressAndSetTheCookieOnlyForTheRightKey(bool isRightKey)
    {
        var server = await WorkbenchTestsInitializer.SharedServer;
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { BaseAddress = server.Address };

        var key = isRightKey ? WorkbenchReviewers.Key : new string('0', 48);
        using var request = WorkbenchTestServer.Remote(HttpMethod.Get, $"workbench/buildings?{WorkbenchReviewers.QueryName}={key}&theme=dark");
        using var response = await client.SendAsync(request, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.IsNotNull(response.Headers.Location);
        Assert.AreEqual("/workbench/buildings?theme=dark", response.Headers.Location.OriginalString);

        var cookie = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.SingleOrDefault(v => v.StartsWith($"{WorkbenchReviewers.CookieName}=", StringComparison.Ordinal))
            : null;

        if (isRightKey)
        {
            Assert.IsNotNull(cookie);
            Assert.IsTrue(cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase), cookie);
            Assert.IsTrue(cookie.Contains("secure", StringComparison.OrdinalIgnoreCase), cookie);
        }
        else
        {
            Assert.IsNull(cookie);
        }
    }

    [TestMethod]
    public async Task AnInvitedReviewer_Should_ReachTheAgentButChangeNothingElse()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;

        await using var local = await WorkbenchTestReviewer.Open(server);
        await using var invited = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Phone", isInvited: true);

        Assert.IsFalse(invited.Session.CanWrite);
        Assert.IsTrue(invited.Session.SavesComments);
        Assert.IsTrue(invited.Session.IsInvitedReviewer);
        Assert.IsFalse(local.Session.IsInvitedReviewer);

        // Waits until the server's copy, the one the agent reads, has it
        var id = await invited.Comment(server, "assistant", "Left from my phone");

        var thread = (await server.Agent.GetThreads(TestContext.CancellationToken)).Single(t => t.Id == id);

        Assert.AreEqual("Phone", thread.Messages[0].Author);
        Assert.IsNull(invited.Session.CreateFlow("Phone's flow"));

        await invited.Change(session => session.SetPageStatus("assistant", WorkbenchPageStatus.Built, "Phone"));

        var pages = await server.Agent.GetPages(TestContext.CancellationToken);

        Assert.AreNotEqual<WorkbenchPageStatus?>(WorkbenchPageStatus.Built, pages.TryGetValue("assistant", out var page) ? page.Status : null);
    }

    [TestMethod]
    public async Task AVisitorWithoutAnInvite_Should_StillKeepTheirCommentsInTheirTab()
    {
        var server = await WorkbenchTestsInitializer.SharedServer;

        await using var visitor = await WorkbenchTestReviewer.Open(server, isLocal: false, name: "Visitor");

        Assert.IsFalse(visitor.Session.SavesComments);
        Assert.IsFalse(visitor.Session.IsInvitedReviewer);
    }
}
