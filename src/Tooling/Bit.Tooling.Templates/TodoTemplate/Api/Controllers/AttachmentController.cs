using Microsoft.AspNetCore.Authorization;
using MimeTypes;
using SystemFile = System.IO.File;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AttachmentController : ControllerBase
{
    private readonly AppSettings _appSettings;

    public AttachmentController(IOptionsSnapshot<AppSettings> setting)
    {
        _appSettings = setting.Value;
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

        var path = Path.Combine($"{_appSettings.UserProfilePhotoPath}\\{userId}.{file.FileName.Split(".").LastOrDefault()}");

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

        var fileName = Directory.GetFiles(_appSettings.UserProfilePhotoPath, $"{userId}.*")
            .Single();

        var path = Path.Combine(_appSettings.UserProfilePhotoPath, $"{userId}.{fileName.Split(".").LastOrDefault()}");

        if (!SystemFile.Exists(path))
            return NotFound();

        SystemFile.Delete(path);

        return Ok();
    }

    [HttpGet("[action]")]
    public ActionResult GetProfilePhoto()
    {
        var userId = User.GetUserId();

        var fileName = Directory.GetFiles(_appSettings.UserProfilePhotoPath, $"{userId}.*")
            .SingleOrDefault();

        if (fileName is null)
            return NotFound();

        var path = Path.Combine(_appSettings.UserProfilePhotoPath, $"{userId}.{fileName.Split(".").LastOrDefault()}");

        if (!SystemFile.Exists(path))
            return NotFound();

        return File(path, MimeTypeMap.GetMimeType(Path.GetExtension(path)));
    }
}
