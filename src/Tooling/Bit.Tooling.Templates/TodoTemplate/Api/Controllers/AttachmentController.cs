using MimeTypes;
using TodoTemplate.Api.Models.Account;
using SystemFile = System.IO.File;

namespace TodoTemplate.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AttachmentController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public AttachmentController(IOptionsSnapshot<AppSettings> setting,
        UserManager<User> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _appSettings = setting.Value;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [DisableRequestSizeLimit]
    [HttpPost("[action]")]
    public async Task UploadProfileImage(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException();

        var userId = User.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        var profileImageName = Guid.NewGuid();

        await using var fileStream = file.OpenReadStream();

        Directory.CreateDirectory(_appSettings.UserProfileImagePath);

        var path = Path.Combine($"{_appSettings.UserProfileImagePath}\\{profileImageName}{Path.GetExtension(file.FileName)}");

        await using var targetStream = SystemFile.Create(path);

        await fileStream.CopyToAsync(targetStream, cancellationToken);

        if (user.ProfileImageName is not null)
        {
            try
            {
                var filePath = Directory.GetFiles(_appSettings.UserProfileImagePath,
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

            await _userManager.UpdateAsync(user);
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

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        var filePath = Directory.GetFiles(_appSettings.UserProfileImagePath, $"{user.ProfileImageName}.*")
            .SingleOrDefault();

        if (filePath is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserImageCouldNotBeFound));

        user.ProfileImageName = null;

        await _userManager.UpdateAsync(user);

        SystemFile.Delete(filePath);
    }

    [HttpGet("[action]")]
    [ResponseCache(NoStore = true)]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new ResourceNotFoundException(nameof(ErrorStrings.UserCouldNotBeFound));

        var filePath = Directory.GetFiles(_appSettings.UserProfileImagePath, $"{user.ProfileImageName}.*")
            .SingleOrDefault();

        if (filePath is null)
            return new EmptyResult();

        return PhysicalFile(Path.Combine(_webHostEnvironment.ContentRootPath, filePath),
            MimeTypeMap.GetMimeType(Path.GetExtension(filePath)));
    }
}
