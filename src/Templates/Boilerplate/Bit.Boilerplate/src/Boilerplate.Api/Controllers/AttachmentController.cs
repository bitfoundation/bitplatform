using Microsoft.AspNetCore.StaticFiles;
using Boilerplate.Server.Models.Identity;
using FluentStorage.Blobs;
using ImageMagick;

namespace Boilerplate.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase
{
    [AutoInject] private UserManager<User> userManager = default!;

    [AutoInject] private IWebHostEnvironment webHostEnvironment = default!;

    [AutoInject] private IContentTypeProvider contentTypeProvider = default!;

    [AutoInject] private IBlobStorage blobStorage = default!;

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
        var destFilePath = $"{AppSettings.UserProfileImagesDir}{destFileName}";

        await using (var requestStream = file.OpenReadStream())
        {
            await blobStorage.WriteAsync(destFilePath, requestStream, cancellationToken: cancellationToken);
        }

        if (user.ProfileImageName is not null)
        {
            try
            {
                var oldFilePath = $"{AppSettings.UserProfileImagesDir}{user.ProfileImageName}";

                if (await blobStorage.ExistsAsync(oldFilePath, cancellationToken))
                {
                    await blobStorage.DeleteAsync(oldFilePath, cancellationToken);
                }
            }
            catch
            {
                // not important
            }
        }

        destFileName = destFileName.Replace(Path.GetExtension(destFileName), "_256.webp");
        var resizedFilePath = $"{AppSettings.UserProfileImagesDir}{destFileName}";

        try
        {
            await using var destFileStream = await blobStorage.OpenReadAsync(destFilePath, cancellationToken);
            using MagickImage sourceImage = new(destFileStream);

            MagickGeometry resizedImageSize = new(256, 256);

            sourceImage.Resize(resizedImageSize);

            await blobStorage.WriteAsync(resizedFilePath, sourceImage.ToByteArray(MagickFormat.WebP), cancellationToken: cancellationToken);

            user.ProfileImageName = destFileName;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
        }
        catch
        {
            await blobStorage.DeleteAsync(resizedFilePath, cancellationToken);

            throw;
        }
        finally
        {
            await blobStorage.DeleteAsync(destFilePath, cancellationToken);
        }
    }

    [HttpDelete]
    public async Task RemoveProfileImage(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var filePath = $"{AppSettings.UserProfileImagesDir}{user.ProfileImageName}";

        if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
            throw new ResourceNotFoundException(Localizer[nameof(AppStrings.UserImageCouldNotBeFound)]);

        user.ProfileImageName = null;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        await blobStorage.DeleteAsync(filePath, cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.ProfileImageName is null)
            throw new ResourceNotFoundException();

        var filePath = $"{AppSettings.UserProfileImagesDir}{user.ProfileImageName}";

        if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
            return new EmptyResult();

        if (contentTypeProvider.TryGetContentType(filePath, out var contentType) is false)
        {
            throw new InvalidOperationException();
        }

        return File(await blobStorage.OpenReadAsync(filePath, cancellationToken), contentType, enableRangeProcessing: true);
    }
}
