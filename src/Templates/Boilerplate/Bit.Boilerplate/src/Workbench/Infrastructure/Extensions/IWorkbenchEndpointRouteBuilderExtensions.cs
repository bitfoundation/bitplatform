using System.IO.Compression;
using Microsoft.AspNetCore.WebUtilities;

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

                var captures = await Task.WhenAll(entries.Select(entry => Capture(screenshotService, entry.Page!, entry.Query, cancellationToken)));

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

                var (png, fileName) = await Capture(screenshotService, page, context.Request.QueryString.Value, cancellationToken);

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
        }
    }

    private static async Task<(byte[] Png, string FileName)> Capture(WorkbenchScreenshotService screenshotService, WorkbenchPageInfo page, string? query, CancellationToken cancellationToken)
    {
        var scenario = WorkbenchScenario.Parse(query);
        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);
        var snapshotId = QueryHelpers.ParseQuery(query).TryGetValue(WorkbenchUrls.SnapshotQueryKey, out var id) ? id.ToString() : null;

        var png = await screenshotService.Capture(WorkbenchUrls.Preview(page.Key, scenario with { Frame = scenario.Backdrop is not null && scenario.Frame }, snapshotId: snapshotId).TrimStart('/'),
                                                  width,
                                                  height,
                                                  device.PixelRatio,
                                                  device.IsHandheld,
                                                  scenario.Theme,
                                                  scenario.Culture,
                                                  cancellationToken);

        var (pixelWidth, pixelHeight) = device.GetPixels(scenario.Rotated);
        var look = scenario.Backdrop is null ? scenario.Data.ToString() : "framed";
        var fileName = $"{page.Key}-{device.Key}-{pixelWidth}x{pixelHeight}-{scenario.Culture ?? CultureInfoManager.DefaultCulture.Name}-{scenario.Theme}-{look}.png".ToLowerInvariant();

        return (png, fileName);
    }
}
