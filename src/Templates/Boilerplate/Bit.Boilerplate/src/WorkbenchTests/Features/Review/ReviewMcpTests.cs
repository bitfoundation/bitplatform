using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Boilerplate.WorkbenchTests.Features.Review;

/// <summary>
/// The Workbench's MCP server, the way a coding agent listed as <c>workbench</c> in the agent settings uses it. Not in
/// parallel: the agent last seen on the review is one for the whole document.
/// </summary>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public class ReviewMcpTests
{
    public TestContext TestContext { get; set; } = default!;

    private WorkbenchTestServer server = default!;
    private McpClient client = default!;
    private WorkbenchTestReviewer reviewer = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        server = await WorkbenchTestsInitializer.SharedServer;
        reviewer = await WorkbenchTestReviewer.Open(server);

        var transport = new HttpClientTransport(new() { Endpoint = new(server.Address, "mcp"), TransportMode = HttpTransportMode.StreamableHttp });

        client = await McpClient.CreateAsync(transport, new() { ClientInfo = new() { Name = "test-agent", Title = "Test Agent", Version = "1.0.0" } }, cancellationToken: TestContext.CancellationToken);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await client.DisposeAsync();
        await reviewer.DisposeAsync();
    }

    private async Task<string> Call(string tool, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: TestContext.CancellationToken);

        Assert.AreNotEqual(true, result.IsError, $"{tool} failed.");

        return string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
    }

    [TestMethod]
    public async Task TheServer_Should_OfferEveryReviewTool()
    {
        var tools = (await client.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(t => t.Name).Order().ToArray();

        Assert.AreSequenceEqual(new[] { "get_comment_screenshot", "get_review", "reply_to_comment", "save_flow", "set_comment_status", "set_comment_tasks", "set_page_status", "wait_for_review" }, tools);
        Assert.AreEqual("workbench", client.ServerInfo.Name);
    }

    [TestMethod]
    public async Task TheReviewLoop_Should_WorkOverMcp()
    {
        var id = await reviewer.Comment(server, "pos-dashboard", "Make the greeting shorter");

        var review = JsonNode.Parse(await Call("get_review", []))!;
        var cursor = (long)review["cursor"]!;

        Assert.Contains(c => (string?)c!["id"] == id, review["comments"]!.AsArray());

        Assert.AreEqual("Done.", await Call("set_comment_status", new() { ["commentId"] = id, ["status"] = "working" }));

        var agent = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.Agent), a => a?.ThreadId == id);

        Assert.AreEqual("Test Agent", agent!.Name, "The agent shows under the title its client introduced itself with.");

        Assert.AreEqual("Done.", await Call("set_comment_tasks", new() { ["commentId"] = id, ["tasks"] = new[] { "Shorten it", "Check RTL" }, ["doneCount"] = 1 }));
        Assert.AreEqual("Sent.", await Call("reply_to_comment", new() { ["commentId"] = id, ["text"] = "Shortened to \"Hi, Sarah\"." }));
        Assert.AreEqual("Done.", await Call("set_comment_status", new() { ["commentId"] = id, ["status"] = "done" }));

        var thread = await WorkbenchTestReviewer.WaitFor(() => Task.FromResult(reviewer.Session.Threads.Single(t => t.Id == id)), t => t.Status is WorkbenchReviewStatus.Done);

        Assert.AreEqual(("Test Agent", "Shortened to \"Hi, Sarah\"."), (thread.Messages[^1].Author, thread.Messages[^1].Text));
        Assert.AreSequenceEqual(new[] { new WorkbenchReviewTask("Shorten it", true), new WorkbenchReviewTask("Check RTL", false) }, thread.Tasks);

        reviewer.Session.Reply(id, "Sarah", "One more thing");

        var waited = JsonNode.Parse(await Call("wait_for_review", new() { ["cursor"] = cursor }))!;

        Assert.Contains(c => (string?)c!["id"] == id && (string?)c["status"] == "open", waited["comments"]!.AsArray(), "The reviewer's reply reopens the comment and reaches the agent.");
    }

    [TestMethod]
    public async Task AToolCallWithAWrongValue_Should_SayWhatIsWrong()
    {
        Assert.AreEqual("There is no comment with that id.", await Call("reply_to_comment", new() { ["commentId"] = "missing", ["text"] = "Hi" }));
        Assert.AreEqual("The status is open, working or done.", await Call("set_comment_status", new() { ["commentId"] = "missing", ["status"] = "finished" }));
        Assert.AreEqual("There is no page with that key.", await Call("set_page_status", new() { ["page"] = "missing", ["status"] = "approved" }));
        Assert.AreEqual("The status is draft, inreview, approved or built.", await Call("set_page_status", new() { ["page"] = "theme", ["status"] = "shipped" }));
        Assert.StartsWith("Nothing was saved:", await Call("save_flow", new() { ["name"] = "Flow", ["screens"] = Array.Empty<object>() }));
    }

    [TestMethod]
    public async Task SavingAFlow_Should_GiveTheBoardsAddress()
    {
        var answer = await Call("save_flow", new()
        {
            ["name"] = "The theme",
            ["screens"] = new[] { new { key = "light", page = "theme", query = "" }, new { key = "dark", page = "theme", query = "?theme=dark" } },
            ["links"] = new[] { new { from = "light", to = "dark", label = "Switch the theme" } }
        });

        var id = Regex.Match(answer, "Saved the flow (?<id>[0-9a-f]+)\\.").Groups["id"].Value;

        Assert.IsFalse(string.IsNullOrEmpty(id), answer);
        Assert.Contains($"/flows/{id}", answer);
        Assert.HasCount(2, (await server.Agent.GetFlows(TestContext.CancellationToken)).Single(f => f.Id == id).Screens);
    }

    [TestMethod]
    public async Task GettingAScreenshotOfACommentThatIsGone_Should_SaySo()
    {
        var result = await client.CallToolAsync("get_comment_screenshot", new Dictionary<string, object?> { ["commentId"] = "missing" }, cancellationToken: TestContext.CancellationToken);

        Assert.AreEqual("There is no comment with that id.", result.Content.OfType<TextContentBlock>().Single().Text);
    }
}
