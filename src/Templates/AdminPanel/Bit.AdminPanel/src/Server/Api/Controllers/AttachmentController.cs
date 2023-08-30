using Microsoft.Extensions.Primitives;
using MimeTypes;
using AdminPanel.Server.Api.Models.Identity;
using SystemFile = System.IO.File;
using ImageMagick;

namespace AdminPanel.Server.Api.Controllers;

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
        if (file is null)
            throw new BadRequestException();

        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException();

        var destFileName = $"{userId}_{file.FileName}";

        var userProfileImagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.UserProfileImagesDir);

        var destFilePath = Path.Combine(userProfileImagesDir, destFileName);

        await using (var requestStream = file.OpenReadStream())
        {
            Directory.CreateDirectory(userProfileImagesDir);

            await using var fileStream = SystemFile.Create(destFilePath);

            await requestStream.CopyToAsync(fileStream, cancellationToken);
        }

        if (user.ProfileImageName is not null)
        {
            try
            {
                var oldFilePath = Path.Combine(userProfileImagesDir, user.ProfileImageName);

                if (SystemFile.Exists(oldFilePath))
                {
                    SystemFile.Delete(oldFilePath);
                }
            }
            catch
            {
                // not important
            }
        }

        destFileName = destFileName.Replace(Path.GetExtension(destFileName), "_256.webp");
        var resizedFilePath = Path.Combine(userProfileImagesDir, destFileName);

        try
        {
            using MagickImage sourceImage = new(destFilePath);

            MagickGeometry resizedImageSize = new(256, 256);

            sourceImage.Resize(resizedImageSize);

            sourceImage.Write(resizedFilePath, MagickFormat.WebP);

            user.ProfileImageName = destFileName;

            await _userManager.UpdateAsync(user);
        }
        catch
        {
            if (SystemFile.Exists(resizedFilePath))
                SystemFile.Delete(resizedFilePath);

            throw;
        }
        finally
        {
            SystemFile.Delete(destFilePath);
        }
    }

    [HttpDelete]
    public async Task RemoveProfileImage()
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var userProfileImageDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.UserProfileImagesDir);

        var filePath = Path.Combine(userProfileImageDirPath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserImageCouldNotBeFound)]);

        user.ProfileImageName = null;

        await _userManager.UpdateAsync(user);

        SystemFile.Delete(filePath);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileImage()
    {
        var userId = UserInformationProvider.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var userProfileImageDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.UserProfileImagesDir);

        var filePath = Path.Combine(userProfileImageDirPath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            return new EmptyResult();

        return PhysicalFile(Path.Combine(_webHostEnvironment.ContentRootPath, filePath),
            MimeTypeMap.GetMimeType(Path.GetExtension(filePath)), enableRangeProcessing: true);
    }
}
