using System.Text;
using System.IO.Compression;
using Bit.Bcrdt.Server;
using Microsoft.Extensions.Localization;

namespace Boilerplate.Workbench.Infrastructure.Extensions;

public static class IWorkbenchEndpointRouteBuilderExtensions
{
    private const int MaxScreenshotsPerArchive = 24;

    extension(IEndpointRouteBuilder endpoints)
    {
        public void MapWorkbenchEndpoints()
        {
            endpoints.MapGet("api/screenshots/zip", async (string? shots, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var entries = WorkbenchUrls.DecodeShots(shots)
                    .Select(shot => (Page: WorkbenchPageCatalog.Find(shot.PageKey), shot.Query))
                    .ToArray();

                if (entries.Length is 0 or > MaxScreenshotsPerArchive || entries.Any(entry => entry.Page is null))
                    return Results.BadRequest();

                var captures = await Task.WhenAll(entries.Select(entry => screenshotService.CapturePage(entry.Page!, entry.Query, cancellationToken)));

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
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);

            endpoints.MapGet("api/screenshots/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                var (png, fileName) = await screenshotService.CapturePage(page, context.Request.QueryString.Value, cancellationToken);

                return Results.File(png, "image/png", fileName);
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);

            endpoints.MapGet("api/thumbnails/{pageKey}", async (string pageKey, bool? phone, WorkbenchThemeMode? theme, HttpContext context, WorkbenchScreenshotService screenshotService) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                var png = await screenshotService.GetThumbnail(page.Key, phone is true, theme is WorkbenchThemeMode.Dark ? WorkbenchThemeMode.Dark : WorkbenchThemeMode.Light);

                context.Response.Headers.CacheControl = "public, max-age=3600";

                return Results.File(png, "image/png");
            });

            endpoints.MapGet("api/review-sheet/{pageKey}", async (string pageKey,
                                                                 string? shots,
                                                                 string? format,
                                                                 WorkbenchReviewAgent agent,
                                                                 WorkbenchScreenshotService screenshotService,
                                                                 IStringLocalizer<WorkbenchStrings> localizer,
                                                                 CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);
                var entries = WorkbenchUrls.DecodeShots(shots).Where(entry => entry.PageKey == page?.Key).ToArray();

                if (page is null || entries.Length is 0 or > MaxScreenshotsPerArchive)
                    return Results.BadRequest();

                var views = await Task.WhenAll(entries.Select(async entry => (WorkbenchScenario.Parse(entry.Query), (await screenshotService.CapturePage(page, entry.Query, cancellationToken)).Png)));

                var threads = (await agent.GetThreads(cancellationToken)).Where(t => t.PageKey == page.Key).ToList();
                var review = (await agent.GetPages(cancellationToken)).GetValueOrDefault(page.Key);

                var html = WorkbenchReviewSheet.Build(page, views, threads, review, localizer);

                return format is "pdf"
                    ? Results.File(await screenshotService.RenderPdf(html, cancellationToken), "application/pdf", $"{page.Key}-review.pdf")
                    : Results.File(Encoding.UTF8.GetBytes(html), "text/html", $"{page.Key}-review.html");
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);

            endpoints.MapGet("api/checks/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                return Results.Text(await screenshotService.RunChecks(page, context.Request.QueryString.Value, cancellationToken), "application/json");
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);

            endpoints.MapReviewEndpoints();
        }

        private void MapReviewEndpoints()
        {
            endpoints.MapBitBcrdtWebSocket(WorkbenchReviewRelay.Route,
                                           (context, documentId) => ValueTask.FromResult(WorkbenchReviewRelay.IsTrusted(context, documentId)),
                                           new() { DocumentHost = endpoints.ServiceProvider.GetRequiredService<BcrdtDocumentHost>() });

            endpoints.MapMcp("mcp");

            var review = endpoints.MapGroup("api/review");

            review.MapGet("", async (bool? includeDone, WorkbenchReviewAgent agent, CancellationToken cancellationToken) =>
            {
                var threads = await agent.GetThreads(cancellationToken);
                var pages = await agent.GetPages(cancellationToken);
                var flows = await agent.GetFlows(cancellationToken);

                return Results.Json(WorkbenchReviewJson.Describe(threads.Where(t => includeDone is true || t.Status is not WorkbenchReviewStatus.Done), pages, threads.Select(t => t.Sequence).DefaultIfEmpty(0).Max(), flows));
            });

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
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);
        }
    }

    private static string AgentName(string? agent) => string.IsNullOrWhiteSpace(agent) ? "Agent" : agent.Trim();
}
