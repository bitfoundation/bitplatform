using MimeTypes;
using AdminPanel.Api.Models.Account;
using SystemFile = System.IO.File;
using Microsoft.Extensions.Primitives;

namespace AdminPanel.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase
{
    [AutoInject] private UserManager<User> _userManager = default!;

    [AutoInject] private IWebHostEnvironment _webHostEnvironment = default!;

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task UploadProfileImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || Request.Headers.TryGetValue("bit_file_id", out StringValues bitFileId) is false)
            throw new BadRequestException();

        var userId = User.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException();

        var fileName = $"{userId}_{bitFileId}_{Path.GetExtension(file.FileName)}";

        await using var requestStream = file.OpenReadStream();

        Directory.CreateDirectory(AppSettings.Value.UserProfileImagePath);

        var path = Path.Combine(AppSettings.Value.UserProfileImagePath, fileName);

        await using var fileStream = SystemFile.Exists(path)
            ? SystemFile.Open(path, FileMode.Append)
            : SystemFile.Create(path);

        await requestStream.CopyToAsync(fileStream, cancellationToken);

        if (user.ProfileImageName is not null)
        {
            try
            {
                var filePath = Path.Combine(AppSettings.Value.UserProfileImagePath, user.ProfileImageName);

                if (SystemFile.Exists(filePath))
                {
                    SystemFile.Delete(filePath);
                }
            }
            catch
            {
                // not important
            }
        }

        try
        {
            user.ProfileImageName = fileName;

            await _userManager.UpdateAsync(user);
        }
        catch
        {
            SystemFile.Delete(path);

            throw;
        }
    }

    [HttpDelete]
    public async Task RemoveProfileImage()
    {
        var userId = User.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var filePath = Path.Combine(AppSettings.Value.UserProfileImagePath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserImageCouldNotBeFound)]);

        user.ProfileImageName = null;

        await _userManager.UpdateAsync(user);

        SystemFile.Delete(filePath);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileImage()
    {
        var userId = User.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var filePath = Path.Combine(AppSettings.Value.UserProfileImagePath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            return new EmptyResult();

        return PhysicalFile(Path.Combine(_webHostEnvironment.ContentRootPath, filePath),
            MimeTypeMap.GetMimeType(Path.GetExtension(filePath)), enableRangeProcessing: true);
    }
}
