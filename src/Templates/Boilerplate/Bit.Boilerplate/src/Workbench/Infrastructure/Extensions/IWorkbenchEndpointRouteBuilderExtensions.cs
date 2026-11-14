using System.Text;
using System.IO.Compression;
using Bit.Bcrdt.Server;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Extensions;

/// <summary>
/// Maps the Workbench's HTTP endpoints: screenshots, thumbnails, review sheets, checks and the review API the agent uses.
/// </summary>
public static class IWorkbenchEndpointRouteBuilderExtensions
{
    private const int MaxScreenshotsPerArchive = 24;

    extension(IEndpointRouteBuilder endpoints)
    {
        public void MapWorkbenchEndpoints()
        {
            // The views listed in shots, as one zip of PNGs.
            endpoints.MapGet("api/screenshots/zip", async (string? shots, HttpContext context, WorkbenchScreenshotService screenshotService, WorkbenchScreenshotLimiter limiter, CancellationToken cancellationToken) =>
            {
                var entries = WorkbenchUrls.DecodeShots(shots)
                    .Select(shot => (Page: WorkbenchPageCatalog.Find(shot.PageKey), shot.Query))
                    .ToArray();

                if (entries.Length is 0 or > MaxScreenshotsPerArchive || entries.Any(entry => entry.Page is null))
                    return Results.BadRequest();

                if (limiter.TryAcquire(context, entries.Length) is false)
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);

                var captures = await CaptureAll(entries, (entry, token) => screenshotService.CapturePage(entry.Page!, entry.Query, token), cancellationToken);

                using var archiveStream = new MemoryStream();

                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var index = 0;

                    foreach (var (png, fileName) in captures)
                    {
                        var entry = archive.CreateEntry($"{++index:00}-{fileName}", CompressionLevel.NoCompression);

                        await using var entryStream = await entry.OpenAsync(cancellationToken);
                        await entryStream.WriteAsync(png, cancellationToken);
                    }
                }

                var archiveName = entries.Select(entry => entry.Page!.Key).Distinct().Count() is 1 ? $"{entries[0].Page!.Key}-screenshots.zip" : "screenshots.zip";

                return Results.File(archiveStream.ToArray(), "application/zip", archiveName);
            });

            // One view as a PNG at the device's real pixel size.
            endpoints.MapGet("api/screenshots/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, WorkbenchScreenshotLimiter limiter, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                if (limiter.TryAcquire(context) is false)
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);

                var (png, fileName) = await screenshotService.CapturePage(page, context.Request.QueryString.Value, cancellationToken);

                return Results.File(png, "image/png", fileName);
            });

            // The home page's thumbnails, captured once per page, device kind and theme.
            endpoints.MapGet("api/thumbnails/{pageKey}", async (string pageKey, bool? phone, WorkbenchThemeMode? theme, HttpContext context, WorkbenchScreenshotService screenshotService) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                var png = await screenshotService.GetThumbnail(page.Key, phone is true, theme is WorkbenchThemeMode.Dark ? WorkbenchThemeMode.Dark : WorkbenchThemeMode.Light);

                // Revalidated every time, so the home page shows a design change once the Workbench restarts.
                context.Response.Headers.CacheControl = "no-cache";

                return Results.File(png, "image/png");
            });

            // The views listed in shots and the page's comments, in one HTML or PDF file.
            endpoints.MapGet("api/review-sheet/{pageKey}", async (string pageKey,
                                                                 string? shots,
                                                                 string? format,
                                                                 HttpContext context,
                                                                 WorkbenchReviewAgent agent,
                                                                 WorkbenchScreenshotService screenshotService,
                                                                 WorkbenchScreenshotLimiter limiter,
                                                                 IStringLocalizer<WorkbenchStrings> localizer,
                                                                 CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);
                var entries = WorkbenchUrls.DecodeShots(shots).Where(entry => entry.PageKey == page?.Key).ToArray();

                if (page is null || entries.Length is 0 or > MaxScreenshotsPerArchive)
                    return Results.BadRequest();

                if (limiter.TryAcquire(context, entries.Length) is false)
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);

                var views = await CaptureAll(entries, async (entry, token) => (WorkbenchScenario.Parse(entry.Query), (await screenshotService.CapturePage(page, entry.Query, token)).Png), cancellationToken);

                var threads = (await agent.GetThreads(cancellationToken)).Where(t => t.PageKey == page.Key).ToList();
                var review = (await agent.GetPages(cancellationToken)).GetValueOrDefault(page.Key);

                var html = WorkbenchReviewSheet.Build(page, views, threads, review, localizer);

                return format is "pdf"
                    ? Results.File(await screenshotService.RenderPdf(html, cancellationToken), "application/pdf", $"{page.Key}-review.pdf")
                    : Results.File(Encoding.UTF8.GetBytes(html), "text/html", $"{page.Key}-review.html");
            });

            // The accessibility and theme-color checks of one view, as JSON.
            endpoints.MapGet("api/checks/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, WorkbenchScreenshotLimiter limiter, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                if (limiter.TryAcquire(context) is false)
                    return Results.StatusCode(StatusCodes.Status429TooManyRequests);

                return Results.Text(await screenshotService.RunChecks(page, context.Request.QueryString.Value, cancellationToken), "application/json");
            });

            endpoints.MapReviewEndpoints();
        }

        /// <summary>
        /// Maps the review document's relay and the HTTP review API. The relay takes only the Workbench's own
        /// connections; the API answers only local requests (see Program.Middlewares).
        /// </summary>
        private void MapReviewEndpoints()
        {
            // The circuits' copies of the review document sync with the server's copy here, proven by the relay's secret header.
            endpoints.MapBitBcrdtWebSocket(WorkbenchReviewRelay.Route,
                                           (context, documentId) => ValueTask.FromResult(WorkbenchReviewRelay.IsTrusted(context, documentId)),
                                           new() { DocumentHost = endpoints.ServiceProvider.GetRequiredService<BcrdtDocumentHost>() });

            var review = endpoints.MapGroup("api/review");

            // The query string that turns a link to this Workbench, like a tunnel's, into an invite whose comments reach the agent.
            review.MapGet("invite", () => Results.Json(new { query = $"{WorkbenchReviewers.QueryName}={WorkbenchReviewers.Key}" }));

            // The comments of the pages named in pages (keys separated by commas), or of every page; several chats each read their own.
            review.MapGet("", async (bool? includeDone, string? pages, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                return Results.Json(await agent.DescribeReview(includeDone is true, PageKeys(pages), cancellationToken));
            });

            // A long poll: answers once a reviewer adds, answers or reopens a comment on one of the pages, or with nothing within a minute.
            review.MapGet("wait", async (long? cursor, string? agent, string? pages, WorkbenchReviewAgent reviewAgent, CancellationToken cancellationToken) =>
            {
                var (next, threads) = await reviewAgent.Wait(AgentName(agent), cursor ?? 0, WorkbenchReviewAgent.MaxWait, cancellationToken, PageKeys(pages));

                return Results.Json(WorkbenchReviewJson.Describe(threads, null, next));
            });

            review.MapPost("{id}/reply", async (string id, WorkbenchReviewReplyRequest request, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                    return Results.BadRequest();

                return await agent.Reply(AgentName(request.Agent), id, request.Text, cancellationToken) ? Results.NoContent() : Results.NotFound();
            });

            review.MapPost("{id}/status", async (string id, WorkbenchReviewStatusRequest request, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                if (Enum.TryParse<WorkbenchReviewStatus>(request.Status, ignoreCase: true, out var status) is false)
                    return Results.BadRequest();

                return await agent.SetStatus(AgentName(request.Agent), id, status, cancellationToken) ? Results.NoContent() : Results.NotFound();
            });

            review.MapPut("{id}/tasks", async (string id, WorkbenchReviewTasksRequest request, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                return await agent.SetTasks(AgentName(request.Agent), id, request.Tasks ?? [], cancellationToken) ? Results.NoContent() : Results.NotFound();
            });

            review.MapPost("pages/{pageKey}/status", async (string pageKey, WorkbenchReviewStatusRequest request, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                if (Enum.TryParse<WorkbenchPageStatus>(request.Status, ignoreCase: true, out var status) is false)
                    return Results.BadRequest();

                return await agent.SetPageStatus(AgentName(request.Agent), pageKey, status, cancellationToken) ? Results.NoContent() : Results.NotFound();
            });

            review.MapPost("flows", async (WorkbenchFlowRequest request, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                var (id, problem) = await agent.SaveFlow(AgentName(request.Agent), request, cancellationToken);

                return id is null ? Results.BadRequest(problem) : Results.Ok(new { Id = id, Board = WorkbenchUrls.Flow(id) });
            });

            review.MapGet("{id}/screenshot", async (string id, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                return await agent.Screenshot(id, cancellationToken) is { } shot ? Results.File(shot.Png, "image/png", shot.FileName) : Results.NotFound();
            });

            endpoints.MapProgressEndpoints();
        }

        /// <summary>
        /// Maps the agents' tasks of the Progress page: start, change, read, wait for the user and remove. Like the review's,
        /// they answer only local requests in dev mode (see Program.Middlewares).
        /// </summary>
        private void MapProgressEndpoints()
        {
            // Which copy of the project this Workbench runs from, as a chat in another worktree asks before it uses or restarts it.
            endpoints.MapGet("api/workbench", () => Results.Json(new
            {
                WorkingTree = WorkbenchFolders.WorkingTree,
                WorkbenchFolders.Project,
                WorkbenchFolders.Branch,
                Data = WorkbenchFolders.Reviews,
                ProcessId = Environment.ProcessId,
                StartedAt = startedAt
            }));

            var progress = endpoints.MapGroup("api/progress");

            progress.MapGet("", async (WorkbenchProgressAgent agent, CancellationToken cancellationToken) =>
            {
                return Results.Json(WorkbenchProgressJson.DescribeList(await agent.GetTasks(cancellationToken)));
            });

            progress.MapPost("", async (WorkbenchProgressUpdate update, WorkbenchProgressAgent agent, CancellationToken cancellationToken) =>
            {
                var (id, problem) = await agent.Create(AgentName(update.Agent), update, cancellationToken);

                return id is null ? Results.BadRequest(problem) : Results.Created(WorkbenchUrls.ProgressTask(id), new { Id = id, Page = WorkbenchUrls.ProgressTask(id) });
            });

            progress.MapGet("{id}", async (string id, WorkbenchProgressAgent agent, CancellationToken cancellationToken) =>
            {
                return await agent.GetTask(id, cancellationToken) is { } task ? Results.Json(WorkbenchProgressJson.Describe(task)) : Results.NotFound();
            });

            progress.MapPatch("{id}", async (string id, WorkbenchProgressUpdate update, WorkbenchProgressAgent agent, CancellationToken cancellationToken) =>
            {
                var (found, problem) = await agent.Update(AgentName(update.Agent), id, update, cancellationToken);

                return found is false ? Results.NotFound() : problem is null ? Results.NoContent() : Results.BadRequest(problem);
            });

            // A long poll: answers once the user answers, finishes a step of theirs or writes to the agent, or with nothing within a minute.
            progress.MapGet("{id}/wait", async (string id, long? cursor, string? agent, WorkbenchProgressAgent progressAgent, CancellationToken cancellationToken) =>
            {
                return await progressAgent.Wait(AgentName(agent), id, cursor ?? 0, WorkbenchProgressAgent.MaxWait, cancellationToken) is { } result
                    ? Results.Json(WorkbenchProgressJson.DescribeWait(result.Cursor, result.Task, result.Events))
                    : Results.NotFound();
            });

            progress.MapDelete("{id}", async (string id, WorkbenchProgressAgent agent, CancellationToken cancellationToken) =>
            {
                return await agent.Delete(id, cancellationToken) ? Results.NoContent() : Results.NotFound();
            });
        }
    }

    private static readonly DateTimeOffset startedAt = DateTimeOffset.UtcNow;

    /// <summary>The page keys of a comma-separated list, or null for every page.</summary>
    private static HashSet<string>? PageKeys(string? pages)
    {
        return string.IsNullOrWhiteSpace(pages) ? null : [.. pages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }

    /// <summary>The name the review shows the agent's changes under.</summary>
    private static string AgentName(string? agent) => string.IsNullOrWhiteSpace(agent) ? "Agent" : agent.Trim();

    /// <summary>Captures every entry at once, and stops the others as soon as one fails.</summary>
    private static async Task<TResult[]> CaptureAll<TEntry, TResult>(IEnumerable<TEntry> entries, Func<TEntry, CancellationToken, Task<TResult>> capture, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        return await Task.WhenAll(entries.Select(async entry =>
        {
            try
            {
                return await capture(entry, stop.Token);
            }
            catch
            {
                stop.Cancel();
                throw;
            }
        }));
    }
}
