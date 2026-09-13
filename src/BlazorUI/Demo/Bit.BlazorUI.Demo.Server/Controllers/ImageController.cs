namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public partial class ImageController : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetImage(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);

        // The point of this endpoint is to be slow, which a cached copy is not: the loading demos would
        // otherwise show their spinner exactly once, and a reload would hit the browser cache instead of
        // the delay.
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";

        return File("images/icon.png", "image/png", enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> GetImageError(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);

        throw new Exception("Image error!");
    }
}
