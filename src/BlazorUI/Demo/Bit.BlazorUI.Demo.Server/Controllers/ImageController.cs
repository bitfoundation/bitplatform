namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public partial class ImageController : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetImage(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);

        return File(System.IO.File.OpenRead("wwwroot/images/icon.png"), "image/png", enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> GetImageError(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);

        throw new Exception("Image error!");
    }
}
