using MimeTypes;
using SystemFile = System.IO.File;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AttachmentController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AttachmentController(IOptionsSnapshot<AppSettings> setting, IWebHostEnvironment webHostEnvironment)
    {
        _appSettings = setting.Value;
        _webHostEnvironment = webHostEnvironment;
    }

    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    [HttpPost("[action]")]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            return BadRequest();

        await using var fileStream = file.OpenReadStream();

        Directory.CreateDirectory(_appSettings.UserProfilePhotoPath);

        var userId = User.GetUserId();

        var path = Path.Combine($"{_appSettings.UserProfilePhotoPath}\\{userId}{Path.GetExtension(file.FileName)}");

        if (SystemFile.Exists(path))
            SystemFile.Delete(path);

        await using var targetStream = SystemFile.Create(path);
        await fileStream.CopyToAsync(targetStream, cancellationToken);

        return Ok();
    }

    [HttpDelete("[action]")]
    public ActionResult RemoveProfilePhoto()
    {
        var userId = User.GetUserId();

        var filePath = Directory.GetFiles(_appSettings.UserProfilePhotoPath, $"{userId}.*")
            .Single();

        if (!SystemFile.Exists(filePath))
            return NotFound();

        SystemFile.Delete(filePath);

        return Ok();
    }

    [HttpGet("[action]")]
    [ResponseCache(NoStore = true)]
    public ActionResult GetProfilePhoto()
    {
        var userId = User.GetUserId();

        var filePath = Directory.GetFiles(_appSettings.UserProfilePhotoPath, $"{userId}.*")
            .SingleOrDefault();

        if (filePath is null)
            return NotFound();

        if (!SystemFile.Exists(filePath))
            return NotFound();

        return PhysicalFile(Path.Combine(_webHostEnvironment.ContentRootPath, filePath), MimeTypeMap.GetMimeType(Path.GetExtension(filePath)));
    }
}
