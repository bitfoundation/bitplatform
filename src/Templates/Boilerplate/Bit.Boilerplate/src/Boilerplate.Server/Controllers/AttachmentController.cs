using Boilerplate.Server.Models.Identity;
using ImageMagick;
using Microsoft.AspNetCore.StaticFiles;
using SystemFile = System.IO.File;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;

    [AutoInject] private IContentTypeProvider contentTypeProvider = default!;

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task UploadProfileImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException();

        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());

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

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
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
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var userProfileImageDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.UserProfileImagesDir);

        var filePath = Path.Combine(userProfileImageDirPath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserImageCouldNotBeFound)]);

        user.ProfileImageName = null;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        SystemFile.Delete(filePath);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileImage()
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var userProfileImageDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppSettings.UserProfileImagesDir);

        var filePath = Path.Combine(userProfileImageDirPath, user.ProfileImageName);

        if (SystemFile.Exists(filePath) is false)
            return new EmptyResult();

        if (contentTypeProvider.TryGetContentType(filePath, out var contentType) is false)
        {
            throw new InvalidOperationException();
        }

        return PhysicalFile(Path.Combine(webHostEnvironment.ContentRootPath, filePath),
            contentType, enableRangeProcessing: true);
    }
}
