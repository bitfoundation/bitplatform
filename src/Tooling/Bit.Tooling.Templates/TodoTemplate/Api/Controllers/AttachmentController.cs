using Bit.Tooling.SourceGenerators;
using MimeTypes;
using TodoTemplate.Api.Models.Account;
using SystemFile = System.IO.File;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public partial class AttachmentController : ControllerBase
{
    [AutoInject] public IOptionsSnapshot<AppSettings> AppSettings;

    [AutoInject] public UserManager<User> UserManager;
    
    [AutoInject] public IWebHostEnvironment WebHostEnvironment;
    
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    [HttpPost("[action]")]
    public async Task UploadProfileImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException();

        var userId = User.GetUserId();

        var user = await UserManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException();

        var profileImageName = Guid.NewGuid().ToString();

        await using var fileStream = file.OpenReadStream();

        Directory.CreateDirectory(AppSettings.Value.UserProfileImagePath);

        var path = Path.Combine($"{AppSettings.Value.UserProfileImagePath}\\{profileImageName}{Path.GetExtension(file.FileName)}");

        await using var targetStream = SystemFile.Create(path);

        await fileStream.CopyToAsync(targetStream, cancellationToken);

        if (user.ProfileImageName is not null)
        {
            try
            {
                var filePath = Directory.GetFiles(AppSettings.Value.UserProfileImagePath,
                    $"{user.ProfileImageName}.*").FirstOrDefault();

                if (filePath != null)
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
            user.ProfileImageName = profileImageName;

            await UserManager.UpdateAsync(user);
        }
        catch
        {
            SystemFile.Delete(path);

            throw;
        }
    }

    [HttpDelete("[action]")]
    public async Task RemoveProfileImage()
    {
        var userId = User.GetUserId();

        var user = await UserManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException();

        var filePath = Directory.GetFiles(AppSettings.Value.UserProfileImagePath, $"{user.ProfileImageName}.*")
            .SingleOrDefault();

        if (filePath is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserImageCouldNotBeFound));

        user.ProfileImageName = null;

        await UserManager.UpdateAsync(user);

        SystemFile.Delete(filePath);
    }

    [HttpGet("[action]")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await UserManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException();

        var filePath = Directory.GetFiles(AppSettings.Value.UserProfileImagePath, $"{user.ProfileImageName}.*")
            .SingleOrDefault();

        if (filePath is null)
            return new EmptyResult();

        return PhysicalFile(Path.Combine(WebHostEnvironment.ContentRootPath, filePath),
            MimeTypeMap.GetMimeType(Path.GetExtension(filePath)));
    }
}
