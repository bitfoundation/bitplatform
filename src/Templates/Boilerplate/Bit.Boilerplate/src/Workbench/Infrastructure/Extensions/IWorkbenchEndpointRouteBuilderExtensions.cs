namespace Boilerplate.Workbench.Infrastructure.Extensions;

public static class IWorkbenchEndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public void MapWorkbenchEndpoints()
        {
            endpoints.MapGet("api/screenshots/{pageKey}", async (string pageKey, HttpContext context, WorkbenchScreenshotService screenshotService, CancellationToken cancellationToken) =>
            {
                var page = WorkbenchPageCatalog.Find(pageKey);

                if (page is null)
                    return Results.NotFound();

                var scenario = WorkbenchScenario.Parse(context.Request.QueryString.Value) with { Frame = false };
                var device = scenario.DevicePreset;
                var (width, height) = device.GetViewport(scenario.Landscape);

                var png = await screenshotService.Capture($"preview/{page.Key}{scenario.ToQueryString()}",
                                                          width,
                                                          height,
                                                          device.PixelRatio,
                                                          device.IsHandheld,
                                                          scenario.Theme,
                                                          scenario.Culture,
                                                          cancellationToken);

                var (pixelWidth, pixelHeight) = device.GetPixels(scenario.Landscape);
                var fileName = $"{page.Key}-{device.Key}-{pixelWidth}x{pixelHeight}-{scenario.Culture ?? CultureInfoManager.DefaultCulture.Name}-{scenario.Theme}.png".ToLowerInvariant();

                return Results.File(png, "image/png", fileName);
            }).RequireRateLimiting(IWorkbenchServiceCollectionExtensions.ScreenshotsRateLimitPolicy);
        }
    }
}
