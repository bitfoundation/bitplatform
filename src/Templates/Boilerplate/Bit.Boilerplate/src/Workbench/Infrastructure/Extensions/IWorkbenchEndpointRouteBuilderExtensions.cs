using System.IO.Compression;

namespace Boilerplate.Workbench.Infrastructure.Extensions;

public static class IWorkbenchEndpointRouteBuilderExtensions
{
    private const int MaxScreenshotsPerArchive = 12;

    extension(IEndpointRouteBuilder endpoints)
    {
        public void MapWorkbenchEndpoints()
        {
            endpoints.MapGet("api/screenshots/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                var (png, fileName) = await Capture(screenshotService, page, WorkbenchScenario.Parse(context.Request.QueryString.Value), cancellationToken);

                return Results.File(png, "image/png", fileName);
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);

            endpoints.MapGet("api/screenshots/{pageKey}/zip", async (string pageKey, string? panes, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);
                var scenarios = WorkbenchUrls.DecodePanes(panes);

                if (page is null || scenarios.Length is 0 or > MaxScreenshotsPerArchive)
                    return Results.BadRequest();

                using var archiveStream = new MemoryStream();

                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var index = 0;

                    foreach (var scenario in scenarios)
                    {
                        var (png, fileName) = await Capture(screenshotService, page, scenario, cancellationToken);
                        var entry = archive.CreateEntry($"{++index:00}-{fileName}", CompressionLevel.NoCompression);

                        await using var entryStream = await entry.OpenAsync(cancellationToken);
                        await entryStream.WriteAsync(png, cancellationToken);
                    }
                }

                return Results.File(archiveStream.ToArray(), "application/zip", $"{page.Key}-screenshots.zip");
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);
        }
    }

    private static async Task<(byte[] Png, string FileName)> Capture(WorkbenchScreenshotService screenshotService, WorkbenchPageInfo page, WorkbenchScenario scenario, CancellationToken cancellationToken)
    {
        scenario = scenario with { Frame = false };

        var device = scenario.DevicePreset;
        var (width, height) = device.GetViewport(scenario.Rotated);

        var png = await screenshotService.Capture(WorkbenchUrls.Preview(page.Key, scenario).TrimStart('/'),
                                                  width,
                                                  height,
                                                  device.PixelRatio,
                                                  device.IsHandheld,
                                                  scenario.Theme,
                                                  scenario.Culture,
                                                  cancellationToken);

        var (pixelWidth, pixelHeight) = device.GetPixels(scenario.Rotated);
        var fileName = $"{page.Key}-{device.Key}-{pixelWidth}x{pixelHeight}-{scenario.Culture ?? CultureInfoManager.DefaultCulture.Name}-{scenario.Theme}-{scenario.Data}.png".ToLowerInvariant();

        return (png, fileName);
    }
}
