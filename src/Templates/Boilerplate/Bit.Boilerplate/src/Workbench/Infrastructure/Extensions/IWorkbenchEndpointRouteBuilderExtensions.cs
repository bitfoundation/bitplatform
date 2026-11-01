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
        /// Maps the review document's relay, the MCP server and the HTTP review API. The relay takes only the Workbench's own
        /// connections; the other two answer only local requests (see Program.Middlewares).
        /// </summary>
        private void MapReviewEndpoints()
        {
            // The circuits' copies of the review document sync with the server's copy here, proven by the relay's secret header.
            endpoints.MapBitBcrdtWebSocket(WorkbenchReviewRelay.Route,
                                           (context, documentId) => ValueTask.FromResult(WorkbenchReviewRelay.IsTrusted(context, documentId)),
                                           new() { DocumentHost = endpoints.ServiceProvider.GetRequiredService<BcrdtDocumentHost>() });

            endpoints.MapMcp("mcp");

            var review = endpoints.MapGroup("api/review");

            review.MapGet("", async (bool? includeDone, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                return Results.Json(await agent.DescribeReview(includeDone is true, cancellationToken));
            });

            // A long poll: answers once a reviewer adds, answers or reopens a comment, or with nothing within a minute.
            review.MapGet("wait", async (long? cursor, string? agent, WorkbenchReviewAgent reviewAgent, CancellationToken cancellationToken) =>
            {
                var (next, threads) = await reviewAgent.Wait(AgentName(agent), cursor ?? 0, WorkbenchReviewAgent.MaxWait, cancellationToken);

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
        }
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
