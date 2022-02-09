using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TodoTemplate.Api.Contracts;

namespace TodoTemplate.Api.Controllers
{
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
        [HttpPost("[action]/{userName}"), AllowAnonymous]
        public async Task<ActionResult> UploadProfilePhoto(string userName, IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null) throw new BadHttpRequestException("The request couldn't be processed");

            await using var fileStream = file.OpenReadStream();
            await using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (Directory.Exists(_appSettings.UserProfilePhotoPath) is false)
                Directory.CreateDirectory(_appSettings.UserProfilePhotoPath);

            var oldPath = Path.Combine(Directory.GetFiles(_appSettings.UserProfilePhotoPath)
                .FirstOrDefault(s => s.Contains(userName)) ?? string.Empty);

            if (!string.IsNullOrEmpty(oldPath)) System.IO.File.Delete(oldPath);

            var path = Path.Combine($"{_appSettings.UserProfilePhotoPath}\\{userName}.{file.FileName.Split(".").LastOrDefault()}");

            await using var targetStream = System.IO.File.Create(path);
            await memoryStream.CopyToAsync(targetStream, cancellationToken);

            return Ok();
        }

        [HttpDelete("[action]/{userName}"), AllowAnonymous]
        public ActionResult RemoveProfilePhoto(string userName, string fileName)
        {
            var path = Path.Combine(_appSettings.UserProfilePhotoPath, $"{userName}.{fileName.Split(".").LastOrDefault()}");
            if (!System.IO.File.Exists(path)) return NotFound();

            System.IO.File.Delete(path);
            return Ok();
        }

        [HttpGet("[action]/{userName}"), AllowAnonymous]
        public ActionResult GetProfilePhoto(string userName)
        {
            if (!Directory.Exists(_appSettings.UserProfilePhotoPath)) return Ok();

            var filePath = Directory.GetFiles(_appSettings.UserProfilePhotoPath)
                .FirstOrDefault(s => s.Contains(userName));

            if (filePath is null) return Ok();

            var file = System.IO.File.ReadAllBytes(filePath);

            return new FileContentResult(file, $"application/{filePath.Split(".").LastOrDefault()}");
        }
    }
}
