using System.Text.Json;
using System.ComponentModel;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;

namespace Boilerplate.Workbench.Infrastructure.Services;

[McpServerToolType]
public sealed partial class WorkbenchReviewTools
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    [AutoInject] private WorkbenchReviewAgent agent = default!;

    [McpServerTool(Name = "get_review", ReadOnly = true)]
    [Description("Returns the review comments people left on pages in the Workbench, each pinned to an element of a view (its device, theme, language, data state and sign-in), with the conversation so far and your task list for it. Also returns each page's review status. Call it once when you start, then call wait_for_review with the cursor it returns.")]
    public async Task<string> GetReview([Description("Also return comments that are done.")] bool includeDone = false, CancellationToken cancellationToken = default)
    {
        var threads = await agent.GetThreads(cancellationToken);
        var pages = await agent.GetPages(cancellationToken);
        var flows = await agent.GetFlows(cancellationToken);

        return JsonSerializer.Serialize(WorkbenchReviewJson.Describe(threads.Where(t => includeDone || t.Status is not WorkbenchReviewStatus.Done), pages, threads.Select(t => t.Sequence).DefaultIfEmpty(0).Max(), flows), jsonOptions);
    }

    [McpServerTool(Name = "wait_for_review", ReadOnly = true)]
    [Description("Waits until a reviewer adds a comment, replies to one or reopens one in the Workbench, and returns those comments as soon as it happens, or an empty list after about 50 seconds. Call it again right away with the cursor it returns, so you keep listening while the user reviews. Stop when the user says the review is over.")]
    public async Task<string> WaitForReview(McpServer server,
                                            [Description("The cursor from the previous get_review or wait_for_review.")] long cursor,
                                            CancellationToken cancellationToken = default)
    {
        var (next, threads) = await agent.Wait(AgentName(server), cursor, WorkbenchReviewAgent.MaxWait, cancellationToken);

        return JsonSerializer.Serialize(WorkbenchReviewJson.Describe(threads, null, next), jsonOptions);
    }

    [McpServerTool(Name = "reply_to_comment")]
    [Description("Replies on a comment's thread in the Workbench, where the reviewer sees it at once. Say what you changed, or ask what you need to know.")]
    public async Task<string> ReplyToComment(McpServer server, [Description("The comment's id.")] string commentId, [Description("Your reply, in the reviewer's language.")] string text, CancellationToken cancellationToken = default)
    {
        return await agent.Reply(AgentName(server), commentId, text, cancellationToken) ? "Sent." : "There is no comment with that id.";
    }

    [McpServerTool(Name = "set_comment_status", Idempotent = true)]
    [Description("Marks a comment as working (you started on it), done (the change is in and hot reload shows it) or open again. The reviewer sees the status on the comment's pin and in the list.")]
    public async Task<string> SetCommentStatus(McpServer server, [Description("The comment's id.")] string commentId, [Description("open, working or done.")] string status, CancellationToken cancellationToken = default)
    {
        if (Enum.TryParse<WorkbenchReviewStatus>(status, ignoreCase: true, out var parsed) is false)
            return "The status is open, working or done.";

        return await agent.SetStatus(AgentName(server), commentId, parsed, cancellationToken) ? "Done." : "There is no comment with that id.";
    }

    [McpServerTool(Name = "set_comment_tasks", Idempotent = true)]
    [Description("Shows your plan for a comment as a checklist under it in the Workbench, and ticks items as you finish them. Send the whole list each time.")]
    public async Task<string> SetCommentTasks(McpServer server,
                                              [Description("The comment's id.")] string commentId,
                                              [Description("The steps, in order.")] string[] tasks,
                                              [Description("How many of the steps, from the first, are done.")] int doneCount = 0,
                                              CancellationToken cancellationToken = default)
    {
        var list = tasks.Select((task, index) => new WorkbenchReviewTask(task, index < doneCount));

        return await agent.SetTasks(AgentName(server), commentId, list, cancellationToken) ? "Done." : "There is no comment with that id.";
    }

    [McpServerTool(Name = "set_page_status", Idempotent = true)]
    [Description("Sets a page's review status, shown on the Workbench's home page and next to the page's title: draft, inreview, approved (the user approved the design) or built (it is built into the app).")]
    public async Task<string> SetPageStatus(McpServer server, [Description("The page's key, as in /workbench/<key>.")] string page, [Description("draft, inreview, approved or built.")] string status, CancellationToken cancellationToken = default)
    {
        if (Enum.TryParse<WorkbenchPageStatus>(status, ignoreCase: true, out var parsed) is false)
            return "The status is draft, inreview, approved or built.";

        return await agent.SetPageStatus(AgentName(server), page, parsed, cancellationToken) ? "Done." : "There is no page with that key.";
    }

    [McpServerTool(Name = "save_flow")]
    [Description("Draws a flow on a board in the Workbench: the screens of a task side by side, each a page in the state that matters, like a list and then the dialog its button opens, with arrows for how the user gets from one to the next. The user opens it to understand the app and to talk it through in a meeting. Give the id of a flow you saved before to draw it again.")]
    public async Task<string> SaveFlow(McpServer server,
                                       [Description("The flow's name, in the user's language.")] string name,
                                       [Description("The screens, in the order the user meets them.")] WorkbenchFlowScreenRequest[] screens,
                                       [Description("The arrows between the screens.")] WorkbenchFlowLinkRequest[]? links = null,
                                       [Description("The id of a flow to draw again, or empty for a new one.")] string? flowId = null,
                                       CancellationToken cancellationToken = default)
    {
        var (id, problem) = await agent.SaveFlow(AgentName(server), new(name, screens, links, flowId, null), cancellationToken);

        return id is null ? $"Nothing was saved: {problem}" : $"Saved the flow {id}. The user sees it at {WorkbenchUrls.Flow(id)}.";
    }

    [McpServerTool(Name = "get_comment_screenshot", ReadOnly = true)]
    [Description("Returns a screenshot of the view a comment was left on, as it looked then: the same device, settings and what was typed or opened on the page. The comment's element is at its x and y, as fractions of the element's box.")]
    public async Task<ContentBlock> GetCommentScreenshot([Description("The comment's id.")] string commentId, CancellationToken cancellationToken = default)
    {
        return await agent.Screenshot(commentId, cancellationToken) is { } shot
            ? ImageContentBlock.FromBytes(shot.Png, "image/png")
            : new TextContentBlock { Text = "There is no comment with that id." };
    }

    private static string AgentName(McpServer server) => server.ClientInfo?.Title ?? server.ClientInfo?.Name ?? "Agent";
}
